#!/usr/bin/env bash
# Self-check for prepare-test-environment.sh's opt-in dist-upgrade branch.
#
# Stubs sshpass/curl/sleep on PATH so both paths run offline in ~0s, then
# asserts on the commands the script actually issued.
#
# Run: bash tests/infrastructure/scripts/prepare-test-environment.test.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$SCRIPT_DIR/prepare-test-environment.sh"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

mkdir -p "$TMP/bin"

# Fake sshpass: log every invocation, emit a plausible dpkg-query result.
cat > "$TMP/bin/sshpass" <<'STUB'
#!/usr/bin/env bash
echo "$*" >> "$STUB_LOG"
case "$*" in
    *dpkg-query*) echo -e "proxmox-kernel-6.14\t6.14.11-1\npve-manager\t9.2.1" ;;
esac
exit 0
STUB

# wait-for-api.sh greps curl output for "version"; sleep must not really sleep.
cat > "$TMP/bin/curl" <<'STUB'
#!/usr/bin/env bash
echo '{"data":{"version":"9.2.1"}}'
STUB
cat > "$TMP/bin/sleep" <<'STUB'
#!/usr/bin/env bash
exit 0
STUB

chmod +x "$TMP/bin/"*
export PATH="$TMP/bin:$PATH"

fail=0
check() {
    local desc="$1" haystack="$2" needle="$3" want="$4"
    if grep -q -- "$needle" "$haystack"; then found=yes; else found=no; fi
    if [[ "$found" == "$want" ]]; then
        echo "  ok: $desc"
    else
        echo "  FAIL: $desc (expected present=$want, got present=$found)"
        fail=1
    fi
}

echo "case 1: no dist-upgrade argument — lane 1 path must be untouched"
export STUB_LOG="$TMP/log1"
: > "$STUB_LOG"
bash "$TARGET" 10.0.0.1 secret > "$TMP/out1" 2>&1
check "no dist-upgrade issued"      "$STUB_LOG" "dist-upgrade"    no
check "no reboot issued"            "$STUB_LOG" "systemctl reboot" no
check "no package set recorded"     "$STUB_LOG" "dpkg-query"      no
check "storage still configured"    "$STUB_LOG" "pvesm set local" yes

echo "case 2: dist-upgrade requested"
export STUB_LOG="$TMP/log2"
: > "$STUB_LOG"
bash "$TARGET" 10.0.0.1 secret 1 "$TMP/packages.txt" > "$TMP/out2" 2>&1
check "dist-upgrade issued"         "$STUB_LOG" "dist-upgrade"    yes
check "reboot issued"               "$STUB_LOG" "systemctl reboot" yes
check "package set recorded"        "$STUB_LOG" "dpkg-query"      yes
check "storage still configured"    "$STUB_LOG" "pvesm set local" yes

# The reboot must be issued after the upgrade, or the node records a package
# set it never booted.
upgrade_line=$(grep -n "dist-upgrade" "$STUB_LOG" | head -1 | cut -d: -f1)
reboot_line=$(grep -n "systemctl reboot" "$STUB_LOG" | head -1 | cut -d: -f1)
if [[ "$reboot_line" -gt "$upgrade_line" ]]; then
    echo "  ok: reboot ordered after dist-upgrade"
else
    echo "  FAIL: reboot ordered before dist-upgrade"
    fail=1
fi

if [[ -s "$TMP/packages.txt" ]]; then
    echo "  ok: package file non-empty"
else
    echo "  FAIL: package file empty or missing"
    fail=1
fi

if [[ "$fail" -eq 0 ]]; then
    echo "PASS"
else
    echo "FAILED"
    exit 1
fi
