#!/usr/bin/env bash
# =============================================================================
# setup-runner.sh
# Sets up a self-hosted GitHub Actions runner for PSProxmoxVE integration tests.
#
# Intended to run inside a Debian 12 or Ubuntu 24.04 environment (LXC, VM, or
# bare metal) that has network access to the Proxmox VE API under test.
#
# Prerequisites: root or sudo access, internet connectivity.
#
# Usage:
#   sudo ./setup-runner.sh \
#       --repo GoodOlClint/PSProxmoxVE \
#       --token AXXXXXXXXXXXXXXXXXXXXXXXXXXXX \
#       --labels self-hosted,proxmox,integration
#
# Read through the script before running it -- no surprises.
# =============================================================================
set -euo pipefail

# ---------------------------------------------------------------------------
# Parse arguments
# ---------------------------------------------------------------------------
REPO=""
TOKEN=""
LABELS="self-hosted,proxmox,integration"
RUNNER_DIR="/opt/github-runner"
RUNNER_USER="github-runner"

while [[ $# -gt 0 ]]; do
    case $1 in
        --repo)   REPO="$2";   shift 2 ;;
        --token)  TOKEN="$2";  shift 2 ;;
        --labels) LABELS="$2"; shift 2 ;;
        --help|-h)
            echo "Usage: $0 --repo OWNER/REPO --token TOKEN [--labels LABELS]"
            echo ""
            echo "  --repo     GitHub repository (e.g. GoodOlClint/PSProxmoxVE)"
            echo "  --token    Runner registration token from GitHub Settings > Actions > Runners"
            echo "  --labels   Comma-separated labels (default: self-hosted,proxmox,integration)"
            exit 0
            ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

# ---------------------------------------------------------------------------
# Validate required arguments
# ---------------------------------------------------------------------------
[[ -z "$REPO" ]]  && { echo "ERROR: --repo required (e.g. GoodOlClint/PSProxmoxVE)"; exit 1; }
[[ -z "$TOKEN" ]] && { echo "ERROR: --token required (get from GitHub Settings > Actions > Runners)"; exit 1; }

# ---------------------------------------------------------------------------
# Detect OS
# ---------------------------------------------------------------------------
if [[ -f /etc/os-release ]]; then
    # shellcheck disable=SC1091
    source /etc/os-release
    echo "Detected OS: $PRETTY_NAME"
else
    echo "WARNING: Cannot detect OS. Proceeding assuming Debian/Ubuntu."
fi

# ---------------------------------------------------------------------------
# Helper: retry a command up to N times
# ---------------------------------------------------------------------------
retry() {
    local retries=$1; shift
    local count=0
    until "$@"; do
        count=$((count + 1))
        if [[ $count -ge $retries ]]; then
            echo "ERROR: Command failed after $retries attempts: $*"
            return 1
        fi
        echo "Retry $count/$retries..."
        sleep 3
    done
}

# ---------------------------------------------------------------------------
# 1. System packages
# ---------------------------------------------------------------------------
echo ""
echo "=== Installing system prerequisites ==="
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq curl jq git wget apt-transport-https software-properties-common \
    lsb-release ca-certificates gnupg unzip

# ---------------------------------------------------------------------------
# 2. .NET SDK 10.0
# ---------------------------------------------------------------------------
echo ""
echo "=== Installing .NET SDK 10.0 ==="
# Microsoft package repository
wget -q "https://packages.microsoft.com/config/$(lsb_release -is | tr '[:upper:]' '[:lower:]')/$(lsb_release -rs)/packages-microsoft-prod.deb" \
    -O /tmp/packages-microsoft-prod.deb
dpkg -i /tmp/packages-microsoft-prod.deb
rm -f /tmp/packages-microsoft-prod.deb
apt-get update -qq
apt-get install -y -qq dotnet-sdk-10.0

echo "  .NET version: $(dotnet --version)"

# ---------------------------------------------------------------------------
# 3. PowerShell 7.x
# ---------------------------------------------------------------------------
echo ""
echo "=== Installing PowerShell 7.x ==="
# PowerShell is available from the Microsoft repository added above.
apt-get install -y -qq powershell

echo "  PowerShell version: $(pwsh -NoProfile -Command '$PSVersionTable.PSVersion.ToString()')"

# ---------------------------------------------------------------------------
# 4. Terraform
# ---------------------------------------------------------------------------
echo ""
echo "=== Installing Terraform ==="
wget -qO- https://apt.releases.hashicorp.com/gpg | gpg --dearmor -o /usr/share/keyrings/hashicorp-archive-keyring.gpg
echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com $(lsb_release -cs) main" \
    > /etc/apt/sources.list.d/hashicorp.list
apt-get update -qq
apt-get install -y -qq terraform

echo "  Terraform version: $(terraform version -json | jq -r '.terraform_version')"

# ---------------------------------------------------------------------------
# 5. Create runner service account
# ---------------------------------------------------------------------------
echo ""
echo "=== Creating runner service account ==="
if ! id "$RUNNER_USER" &>/dev/null; then
    useradd -m -s /bin/bash "$RUNNER_USER"
    echo "  Created user: $RUNNER_USER"
else
    echo "  User $RUNNER_USER already exists"
fi

# ---------------------------------------------------------------------------
# 6. Download and configure GitHub Actions runner
# ---------------------------------------------------------------------------
echo ""
echo "=== Downloading GitHub Actions runner ==="
mkdir -p "$RUNNER_DIR"

# Determine the latest runner version from the GitHub API.
RUNNER_VERSION=$(curl -fsSL https://api.github.com/repos/actions/runner/releases/latest | jq -r '.tag_name' | sed 's/^v//')
RUNNER_ARCH="x64"
RUNNER_TAR="actions-runner-linux-${RUNNER_ARCH}-${RUNNER_VERSION}.tar.gz"
RUNNER_URL="https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/${RUNNER_TAR}"

echo "  Runner version: $RUNNER_VERSION"
echo "  Download URL:   $RUNNER_URL"

if [[ ! -f "$RUNNER_DIR/.runner" ]]; then
    curl -fsSL "$RUNNER_URL" -o "/tmp/$RUNNER_TAR"
    tar xzf "/tmp/$RUNNER_TAR" -C "$RUNNER_DIR"
    rm -f "/tmp/$RUNNER_TAR"
else
    echo "  Runner already extracted -- skipping download"
fi

chown -R "$RUNNER_USER":"$RUNNER_USER" "$RUNNER_DIR"

# ---------------------------------------------------------------------------
# 7. Configure the runner
# ---------------------------------------------------------------------------
echo ""
echo "=== Configuring runner ==="
cd "$RUNNER_DIR"

# Run config as the service account.
sudo -u "$RUNNER_USER" ./config.sh \
    --url "https://github.com/$REPO" \
    --token "$TOKEN" \
    --labels "$LABELS" \
    --name "$(hostname)-proxmox-runner" \
    --work _work \
    --unattended \
    --replace

# ---------------------------------------------------------------------------
# 8. Install and start the systemd service
# ---------------------------------------------------------------------------
echo ""
echo "=== Installing as systemd service ==="
./svc.sh install "$RUNNER_USER"
./svc.sh start

echo ""
echo "=== Runner setup complete ==="
echo "  Runner directory: $RUNNER_DIR"
echo "  Service user:     $RUNNER_USER"
echo "  Labels:           $LABELS"
echo "  Repository:       https://github.com/$REPO"
echo ""
echo "Verify the runner appears in your repository under:"
echo "  Settings > Actions > Runners"
echo ""
echo "To remove this runner later:"
echo "  cd $RUNNER_DIR"
echo "  ./svc.sh stop"
echo "  ./svc.sh uninstall"
echo "  ./config.sh remove --token <NEW_TOKEN>"
