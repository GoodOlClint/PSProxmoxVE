#!/usr/bin/env bash
# =============================================================================
# entrypoint.sh
# Docker entrypoint for the self-hosted GitHub Actions runner.
#
# Required environment variables:
#   REPO_URL      - Full GitHub repository URL (e.g. https://github.com/GoodOlClint/PSProxmoxVE)
#   RUNNER_TOKEN  - Registration token from GitHub Settings > Actions > Runners
#
# Optional environment variables:
#   RUNNER_LABELS - Comma-separated labels (default: self-hosted,proxmox,integration)
#   RUNNER_NAME   - Runner name (default: hostname)
#   RUNNER_GROUP  - Runner group (default: Default)
#   EPHEMERAL     - Set to "true" for single-job ephemeral mode (default: false)
# =============================================================================
set -euo pipefail

RUNNER_LABELS="${RUNNER_LABELS:-self-hosted,proxmox,integration}"
RUNNER_NAME="${RUNNER_NAME:-$(hostname)}"
RUNNER_GROUP="${RUNNER_GROUP:-Default}"
EPHEMERAL="${EPHEMERAL:-false}"

# ---------------------------------------------------------------------------
# Validate required environment variables
# ---------------------------------------------------------------------------
if [[ -z "${REPO_URL:-}" ]]; then
    echo "ERROR: REPO_URL environment variable is required."
    echo "  Example: -e REPO_URL=https://github.com/GoodOlClint/PSProxmoxVE"
    exit 1
fi

if [[ -z "${RUNNER_TOKEN:-}" ]]; then
    echo "ERROR: RUNNER_TOKEN environment variable is required."
    echo "  Get one from: ${REPO_URL}/settings/actions/runners/new"
    exit 1
fi

# ---------------------------------------------------------------------------
# Configure the runner
# ---------------------------------------------------------------------------
CONFIG_ARGS=(
    --url "$REPO_URL"
    --token "$RUNNER_TOKEN"
    --labels "$RUNNER_LABELS"
    --name "$RUNNER_NAME"
    --runnergroup "$RUNNER_GROUP"
    --work _work
    --unattended
    --replace
)

if [[ "$EPHEMERAL" == "true" ]]; then
    CONFIG_ARGS+=(--ephemeral)
    echo "Ephemeral mode enabled -- runner will exit after one job."
fi

echo "Configuring runner..."
echo "  Repository: $REPO_URL"
echo "  Name:       $RUNNER_NAME"
echo "  Labels:     $RUNNER_LABELS"

/opt/github-runner/config.sh "${CONFIG_ARGS[@]}"

# ---------------------------------------------------------------------------
# Deregister on shutdown (best-effort)
# ---------------------------------------------------------------------------
cleanup() {
    echo ""
    echo "Caught signal -- removing runner registration..."
    /opt/github-runner/config.sh remove --token "$RUNNER_TOKEN" || true
}
trap cleanup SIGTERM SIGINT

# ---------------------------------------------------------------------------
# Start the runner
# ---------------------------------------------------------------------------
echo "Starting runner..."
/opt/github-runner/run.sh &
wait $!
