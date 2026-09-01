#!/usr/bin/env bash
# PSProxmoxVE integration test orchestration script.
#
# Single source of truth for the provision → test → cleanup lifecycle.
# Called by both the GitHub Actions workflow and the local dev container.
#
# Provisions two PVE nodes per version (a/b) for cluster testing, plus a
# small storage VM on the same isolated VLAN serving NFS, iSCSI, and the
# auto-install answer files.
#
# Usage:
#   run-integration.sh provision [8|9|all]         Provision storage VM + nested PVE VMs
#   run-integration.sh test [8|9|all] [filter]    Run integration tests (default: all, no filter)
#   run-integration.sh cleanup [8|9|all]           Destroy provisioned VMs
#   run-integration.sh all [8|9|all]              Full lifecycle: provision → test → cleanup
#
#   The optional [filter] is a comma-separated list of test area names.
#   Each name is matched against integration test filenames (case-insensitive).
#   Examples:
#     run-integration.sh test 9 Connection,VMs    # Run Connection + VMs tests for PVE 9
#     run-integration.sh test all Cluster          # Run Cluster tests for all PVE versions
#
# Required env vars (provision/cleanup):
#   PVE_ENDPOINT       Parent PVE API URL (e.g. https://pve.example.com:8006)
#   PVE_API_TOKEN      Parent PVE API token
#   PVE_PASSWORD       Root password for nested PVE instances
#
# Required env vars (test with pre-existing PVE):
#   PVETEST_HOST       PVE host IP (node A)
#   PVETEST_PASSWORD   Root password for the PVE instances
#   Set SKIP_PROVISION=true
#
# Optional env vars:
#   PVE_TARGET_NODE    Parent PVE node name; unset or "auto" picks the online
#                      node with the most free memory (token needs PVEAuditor
#                      on /nodes for the memory stats)
#   PVE_VM_MEM_GB      Memory per nested PVE VM in GiB, for the headroom check
#                      (default: 8, matches the Terraform default)
#   PVE_MEM_HEADROOM_GB Free memory to leave the parent node (default: 8)
#   CACHE_DIR          ISO/image cache (default: /opt/pve-integration)
#   WORK_DIR           Temp dir for build artifacts (default: $CACHE_DIR/work)
#   CONFIG_FILE        Test config JSON path (default: $WORK_DIR/config.json)
#   MODULE_ARTIFACT    Path to built module DLLs (default: ./publish/netstandard2.0)
#   PVE_VERSIONS       Space-separated versions to provision (default: "9"; "9 8" still works)
#   STORAGE_ISCSI_IQN  iSCSI IQN for storage target (default: iqn.2024-01.local.test:storage)
#   STORAGE_VM_FQDN    DNS name of the storage VM; it boots via DHCP with
#                      hostname pvetest-storage and must be resolvable from
#                      the runner and the CI VLAN (default: pvetest-storage.test.local)
#   STORAGE_VMID       VMID for the storage VM (default: 5080)

set -euo pipefail

# ── Paths ───────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$INFRA_DIR/../.." && pwd)"

# ── Defaults ──────────────────────────────────────────────────────
CACHE_DIR="${CACHE_DIR:-/opt/pve-integration}"
# Always use a path under CACHE_DIR (shared mount) so files are visible
# to sibling Docker containers. Do NOT use RUNNER_TEMP — it's container-local
# in CI and invisible to the Docker host.
WORK_DIR="${WORK_DIR:-$CACHE_DIR/work}"
CONFIG_FILE="${CONFIG_FILE:-$WORK_DIR/config.json}"
MODULE_ARTIFACT="${MODULE_ARTIFACT:-$REPO_ROOT/publish/netstandard2.0}"
PVE_VERSIONS="${PVE_VERSIONS:-9}"
SKIP_PROVISION="${SKIP_PROVISION:-false}"
STORAGE_ISCSI_IQN="${STORAGE_ISCSI_IQN:-iqn.2024-01.local.test:storage}"
STORAGE_VM_FQDN="${STORAGE_VM_FQDN:-pvetest-storage.test.local}"
STORAGE_VMID="${STORAGE_VMID:-5080}"
# Keypair for SSH to the storage VM (cloud images refuse password SSH)
STORAGE_VM_SSH_KEY="${STORAGE_VM_SSH_KEY:-$WORK_DIR/storage-vm-key}"
# Must match CLOUD_IMAGE_FILENAME in ensure-cloud-images.sh
CLOUD_IMAGE_NAME="noble-server-cloudimg-amd64.qcow2"
# Store Terraform state on the shared mount so it persists across CI jobs
TF_STATE_FILE="$WORK_DIR/terraform.tfstate"

# ── Node config ─────────────────────────────────────────────────
# Each version gets two nodes: a (primary) and b (secondary).
# ISOs are per-version; nodes within a version share the same base ISO.

pve_iso() {
    local ver="${1%%[ab]}"  # strip suffix: "9a" -> "9"
    case "$ver" in
        9) echo "${PVE9_ISO:-proxmox-ve_9.1-1.iso}" ;;
        8) echo "${PVE8_ISO:-proxmox-ve_8.4-1.iso}" ;;
        *) echo "ERROR: unknown PVE version '$ver'" >&2; exit 1 ;;
    esac
}

# Generic auto-install ISO name. The answer-server host and first-boot.sh are
# both baked into the ISO, so both are in its name: WORK_DIR survives a run
# whose cleanup was skipped, and two lanes with differing first-boot.sh must
# not collide on one cached name.
pve_auto_iso() {
    local base first_boot_hash
    base="$(pve_iso "$1")"
    first_boot_hash="$(sha256sum "$SCRIPT_DIR/first-boot.sh" | cut -c1-12)"
    echo "${base%.iso}-auto-${STORAGE_VM_FQDN//./-}-${first_boot_hash}.iso"
}

pve_vmid() {
    case "$1" in
        9a) echo "${PVE9A_VMID:-5091}" ;; 9b) echo "${PVE9B_VMID:-5092}" ;;
        8a) echo "${PVE8A_VMID:-5081}" ;; 8b) echo "${PVE8B_VMID:-5082}" ;;
        *) echo "ERROR: unknown node '$1'" >&2; exit 1 ;;
    esac
}

pve_vmname() {
    case "$1" in
        9a) echo "pve-test-9a" ;; 9b) echo "pve-test-9b" ;;
        8a) echo "pve-test-8a" ;; 8b) echo "pve-test-8b" ;;
        *) echo "ERROR: unknown node '$1'" >&2; exit 1 ;;
    esac
}

pve_fqdn() {
    case "$1" in
        9a) echo "pve9a.test.local" ;; 9b) echo "pve9b.test.local" ;;
        8a) echo "pve8a.test.local" ;; 8b) echo "pve8b.test.local" ;;
        *) echo "ERROR: unknown node '$1'" >&2; exit 1 ;;
    esac
}

# Deterministic MAC addresses for each node (lowercase for answer server matching).
pve_mac() {
    case "$1" in
        9a) echo "aa:bb:cc:00:09:1a" ;; 9b) echo "aa:bb:cc:00:09:1b" ;;
        8a) echo "aa:bb:cc:00:08:1a" ;; 8b) echo "aa:bb:cc:00:08:1b" ;;
        *) echo "ERROR: unknown node '$1'" >&2; exit 1 ;;
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

# ── CI helpers ────────────────────────────────────────────────────
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

# ── Target node selection ───────────────────────────────────────────
# The chosen node is persisted to the shared mount: cleanup runs in a later
# job with its own environment and must aim at the node provision picked.
TARGET_NODE_FILE="$WORK_DIR/target-node"

# "node freeGiB" per online node; nodes without memory stats (token lacks
# Sys.Audit on /nodes) are omitted.
pve_free_nodes() {
    curl -ksf -H "Authorization: PVEAPIToken=$PVE_API_TOKEN" \
        "$PVE_ENDPOINT/api2/json/cluster/resources?type=node" |
        jq -r '.data[]
            | select(.status == "online" and .maxmem != null)
            | "\(.node) \((.maxmem - .mem) / 1073741824 | floor)"'
}

# resolve_target_node <node-count>: sets PVE_TARGET_NODE (auto mode picks the
# online node with the most free memory) and refuses to provision onto a node
# without enough headroom — a polite CI failure beats OOM-killing a guest on
# the parent hypervisor.
resolve_target_node() {
    local count="$1"
    # +2 GiB for the storage VM provisioned alongside the nested nodes
    local need=$(( count * ${PVE_VM_MEM_GB:-8} + 2 + ${PVE_MEM_HEADROOM_GB:-8} ))
    local nodes
    nodes="$(pve_free_nodes || true)"

    if [[ -z "${PVE_TARGET_NODE:-}" || "$PVE_TARGET_NODE" == "auto" ]]; then
        if [[ -z "$nodes" ]]; then
            ci_error "Node auto-selection needs memory stats from /cluster/resources — grant the token's user PVEAuditor on /nodes, or pin PVE_TARGET_NODE"
            exit 1
        fi
        PVE_TARGET_NODE="$(sort -k2 -rn <<<"$nodes" | head -1 | cut -d' ' -f1)"
        log "Auto-selected target node: $PVE_TARGET_NODE"
    fi

    local free
    free="$(awk -v n="$PVE_TARGET_NODE" '$1 == n {print $2}' <<<"$nodes")"
    if [[ -z "$free" ]]; then
        log "WARNING: no memory stats for $PVE_TARGET_NODE (token lacks PVEAuditor on /nodes?) — skipping the headroom check"
    elif (( free < need )); then
        ci_error "Refusing to provision: $PVE_TARGET_NODE has ${free}GiB free, need ${need}GiB ($count VMs x ${PVE_VM_MEM_GB:-8}GiB + 2GiB storage VM + ${PVE_MEM_HEADROOM_GB:-8}GiB headroom)"
        exit 1
    else
        log "Target node $PVE_TARGET_NODE: ${free}GiB free, need ${need}GiB"
    fi

    mkdir -p "$WORK_DIR"
    printf '%s' "$PVE_TARGET_NODE" > "$TARGET_NODE_FILE"
    export PVE_TARGET_NODE
}

# ── Subcommands ─────────────────────────────────────────────────────

cmd_provision() {
    local requested="${1:-all}"
    # Determine which versions/nodes to prepare and which to target
    local provision_versions="$PVE_VERSIONS"
    local provision_nodes="$ALL_NODES"
    if [[ "$requested" != "all" ]]; then
        provision_versions="$requested"
        provision_nodes=""
        for v in $provision_versions; do
            provision_nodes="$provision_nodes ${v}a ${v}b"
        done
    fi
    log "Starting provisioning..."
    log "  Versions: $provision_versions"
    log "  Nodes:$provision_nodes"
    log "  Storage: dedicated VM at $STORAGE_VM_FQDN (NFS + iSCSI + answer server)"
    require_env PVE_ENDPOINT
    require_env PVE_API_TOKEN
    require_env PVE_PASSWORD
    resolve_target_node "$(wc -w <<<"$provision_nodes")"

    ci_mask "$PVE_PASSWORD"
    mkdir -p "$WORK_DIR" "$CACHE_DIR"

    # Ensure base ISOs (one per version, not per node)
    for v in $provision_versions; do
        log "Ensuring base ISO for PVE $v..."
        bash "$SCRIPT_DIR/ensure-base-iso.sh" "$(pve_iso "$v")" "$CACHE_DIR"
    done

    # Ensure cloud images
    log "Ensuring cloud images..."
    local cloud_output
    cloud_output=$(bash "$SCRIPT_DIR/ensure-cloud-images.sh" "$CACHE_DIR")
    CLOUD_IMAGE_PATH=$(echo "$cloud_output" | grep "^CLOUD_IMAGE_PATH=" | cut -d= -f2)
    OVA_PATH=$(echo "$cloud_output" | grep "^OVA_PATH=" | cut -d= -f2)

    # Generate per-MAC answer files for the HTTP answer server.
    # Each node gets a file named by its MAC address so the server can
    # route the correct answer to each VM during auto-install.
    log "Generating answer files..."
    local escaped_pve_password
    escaped_pve_password=$(printf '%s' "$PVE_PASSWORD" | sed 's/[\/&\\]/\\&/g')
    mkdir -p "$WORK_DIR/answer-server/answers"

    # Default answer file (fallback for unknown MACs)
    sed -e "s/\${root_password}/${escaped_pve_password}/" \
        -e "s/\${fqdn}/pve-default.test.local/" \
        "$INFRA_DIR/answer.toml.tftpl" > "$WORK_DIR/answer-server/default.toml"

    for node in $provision_nodes; do
        local mac fqdn
        mac="$(pve_mac "$node")"
        fqdn="$(pve_fqdn "$node")"
        # Answer file named by lowercase MAC (server matches on MAC)
        sed -e "s/\${root_password}/${escaped_pve_password}/" \
            -e "s/\${fqdn}/${fqdn}/" \
            "$INFRA_DIR/answer.toml.tftpl" > "$WORK_DIR/answer-server/answers/${mac}.toml"
    done

    # Prepare generic HTTP auto-install ISOs (one per PVE version, not per node).
    # The first-boot script is embedded in the ISO via --on-first-boot so that
    # [first-boot] source = "from-iso" in the answer file still works.
    for v in $provision_versions; do
        local base_iso_name generic_iso
        base_iso_name="$(pve_iso "$v")"
        generic_iso="$WORK_DIR/$(pve_auto_iso "$v")"
        if [ ! -f "$generic_iso" ]; then
            log "Preparing HTTP auto-install ISO for PVE $v..."
            proxmox-auto-install-assistant prepare-iso \
                --fetch-from http \
                --url "http://${STORAGE_VM_FQDN}:8000/answer" \
                --on-first-boot "$SCRIPT_DIR/first-boot.sh" \
                --tmp "$WORK_DIR" \
                --output "$generic_iso" \
                "$CACHE_DIR/$base_iso_name"
        else
            log "HTTP auto-install ISO for PVE $v already exists, skipping."
        fi
    done

    # Terraform — remove any stale .tfvars from previous manual runs
    rm -f "$INFRA_DIR/terraform.tfvars"

    log "Running Terraform init..."
    (cd "$INFRA_DIR" && terraform init -input=false -reconfigure)

    # tfvars must carry every version even on a subset run — a filtered var map
    # makes terraform destroy the other versions' VMs still in state; subset
    # applies are limited with -target only.
    log "Building Terraform vars..."
    local tfvars="$WORK_DIR/instances.tfvars.json"

    # Build pve_isos map: version -> ISO path (one per version)
    local isos='{}'
    for v in $PVE_VERSIONS; do
        local iso_path="$WORK_DIR/$(pve_auto_iso "$v")"
        isos="$(jq --arg key "$v" --arg path "$iso_path" \
            '. + {($key): $path}' <<<"$isos")"
    done

    # Build pve_instances map: node -> VM config (references version, not ISO path)
    local instances='{}'
    for node in $ALL_NODES; do
        local v="${node%[ab]}"
        local vm_id vm_name mac
        vm_id="$(pve_vmid "$node")"
        vm_name="$(pve_vmname "$node")"
        mac="$(pve_mac "$node")"
        instances="$(jq \
            --arg key "$node" \
            --arg pve_version "$v" \
            --arg vm_name "$vm_name" \
            --argjson vm_id "$vm_id" \
            --arg mac_address "$mac" \
            '. + {($key): {pve_version: $pve_version, vm_id: $vm_id, vm_name: $vm_name, mac_address: $mac_address}}' \
            <<<"$instances")"
    done

    jq -n --argjson pve_instances "$instances" --argjson pve_isos "$isos" \
        '{pve_instances: $pve_instances, pve_isos: $pve_isos}' > "$tfvars"

    # The nested PVE installers fetch their answer files from the storage VM,
    # so it must be provisioned and configured before they boot: phase one
    # creates and configures the storage VM, phase two everything else.
    if [[ ! -f "$STORAGE_VM_SSH_KEY" ]]; then
        ssh-keygen -t ed25519 -f "$STORAGE_VM_SSH_KEY" -N '' -C 'pvetest-storage' >/dev/null
    fi

    tf_apply() {
        # TMPDIR: use work dir to avoid filling the container's /tmp with
        # multi-GB ISO uploads.
        (cd "$INFRA_DIR" && \
            TMPDIR="$WORK_DIR" \
            TF_VAR_proxmox_endpoint="$PVE_ENDPOINT" \
            TF_VAR_proxmox_api_token="$PVE_API_TOKEN" \
            TF_VAR_target_node="$PVE_TARGET_NODE" \
            TF_VAR_test_vm_password="$PVE_PASSWORD" \
            TF_VAR_cloud_image_path="$CLOUD_IMAGE_PATH" \
            TF_VAR_storage_vmid="$STORAGE_VMID" \
            TF_VAR_storage_vm_ssh_public_key="$(cat "${STORAGE_VM_SSH_KEY}.pub")" \
            terraform apply -auto-approve -input=false -state="$TF_STATE_FILE" -var-file="$tfvars" "$@")
    }

    log "Running Terraform apply (storage VM)..."
    tf_apply \
        -target=proxmox_virtual_environment_file.storage_cloud_image \
        -target=proxmox_virtual_environment_vm.storage

    log "Configuring storage VM at $STORAGE_VM_FQDN..."
    bash "$SCRIPT_DIR/setup-storage-server.sh" \
        "$STORAGE_VM_FQDN" "$STORAGE_VM_SSH_KEY" "$STORAGE_ISCSI_IQN" "$WORK_DIR/answer-server"

    log "Running Terraform apply (PVE nodes)..."
    local tf_targets=""
    if [[ "$requested" != "all" ]]; then
        for v in $provision_versions; do
            tf_targets="$tf_targets -target=proxmox_virtual_environment_file.auto_iso[\"$v\"]"
        done
        for node in $provision_nodes; do
            tf_targets="$tf_targets -target=proxmox_virtual_environment_vm.nested_pve[\"$node\"]"
        done
        log "Terraform targets: $tf_targets"
    fi

    tf_apply $tf_targets

    # Wait for PVE instances to boot and discover IPs
    for node in $provision_nodes; do
        log "Waiting for $node to boot..."
        local output
        output=$(bash "$SCRIPT_DIR/wait-for-pve.sh" \
            "$PVE_ENDPOINT" "$PVE_API_TOKEN" "$PVE_TARGET_NODE" \
            "$(pve_vmid "$node")" "$PVE_PASSWORD" 900)
        local ip node_name
        ip=$(echo "$output" | grep "^IP=" | cut -d= -f2)
        node_name=$(echo "$output" | grep "^NODE=" | cut -d= -f2)
        log "$node ready at $ip (node: $node_name)"
        jq -n --arg host "$ip" --arg node "$node_name" \
            '{host: $host, node: $node}' > "$WORK_DIR/${node}.json"
    done

    # Prepare test environments on provisioned PVE nodes
    for node in $provision_nodes; do
        local ip
        ip=$(jq -r .host "$WORK_DIR/${node}.json")
        log "Preparing test environment on $node ($ip)..."
        bash "$SCRIPT_DIR/prepare-test-environment.sh" "$ip" "$PVE_PASSWORD"
    done

    # Write test config — merge with existing config to preserve entries
    # from previously provisioned versions
    log "Writing test config to $CONFIG_FILE..."
    local config='{}'
    if [[ -f "$CONFIG_FILE" ]]; then
        config=$(cat "$CONFIG_FILE")
    fi

    for v in $provision_versions; do
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
        --arg storage_ip "$STORAGE_VM_FQDN" \
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
    local test_filter="${2:-}"
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
            : "${PVETEST_PASSWORD:?Set PVETEST_PASSWORD when using SKIP_PROVISION}"
            export PVETEST_PORT="${PVETEST_PORT:-8006}"
            export PVETEST_NODE="${PVETEST_NODE:-pve}"
            export PVETEST_STORAGE="${PVETEST_STORAGE:-local}"
            export PVETEST_CLOUD_IMAGE_PATH="${PVETEST_CLOUD_IMAGE_PATH:-}"
            export PVETEST_OVA_PATH="${PVETEST_OVA_PATH:-}"
            export PVETEST_HOST_B="${PVETEST_HOST_B:-}"
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
            export PVETEST_PORT=8006
            export PVETEST_NODE=$(jq -r ".pve${v}.nodes.a.node" "$CONFIG_FILE")
            export PVETEST_STORAGE=local
            export PVETEST_CLOUD_IMAGE_PATH=$(jq -r '.cloud_image_path' "$CONFIG_FILE")
            export PVETEST_OVA_PATH=$(jq -r '.ova_path' "$CONFIG_FILE")
            # Secondary node (b)
            export PVETEST_HOST_B=$(jq -r ".pve${v}.nodes.b.host" "$CONFIG_FILE")
            # Storage services (dedicated VM on the CI VLAN)
            export PVETEST_STORAGE_VM_IP=$(jq -r '.storage.ip' "$CONFIG_FILE")
            export PVETEST_ISCSI_IQN=$(jq -r '.storage.iscsi_iqn' "$CONFIG_FILE")
            export PVETEST_NFS_EXPORT=$(jq -r '.storage.nfs_export' "$CONFIG_FILE")
        fi

        export PVETEST_ISO_PATH="$iso_path"
        export PVETEST_PVE_VERSION="$v"
        export PVETEST_PASSWORD="${PVETEST_PASSWORD:-${PVE_PASSWORD:-}}"

        # Verify API reachable (node A) using ticket auth
        log "Verifying PVE $v node A API at $PVETEST_HOST:$PVETEST_PORT..."
        if ! curl -sk --connect-timeout 10 \
            -d "username=root@pam&password=${PVETEST_PASSWORD}" \
            "https://${PVETEST_HOST}:${PVETEST_PORT}/api2/json/access/ticket" | grep -q '"ticket"'; then
            ci_error "Cannot authenticate to PVE $v node A at ${PVETEST_HOST}:${PVETEST_PORT}"
            overall_exit=3
            continue
        fi

        # Verify node B if available
        if [[ -n "${PVETEST_HOST_B:-}" ]]; then
            log "Verifying PVE $v node B API at $PVETEST_HOST_B:$PVETEST_PORT..."
            if ! curl -sk --connect-timeout 10 \
                -d "username=root@pam&password=${PVETEST_PASSWORD}" \
                "https://${PVETEST_HOST_B}:${PVETEST_PORT}/api2/json/access/ticket" | grep -q '"ticket"'; then
                ci_error "Cannot authenticate to PVE $v node B at ${PVETEST_HOST_B}:${PVETEST_PORT}"
                overall_exit=3
                continue
            fi
        fi

        # Run Pester
        local test_exit=0
        local pester_filter_arg=""
        if [[ -n "$test_filter" ]]; then
            pester_filter_arg="$test_filter"
            log "Test filter: $test_filter"
        fi

        pwsh -NoProfile -Command "
            \$PveVersion = '$v'
            \$TestFilter = '$pester_filter_arg'
            Import-Module Pester -MinimumVersion 5.0
            \$config = New-PesterConfiguration

            if (\$TestFilter) {
                # Build list of matching test files
                \$integrationDir = 'tests/PSProxmoxVE.Tests/Integration'
                \$areas = \$TestFilter -split ','
                \$paths = @()
                foreach (\$area in \$areas) {
                    \$area = \$area.Trim()
                    \$matched = Get-ChildItem \"\$integrationDir/*\${area}*.Tests.ps1\" -ErrorAction SilentlyContinue
                    if (\$matched) {
                        \$paths += \$matched.FullName
                    } else {
                        Write-Warning \"No test files matched filter: \$area\"
                    }
                }
                if (\$paths.Count -eq 0) {
                    Write-Error \"No test files matched any filter in: \$TestFilter\"
                    exit 1
                }
                \$config.Run.Path = \$paths
                Write-Host \"Running \$(\$paths.Count) test file(s):\"
                \$paths | ForEach-Object { Write-Host \"  \$_\" }
            } else {
                \$config.Run.Path = 'tests/PSProxmoxVE.Tests/Integration'
            }

            \$config.Filter.Tag = 'Integration'
            \$config.Run.PassThru = \$true
            \$config.Output.Verbosity = 'Detailed'
            \$config.TestResult.Enabled = \$true
            \$config.TestResult.OutputFormat = 'NUnitXml'
            \$config.TestResult.OutputPath = \"TestResults/integration-results-pve\${PveVersion}.xml\"
            \$result = Invoke-Pester -Configuration \$config
            if (-not \$result -or \$result.TotalCount -eq 0) {
                Write-Error 'No integration tests were discovered or executed'
                exit 1
            }
            if (\$result.FailedCount -gt 0) {
                exit 1
            }
        " || test_exit=$?

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
    local requested="${1:-all}"
    log "Starting cleanup..."

    require_env PVE_ENDPOINT
    require_env PVE_API_TOKEN
    if [[ -z "${PVE_TARGET_NODE:-}" || "$PVE_TARGET_NODE" == "auto" ]]; then
        PVE_TARGET_NODE="$(cat "$TARGET_NODE_FILE" 2>/dev/null || true)"
    fi
    require_env PVE_TARGET_NODE

    # Build tfvars for all versions (Terraform needs the full variable map)
    local tfvars="$WORK_DIR/instances.tfvars.json"
    if [[ ! -f "$tfvars" ]]; then
        # Generate minimal tfvars if none exist (cleanup without prior provision)
        local instances='{}' isos='{}'
        for node in $ALL_NODES; do
            local v="${node%[ab]}" vm_id vm_name mac
            vm_id="$(pve_vmid "$node")"
            vm_name="$(pve_vmname "$node")"
            mac="$(pve_mac "$node")"
            instances="$(jq \
                --arg key "$node" \
                --arg pve_version "$v" \
                --arg vm_name "$vm_name" \
                --argjson vm_id "$vm_id" \
                --arg mac_address "$mac" \
                '. + {($key): {pve_version: $pve_version, vm_id: $vm_id, vm_name: $vm_name, mac_address: $mac_address}}' \
                <<<"$instances")"
            isos="$(jq --arg key "$v" --arg path "/dev/null" \
                '. + {($key): $path}' <<<"$isos")"
        done
        mkdir -p "$WORK_DIR"
        jq -n --argjson pve_instances "$instances" --argjson pve_isos "$isos" \
            '{pve_instances: $pve_instances, pve_isos: $pve_isos}' > "$tfvars"
    fi

    (cd "$INFRA_DIR" && terraform init -input=false 2>/dev/null)

    # Build -target flags when destroying a subset
    local tf_targets=""
    if [[ "$requested" != "all" ]]; then
        local cleanup_nodes=""
        for v in $requested; do
            cleanup_nodes="$cleanup_nodes ${v}a ${v}b"
            tf_targets="$tf_targets -target=proxmox_virtual_environment_file.auto_iso[\"$v\"]"
        done
        for node in $cleanup_nodes; do
            tf_targets="$tf_targets -target=proxmox_virtual_environment_vm.nested_pve[\"$node\"]"
        done
        log "Destroying PVE $requested nodes only..."
    else
        log "Destroying all resources..."
    fi

    (cd "$INFRA_DIR" && \
        TF_VAR_proxmox_endpoint="$PVE_ENDPOINT" \
        TF_VAR_proxmox_api_token="$PVE_API_TOKEN" \
        TF_VAR_target_node="$PVE_TARGET_NODE" \
        TF_VAR_test_vm_password="${PVE_PASSWORD:-placeholder}" \
        TF_VAR_storage_vmid="$STORAGE_VMID" \
        terraform destroy -auto-approve -input=false -state="$TF_STATE_FILE" -var-file="$tfvars" $tf_targets)

    # Clean up work directory when destroying all
    if [[ "$requested" == "all" ]]; then
        rm -f "$CONFIG_FILE" "$WORK_DIR"/instances.tfvars.json "$TARGET_NODE_FILE"
    fi

    log "Cleanup complete."
}

cmd_force_cleanup() {
    local requested="${1:-all}"
    local cleanup_nodes="$ALL_NODES"
    if [[ "$requested" != "all" ]]; then
        cleanup_nodes=""
        for v in $requested; do
            cleanup_nodes="$cleanup_nodes ${v}a ${v}b"
        done
    fi

    log "Force cleanup — bypassing Terraform, using direct API calls..."

    # preflight-cleanup.sh reads PVE_TARGET_NODE from the environment; in auto
    # mode resolve it from the node provision picked.
    if [[ -z "${PVE_TARGET_NODE:-}" || "$PVE_TARGET_NODE" == "auto" ]]; then
        PVE_TARGET_NODE="$(cat "$TARGET_NODE_FILE" 2>/dev/null || true)"
        [[ -n "$PVE_TARGET_NODE" ]] && export PVE_TARGET_NODE
    fi

    # Destroy VMs via the PVE API (works even with broken Terraform state)
    # Track which versions we've already cleaned up ISOs for (generic ISOs are shared)
    local cleaned_iso_versions=""
    for node in $cleanup_nodes; do
        local vm_id v iso_file
        vm_id="$(pve_vmid "$node")"
        v="${node%[ab]}"
        # Only clean up the generic ISO once per version
        iso_file=""
        if [[ ! " $cleaned_iso_versions " =~ " $v " ]]; then
            iso_file="$(pve_auto_iso "$v")"
            cleaned_iso_versions="$cleaned_iso_versions $v"
        fi
        log "Force cleaning $node (VMID $vm_id)..."
        bash "$SCRIPT_DIR/preflight-cleanup.sh" \
            "${PVE_ENDPOINT:-}" "${PVE_API_TOKEN:-}" \
            "$vm_id" "$iso_file" "$INFRA_DIR" \
            || true
    done

    # Unconditional: the state wipe below is unconditional too, and a storage
    # VM surviving its state entry cannot be reclaimed by the next provision.
    log "Force cleaning storage VM (VMID $STORAGE_VMID)..."
    bash "$SCRIPT_DIR/preflight-cleanup.sh" \
        "${PVE_ENDPOINT:-}" "${PVE_API_TOKEN:-}" \
        "$STORAGE_VMID" "$CLOUD_IMAGE_NAME" "$INFRA_DIR" \
        || true

    # Remove Terraform state (both local and shared mount) so next provision starts clean.
    # Keep .terraform.lock.hcl (provider version lock) for reproducibility.
    log "Removing Terraform state..."
    rm -f "$INFRA_DIR/terraform.tfstate" "$INFRA_DIR/terraform.tfstate.backup"
    rm -f "$TF_STATE_FILE" "${TF_STATE_FILE}.backup"
    rm -rf "$INFRA_DIR/.terraform"

    # Remove work artifacts, including locally cached auto-install ISOs
    rm -f "$CONFIG_FILE" "$WORK_DIR"/instances.tfvars.json "$TARGET_NODE_FILE"
    rm -f "$WORK_DIR"/*-auto-*.iso "$WORK_DIR"/*-http-auto.iso

    log "Force cleanup complete. Next provision will start from scratch."
}

cmd_taint() {
    local requested="${1:-all}"
    local taint_versions="$PVE_VERSIONS"
    local taint_nodes="$ALL_NODES"
    if [[ "$requested" != "all" ]]; then
        taint_versions="$requested"
        taint_nodes=""
        for v in $taint_versions; do
            taint_nodes="$taint_nodes ${v}a ${v}b"
        done
    fi

    log "Tainting PVE VMs for reprovisioning..."
    (cd "$INFRA_DIR" && terraform init -input=false -reconfigure 2>/dev/null)

    # Taint ISOs (keyed by version, e.g. "9")
    for v in $taint_versions; do
        log "  Tainting ISO: PVE $v"
        (cd "$INFRA_DIR" && \
            terraform taint -state="$TF_STATE_FILE" "proxmox_virtual_environment_file.auto_iso[\"$v\"]") 2>/dev/null || true
    done

    # Taint VMs (keyed by node, e.g. "9a")
    for node in $taint_nodes; do
        log "  Tainting VM: $node"
        (cd "$INFRA_DIR" && \
            terraform taint -state="$TF_STATE_FILE" "proxmox_virtual_environment_vm.nested_pve[\"$node\"]") 2>/dev/null || true
    done

    log "Taint complete. Next 'provision' will recreate these VMs."
}

cmd_all() {
    local test_versions="${1:-all}"
    local test_exit=0

    trap 'log "Running cleanup after test run..."; cmd_cleanup "$test_versions" || true' EXIT

    cmd_provision "$test_versions"
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
        force-cleanup) cmd_force_cleanup "$@" ;;
        taint)        cmd_taint "$@" ;;
        all)          cmd_all "$@" ;;
        *)
            echo "Usage: $(basename "$0") {provision|test|cleanup|force-cleanup|taint|all} [8|9|all] [test-filter]"
            echo ""
            echo "Subcommands:"
            echo "  provision [8|9|all]        Provision storage VM + nested PVE VMs"
            echo "  test [8|9|all] [filter]    Run integration tests (default: all versions, no filter)"
            echo "  cleanup [8|9|all]          Destroy resources via terraform destroy (default: all)"
            echo "  force-cleanup [8|9|all]    Bypass Terraform — destroy via API + wipe state (recovery)"
            echo "  taint [8|9|all]            Mark VMs for recreation on next provision"
            echo "  all [8|9|all]              Full lifecycle: provision → test → cleanup"
            echo ""
            echo "Test filter: comma-separated area names matching test filenames."
            echo "  Examples: Connection,VMs   Cluster   Storage,Network"
            exit 1
            ;;
    esac
}

main "$@"
