#!/usr/bin/env bash
# Self-check for ensure-base-iso.sh's checksum verification.
#
# Stubs curl on PATH so every path runs offline in ~0s; sha256sum is the real
# binary, so the checksum comparisons are genuine. The fake upstream is a
# small file whose real sha256 is embedded in a fake SHA256SUMS.
#
# Run: bash tests/infrastructure/scripts/ensure-base-iso.test.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$SCRIPT_DIR/ensure-base-iso.sh"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

mkdir -p "$TMP/bin" "$TMP/cache" "$TMP/upstream"

ISO_NAME="proxmox-ve_9.1-1.iso"
GOOD_CONTENT="$TMP/upstream/good.iso"
printf 'good iso bytes' > "$GOOD_CONTENT"
GOOD_SHA="$(sha256sum "$GOOD_CONTENT" | cut -d' ' -f1)"

BAD_CONTENT="$TMP/upstream/bad.iso"
printf 'corrupted bytes' > "$BAD_CONTENT"

SUMS_FILE="$TMP/upstream/SHA256SUMS"
printf '%s  %s\n' "$GOOD_SHA" "$ISO_NAME" > "$SUMS_FILE"

SUMS_FILE_MISSING="$TMP/upstream/SHA256SUMS_MISSING"
printf '%s  %s\n' "$GOOD_SHA" "some-other.iso" > "$SUMS_FILE_MISSING"

# Fake curl. Serves the SHA256SUMS fixture pointed to by $SUMS_SOURCE, and the
# ISO bytes pointed to by $ISO_SOURCE, to whatever -o path was requested.
# CURL_FAIL_SUMS / CURL_FAIL_ISO make the corresponding fetch fail.
cat > "$TMP/bin/curl" <<'STUB'
#!/usr/bin/env bash
echo "curl $*" >> "$STUB_LOG"
url="${!#}"
out=""
prev=""
for a in "$@"; do
    if [[ "$prev" == "-o" ]]; then
        out="$a"
    fi
    prev="$a"
done

case "$url" in
    *SHA256SUMS*)
        [[ "${CURL_FAIL_SUMS:-0}" == "1" ]] && exit 22
        cp "$SUMS_SOURCE" "$out"
        ;;
    *)
        [[ "${CURL_FAIL_ISO:-0}" == "1" ]] && exit 22
        cp "$ISO_SOURCE" "$out"
        ;;
esac
exit 0
STUB
chmod +x "$TMP/bin/"*
export PATH="$TMP/bin:$PATH"

fail=0
pass() { echo "  ok: $1"; }
fatal() { echo "  FAIL: $1"; fail=1; }

run() {
    export STUB_LOG="$TMP/log"
    : > "$STUB_LOG"
    rm -rf "$TMP/cache"
    mkdir -p "$TMP/cache"
    "$@"
}

echo "case 1: nothing cached, good download — must verify and succeed"
export SUMS_SOURCE="$SUMS_FILE" ISO_SOURCE="$GOOD_CONTENT" CURL_FAIL_SUMS=0 CURL_FAIL_ISO=0
if run bash "$TARGET" "$ISO_NAME" "$TMP/cache" > "$TMP/out1" 2>&1; then
    pass "exits 0 on a verified download"
    [[ -f "$TMP/cache/$ISO_NAME" ]] && pass "ISO left in cache" || fatal "ISO missing from cache"
    grep -q "Checksum verified" "$TMP/out1" && pass "reports verification" || fatal "silent on verification"
else
    fatal "exited non-zero on a good download: $(cat "$TMP/out1")"
fi

echo "case 2: already cached with a matching checksum — must not re-download"
export SUMS_SOURCE="$SUMS_FILE" ISO_SOURCE="$GOOD_CONTENT" CURL_FAIL_SUMS=0 CURL_FAIL_ISO=0
export STUB_LOG="$TMP/log2"
: > "$STUB_LOG"
if bash "$TARGET" "$ISO_NAME" "$TMP/cache" > "$TMP/out2" 2>&1; then
    pass "exits 0 on a fresh, matching cache hit"
    grep -q "SHA256SUMS" "$STUB_LOG" && pass "still re-verifies the cache hit" || fatal "skipped re-verification"
    grep -q "$ISO_NAME\$" "$STUB_LOG" && fatal "re-downloaded the ISO on a cache hit" || pass "did not re-download the ISO"
else
    fatal "exited non-zero on a valid cache hit: $(cat "$TMP/out2")"
fi

echo "case 3: cached copy is corrupted — must delete, re-download, and verify"
export SUMS_SOURCE="$SUMS_FILE" ISO_SOURCE="$GOOD_CONTENT" CURL_FAIL_SUMS=0 CURL_FAIL_ISO=0
run cp "$BAD_CONTENT" "$TMP/cache/$ISO_NAME"
if bash "$TARGET" "$ISO_NAME" "$TMP/cache" > "$TMP/out3" 2>&1; then
    pass "exits 0 after replacing a corrupted cached copy"
    actual_sha="$(sha256sum "$TMP/cache/$ISO_NAME" | cut -d' ' -f1)"
    [[ "$actual_sha" == "$GOOD_SHA" ]] && pass "cache now holds the good bytes" || fatal "cache still holds bad bytes"
    grep -q "failed checksum verification" "$TMP/out3" && pass "names the corrupted-cache path" || fatal "silent about the corrupted cache"
else
    fatal "did not recover from a corrupted cache: $(cat "$TMP/out3")"
fi

echo "case 4: downloaded bytes do not match the checksum — must fail and not leave a bad file behind"
export SUMS_SOURCE="$SUMS_FILE" ISO_SOURCE="$BAD_CONTENT" CURL_FAIL_SUMS=0 CURL_FAIL_ISO=0
if run bash "$TARGET" "$ISO_NAME" "$TMP/cache" > "$TMP/out4" 2>&1; then
    fatal "exited 0 despite a checksum mismatch"
else
    pass "checksum mismatch fails the run"
    [[ -f "$TMP/cache/$ISO_NAME" ]] && fatal "bad ISO left in cache" || pass "bad ISO removed from cache"
fi

echo "case 5: upstream SHA256SUMS does not list this ISO — must fail"
export SUMS_SOURCE="$SUMS_FILE_MISSING" ISO_SOURCE="$GOOD_CONTENT" CURL_FAIL_SUMS=0 CURL_FAIL_ISO=0
if run bash "$TARGET" "$ISO_NAME" "$TMP/cache" > "$TMP/out5" 2>&1; then
    fatal "exited 0 despite the ISO being absent from SHA256SUMS"
else
    pass "missing SHA256SUMS entry fails the run"
    grep -q "not listed" "$TMP/out5" && pass "names the cause" || fatal "failure message does not mention the missing entry"
fi

echo "case 6: SHA256SUMS fetch itself fails — must fail rather than skip verification"
export SUMS_SOURCE="$SUMS_FILE" ISO_SOURCE="$GOOD_CONTENT" CURL_FAIL_SUMS=1 CURL_FAIL_ISO=0
if run bash "$TARGET" "$ISO_NAME" "$TMP/cache" > "$TMP/out6" 2>&1; then
    fatal "exited 0 despite being unable to fetch SHA256SUMS"
else
    pass "a failed SHA256SUMS fetch fails the run (verification cannot be silently skipped)"
fi

if [[ "$fail" -eq 0 ]]; then
    echo "PASS"
else
    echo "FAILED"
    exit 1
fi
