#!/usr/bin/env bash
# PSProxmoxVE integration test orchestration script.
#
# Single source of truth for the provision → test → cleanup lifecycle.
# Called by both the GitHub Actions workflow and the local dev container.
#
# Provisions two PVE nodes per version (a/b) for cluster testing, plus
# Docker containers on the runner host for iSCSI/NFS shared storage.
#
# Usage:
#   run-integration.sh provision [8|9|all]         Provision nested PVE VMs + start storage containers
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
#   CONFIG_FILE        Test config JSON path (default: $WORK_DIR/config.json)
#   MODULE_ARTIFACT    Path to built module DLLs (default: ./publish/netstandard2.0)
#   PVE_VERSIONS       Space-separated versions to provision (default: "9 8")
#   STORAGE_ISCSI_IQN  iSCSI IQN for storage target (default: iqn.2024-01.local.test:storage)

set -euo pipefail

# ── Paths ───────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$INFRA_DIR/../.." && pwd)"

# ── Defaults ────────────────────────────────────────────────────────
CACHE_DIR="${CACHE_DIR:-/opt/pve-integration}"
WORK_DIR="${WORK_DIR:-${RUNNER_TEMP:-$CACHE_DIR/work}}"
CONFIG_FILE="${CONFIG_FILE:-$WORK_DIR/config.json}"
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
        *) echo "ERROR: unknown PVE version '$ver'" >&2; exit 1 ;;
    esac
}

pve_vmid() {
    case "$1" in
        9a) echo "${PVE9A_VMID:-99091}" ;; 9b) echo "${PVE9B_VMID:-99092}" ;;
        8a) echo "${PVE8A_VMID:-99081}" ;; 8b) echo "${PVE8B_VMID:-99082}" ;;
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
    log "  Storage: Docker containers (iSCSI + NFS)"
    require_env PVE_ENDPOINT
    require_env PVE_API_TOKEN
    require_env PVE_TARGET_NODE
    require_env PVE_PASSWORD

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

    # Discover Docker host IP early — needed for the HTTP auto-install ISO URL
    # and for the docker_host_ip Terraform variable.
    local storage_ip
    storage_ip=$(docker run --rm --net=host alpine ip route get 1.1.1.1 2>/dev/null | awk '{for(i=1;i<=NF;i++) if($i=="src") print $(i+1)}')
    if [ -z "$storage_ip" ]; then
        storage_ip=$(docker info --format '{{.Swarm.NodeAddr}}' 2>/dev/null | cut -d: -f1)
    fi
    if [ -z "$storage_ip" ]; then
        ci_error "Could not determine Docker host IP for storage services"
        exit 1
    fi
    log "Docker host IP: $storage_ip"

    # Generate per-MAC answer files for the HTTP answer server.
    # Each node gets a file named by its MAC address so the server can
    # route the correct answer to each VM during auto-install.
    log "Generating answer files..."
    local escaped_pve_password
    escaped_pve_password=$(printf '%s' "$PVE_PASSWORD" | sed 's/[\/&\\]/\\&/g')
    mkdir -p "$WORK_DIR/answers"

    # Default answer file (fallback for unknown MACs)
    sed -e "s/\${root_password}/${escaped_pve_password}/" \
        -e "s/\${fqdn}/pve-default.test.local/" \
        "$INFRA_DIR/answer.toml.tftpl" > "$WORK_DIR/default-answer.toml"

    for node in $provision_nodes; do
        local mac fqdn
        mac="$(pve_mac "$node")"
        fqdn="$(pve_fqdn "$node")"
        # Answer file named by lowercase MAC with colons (server matches on MAC)
        sed -e "s/\${root_password}/${escaped_pve_password}/" \
            -e "s/\${fqdn}/${fqdn}/" \
            "$INFRA_DIR/answer.toml.tftpl" > "$WORK_DIR/answers/${mac}.toml"
    done

    # Prepare generic HTTP auto-install ISOs (one per PVE version, not per node).
    # The first-boot script is embedded in the ISO via --on-first-boot so that
    # [first-boot] source = "from-iso" in the answer file still works.
    for v in $provision_versions; do
        local base_iso_name generic_iso
        base_iso_name="$(pve_iso "$v")"
        generic_iso="$WORK_DIR/${base_iso_name%.iso}-http-auto.iso"
        if [ ! -f "$generic_iso" ]; then
            log "Preparing HTTP auto-install ISO for PVE $v..."
            proxmox-auto-install-assistant prepare-iso \
                --fetch-from http \
                --url "http://${storage_ip}:8000/answer" \
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
    (cd "$INFRA_DIR" && terraform init -input=false)

    # Always build tfvars for ALL versions to keep Terraform state consistent.
    # When provisioning a subset, we use -target to limit the apply.
    log "Building Terraform vars..."
    local tfvars="$WORK_DIR/instances.tfvars.json"

    # Build pve_isos map: version -> ISO path (one per version)
    local isos='{}'
    for v in $PVE_VERSIONS; do
        local iso_name
        iso_name="$(pve_iso "$v")"
        local iso_path="$WORK_DIR/${iso_name%.iso}-http-auto.iso"
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

    log "Running Terraform apply (PVE nodes)..."
    # Build -target flags when provisioning a subset of versions.
    # This prevents Terraform from destroying VMs for other versions
    # that exist in state but aren't in the filtered tfvars.
    local tf_targets=""
    if [[ "$requested" != "all" ]]; then
        for v in $provision_versions; do
            tf_targets="$tf_targets -target=proxmox_virtual_environment_file.auto_iso[\"$v\"]"
        done
        for node in $provision_nodes; do
            tf_targets="$tf_targets -target=proxmox_virtual_environment_vm.nested_pve[\"$node\"]"
        done
        # Always include shared Docker storage and answer server resources
        tf_targets="$tf_targets -target=docker_image.ubuntu"
        tf_targets="$tf_targets -target=docker_container.iscsi_target"
        tf_targets="$tf_targets -target=docker_container.nfs_server"
        tf_targets="$tf_targets -target=docker_container.answer_server"
        tf_targets="$tf_targets -target=docker_volume.iscsi_data"
        tf_targets="$tf_targets -target=docker_volume.nfs_data"
        log "Terraform targets: $tf_targets"
    fi

    # TMPDIR: use work dir to avoid filling the container's /tmp with multi-GB ISO uploads.
    (cd "$INFRA_DIR" && \
        TMPDIR="$WORK_DIR" \
        TF_VAR_proxmox_endpoint="$PVE_ENDPOINT" \
        TF_VAR_proxmox_api_token="$PVE_API_TOKEN" \
        TF_VAR_target_node="$PVE_TARGET_NODE" \
        TF_VAR_test_vm_password="$PVE_PASSWORD" \
        TF_VAR_docker_host_ip="$storage_ip" \
        TF_VAR_answer_files_dir="$WORK_DIR/answers" \
        TF_VAR_default_answer_file="$WORK_DIR/default-answer.toml" \
        terraform apply -auto-approve -input=false -var-file="$tfvars" $tf_targets)

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
            # Storage services (Docker on runner)
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
            \$config.Output.Verbosity = 'Detailed'
            \$config.TestResult.Enabled = \$true
            \$config.TestResult.OutputFormat = 'NUnitXml'
            \$config.TestResult.OutputPath = \"TestResults/integration-results-pve\${PveVersion}.xml\"
            Invoke-Pester -Configuration \$config
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
    require_env PVE_TARGET_NODE

    # Discover Docker host IP for the docker_host_ip variable
    local storage_ip
    storage_ip=$(docker run --rm --net=host alpine ip route get 1.1.1.1 2>/dev/null | awk '{for(i=1;i<=NF;i++) if($i=="src") print $(i+1)}')
    if [ -z "$storage_ip" ]; then
        storage_ip=$(docker info --format '{{.Swarm.NodeAddr}}' 2>/dev/null | cut -d: -f1)
    fi

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

    # Ensure answer file paths exist (terraform destroy validates host_path mounts)
    mkdir -p "$WORK_DIR/answers"
    touch "$WORK_DIR/default-answer.toml"

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
        TF_VAR_docker_host_ip="${storage_ip:-127.0.0.1}" \
        TF_VAR_answer_files_dir="${WORK_DIR}/answers" \
        TF_VAR_default_answer_file="${WORK_DIR}/default-answer.toml" \
        terraform destroy -auto-approve -input=false -var-file="$tfvars" $tf_targets) || true

    # Clean up work directory when destroying all
    if [[ "$requested" == "all" ]]; then
        rm -f "$CONFIG_FILE" "$WORK_DIR"/instances.tfvars.json
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

    # Destroy VMs via the PVE API (works even with broken Terraform state)
    # Track which versions we've already cleaned up ISOs for (generic ISOs are shared)
    local cleaned_iso_versions=""
    for node in $cleanup_nodes; do
        local vm_id v iso_name iso_file
        vm_id="$(pve_vmid "$node")"
        v="${node%[ab]}"
        iso_name="$(pve_iso "$v")"
        # Only clean up the generic ISO once per version
        iso_file=""
        if [[ ! " $cleaned_iso_versions " =~ " $v " ]]; then
            iso_file="${iso_name%.iso}-http-auto.iso"
            cleaned_iso_versions="$cleaned_iso_versions $v"
        fi
        log "Force cleaning $node (VMID $vm_id)..."
        bash "$SCRIPT_DIR/preflight-cleanup.sh" \
            "${PVE_ENDPOINT:-}" "${PVE_API_TOKEN:-}" \
            "$vm_id" "$iso_file" "$INFRA_DIR" \
            || true
    done

    # Always stop Docker containers in force mode — leaving them causes
    # Terraform to fail on next provision (container already exists).
    log "Stopping storage and answer server containers..."
    docker rm -f pvetest-iscsi pvetest-nfs pvetest-answer-server 2>/dev/null || true
    docker volume rm pvetest-iscsi-data pvetest-nfs-data 2>/dev/null || true

    # Remove Terraform state so next provision starts clean
    log "Removing Terraform state..."
    rm -f "$INFRA_DIR/terraform.tfstate" "$INFRA_DIR/terraform.tfstate.backup"
    rm -f "$INFRA_DIR/.terraform.lock.hcl"
    rm -rf "$INFRA_DIR/.terraform"

    # Remove work artifacts
    rm -f "$CONFIG_FILE" "$WORK_DIR"/instances.tfvars.json

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
    (cd "$INFRA_DIR" && terraform init -input=false 2>/dev/null)

    # Taint ISOs (keyed by version, e.g. "9")
    for v in $taint_versions; do
        log "  Tainting ISO: PVE $v"
        (cd "$INFRA_DIR" && \
            terraform taint "proxmox_virtual_environment_file.auto_iso[\"$v\"]") 2>/dev/null || true
    done

    # Taint VMs (keyed by node, e.g. "9a")
    for node in $taint_nodes; do
        log "  Tainting VM: $node"
        (cd "$INFRA_DIR" && \
            terraform taint "proxmox_virtual_environment_vm.nested_pve[\"$node\"]") 2>/dev/null || true
    done

    log "Taint complete. Next 'provision' will recreate these VMs."
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
        force-cleanup) cmd_force_cleanup "$@" ;;
        taint)        cmd_taint "$@" ;;
        all)          cmd_all "$@" ;;
        *)
            echo "Usage: $(basename "$0") {provision|test|cleanup|taint|all} [8|9|all] [test-filter]"
            echo ""
            echo "Subcommands:"
            echo "  provision [8|9|all]        Provision nested PVE VMs + storage containers"
            echo "  test [8|9|all] [filter]    Run integration tests (default: all versions, no filter)"
            echo "  cleanup [8|9|all]          Destroy resources via terraform destroy (default: all)"
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
