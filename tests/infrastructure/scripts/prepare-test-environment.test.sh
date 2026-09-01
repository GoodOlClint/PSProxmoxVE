#!/usr/bin/env bash
# Self-check for prepare-test-environment.sh's opt-in dist-upgrade branch.
#
# Stubs sshpass/curl/sleep on PATH so every path runs offline in ~0s, then
# asserts on the commands the script actually issued.
#
# The stub is deliberately stateful: boot_id must differ across the reboot, and
# case 3 pins the failure by returning the SAME boot_id twice.
#
# Run: bash tests/infrastructure/scripts/prepare-test-environment.test.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$SCRIPT_DIR/prepare-test-environment.sh"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

mkdir -p "$TMP/bin"

# Fake sshpass. Logs every invocation; emits plausible output per command.
# BOOT_ID_STUCK=1 makes it return an unchanging boot_id, simulating a node that
# never rebooted.
cat > "$TMP/bin/sshpass" <<'STUB'
#!/usr/bin/env bash
echo "$*" >> "$STUB_LOG"
case "$*" in
    *boot_id*)
        if [[ "${BOOT_ID_STUCK:-0}" == "1" ]]; then
            echo "11111111-1111-1111-1111-111111111111"
        else
            n=0
            [[ -f "$STUB_STATE/boot_calls" ]] && n=$(cat "$STUB_STATE/boot_calls")
            n=$((n + 1))
            echo "$n" > "$STUB_STATE/boot_calls"
            if [[ "$n" -le 1 ]]; then
                echo "11111111-1111-1111-1111-111111111111"
            else
                echo "22222222-2222-2222-2222-222222222222"
            fi
        fi
        ;;
    *dpkg-query*)
        # DPKG_EMPTY=1 simulates a failed query whose output is swallowed by
        # the remote `| sort`, which succeeds on empty input.
        [[ "${DPKG_EMPTY:-0}" == "1" ]] || printf 'proxmox-kernel-6.14\t6.14.11-1\npve-manager\t9.2.1\n'
        ;;
    *uname*)
        printf '# running-kernel\t6.14.11-1-pve\n'
        ;;
esac
exit 0
STUB

# wait-for-api.sh greps curl output for "version"; log the call so the test can
# assert the wait actually ran. sleep must not really sleep.
cat > "$TMP/bin/curl" <<'STUB'
#!/usr/bin/env bash
echo "curl $*" >> "$STUB_LOG"
echo '{"data":{"version":"9.2.1"}}'
STUB
cat > "$TMP/bin/sleep" <<'STUB'
#!/usr/bin/env bash
exit 0
STUB

chmod +x "$TMP/bin/"*
export PATH="$TMP/bin:$PATH"
export STUB_STATE="$TMP"

fail=0
check() {
    local desc="$1" haystack="$2" needle="$3" want="$4"
    local found=no
    grep -q -- "$needle" "$haystack" && found=yes
    if [[ "$found" == "$want" ]]; then
        echo "  ok: $desc"
    else
        echo "  FAIL: $desc (expected present=$want, got present=$found)"
        fail=1
    fi
}
pass() { echo "  ok: $1"; }
fatal() { echo "  FAIL: $1"; fail=1; }

echo "case 1: no dist-upgrade argument — lane 1 path must be untouched"
export STUB_LOG="$TMP/log1"
: > "$STUB_LOG"; rm -f "$TMP/boot_calls"
bash "$TARGET" 10.0.0.1 secret > "$TMP/out1" 2>&1
check "no dist-upgrade issued"      "$STUB_LOG" "dist-upgrade"    no
check "no reboot issued"            "$STUB_LOG" "systemctl reboot" no
check "no package set recorded"     "$STUB_LOG" "dpkg-query"      no
check "no boot_id probe"            "$STUB_LOG" "boot_id"         no
check "storage still configured"    "$STUB_LOG" "pvesm set local" yes

echo "case 2: dist-upgrade requested"
export STUB_LOG="$TMP/log2"
: > "$STUB_LOG"; rm -f "$TMP/boot_calls"
bash "$TARGET" 10.0.0.1 secret 1 "$TMP/packages.txt" > "$TMP/out2" 2>&1
check "dist-upgrade issued"         "$STUB_LOG" "dist-upgrade"    yes
check "reboot issued"               "$STUB_LOG" "systemctl reboot" yes
check "package set recorded"        "$STUB_LOG" "dpkg-query"      yes
check "boot_id checked"             "$STUB_LOG" "boot_id"         yes
check "waited for the API"          "$STUB_LOG" "api2/json/version" yes
check "waited for pmxcfs"           "$STUB_LOG" "/etc/pve/storage.cfg" yes
check "storage still configured"    "$STUB_LOG" "pvesm set local" yes

# The stub replaces the remote shell, so it cannot observe what that shell does
# with a command — only which command was sent. These two assert at that level,
# because both defects live in the command string itself:
#   - without `set -o pipefail`, a failed remote dpkg-query is masked by `sort`,
#     which succeeds on empty input and makes ssh return 0.
#   - bash's builtin `echo` does not interpret \t without -e, so `echo` here
#     would write the one row in the file lacking a real tab.
check "query sets remote pipefail"  "$STUB_LOG" "set -o pipefail" yes
check "kernel capture uses printf"  "$STUB_LOG" "printf '# running-kernel" yes

# The reboot must be issued after the upgrade, or the node records a package
# set it never booted.
upgrade_line=$(grep -n "dist-upgrade" "$STUB_LOG" | head -1 | cut -d: -f1)
reboot_line=$(grep -n "systemctl reboot" "$STUB_LOG" | head -1 | cut -d: -f1)
api_line=$(grep -n "api2/json/version" "$STUB_LOG" | head -1 | cut -d: -f1)
[[ "$reboot_line" -gt "$upgrade_line" ]] \
    && pass "reboot ordered after dist-upgrade" \
    || fatal "reboot ordered before dist-upgrade"
[[ "$api_line" -gt "$reboot_line" ]] \
    && pass "API wait ordered after reboot" \
    || fatal "API wait ordered before reboot"

# The running-kernel row must carry a REAL tab, like every dpkg-query row.
# `echo "...\t..."` in bash emits a literal backslash-t and would fail here.
if grep -q '^# running-kernel' "$TMP/packages.txt"; then
    pass "running kernel recorded"
    if grep -qP '^# running-kernel\t' "$TMP/packages.txt" 2>/dev/null \
        || awk -F'\t' '/^# running-kernel/ && NF == 2 {found=1} END {exit !found}' "$TMP/packages.txt"; then
        pass "running-kernel row uses a real tab"
    else
        fatal "running-kernel row has a literal backslash-t, not a tab"
    fi
else
    fatal "running kernel not recorded"
fi

[[ -s "$TMP/packages.txt" ]] && pass "package file non-empty" || fatal "package file empty or missing"

echo "case 3: node never rebooted — must be fatal"
export STUB_LOG="$TMP/log3"
: > "$STUB_LOG"; rm -f "$TMP/boot_calls"
if BOOT_ID_STUCK=1 bash "$TARGET" 10.0.0.1 secret 1 "$TMP/packages3.txt" > "$TMP/out3" 2>&1; then
    fatal "script exited 0 despite an unchanged boot_id"
else
    pass "unchanged boot_id fails the run"
    grep -q "did not reboot" "$TMP/out3" \
        && pass "failure names the cause" \
        || fatal "failure message does not mention the reboot"
fi

echo "case 4: dpkg-query produced nothing — must be fatal"
export STUB_LOG="$TMP/log4"
: > "$STUB_LOG"; rm -f "$TMP/boot_calls"
if DPKG_EMPTY=1 bash "$TARGET" 10.0.0.1 secret 1 "$TMP/packages4.txt" > "$TMP/out4" 2>&1; then
    fatal "script exited 0 despite an empty package set"
else
    pass "empty package set fails the run"
    grep -q "empty package set" "$TMP/out4" \
        && pass "failure names the cause" \
        || fatal "failure message does not mention the package set"
fi

if [[ "$fail" -eq 0 ]]; then
    echo "PASS"
else
    echo "FAILED"
    exit 1
fi
