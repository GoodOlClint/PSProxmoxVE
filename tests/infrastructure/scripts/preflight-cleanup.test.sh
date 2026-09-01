#!/usr/bin/env bash
# Self-check for preflight-cleanup.sh's ISO cleanup branch.
#
# Stubs curl and sleep on PATH so every path runs offline in ~0s, then asserts
# on the DELETEs the script actually issued.
#
# Run: bash tests/infrastructure/scripts/preflight-cleanup.test.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$SCRIPT_DIR/preflight-cleanup.sh"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
mkdir -p "$TMP/bin" "$TMP/tf"

# Storage listing the stub serves. Two generated ISOs of the same family (the
# current first-boot.sh hash and a stale one), plus two volumes that must never
# be touched: the pinned base ISO and an unrelated upload.
cat > "$TMP/content.json" <<'JSON'
{"data":[
 {"volid":"ci-isos:iso/proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-158bb4537f15.iso"},
 {"volid":"ci-isos:iso/proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-0badc0ffee12.iso"},
 {"volid":"ci-isos:iso/proxmox-ve_9.2-1.iso"},
 {"volid":"ci-isos:iso/someone-elses.iso"},
 {"volid":"ci-isos:iso/proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-manual-backup.iso"},
 {"volid":"ci-isos:iso/proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-extra-0badc0ffee12.iso"},
 {"volid":"ci-isos:import/noble-server-cloudimg-amd64.qcow2"},
 {"volid":"ci-isos:iso/noble-server-cloudimg-amd64.qcow2"},
 {"volid":"ci-isos:iso/proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-aaaaaaaaaaaa.qcow2"},
 {"volid":"ci-isos:iso/evil-proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-bbbbbbbbbbbb.iso"},
 {"volid":"ci-isos:iso/proxmox-ve_9.2-1-auto-OTHERHOST-cccccccccccc.iso"},
 {"volid":"other-storage:iso/proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-dddddddddddd.iso"},
 {"volid":"ci-isos:iso/proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-eeeeeeeeeeee\n.iso"},
 {"volid":"ci-isos:iso/plain-image-0123456789ab.iso"},
 {"volid":"ci-isos:iso/plain-image-ffffffffffff.iso"}
]}
JSON

cat > "$TMP/bin/curl" <<'STUB'
#!/usr/bin/env bash
args="$*"
if [[ "$args" == *"-X DELETE"* ]]; then
    for a in "$@"; do [[ "$a" == http* ]] && echo "DELETE $a" >> "$STUB_LOG"; done
    # The script asks for the status with -w; echo what it expects to read.
    [[ "$args" == *"%{http_code}"* ]] && echo "${STUB_DELETE_CODE:-200}"
    exit 0
fi
if [[ "$args" == *"/storage/"*"/content"* ]]; then
    cat "$STUB_CONTENT"; exit 0
fi
if [[ "$args" == *"status/current"* ]]; then
    echo '{"data":{}}'; exit 0
fi
echo '{"data":[]}'
STUB
printf '#!/usr/bin/env bash\nexit 0\n' > "$TMP/bin/sleep"
chmod +x "$TMP/bin/curl" "$TMP/bin/sleep"

export PATH="$TMP/bin:$PATH"
export STUB_CONTENT="$TMP/content.json"
export PVE_TARGET_NODE=pve-test
export TF_VAR_iso_storage=ci-isos

# Records the exit status rather than discarding it: the script is best-effort by
# design (set -uo pipefail, no -e), so a path that dies early would otherwise
# still issue the expected DELETEs and pass.
run_case() {
    STUB_LOG="$TMP/log.$1"; export STUB_LOG; : > "$STUB_LOG"
    : > "$TMP/tf/terraform.tfstate"
    set +e
    bash "$TARGET" https://pve.example.com:8006 token@pam!t=x 5091 "$2" "$TMP/tf" >"$TMP/out.$1" 2>&1
    echo $? > "$TMP/rc.$1"
    set -e
}

assert_completed() {
    [ "$(cat "$TMP/rc.$1")" = "0" ] || fail "$1: script exited $(cat "$TMP/rc.$1")" "$TMP/out.$1"
    grep -q "Pre-flight cleanup complete" "$TMP/out.$1" || fail "$1: did not reach the end" "$TMP/out.$1"
    [ -f "$TMP/tf/terraform.tfstate" ] && fail "$1: stale Terraform state survived" "$TMP/out.$1"
    return 0
}

fail() { echo "FAIL: $1"; echo "--- delete log ---"; cat "$2" 2>/dev/null; exit 1; }

# 1. The whole generated family goes, and nothing else does.
run_case family "proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-158bb4537f15.iso"
assert_completed family
grep -q "158bb4537f15" "$TMP/log.family" || fail "current-hash ISO was not deleted" "$TMP/log.family"
grep -q "0badc0ffee12" "$TMP/log.family" || fail "stale-hash ISO was not deleted (#105)" "$TMP/log.family"
grep -q "proxmox-ve_9.2-1.iso" "$TMP/log.family" && fail "deleted the pinned base ISO" "$TMP/log.family"
grep -q "someone-elses" "$TMP/log.family" && fail "deleted an unrelated volume" "$TMP/log.family"

# 2. The storage VM's cloud image — the real non-family argument, and not an
#    .iso — matches only itself and drags nothing else with it.
run_case cloudimage "noble-server-cloudimg-amd64.qcow2"
assert_completed cloudimage
grep -q "noble-server-cloudimg" "$TMP/log.cloudimage" || fail "cloud image was not deleted" "$TMP/log.cloudimage"
[ "$(grep -c DELETE "$TMP/log.cloudimage")" = "1" ] || fail "cloud image cleanup deleted more than itself" "$TMP/log.cloudimage"

# 2b. A hand-uploaded sibling sharing the family prefix but NOT the hash shape
#     must survive: a prefix-only test would delete it.
run_case notfamily "proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-158bb4537f15.iso"
grep -q "manual-backup" "$TMP/log.notfamily" && fail "deleted a non-hash sibling" "$TMP/log.notfamily"
grep -q "ci-example-com-extra-0badc0ffee12" "$TMP/log.notfamily" && fail "deleted a longer-FQDN family" "$TMP/log.notfamily"
grep -q "aaaaaaaaaaaa.qcow2" "$TMP/log.notfamily" && fail "deleted a non-.iso under the family prefix" "$TMP/log.notfamily"
grep -q "evil-" "$TMP/log.notfamily" && fail "matched the prefix mid-string instead of anchoring" "$TMP/log.notfamily"
grep -q "OTHERHOST" "$TMP/log.notfamily" && fail "deleted another host family (prefix truncated?)" "$TMP/log.notfamily"
grep -q "other-storage" "$TMP/log.notfamily" && fail "deleted a volume on a different storage" "$TMP/log.notfamily"
# A volid carrying a newline splits the line-delimited channel: the tail arrives
# as a DELETE the matcher never approved.
grep -qE "content/\.iso$" "$TMP/log.notfamily" && fail "a newline in a volid produced an unapproved DELETE" "$TMP/log.notfamily"
grep -q "eeeeeeeeeeee" "$TMP/log.notfamily" && fail "deleted a volid containing a control character" "$TMP/log.notfamily"

# The DELETE must go to the percent-encoded volid, not a raw one: a bare volid
# would be read by PVE as extra path segments.
grep -q "content/ci-isos%3Aiso%2Fproxmox" "$TMP/log.family" || fail "volid was not URL-encoded in the DELETE" "$TMP/log.family"

# 3. A quote in the filename is data, not Python source. Before the fix this
#    interpolated into the program text and could execute (#111).
run_case inject "x') or __import__('os').system('touch $TMP/PWNED') or ''.endswith('y"
[ -e "$TMP/PWNED" ] && fail "filename was executed as Python (#111)" "$TMP/log.inject"
[ "$(grep -c DELETE "$TMP/log.inject" || true)" = "0" ] || fail "injection payload matched a volume" "$TMP/log.inject"
grep -q "No orphaned ISO found" "$TMP/out.inject" || fail "inject case did not reach the ISO branch" "$TMP/out.inject"
assert_completed inject

# 4. Unset storage skips only the ISO branch; VM destroy and state cleanup still run.
TF_VAR_iso_storage="" run_case nostorage "proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-158bb4537f15.iso"
grep -q "skipping ISO cleanup" "$TMP/out.nostorage" || fail "expected the skip warning" "$TMP/out.nostorage"
[ "$(grep -c DELETE "$TMP/log.nostorage" || true)" = "0" ] || fail "deleted an ISO with no storage configured" "$TMP/log.nostorage"
assert_completed nostorage

# 4b. A hash-shaped name that is NOT a generated auto-install ISO is not a
#     family: without the -auto- anchor it would sweep unrelated siblings.
run_case plain "plain-image-0123456789ab.iso"
assert_completed plain
grep -q "plain-image-0123456789ab" "$TMP/log.plain" || fail "plain image was not deleted" "$TMP/log.plain"
[ "$(grep -c DELETE "$TMP/log.plain")" = "1" ] || fail "a non-auto name swept siblings" "$TMP/log.plain"

# 5. A rejected DELETE is reported, not logged as done.
STUB_DELETE_CODE=403 run_case rejected "proxmox-ve_9.2-1-auto-pvetest-storage-ci-example-com-158bb4537f15.iso"
grep -q "ISO cleanup done" "$TMP/out.rejected" && fail "a 403 DELETE was reported as done" "$TMP/out.rejected"
grep -q "returned 403" "$TMP/out.rejected" || fail "a rejected DELETE was not surfaced" "$TMP/out.rejected"
assert_completed rejected

echo "PASS: preflight-cleanup.sh ISO cleanup"
