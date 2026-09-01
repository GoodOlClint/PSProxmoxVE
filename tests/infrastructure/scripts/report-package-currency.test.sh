#!/usr/bin/env bash
# Self-check for report-package-currency.sh.
#
# Uses a REAL temp git repo with a REAL bare remote, so fetch / hash-object /
# mktree / commit-tree / push all run for real and the data-branch behaviour is
# actually exercised. Only `gh` is stubbed — it is the one thing that would
# reach the network.
#
# Run: bash tests/infrastructure/scripts/report-package-currency.test.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$SCRIPT_DIR/report-package-currency.sh"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
mkdir -p "$TMP/bin"

cat > "$TMP/bin/gh" <<'STUB'
#!/usr/bin/env bash
echo "gh $*" >> "$STUB_LOG"
if [[ "$1" == "issue" && "$2" == "list" ]]; then
    # GH_LIST_FAIL=1 simulates an API/auth/rate-limit failure, which must not be
    # read as "no issue exists" — that turns the upsert into an append.
    [[ "${GH_LIST_FAIL:-0}" == "1" ]] && exit 1
    [[ -n "${EXISTING_ISSUE:-}" ]] && echo "${EXISTING_ISSUE}"
fi
exit 0
STUB
chmod +x "$TMP/bin/gh"
export PATH="$TMP/bin:$PATH"
export BASELINE_BRANCH="ci/package-baseline"

# A session exports these for its own bot identity; the script must override
# them so CI-authored data commits are github-actions[bot]. Set them here so
# the test proves the override rather than inheriting a clean environment.
export GIT_AUTHOR_NAME="someone-else"
export GIT_AUTHOR_EMAIL="someone-else@example.com"
export GIT_COMMITTER_NAME="someone-else"
export GIT_COMMITTER_EMAIL="someone-else@example.com"

fail=0
pass() { echo "  ok: $1"; }
fatal() { echo "  FAIL: $1"; fail=1; }
check() {
    local desc="$1" needle="$2" want="$3" found=no
    cat "$STUB_LOG" "$OUT" 2>/dev/null | grep -q -- "$needle" && found=yes
    [[ "$found" == "$want" ]] && echo "  ok: $desc" \
        || { echo "  FAIL: $desc (expected present=$want, got present=$found)"; fail=1; }
}

SET_A=$'libc6:amd64\t2:2.36-9+deb12u13+b1\npve-manager\t9.2.1~rc1\nproxmox-kernel-6.14\t6.14.11-1\n# running-kernel\t6.14.11-1-pve\n'
SET_B=$'libc6:amd64\t2:2.36-9+deb12u14+b1\npve-manager\t9.2.2~rc1\nproxmox-kernel-6.14\t6.14.11-2\n# running-kernel\t6.14.11-2-pve\n'

# Fresh repo + bare remote per case, so cases cannot leak state into each other.
newrepo() {
    local n="$1"
    REMOTE="$TMP/remote$n.git"; REPO="$TMP/repo$n"
    git init --quiet --bare "$REMOTE"
    git init --quiet "$REPO"
    git -C "$REPO" remote add origin "$REMOTE"
    echo seed > "$REPO/README"
    git -C "$REPO" add README
    git -C "$REPO" -c user.name=t -c user.email=t@e commit --quiet -m seed
    git -C "$REPO" push --quiet origin HEAD:refs/heads/main
    PKGS="$REPO/packages"; mkdir -p "$PKGS"
}
mkpkgs() { printf '%s' "$2" > "$PKGS/$1-packages.txt"; }
# Compare via files: $(...) strips trailing newlines, which would make an
# exact-content assertion fail against a set that legitimately ends in one.
baseline_matches() {
    git -C "$REMOTE" show "refs/heads/${BASELINE_BRANCH}:pve-package-baseline.txt" > "$TMP/got.txt" 2>/dev/null || return 1
    printf '%s' "$1" > "$TMP/want.txt"
    diff -q "$TMP/got.txt" "$TMP/want.txt" >/dev/null 2>&1
}
run_target() { ( cd "$REPO" && bash "$TARGET" packages "$1" ); }

echo "case 1: no baseline yet — seed the branch, no issue"
newrepo 1; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log1"; : > "$STUB_LOG"; export OUT="$TMP/out1"
run_target success > "$OUT" 2>&1
check "no issue for a seed" "gh issue create" no
grep -q "Baseline seeded" "$OUT" && pass "report says it seeded" || fatal "seed not described"
baseline_matches "$SET_A" && pass "baseline seeded on the data branch" || fatal "baseline not on the data branch"
author="$(git -C "$REMOTE" log -1 --format='%an <%ae>' "refs/heads/${BASELINE_BRANCH}")"
[[ "$author" == "github-actions[bot] <41898282+github-actions[bot]@users.noreply.github.com>" ]] \
    && pass "commit authored by github-actions[bot]" || fatal "wrong author: $author"
files="$(git -C "$REMOTE" ls-tree --name-only "refs/heads/${BASELINE_BRANCH}")"
[[ "$files" == "pve-package-baseline.txt" ]] && pass "data branch holds only the baseline" || fatal "unexpected tree: $files"
# The plumbing approach exists so the checkout is never touched: no local
# branch, no branch switch, no dirty tree. A regression to checkout/commit
# would still push correctly and pass every assertion above.
git -C "$REPO" rev-parse --verify --quiet "refs/heads/${BASELINE_BRANCH}" >/dev/null \
    && fatal "a local ${BASELINE_BRANCH} branch was created" \
    || pass "no local data branch created"
[[ "$(git -C "$REPO" rev-parse --abbrev-ref HEAD)" != "$BASELINE_BRANCH" ]] \
    && pass "checkout stayed on its original branch" || fatal "checkout was switched"
# -uno: the downloaded packages/ dir is legitimately untracked here and in CI.
# What must not happen is a change to TRACKED files — a stray commit, a
# checkout switch, or an index left half-staged.
[[ -z "$(git -C "$REPO" status --porcelain -uno)" ]] \
    && pass "no tracked files touched" || fatal "tracked files were modified"

echo "case 2: baseline matches — must stay silent and not commit"
newrepo 2; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log2"; : > "$STUB_LOG"; export OUT="$TMP/out2"
run_target success > /dev/null 2>&1              # seed
before="$(git -C "$REMOTE" rev-parse "refs/heads/${BASELINE_BRANCH}")"
: > "$STUB_LOG"
run_target success > "$OUT" 2>&1                 # second run, same packages
check "no issue created"  "gh issue create" no
after="$(git -C "$REMOTE" rev-parse "refs/heads/${BASELINE_BRANCH}")"
[[ "$before" == "$after" ]] && pass "no new commit when unchanged" || fatal "committed despite no drift"
grep -q "Nothing to report" "$OUT" && pass "says nothing to report" || fatal "did not report quiet exit"

echo "case 3: drift — issue created and the branch advances with history"
newrepo 3; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log3"; : > "$STUB_LOG"; export OUT="$TMP/out3"
run_target success > /dev/null 2>&1              # seed
mkpkgs 9a "$SET_B"; mkpkgs 9b "$SET_B"
: > "$STUB_LOG"
run_target success > "$OUT" 2>&1
check "issue created"      "gh issue create" yes
check "issue labelled"     "gh issue create.*--label pve-currency" yes
baseline_matches "$SET_B" && pass "baseline updated to the new set" || fatal "baseline not updated"
count="$(git -C "$REMOTE" rev-list --count "refs/heads/${BASELINE_BRANCH}")"
[[ "$count" -eq 2 ]] && pass "history kept (2 commits)" || fatal "expected 2 commits, got $count"
grep -q "9.2.2~rc1" "$OUT" && pass "diff names the new version" || fatal "diff missing the new version"
grep -q '^-pve-manager' "$OUT" && grep -q '^+pve-manager' "$OUT" \
    && pass "diff has a direction (old removed, new added)" || fatal "diff direction missing"
grep -qE '^\-pve-manager\s+9\.2\.1~rc1' "$OUT" \
    && pass "old version on the minus side" || fatal "diff direction is reversed"

echo "case 4: drift with an issue already open — edit, never create a second"
newrepo 4; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log4"; : > "$STUB_LOG"; export OUT="$TMP/out4"
run_target success > /dev/null 2>&1
mkpkgs 9a "$SET_B"; mkpkgs 9b "$SET_B"
: > "$STUB_LOG"
EXISTING_ISSUE=42 run_target success > "$OUT" 2>&1
check "issue edited"    "gh issue edit 42" yes
check "no second issue" "gh issue create"  no

echo "case 5: nodes disagree though the baseline matches — report, no commit"
newrepo 5; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log5"; : > "$STUB_LOG"; export OUT="$TMP/out5"
run_target success > /dev/null 2>&1
mkpkgs 9b "$SET_B"
before="$(git -C "$REMOTE" rev-parse "refs/heads/${BASELINE_BRANCH}")"
: > "$STUB_LOG"
run_target success > "$OUT" 2>&1
check "issue raised for the mismatch" "gh issue create" yes
after="$(git -C "$REMOTE" rev-parse "refs/heads/${BASELINE_BRANCH}")"
[[ "$before" == "$after" ]] && pass "no commit when only the nodes disagree" || fatal "committed on a mismatch-only run"
grep -q "Nodes disagree" "$OUT" && pass "report names the mismatch" || fatal "mismatch heading missing"
grep -q "9.2.2~rc1" "$OUT" && pass "mismatch report includes the diff body" || fatal "mismatch diff body missing"

echo "case 6: very large drift — must still report (SIGPIPE regression)"
newrepo 6
python3 -c "
import io
old=[]; new=[]
for i in range(3000):
    old.append('pkg-%04d\t1.0.%d' % (i, i))
    new.append('pkg-%04d\t2.0.%d' % (i, i))
io.open('$PKGS/9a-packages.txt','w').write('\n'.join(old)+'\n')
io.open('$PKGS/9b-packages.txt','w').write('\n'.join(old)+'\n')
"
export STUB_LOG="$TMP/log6"; : > "$STUB_LOG"; export OUT="$TMP/out6"
run_target success > /dev/null 2>&1              # seed with the old set
python3 -c "
import io
new=['pkg-%04d\t2.0.%d' % (i, i) for i in range(3000)]
io.open('$PKGS/9a-packages.txt','w').write('\n'.join(new)+'\n')
io.open('$PKGS/9b-packages.txt','w').write('\n'.join(new)+'\n')
"
: > "$STUB_LOG"
if run_target success > "$OUT" 2>&1; then pass "large drift did not abort"; else fatal "large drift aborted — SIGPIPE regression"; fi
check "large drift still raises the issue" "gh issue create" yes

echo "case 7: suite outcomes are distinguished"
newrepo 7; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log7"; : > "$STUB_LOG"; export OUT="$TMP/out7"
run_target success > /dev/null 2>&1
mkpkgs 9a "$SET_B"; mkpkgs 9b "$SET_B"
run_target failure > "$OUT" 2>&1
grep -q "report-only" "$OUT" && pass "failure explains the green check" || fatal "failure not explained"
newrepo 8; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export OUT="$TMP/out8"
run_target success > /dev/null 2>&1
mkpkgs 9a "$SET_B"; mkpkgs 9b "$SET_B"
run_target not-run > "$OUT" 2>&1
grep -q "did not run" "$OUT" && pass "not-run is distinct from failure" || fatal "not-run not distinguished"
newrepo 15; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export OUT="$TMP/out15"
run_target success > /dev/null 2>&1
mkpkgs 9a "$SET_B"; mkpkgs 9b "$SET_B"
run_target inconclusive > "$OUT" 2>&1
grep -q "did not produce a valid result" "$OUT" \
    && pass "inconclusive is distinct from failure" || fatal "inconclusive not distinguished"
grep -q '\*\*inconclusive\*\*' "$OUT" \
    && pass "summary line reports the real outcome" || fatal "summary line does not name the outcome"

echo "case 8: only one node reported — must be fatal"
newrepo 9; mkpkgs 9a "$SET_A"
export STUB_LOG="$TMP/log9"; : > "$STUB_LOG"; export OUT="$TMP/out9"
if run_target success > "$OUT" 2>&1; then fatal "exited 0 with one node — mismatch check silently skipped"; else pass "single node fails the run"; fi

echo "case 9: no package files at all — must be fatal"
newrepo 10
export STUB_LOG="$TMP/log10"; : > "$STUB_LOG"; export OUT="$TMP/out10"
if run_target success > "$OUT" 2>&1; then fatal "exited 0 with no package files"; else pass "missing package files fail the run"; fi

echo "case 10: non-dpkg content is rejected"
newrepo 11; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
printf '%s' $'pve-manager\t9.2.1\n::error::injected\n' > "$PKGS/9a-packages.txt"
export STUB_LOG="$TMP/log11"; : > "$STUB_LOG"; export OUT="$TMP/out11"
if run_target success > "$OUT" 2>&1; then fatal "accepted a package file with injected content"; else pass "non-dpkg rows fail the run"; fi

echo "case 12: unreadable input — diff rc=2 must be fatal, not 'differs'"
newrepo 13; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log13"; : > "$STUB_LOG"; export OUT="$TMP/out13"
run_target success > /dev/null 2>&1
mkpkgs 9a "$SET_B"; mkpkgs 9b "$SET_B"
# Break the NON-reference node. Breaking the reference instead breaks every
# comparison at once and the run dies later at `hash-object`, satisfying
# "exit non-zero" without any guard being involved.
#
# What actually catches this now is the -r/-s readability check, which runs
# before any diff. That makes the four `diff` rc>1 guards unreachable from a
# black-box test — they are defence in depth against a file that becomes
# unreadable mid-run, not tested behaviour, and this case does not claim to
# cover them.
chmod 000 "$PKGS/9b-packages.txt"
: > "$STUB_LOG"
if run_target success > "$OUT" 2>&1; then
    fatal "exited 0 with an unreadable package set"
else
    pass "unreadable input fails the run"
fi
grep -q "9b-packages.txt is empty or unreadable" "$OUT" \
    && pass "failure names the unreadable file" || fatal "did not name the read failure"
check "no issue on a failed compare" "gh issue create" no
chmod 644 "$PKGS/9b-packages.txt"

echo "case 13: fetch fails for a real reason — fatal, not 'first run'"
newrepo 14; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log14"; : > "$STUB_LOG"; export OUT="$TMP/out14"
run_target success > /dev/null 2>&1              # seed, so a baseline exists
git -C "$REPO" remote set-url origin "$TMP/definitely-not-a-repo.git"
: > "$STUB_LOG"
if run_target success > "$OUT" 2>&1; then
    fatal "exited 0 on a broken remote — would report 'seeded' and lose the cause"
else
    pass "unreachable remote fails the run"
fi
grep -q "could not reach origin" "$OUT" \
    && pass "failure names the unreachable remote" || fatal "remote failure not named"

echo "case 14: a concurrent writer advanced the branch — must not clobber it"
newrepo 16; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log16"; : > "$STUB_LOG"; export OUT="$TMP/out16"
run_target success > /dev/null 2>&1              # seed
# Simulate another writer landing a commit after our fetch would have run.
other="$TMP/other"; git clone -q --branch "$BASELINE_BRANCH" "$REMOTE" "$other"
echo "someone else" > "$other/pve-package-baseline.txt"
git -C "$other" add pve-package-baseline.txt
git -C "$other" -c user.name=o -c user.email=o@e commit -qm "concurrent write"
git -C "$other" push -q origin "$BASELINE_BRANCH"
theirs="$(git -C "$REMOTE" rev-parse "refs/heads/${BASELINE_BRANCH}")"
# Our run fetches the new tip, so it parents correctly and succeeds — the point
# is that their commit stays in history and is never discarded.
mkpkgs 9a "$SET_B"; mkpkgs 9b "$SET_B"
: > "$STUB_LOG"
run_target success > "$OUT" 2>&1 || true
if git -C "$REMOTE" merge-base --is-ancestor "$theirs" "refs/heads/${BASELINE_BRANCH}"; then
    pass "the concurrent commit is still in history"
else
    fatal "the concurrent commit was discarded — force-push or wrong parent"
fi

echo "case 15: branch exists without the baseline file — must be fatal"
newrepo 17; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log17"; : > "$STUB_LOG"; export OUT="$TMP/out17"
run_target success > /dev/null 2>&1              # seed
# Replace the branch with a commit holding a DIFFERENT filename. Treating this
# as "first run" would skip the drift diff and then build a parentless commit
# whose push is rejected — wedging the lane on every subsequent run.
blob="$(git -C "$REPO" hash-object -w "$PKGS/9a-packages.txt")"
tree="$(printf '100644 blob %s\tsomething-else.txt\n' "$blob" | git -C "$REPO" mktree)"
c="$(git -C "$REPO" -c user.name=o -c user.email=o@e commit-tree "$tree" -m "wrong file")"
git -C "$REPO" push -q -f origin "$c:refs/heads/${BASELINE_BRANCH}"
: > "$STUB_LOG"
if run_target success > "$OUT" 2>&1; then
    fatal "exited 0 with a branch that has no baseline file"
else
    pass "branch without the baseline file fails the run"
fi
grep -q "has no pve-package-baseline.txt" "$OUT" \
    && pass "failure names the inconsistent branch" || fatal "inconsistent branch not named"

echo "case 16: enormous node divergence — body must be capped, not 422"
newrepo 18
python3 -c "
import io
a=['pkg-%04d\t1.0.%d' % (i,i) for i in range(3000)]
b=['pkg-%04d\t9.9.%d' % (i,i) for i in range(3000)]
io.open('$PKGS/9a-packages.txt','w').write('\n'.join(a)+'\n')
io.open('$PKGS/9b-packages.txt','w').write('\n'.join(b)+'\n')
"
export STUB_LOG="$TMP/log18"; : > "$STUB_LOG"; export OUT="$TMP/out18"
if run_target success > "$OUT" 2>&1; then pass "enormous divergence did not abort"; else fatal "enormous divergence aborted"; fi
check "issue still raised" "gh issue create" yes
# The body handed to gh must stay under GitHub's 65536-char limit.
body_line=$(grep -n -- "--- report body ---" "$OUT" | head -1 | cut -d: -f1)
end_line=$(grep -n -- "-------------------" "$OUT" | head -1 | cut -d: -f1)
body_bytes=$(sed -n "$((body_line+1)),$((end_line-1))p" "$OUT" | wc -c)
if [[ "$body_bytes" -lt 65536 ]]; then
    pass "report body stayed under the GitHub limit (${body_bytes} bytes)"
else
    fatal "report body is ${body_bytes} bytes — gh would 422 and the run would report nothing"
fi
grep -q "truncated" "$OUT" && pass "truncation is marked" || fatal "truncation not marked"

echo "case 11: issue lookup fails — fatal, not a second issue"
newrepo 12; mkpkgs 9a "$SET_A"; mkpkgs 9b "$SET_A"
export STUB_LOG="$TMP/log12"; : > "$STUB_LOG"; export OUT="$TMP/out12"
run_target success > /dev/null 2>&1
mkpkgs 9a "$SET_B"; mkpkgs 9b "$SET_B"
: > "$STUB_LOG"
if GH_LIST_FAIL=1 run_target success > "$OUT" 2>&1; then fatal "exited 0 despite a failed issue lookup"; else pass "failed issue lookup fails the run"; fi
check "no issue created on lookup failure" "gh issue create" no

if [[ "$fail" -eq 0 ]]; then echo "PASS"; else echo "FAILED"; exit 1; fi
