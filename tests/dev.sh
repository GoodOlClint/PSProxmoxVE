#!/usr/bin/env bash
# Helper script for the dev/test containers.
#
# Usage:
#   ./tests/dev.sh              # Start dev container and open pwsh shell
#   ./tests/dev.sh build        # Build the module inside the container
#   ./tests/dev.sh test         # Run unit tests
#   ./tests/dev.sh integration  # Run integration tests against existing PVE (needs .env.test)
#   ./tests/dev.sh provision    # Full CI flow: provision → test → cleanup (x86 only)
#   ./tests/dev.sh cleanup      # Destroy provisioned VMs (x86 only)
#   ./tests/dev.sh stop         # Stop all containers
#   ./tests/dev.sh rebuild      # Rebuild container image(s)

set -euo pipefail
cd "$(dirname "$0")/.."

COMPOSE="docker compose -f tests/docker-compose.test.yml"
DEV_CONTAINER="psproxmoxve-dev"
INFRA_CONTAINER="psproxmoxve-dev-infra"
MODULE_PATH="/usr/local/share/powershell/Modules/PSProxmoxVE"
RUN_INTEGRATION="tests/infrastructure/scripts/run-integration.sh"

ensure_dev() {
    if ! docker inspect "$DEV_CONTAINER" --format '{{.State.Running}}' 2>/dev/null | grep -q true; then
        echo "Starting dev container..."
        $COMPOSE up -d dev
    fi
}

ensure_infra() {
    if ! docker inspect "$INFRA_CONTAINER" --format '{{.State.Running}}' 2>/dev/null | grep -q true; then
        echo "Starting infra container (x86 only)..."
        $COMPOSE --profile infra up -d dev-infra
    fi
}

build_module() {
    local container="$1"
    docker exec "$container" bash -c "
        dotnet publish src/PSProxmoxVE/PSProxmoxVE.csproj \
            -c Release -f netstandard2.0 -o /tmp/publish 2>&1 | tail -1 && \
        cp -r /tmp/publish/* $MODULE_PATH/ && \
        echo 'Module installed to $MODULE_PATH'
    "
}

case "${1:-shell}" in
    shell)
        ensure_dev
        docker exec -it "$DEV_CONTAINER" pwsh -NoProfile
        ;;

    build)
        ensure_dev
        build_module "$DEV_CONTAINER"
        ;;

    test)
        ensure_dev
        build_module "$DEV_CONTAINER"
        docker exec "$DEV_CONTAINER" pwsh -NoProfile -Command "
            \$config = New-PesterConfiguration
            \$config.Run.Path = 'tests/PSProxmoxVE.Tests'
            \$config.Run.Exit = \$true
            \$config.Filter.ExcludeTag = @('Integration')
            \$config.Output.Verbosity = 'Detailed'
            Invoke-Pester -Configuration \$config
        "
        ;;

    integration)
        # Run integration tests against a pre-existing PVE (set via .env.test)
        ensure_dev
        build_module "$DEV_CONTAINER"
        docker exec "$DEV_CONTAINER" bash -c "
            SKIP_PROVISION=true bash $RUN_INTEGRATION test ${2:-all}
        "
        ;;

    provision)
        # Full CI lifecycle: provision → test → cleanup (x86 infra container)
        ensure_infra
        build_module "$INFRA_CONTAINER"
        docker exec "$INFRA_CONTAINER" bash "$RUN_INTEGRATION" all "${2:-all}"
        ;;

    cleanup)
        # Destroy provisioned VMs
        ensure_infra
        docker exec "$INFRA_CONTAINER" bash "$RUN_INTEGRATION" cleanup
        ;;

    stop)
        $COMPOSE --profile infra down
        ;;

    rebuild)
        $COMPOSE --profile infra down
        $COMPOSE build --no-cache dev
        $COMPOSE --profile infra build --no-cache dev-infra
        $COMPOSE up -d dev
        ;;

    *)
        echo "Usage: $0 {shell|build|test|integration|provision|cleanup|stop|rebuild}"
        echo ""
        echo "  shell          Open pwsh in the dev container"
        echo "  build          Build the module"
        echo "  test           Run unit tests"
        echo "  integration    Run integration tests against existing PVE (.env.test)"
        echo "  provision      Full CI flow: provision → test → cleanup (x86 only)"
        echo "  cleanup        Destroy provisioned VMs (x86 only)"
        echo "  stop           Stop all containers"
        echo "  rebuild        Rebuild container images"
        exit 1
        ;;
esac
