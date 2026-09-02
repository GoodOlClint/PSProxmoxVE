#!/usr/bin/env bash
# Self-check for ensure-cloud-images.sh's checksum verification.
#
# Stubs curl, date and stat on PATH so every path runs offline in ~0s;
# sha256sum is the real binary, so the comparisons are genuine. The fake
# cloud image is served under its Ubuntu upstream name (.img) but cached
# under a different local name (.qcow2) — the checksum must still match,
# because ensure-cloud-images.sh renames the SHA256SUMS entry, not the
# bytes.
#
# Run: bash tests/infrastructure/scripts/ensure-cloud-images.test.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$SCRIPT_DIR/ensure-cloud-images.sh"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

mkdir -p "$TMP/bin" "$TMP/cache" "$TMP/upstream"

UPSTREAM_IMG_NAME="noble-server-cloudimg-amd64.img"
LOCAL_IMG_NAME="noble-server-cloudimg-amd64.qcow2"
OVA_NAME="ubuntu-24.04-server-cloudimg-amd64.ova"

GOOD_IMG="$TMP/upstream/good.img"
printf 'good cloud image bytes' > "$GOOD_IMG"
GOOD_IMG_SHA="$(sha256sum "$GOOD_IMG" | cut -d' ' -f1)"

GOOD_OVA="$TMP/upstream/good.ova"
printf 'good ova bytes' > "$GOOD_OVA"
GOOD_OVA_SHA="$(sha256sum "$GOOD_OVA" | cut -d' ' -f1)"

BAD_CONTENT="$TMP/upstream/bad"
printf 'corrupted bytes' > "$BAD_CONTENT"

# Ubuntu publishes the binary-mode "*filename" marker.
IMG_SUMS="$TMP/upstream/img-sums"
printf '%s *%s\n' "$GOOD_IMG_SHA" "$UPSTREAM_IMG_NAME" > "$IMG_SUMS"
OVA_SUMS="$TMP/upstream/ova-sums"
printf '%s *%s\n' "$GOOD_OVA_SHA" "$OVA_NAME" > "$OVA_SUMS"

# Fake curl: serves $IMG_SUMS/$OVA_SUMS for their SHA256SUMS URLs, and the
# fixture pointed to by IMG_SOURCE/OVA_SOURCE for the image/OVA URLs.
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
    */noble/current/SHA256SUMS) cp "$IMG_SUMS" "$out" ;;
    */releases/24.04/release/SHA256SUMS) cp "$OVA_SUMS" "$out" ;;
    *.img)
        [[ "${CURL_FAIL_IMG:-0}" == "1" ]] && exit 22
        cp "$IMG_SOURCE" "$out"
        ;;
    *.ova)
        [[ "${CURL_FAIL_OVA:-0}" == "1" ]] && exit 22
        cp "$OVA_SOURCE" "$out"
        ;;
esac
exit 0
STUB

# Fixed "now" and an mtime helper so age math is deterministic without
# touching the real clock: files pre-dated via $TMP/bin/touch-old.
cat > "$TMP/bin/date" <<'STUB'
#!/usr/bin/env bash
if [[ "$1" == "+%s" ]]; then
    echo 2000000000
else
    exec /usr/bin/date "$@"
fi
STUB
cat > "$TMP/bin/stat" <<'STUB'
#!/usr/bin/env bash
# Only the two forms ensure-cloud-images.sh calls are stubbed.
for a in "$@"; do
    :
done
target="${!#}"
if [[ -f "${target}.age_days" ]]; then
    age="$(cat "${target}.age_days")"
    echo $(( 2000000000 - age * 86400 ))
else
    echo 2000000000
fi
STUB

chmod +x "$TMP/bin/"*
export PATH="$TMP/bin:$PATH"
export IMG_SUMS OVA_SUMS

fail=0
pass() { echo "  ok: $1"; }
fatal() { echo "  FAIL: $1"; fail=1; }

reset_cache() {
    rm -rf "$TMP/cache"
    mkdir -p "$TMP/cache"
}

echo "case 1: nothing cached — both files download and verify"
reset_cache
export IMG_SOURCE="$GOOD_IMG" OVA_SOURCE="$GOOD_OVA" CURL_FAIL_IMG=0 CURL_FAIL_OVA=0
export STUB_LOG="$TMP/log1"; : > "$STUB_LOG"
if bash "$TARGET" "$TMP/cache" > "$TMP/out1" 2>&1; then
    pass "exits 0"
    [[ -f "$TMP/cache/$LOCAL_IMG_NAME" ]] && pass "cloud image cached" || fatal "cloud image missing"
    [[ -f "$TMP/cache/$OVA_NAME" ]] && pass "OVA cached" || fatal "OVA missing"
    grep -q "CLOUD_IMAGE_PATH=" "$TMP/out1" && pass "emits CLOUD_IMAGE_PATH" || fatal "missing CLOUD_IMAGE_PATH output"
else
    fatal "exited non-zero on a clean run: $(cat "$TMP/out1")"
fi

echo "case 2: fresh cache with matching checksums — must re-verify, not re-download"
reset_cache
cp "$GOOD_IMG" "$TMP/cache/$LOCAL_IMG_NAME"
cp "$GOOD_OVA" "$TMP/cache/$OVA_NAME"
export IMG_SOURCE="$GOOD_IMG" OVA_SOURCE="$GOOD_OVA" CURL_FAIL_IMG=0 CURL_FAIL_OVA=0
export STUB_LOG="$TMP/log2"; : > "$STUB_LOG"
if bash "$TARGET" "$TMP/cache" > "$TMP/out2" 2>&1; then
    pass "exits 0 on a fresh, matching cache"
    grep -q "SHA256SUMS" "$STUB_LOG" && pass "re-verifies the cache hit" || fatal "skipped re-verification"
    grep -q '\.img$' "$STUB_LOG" && fatal "re-downloaded the cloud image on a cache hit" || pass "did not re-download the cloud image"
    grep -q '\.ova$' "$STUB_LOG" && fatal "re-downloaded the OVA on a cache hit" || pass "did not re-download the OVA"
else
    fatal "exited non-zero on a valid fresh cache: $(cat "$TMP/out2")"
fi

echo "case 3: fresh cache but corrupted bytes — must re-download and fix it"
reset_cache
cp "$BAD_CONTENT" "$TMP/cache/$LOCAL_IMG_NAME"
cp "$GOOD_OVA" "$TMP/cache/$OVA_NAME"
export IMG_SOURCE="$GOOD_IMG" OVA_SOURCE="$GOOD_OVA" CURL_FAIL_IMG=0 CURL_FAIL_OVA=0
export STUB_LOG="$TMP/log3"; : > "$STUB_LOG"
if bash "$TARGET" "$TMP/cache" > "$TMP/out3" 2>&1; then
    actual="$(sha256sum "$TMP/cache/$LOCAL_IMG_NAME" | cut -d' ' -f1)"
    [[ "$actual" == "$GOOD_IMG_SHA" ]] && pass "corrupted cloud image replaced with good bytes" || fatal "corrupted cloud image not fixed"
else
    fatal "did not recover from a corrupted fresh cache: $(cat "$TMP/out3")"
fi

echo "case 4: downloaded bytes do not match upstream checksum — must fail"
reset_cache
export IMG_SOURCE="$BAD_CONTENT" OVA_SOURCE="$GOOD_OVA" CURL_FAIL_IMG=0 CURL_FAIL_OVA=0
export STUB_LOG="$TMP/log4"; : > "$STUB_LOG"
if bash "$TARGET" "$TMP/cache" > "$TMP/out4" 2>&1; then
    fatal "exited 0 despite a checksum mismatch on the cloud image"
else
    pass "checksum mismatch fails the run"
    [[ -f "$TMP/cache/$LOCAL_IMG_NAME" ]] && fatal "bad cloud image left in cache" || pass "bad cloud image not left in cache"
fi

echo "case 5: corrupted fresh cache AND the redownload fails — must not hand back the corrupt file"
reset_cache
cp "$BAD_CONTENT" "$TMP/cache/$LOCAL_IMG_NAME"
cp "$GOOD_OVA" "$TMP/cache/$OVA_NAME"
export IMG_SOURCE="$GOOD_IMG" OVA_SOURCE="$GOOD_OVA" CURL_FAIL_IMG=1 CURL_FAIL_OVA=0
export STUB_LOG="$TMP/log5"; : > "$STUB_LOG"
if bash "$TARGET" "$TMP/cache" > "$TMP/out5" 2>&1; then
    fatal "exited 0 despite a corrupted cache whose redownload failed: $(cat "$TMP/out5")"
else
    pass "fails the run rather than falling back to the corrupt file"
    [[ -f "$TMP/cache/$LOCAL_IMG_NAME" ]] && fatal "corrupt cloud image left in cache" || pass "corrupt cloud image removed, not handed back"
fi

echo "case 6: stale-by-age cache still verifies AND the redownload fails — must fall back to it"
reset_cache
cp "$GOOD_IMG" "$TMP/cache/$LOCAL_IMG_NAME"
echo 10 > "$TMP/cache/$LOCAL_IMG_NAME.age_days"
cp "$GOOD_OVA" "$TMP/cache/$OVA_NAME"
export IMG_SOURCE="$GOOD_IMG" OVA_SOURCE="$GOOD_OVA" CURL_FAIL_IMG=1 CURL_FAIL_OVA=0
export STUB_LOG="$TMP/log6"; : > "$STUB_LOG"
if bash "$TARGET" "$TMP/cache" > "$TMP/out6" 2>&1; then
    pass "falls back to the still-good stale-by-age copy"
    grep -q "using stale cached copy" "$TMP/out6" && pass "reports the fallback" || fatal "silent about the fallback"
    [[ -f "$TMP/cache/$LOCAL_IMG_NAME" ]] && pass "verified stale copy kept" || fatal "verified stale copy removed"
else
    fatal "exited non-zero despite a stale-by-age copy that still verifies: $(cat "$TMP/out6")"
fi

echo "case 7: stale-by-age cache no longer verifies AND the redownload fails — must not hand it back"
reset_cache
cp "$BAD_CONTENT" "$TMP/cache/$LOCAL_IMG_NAME"
echo 10 > "$TMP/cache/$LOCAL_IMG_NAME.age_days"
cp "$GOOD_OVA" "$TMP/cache/$OVA_NAME"
export IMG_SOURCE="$GOOD_IMG" OVA_SOURCE="$GOOD_OVA" CURL_FAIL_IMG=1 CURL_FAIL_OVA=0
export STUB_LOG="$TMP/log7"; : > "$STUB_LOG"
if bash "$TARGET" "$TMP/cache" > "$TMP/out7" 2>&1; then
    fatal "exited 0 despite a stale-by-age copy that no longer verifies: $(cat "$TMP/out7")"
else
    pass "fails the run rather than falling back to an unverifiable stale-by-age copy"
    [[ -f "$TMP/cache/$LOCAL_IMG_NAME" ]] && fatal "unverifiable stale-by-age copy left in cache" || pass "unverifiable stale-by-age copy removed"
fi

if [[ "$fail" -eq 0 ]]; then
    echo "PASS"
else
    echo "FAILED"
    exit 1
fi
