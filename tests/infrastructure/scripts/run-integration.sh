#!/usr/bin/env bash
# PSProxmoxVE integration test orchestration script.
#
# Single source of truth for the provision → test → cleanup lifecycle.
# Called by both the GitHub Actions workflow and the local dev container.
#
# Provisions two PVE nodes per version (a/b) for cluster testing, plus
# an Ubuntu storage VM for iSCSI/NFS shared storage testing.
#
# Usage:
#   run-integration.sh provision          Provision nested PVE VMs + storage VM
#   run-integration.sh test [8|9|all]     Run integration tests (default: all)
#   run-integration.sh cleanup            Destroy provisioned VMs
#   run-integration.sh all [8|9|all]      Full lifecycle: provision → test → cleanup
#
# Required env vars (provision/cleanup):
#   PVE_ENDPOINT       Parent PVE API URL (e.g. https://pve.example.com:8006)
#   PVE_API_TOKEN      Parent PVE API token
#   PVE_TARGET_NODE    Parent PVE node name
#   PVE_PASSWORD       Root password for nested PVE instances
#
# Required env vars (test with pre-existing PVE):
#   PVETEST_HOST       PVE host IP (node A)
#   PVETEST_APITOKEN   PVE API token (node A)
#   Set SKIP_PROVISION=true
#
# Optional env vars:
#   CACHE_DIR          ISO/image cache (default: /opt/pve-isos)
#   WORK_DIR           Temp dir for build artifacts (default: $RUNNER_TEMP or /tmp/pve-integration)
#   CONFIG_FILE        Test config JSON path (default: $CACHE_DIR/test-config.json)
#   MODULE_ARTIFACT    Path to built module DLLs (default: ./publish/netstandard2.0)
#   PVE_VERSIONS       Space-separated versions to provision (default: "9 8")
#   STORAGE_ISCSI_IQN  iSCSI IQN for storage target (default: iqn.2024-01.local.test:storage)

set -euo pipefail

# ── Paths ───────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$INFRA_DIR/../.." && pwd)"

# ── Defaults ────────────────────────────────────────────────────────
CACHE_DIR="${CACHE_DIR:-/opt/pve-isos}"
WORK_DIR="${WORK_DIR:-${RUNNER_TEMP:-/tmp/pve-integration}}"
CONFIG_FILE="${CONFIG_FILE:-$CACHE_DIR/test-config.json}"
MODULE_ARTIFACT="${MODULE_ARTIFACT:-$REPO_ROOT/publish/netstandard2.0}"
PVE_VERSIONS="${PVE_VERSIONS:-9 8}"
SKIP_PROVISION="${SKIP_PROVISION:-false}"
STORAGE_ISCSI_IQN="${STORAGE_ISCSI_IQN:-iqn.2024-01.local.test:storage}"
STORAGE_COMPOSE="$INFRA_DIR/docker-compose.storage.yml"

# ── Node config ───────────────────────────────────────────────────
# Each version gets two nodes: a (primary) and b (secondary).
# ISOs are per-version; nodes within a version share the same base ISO.

pve_iso() {
    local ver="${1%%[ab]}"  # strip suffix: "9a" -> "9"
    case "$ver" in
        9) echo "${PVE9_ISO:-proxmox-ve_9.1-1.iso}" ;;
        8) echo "${PVE8_ISO:-proxmox-ve_8.4-1.iso}" ;;
    esac
}

pve_vmid() {
    case "$1" in
        9a) echo "${PVE9A_VMID:-99091}" ;; 9b) echo "${PVE9B_VMID:-99092}" ;;
        8a) echo "${PVE8A_VMID:-99081}" ;; 8b) echo "${PVE8B_VMID:-99082}" ;;
    esac
}

pve_vmname() {
    case "$1" in
        9a) echo "pve-test-9a" ;; 9b) echo "pve-test-9b" ;;
        8a) echo "pve-test-8a" ;; 8b) echo "pve-test-8b" ;;
    esac
}

pve_fqdn() {
    case "$1" in
        9a) echo "pve9a.test.local" ;; 9b) echo "pve9b.test.local" ;;
        8a) echo "pve8a.test.local" ;; 8b) echo "pve8b.test.local" ;;
    esac
}

# Expand versions to node list: "9 8" -> "9a 9b 8a 8b"
expand_nodes() {
    local nodes=""
    for v in $PVE_VERSIONS; do
        nodes="$nodes ${v}a ${v}b"
    done
    echo $nodes
}

ALL_NODES="$(expand_nodes)"

# ── CI helpers ──────────────────────────────────────────────────────
ci_mask()  { [[ "${GITHUB_ACTIONS:-}" == "true" ]] && echo "::add-mask::$1" || true; }
ci_error() { [[ "${GITHUB_ACTIONS:-}" == "true" ]] && echo "::error::$1" || echo "ERROR: $1" >&2; }

log() { echo "==> $*"; }

require_env() {
    local var="$1"
    if [[ -z "${!var:-}" ]]; then
        ci_error "Required environment variable $var is not set"
        exit 1
    fi
}

# ── Subcommands ─────────────────────────────────────────────────────

cmd_provision() {
    log "Starting provisioning..."
    log "  Versions: $PVE_VERSIONS"
    log "  Nodes: $ALL_NODES"
    log "  Storage: Docker containers (iSCSI + NFS)"
    require_env PVE_ENDPOINT
    require_env PVE_API_TOKEN
    require_env PVE_TARGET_NODE
    require_env PVE_PASSWORD

    ci_mask "$PVE_PASSWORD"
    mkdir -p "$WORK_DIR" "$CACHE_DIR"

    # Ensure base ISOs (one per version, not per node)
    for v in $PVE_VERSIONS; do
        log "Ensuring base ISO for PVE $v..."
        bash "$SCRIPT_DIR/ensure-base-iso.sh" "$(pve_iso "$v")" "$CACHE_DIR"
    done

    # Ensure cloud images
    log "Ensuring cloud images..."
    local cloud_output
    cloud_output=$(bash "$SCRIPT_DIR/ensure-cloud-images.sh" "$CACHE_DIR")
    CLOUD_IMAGE_PATH=$(echo "$cloud_output" | grep "^CLOUD_IMAGE_PATH=" | cut -d= -f2)
    OVA_PATH=$(echo "$cloud_output" | grep "^OVA_PATH=" | cut -d= -f2)

    # Generate per-node answer files (each needs unique FQDN for clustering)
    log "Generating answer files..."
    local escaped_pve_password
    escaped_pve_password=$(printf '%s' "$PVE_PASSWORD" | sed 's/[\/&\\]/\\&/g')
    for node in $ALL_NODES; do
        local fqdn
        fqdn="$(pve_fqdn "$node")"
        sed -e "s/\${root_password}/${escaped_pve_password}/" \
            -e "s/\${fqdn}/${fqdn}/" \
            "$INFRA_DIR/answer.toml.tftpl" > "$WORK_DIR/answer-${node}.toml"
    done

    # Prepare auto-install ISOs (one per node — each has unique answer file)
    for node in $ALL_NODES; do
        local iso_name
        iso_name="$(pve_iso "$node")"
        log "Preparing auto-install ISO for $node..."
        bash "$SCRIPT_DIR/prepare-auto-iso.sh" \
            "$CACHE_DIR/$iso_name" \
            "$WORK_DIR/answer-${node}.toml" \
            "$SCRIPT_DIR/first-boot.sh" \
            "$WORK_DIR/${iso_name%.iso}-${node}-auto.iso" \
            --cache-dir "$CACHE_DIR"
    done

    # Terraform — remove any stale .tfvars from previous manual runs
    rm -f "$INFRA_DIR/terraform.tfvars"

    log "Running Terraform init..."
    (cd "$INFRA_DIR" && terraform init -input=false)

    log "Building Terraform vars..."
    local tfvars="$WORK_DIR/instances.tfvars.json"
    local instances='{}'
    for node in $ALL_NODES; do
        local iso_name
        iso_name="$(pve_iso "$node")"
        local iso_path="$WORK_DIR/${iso_name%.iso}-${node}-auto.iso"
        local vm_id
        vm_id="$(pve_vmid "$node")"
        local vm_name
        vm_name="$(pve_vmname "$node")"
        instances="$(jq \
            --arg key "$node" \
            --arg iso_local_path "$iso_path" \
            --arg vm_name "$vm_name" \
            --argjson vm_id "$vm_id" \
            '. + {($key): {iso_local_path: $iso_local_path, vm_id: $vm_id, vm_name: $vm_name}}' \
            <<<"$instances")"
    done

    jq -n --argjson pve_instances "$instances" \
        '{pve_instances: $pve_instances}' > "$tfvars"

    log "Running Terraform apply (PVE nodes)..."
    # TMPDIR: use work dir to avoid filling the container's /tmp with multi-GB ISO uploads.
    (cd "$INFRA_DIR" && \
        TMPDIR="$WORK_DIR" \
        TF_VAR_proxmox_endpoint="$PVE_ENDPOINT" \
        TF_VAR_proxmox_api_token="$PVE_API_TOKEN" \
        TF_VAR_target_node="$PVE_TARGET_NODE" \
        TF_VAR_test_vm_password="$PVE_PASSWORD" \
        terraform apply -auto-approve -input=false -var-file="$tfvars")

    # Start iSCSI/NFS storage containers on the Docker host
    log "Starting storage containers (iSCSI + NFS)..."
    # Get the Docker host's real IP (not the container's). The storage containers
    # use host networking, so PVE nodes reach them via the host's IP.
    local storage_ip
    storage_ip=$(docker info --format '{{range .Swarm.RemoteManagers}}{{.Addr}}{{end}}' 2>/dev/null | cut -d: -f1)
    if [ -z "$storage_ip" ]; then
        # Fallback: get IP of the default route's interface on the Docker host
        storage_ip=$(docker run --rm --net=host alpine ip route get 1.1.1.1 2>/dev/null | awk '{for(i=1;i<=NF;i++) if($i=="src") print $(i+1)}')
    fi
    if [ -z "$storage_ip" ]; then
        ci_error "Could not determine Docker host IP for storage services"
        exit 1
    fi
    ISCSI_IQN="$STORAGE_ISCSI_IQN" \
        docker compose -f "$STORAGE_COMPOSE" up -d
    log "Storage services ready at $storage_ip (iSCSI: $STORAGE_ISCSI_IQN)"

    # Wait for PVE instances and create API tokens (per node)
    for node in $ALL_NODES; do
        log "Waiting for $node to boot and creating API token..."
        local output
        output=$(bash "$SCRIPT_DIR/create-api-token.sh" \
            "$PVE_ENDPOINT" "$PVE_API_TOKEN" \
            "$(pve_vmid "$node")" "$PVE_PASSWORD" 900)
        local ip token
        ip=$(echo "$output" | grep "^IP=" | cut -d= -f2)
        token=$(echo "$output" | grep "^TOKEN=" | cut -d= -f2-)
        # Node name = hostname portion of FQDN (e.g. pve9a.test.local -> pve9a)
        local node_name
        node_name="$(pve_fqdn "$node" | cut -d. -f1)"
        log "$node ready at $ip (node: $node_name)"
        jq -n --arg host "$ip" --arg token "$token" --arg node "$node_name" \
            '{host: $host, token: $token, node: $node}' > "$WORK_DIR/${node}.json"
    done

    # Prepare test environments on all PVE nodes
    for node in $ALL_NODES; do
        local ip
        ip=$(jq -r .host "$WORK_DIR/${node}.json")
        log "Preparing test environment on $node ($ip)..."
        bash "$SCRIPT_DIR/prepare-test-environment.sh" "$ip" "$PVE_PASSWORD"
    done

    # Write test config
    log "Writing test config to $CONFIG_FILE..."
    local config='{}'

    for v in $PVE_VERSIONS; do
        local node_a="${v}a"
        local node_b="${v}b"
        local version_config
        version_config=$(jq -n \
            --argjson a "$(cat "$WORK_DIR/${node_a}.json")" \
            --argjson b "$(cat "$WORK_DIR/${node_b}.json")" \
            '{nodes: {a: $a, b: $b}}')
        config=$(jq --arg key "pve${v}" --argjson val "$version_config" \
            '. + {($key): $val}' <<<"$config")
    done

    config=$(jq \
        --arg cloud_image "${CLOUD_IMAGE_PATH:-}" \
        --arg ova "${OVA_PATH:-}" \
        --arg storage_ip "$storage_ip" \
        --arg storage_iqn "$STORAGE_ISCSI_IQN" \
        '. + {
            storage: {ip: $storage_ip, iscsi_iqn: $storage_iqn, nfs_export: ($storage_ip + ":/srv/nfs/shared")},
            cloud_image_path: $cloud_image,
            ova_path: $ova
        }' <<<"$config")

    echo "$config" | jq . > "$CONFIG_FILE"
    log "Test config written to $CONFIG_FILE"
    jq . "$CONFIG_FILE"
    log "Provisioning complete."
}

cmd_test() {
    local requested="${1:-all}"
    local versions_to_test

    if [[ "$requested" == "all" ]]; then
        versions_to_test="$PVE_VERSIONS"
    else
        versions_to_test="$requested"
    fi

    # Install module
    local module_path="${MODULE_PATH:-$HOME/.local/share/powershell/Modules/PSProxmoxVE}"
    if [[ -d "$MODULE_ARTIFACT" ]] && ls "$MODULE_ARTIFACT"/*.dll >/dev/null 2>&1; then
        log "Installing module from $MODULE_ARTIFACT..."
        mkdir -p "$module_path"
        cp -r "$MODULE_ARTIFACT"/* "$module_path/"
    else
        # Try building it
        log "Module artifact not found at $MODULE_ARTIFACT, building..."
        (cd "$REPO_ROOT" && dotnet publish src/PSProxmoxVE/PSProxmoxVE.csproj \
            -c Release -f netstandard2.0 -o /tmp/pve-module-publish 2>&1 | tail -1)
        mkdir -p "$module_path"
        cp -r /tmp/pve-module-publish/* "$module_path/"
    fi

    # Create test ISO
    local iso_path="$WORK_DIR/pvetest.iso"
    mkdir -p "$WORK_DIR"
    if [[ ! -f "$iso_path" ]]; then
        dd if=/dev/urandom of="$iso_path" bs=1M count=1 2>/dev/null
    fi

    local overall_exit=0

    for v in $versions_to_test; do
        log "Running integration tests for PVE $v..."

        # Set env vars from config or from environment
        if [[ "$SKIP_PROVISION" == "true" ]]; then
            : "${PVETEST_HOST:?Set PVETEST_HOST when using SKIP_PROVISION}"
            : "${PVETEST_APITOKEN:?Set PVETEST_APITOKEN when using SKIP_PROVISION}"
            export PVETEST_PORT="${PVETEST_PORT:-8006}"
            export PVETEST_NODE="${PVETEST_NODE:-pve}"
            export PVETEST_STORAGE="${PVETEST_STORAGE:-local}"
            export PVETEST_CLOUD_IMAGE_PATH="${PVETEST_CLOUD_IMAGE_PATH:-}"
            export PVETEST_OVA_PATH="${PVETEST_OVA_PATH:-}"
            # Secondary node and storage VM may not be available in skip mode
            export PVETEST_HOST_B="${PVETEST_HOST_B:-}"
            export PVETEST_APITOKEN_B="${PVETEST_APITOKEN_B:-}"
            export PVETEST_STORAGE_VM_IP="${PVETEST_STORAGE_VM_IP:-}"
            export PVETEST_ISCSI_IQN="${PVETEST_ISCSI_IQN:-}"
            export PVETEST_NFS_EXPORT="${PVETEST_NFS_EXPORT:-}"
        else
            if [[ ! -f "$CONFIG_FILE" ]]; then
                ci_error "No test config found at $CONFIG_FILE — run 'provision' first or set SKIP_PROVISION=true"
                exit 1
            fi
            # Primary node (a)
            export PVETEST_HOST=$(jq -r ".pve${v}.nodes.a.host" "$CONFIG_FILE")
            export PVETEST_APITOKEN=$(jq -r ".pve${v}.nodes.a.token" "$CONFIG_FILE")
            export PVETEST_PORT=8006
            export PVETEST_NODE=$(jq -r ".pve${v}.nodes.a.node" "$CONFIG_FILE")
            export PVETEST_STORAGE=local
            export PVETEST_CLOUD_IMAGE_PATH=$(jq -r '.cloud_image_path' "$CONFIG_FILE")
            export PVETEST_OVA_PATH=$(jq -r '.ova_path' "$CONFIG_FILE")
            # Secondary node (b)
            export PVETEST_HOST_B=$(jq -r ".pve${v}.nodes.b.host" "$CONFIG_FILE")
            export PVETEST_APITOKEN_B=$(jq -r ".pve${v}.nodes.b.token" "$CONFIG_FILE")
            # Storage services (Docker on runner)
            export PVETEST_STORAGE_VM_IP=$(jq -r '.storage.ip' "$CONFIG_FILE")
            export PVETEST_ISCSI_IQN=$(jq -r '.storage.iscsi_iqn' "$CONFIG_FILE")
            export PVETEST_NFS_EXPORT=$(jq -r '.storage.nfs_export' "$CONFIG_FILE")
        fi

        export PVETEST_ISO_PATH="$iso_path"
        export PVETEST_PVE_VERSION="$v"
        export PVETEST_PASSWORD="${PVETEST_PASSWORD:-${PVE_PASSWORD:-}}"

        # Verify API reachable (node A)
        log "Verifying PVE $v node A API at $PVETEST_HOST:$PVETEST_PORT..."
        if ! curl -sk --connect-timeout 10 \
            -H "Authorization: PVEAPIToken=${PVETEST_APITOKEN}" \
            "https://${PVETEST_HOST}:${PVETEST_PORT}/api2/json/nodes" | grep -q '"node"'; then
            ci_error "Cannot reach PVE $v node A API at ${PVETEST_HOST}:${PVETEST_PORT}"
            overall_exit=3
            continue
        fi

        # Verify node B if available
        if [[ -n "${PVETEST_HOST_B:-}" ]]; then
            log "Verifying PVE $v node B API at $PVETEST_HOST_B:$PVETEST_PORT..."
            if ! curl -sk --connect-timeout 10 \
                -H "Authorization: PVEAPIToken=${PVETEST_APITOKEN_B}" \
                "https://${PVETEST_HOST_B}:${PVETEST_PORT}/api2/json/nodes" | grep -q '"node"'; then
                ci_error "Cannot reach PVE $v node B API at ${PVETEST_HOST_B}:${PVETEST_PORT}"
                overall_exit=3
                continue
            fi
        fi

        # Run Pester
        local test_exit=0
        pwsh -NoProfile -Command '
            Import-Module Pester -MinimumVersion 5.0
            $config = New-PesterConfiguration
            $config.Run.Path = "tests/PSProxmoxVE.Tests/Integration"
            $config.Filter.Tag = "Integration"
            $config.Output.Verbosity = "Detailed"
            $config.TestResult.Enabled = $true
            $config.TestResult.OutputFormat = "NUnitXml"
            $config.TestResult.OutputPath = "TestResults/integration-results-pve'"$v"'.xml"
            Invoke-Pester -Configuration $config
        ' || test_exit=$?

        if [[ $test_exit -ne 0 ]]; then
            ci_error "PVE $v integration tests failed (exit code $test_exit)"
            overall_exit=3
        else
            log "PVE $v integration tests passed."
        fi
    done

    return $overall_exit
}

cmd_cleanup() {
    log "Starting cleanup..."

    # Clean up PVE nodes
    for node in $ALL_NODES; do
        local iso_name
        iso_name="$(pve_iso "$node")"
        log "Cleaning up $node (VMID $(pve_vmid "$node"))..."
        bash "$SCRIPT_DIR/preflight-cleanup.sh" \
            "${PVE_ENDPOINT:-}" "${PVE_API_TOKEN:-}" \
            "$(pve_vmid "$node")" "${iso_name%.iso}-${node}-auto.iso" "$INFRA_DIR" \
            || true
    done

    # Stop storage containers
    log "Stopping storage containers..."
    docker compose -f "$STORAGE_COMPOSE" down -v 2>/dev/null || true

    log "Cleanup complete."
}

cmd_all() {
    local test_versions="${1:-all}"
    local test_exit=0

    trap 'log "Running cleanup after test run..."; cmd_cleanup || true' EXIT

    cmd_provision
    cmd_test "$test_versions" || test_exit=$?

    if [[ $test_exit -ne 0 ]]; then
        log "Tests failed with exit code $test_exit. Cleanup will still run."
    fi

    # Trap handles cleanup on exit
    return $test_exit
}

# ── Main ────────────────────────────────────────────────────────────
main() {
    local cmd="${1:-}"
    shift || true

    case "$cmd" in
        provision)    cmd_provision "$@" ;;
        test)         cmd_test "$@" ;;
        cleanup)      cmd_cleanup "$@" ;;
        all)          cmd_all "$@" ;;
        *)
            echo "Usage: $(basename "$0") {provision|test|cleanup|all} [8|9|all]"
            echo ""
            echo "Subcommands:"
            echo "  provision          Provision nested PVE VMs + storage VM"
            echo "  test [8|9|all]     Run integration tests (default: all versions)"
            echo "  cleanup            Destroy all provisioned VMs"
            echo "  all [8|9|all]      Full lifecycle: provision → test → cleanup"
            exit 1
            ;;
    esac
}

main "$@"
