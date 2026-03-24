#!/usr/bin/env bash
# PSProxmoxVE integration test orchestration script.
#
# Single source of truth for the provision → test → cleanup lifecycle.
# Called by both the GitHub Actions workflow and the local dev container.
#
# Usage:
#   run-integration.sh provision          Provision nested PVE VMs
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
#   PVETEST_HOST       PVE host IP
#   PVETEST_APITOKEN   PVE API token
#   Set SKIP_PROVISION=true
#
# Optional env vars:
#   CACHE_DIR          ISO/image cache (default: /opt/pve-isos)
#   WORK_DIR           Temp dir for build artifacts (default: $RUNNER_TEMP or /tmp/pve-integration)
#   CONFIG_FILE        Test config JSON path (default: $CACHE_DIR/test-config.json)
#   MODULE_ARTIFACT    Path to built module DLLs (default: ./publish/netstandard2.0)
#   PVE_VERSIONS       Space-separated versions to provision (default: "9 8")

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

# ── Version config ──────────────────────────────────────────────────
pve_iso()    { case "$1" in 9) echo "${PVE9_ISO:-proxmox-ve_9.1-1.iso}";; 8) echo "${PVE8_ISO:-proxmox-ve_8.4-1.iso}";; esac; }
pve_vmid()   { case "$1" in 9) echo "${PVE9_VMID:-99909}";; 8) echo "${PVE8_VMID:-99908}";; esac; }
pve_vmname() { case "$1" in 9) echo "pve-test-pve9";; 8) echo "pve-test-pve8";; esac; }

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
    require_env PVE_ENDPOINT
    require_env PVE_API_TOKEN
    require_env PVE_TARGET_NODE
    require_env PVE_PASSWORD

    ci_mask "$PVE_PASSWORD"
    mkdir -p "$WORK_DIR" "$CACHE_DIR"

    # Ensure base ISOs
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

    # Pre-flight cleanup
    for v in $PVE_VERSIONS; do
        local iso_name
        iso_name="$(pve_iso "$v")"
        log "Pre-flight cleanup for PVE $v (VMID $(pve_vmid "$v"))..."
        bash "$SCRIPT_DIR/preflight-cleanup.sh" \
            "$PVE_ENDPOINT" "$PVE_API_TOKEN" \
            "$(pve_vmid "$v")" "${iso_name%.iso}-auto.iso" "$INFRA_DIR"
    done

    # Generate answer file
    log "Generating answer file..."
    local escaped_pve_password
    escaped_pve_password=$(printf '%s' "$PVE_PASSWORD" | sed 's/[\/&\\]/\\&/g')
    sed "s/\${root_password}/${escaped_pve_password}/" \
        "$INFRA_DIR/answer.toml.tftpl" > "$WORK_DIR/answer.toml"

    # Prepare auto-install ISOs
    for v in $PVE_VERSIONS; do
        local iso_name
        iso_name="$(pve_iso "$v")"
        log "Preparing auto-install ISO for PVE $v..."
        bash "$SCRIPT_DIR/prepare-auto-iso.sh" \
            "$CACHE_DIR/$iso_name" \
            "$WORK_DIR/answer.toml" \
            "$SCRIPT_DIR/first-boot.sh" \
            "$WORK_DIR/${iso_name%.iso}-auto.iso" \
            --cache-dir "$CACHE_DIR"
    done

    # Terraform
    log "Running Terraform init..."
    (cd "$INFRA_DIR" && terraform init -input=false)

    log "Building Terraform vars..."
    local tfvars="$WORK_DIR/instances.tfvars.json"
    local instances='{}'
    for v in $PVE_VERSIONS; do
        local iso_name
        iso_name="$(pve_iso "$v")"
        local iso_path="$WORK_DIR/${iso_name%.iso}-auto.iso"
        local vm_id
        vm_id="$(pve_vmid "$v")"
        local vm_name
        vm_name="$(pve_vmname "$v")"
        instances="$(jq \
            --arg key "pve${v}" \
            --arg iso_local_path "$iso_path" \
            --arg vm_name "$vm_name" \
            --argjson vm_id "$vm_id" \
            '. + {($key): {iso_local_path: $iso_local_path, vm_id: $vm_id, vm_name: $vm_name}}' \
            <<<"$instances")"
    done
    jq -n --argjson pve_instances "$instances" '{pve_instances: $pve_instances}' > "$tfvars"

    log "Running Terraform apply..."
    (cd "$INFRA_DIR" && \
        TF_VAR_proxmox_endpoint="$PVE_ENDPOINT" \
        TF_VAR_proxmox_api_token="$PVE_API_TOKEN" \
        TF_VAR_target_node="$PVE_TARGET_NODE" \
        TF_VAR_test_vm_password="$PVE_PASSWORD" \
        terraform apply -auto-approve -input=false -var-file="$tfvars")

    # Wait for PVE instances and create API tokens
    for v in $PVE_VERSIONS; do
        log "Waiting for PVE $v to boot and creating API token..."
        local output
        output=$(bash "$SCRIPT_DIR/create-api-token.sh" \
            "$PVE_ENDPOINT" "$PVE_API_TOKEN" \
            "$(pve_vmid "$v")" "$PVE_PASSWORD" 900)
        local ip token
        ip=$(echo "$output" | grep "^IP=" | cut -d= -f2)
        token=$(echo "$output" | grep "^TOKEN=" | cut -d= -f2-)
        log "PVE $v ready at $ip"
        echo "{\"host\":\"$ip\",\"token\":\"$token\"}" > "$WORK_DIR/pve${v}.json"
    done

    # Prepare test environments
    for v in $PVE_VERSIONS; do
        local ip
        ip=$(jq -r .host "$WORK_DIR/pve${v}.json")
        log "Preparing test environment on PVE $v ($ip)..."
        bash "$SCRIPT_DIR/prepare-test-environment.sh" "$ip" "$PVE_PASSWORD"
    done

    # Write test config
    log "Writing test config to $CONFIG_FILE..."
    local jq_args=()
    for v in $PVE_VERSIONS; do
        jq_args+=(--argjson "pve${v}" "$(cat "$WORK_DIR/pve${v}.json")")
    done
    jq_args+=(--arg cloud_image "${CLOUD_IMAGE_PATH:-}")
    jq_args+=(--arg ova "${OVA_PATH:-}")

    local jq_expr="{"
    local first=true
    for v in $PVE_VERSIONS; do
        $first || jq_expr+=","
        first=false
        jq_expr+="pve${v}: \$pve${v}"
    done
    jq_expr+=", cloud_image_path: \$cloud_image, ova_path: \$ova}"

    jq -n "${jq_args[@]}" "$jq_expr" > "$CONFIG_FILE"
    log "Test config written to $CONFIG_FILE"
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
        else
            if [[ ! -f "$CONFIG_FILE" ]]; then
                ci_error "No test config found at $CONFIG_FILE — run 'provision' first or set SKIP_PROVISION=true"
                exit 1
            fi
            export PVETEST_HOST=$(jq -r ".pve${v}.host" "$CONFIG_FILE")
            export PVETEST_APITOKEN=$(jq -r ".pve${v}.token" "$CONFIG_FILE")
            export PVETEST_PORT=8006
            export PVETEST_NODE=pve
            export PVETEST_STORAGE=local
            export PVETEST_CLOUD_IMAGE_PATH=$(jq -r '.cloud_image_path' "$CONFIG_FILE")
            export PVETEST_OVA_PATH=$(jq -r '.ova_path' "$CONFIG_FILE")
        fi

        export PVETEST_ISO_PATH="$iso_path"
        export PVETEST_PVE_VERSION="$v"
        export PVETEST_PASSWORD="${PVETEST_PASSWORD:-${PVE_PASSWORD:-}}"

        # Verify API reachable
        log "Verifying PVE $v API at $PVETEST_HOST:$PVETEST_PORT..."
        if ! curl -sk --connect-timeout 10 \
            -H "Authorization: PVEAPIToken=${PVETEST_APITOKEN}" \
            "https://${PVETEST_HOST}:${PVETEST_PORT}/api2/json/nodes" | grep -q '"node"'; then
            ci_error "Cannot reach PVE $v API at ${PVETEST_HOST}:${PVETEST_PORT}"
            overall_exit=3
            continue
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
    for v in $PVE_VERSIONS; do
        local iso_name
        iso_name="$(pve_iso "$v")"
        log "Cleaning up PVE $v (VMID $(pve_vmid "$v"))..."
        bash "$SCRIPT_DIR/preflight-cleanup.sh" \
            "${PVE_ENDPOINT:-}" "${PVE_API_TOKEN:-}" \
            "$(pve_vmid "$v")" "${iso_name%.iso}-auto.iso" "$INFRA_DIR" \
            || true
    done
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
            echo "  provision          Provision nested PVE VMs via Terraform"
            echo "  test [8|9|all]     Run integration tests (default: all versions)"
            echo "  cleanup            Destroy provisioned VMs"
            echo "  all [8|9|all]      Full lifecycle: provision → test → cleanup"
            exit 1
            ;;
    esac
}

main "$@"
