# =============================================================================
# Dockerfile
# Self-hosted GitHub Actions runner for PSProxmoxVE integration tests.
#
# Build:
#   docker build -t psproxmoxve-runner .
#
# Run:
#   docker run -d \
#       -e REPO_URL=https://github.com/GoodOlClint/PSProxmoxVE \
#       -e RUNNER_TOKEN=AXXXXXXXXXXXXXXXXXXXXXXXXXXXX \
#       -e RUNNER_LABELS=self-hosted,proxmox,integration \
#       -e RUNNER_NAME=docker-proxmox-runner \
#       --name psproxmoxve-runner \
#       psproxmoxve-runner
#
# The container must have network access to the Proxmox VE API under test.
# =============================================================================
FROM ubuntu:24.04

LABEL maintainer="GoodOlClint"
LABEL description="Self-hosted GitHub Actions runner for PSProxmoxVE integration tests"

# Avoid interactive prompts during package installation.
ENV DEBIAN_FRONTEND=noninteractive

# ---------------------------------------------------------------------------
# 1. System packages
# ---------------------------------------------------------------------------
RUN apt-get update -qq && \
    apt-get install -y -qq --no-install-recommends \
        curl \
        jq \
        git \
        wget \
        apt-transport-https \
        software-properties-common \
        lsb-release \
        ca-certificates \
        gnupg \
        unzip \
        sudo \
        iputils-ping \
    && rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# 2. .NET SDK 10.0
# ---------------------------------------------------------------------------
RUN wget -q "https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb" \
        -O /tmp/packages-microsoft-prod.deb && \
    dpkg -i /tmp/packages-microsoft-prod.deb && \
    rm -f /tmp/packages-microsoft-prod.deb && \
    apt-get update -qq && \
    apt-get install -y -qq --no-install-recommends dotnet-sdk-10.0 && \
    rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# 3. PowerShell 7.x
# ---------------------------------------------------------------------------
RUN apt-get update -qq && \
    apt-get install -y -qq --no-install-recommends powershell && \
    rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# 4. Terraform
# ---------------------------------------------------------------------------
RUN wget -qO- https://apt.releases.hashicorp.com/gpg | \
        gpg --dearmor -o /usr/share/keyrings/hashicorp-archive-keyring.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com noble main" \
        > /etc/apt/sources.list.d/hashicorp.list && \
    apt-get update -qq && \
    apt-get install -y -qq --no-install-recommends terraform && \
    rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# 5. Create runner user
# ---------------------------------------------------------------------------
RUN useradd -m -s /bin/bash github-runner && \
    echo "github-runner ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers.d/github-runner

# ---------------------------------------------------------------------------
# 6. Download GitHub Actions runner
# ---------------------------------------------------------------------------
ENV RUNNER_DIR=/opt/github-runner

RUN mkdir -p "$RUNNER_DIR" && \
    RUNNER_VERSION=$(curl -fsSL https://api.github.com/repos/actions/runner/releases/latest | jq -r '.tag_name' | sed 's/^v//') && \
    curl -fsSL "https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz" \
        -o /tmp/runner.tar.gz && \
    tar xzf /tmp/runner.tar.gz -C "$RUNNER_DIR" && \
    rm -f /tmp/runner.tar.gz && \
    chown -R github-runner:github-runner "$RUNNER_DIR"

# Install runner dependencies (the runner ships a script for this).
RUN "$RUNNER_DIR/bin/installdependencies.sh"

# ---------------------------------------------------------------------------
# 7. Entrypoint
# ---------------------------------------------------------------------------
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

USER github-runner
WORKDIR /opt/github-runner

ENTRYPOINT ["/entrypoint.sh"]
