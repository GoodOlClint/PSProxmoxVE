#!/usr/bin/env bash
# Reports the result of a package-currency run (lane 2).
#
# Usage: report-package-currency.sh <packages-dir> <suite-outcome>
#
#   packages-dir    directory holding the per-node <node>-packages.txt files
#   suite-outcome   "success" | "failure" | "inconclusive" | "not-run"
#                   failure      = the suite ran and tests failed (report-only)
#                   inconclusive = the suite could not run the tests at all
#
# The baseline lives on an unprotected data branch, NOT on main and NOT in a PR:
#
#   - main is protected (required checks, required review, admin enforced), so a
#     direct push is rejected; and a PR opened with GITHUB_TOKEN never fires the
#     pull_request workflows its own required checks come from, so it would sit
#     unmergeable forever.
#   - The data branch holds one file and is written with git plumbing, so the
#     job's checkout is never disturbed and no orphan-branch working-tree games
#     are needed. `git log ci/package-baseline` is the drift history.
#
# Behaviour:
#   - no baseline yet                -> seed the data branch, no issue
#   - baseline matches               -> exit quietly, touch nothing
#   - baseline differs               -> upsert ONE rolling issue, update the branch
#   - nodes disagree with each other -> always reported; that mismatch is the
#     failure mode that left a node unclustered (see first-boot.sh), and is worth
#     surfacing even when the package set is otherwise unchanged.
#
# Requires gh with issues:write, and a token with contents:write for the push.
# DRY_RUN=1 prints the mutating calls instead of performing them.

set -euo pipefail

PKG_DIR="${1:?Usage: report-package-currency.sh <packages-dir> <suite-outcome>}"
SUITE_OUTCOME="${2:?missing suite outcome}"

ISSUE_LABEL="pve-currency"
ISSUE_TITLE="PVE package currency: upstream drift detected"
BASELINE_BRANCH="${BASELINE_BRANCH:-ci/package-baseline}"
BASELINE_FILE="pve-package-baseline.txt"

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

run() {
    if [[ "${DRY_RUN:-0}" == "1" ]]; then
        echo "DRY_RUN: $*"
    else
        "$@"
    fi
}

# ── Collect the per-node sets ───────────────────────────────────────
shopt -s nullglob
node_files=("$PKG_DIR"/*-packages.txt)
shopt -u nullglob

if [[ ${#node_files[@]} -lt 2 ]]; then
    # Inter-node disagreement is the failure this lane exists to catch, so a
    # comparison that silently did not happen is worse than a loud failure.
    echo "ERROR: need >=2 node package sets to compare, got ${#node_files[@]} in ${PKG_DIR}" >&2
    exit 1
fi

# Pathname expansion already sorts by the current collation, so the glob is in
# node order (9a before 9b). The first is the reference: its set is what the
# baseline tracks; the others are only compared against it.
reference="${node_files[0]}"
reference_node="$(basename "$reference" -packages.txt)"
echo "Reference node: ${reference_node}"

# dpkg cannot legitimately emit anything outside this grammar. These files come
# from a machine that just installed packages from an upstream repo, and their
# contents reach a GitHub issue body — so a row that is not a name/version pair
# means the node lied, which is itself worth failing on.
PKG_RE='^(# running-kernel|[a-z0-9][a-z0-9+.-]*(:[a-z0-9]+)?)'$'\t''[A-Za-z0-9.+:~-]+$'
for f in "${node_files[@]}"; do
    # -s as well as -r: `grep -qv` on a zero-byte file returns 1, so an empty
    # capture would "validate" and seed an empty baseline.
    if [[ ! -r "$f" || ! -s "$f" ]]; then
        echo "ERROR: ${f} is empty or unreadable" >&2
        exit 1
    fi
    # grep returns 2 on an I/O error, which must not read as "no bad rows".
    g_rc=0
    grep -qvE "$PKG_RE" "$f" || g_rc=$?
    case "$g_rc" in
        1) ;;
        0) echo "ERROR: ${f} has rows that are not dpkg name/version pairs" >&2
           grep -nvE "$PKG_RE" "$f" | head -5 >&2
           exit 1 ;;
        *) echo "ERROR: could not read ${f} (grep rc=${g_rc})" >&2
           exit 1 ;;
    esac
done

# ── Do the nodes agree with each other? ─────────────────────────────
node_mismatch=""
for f in "${node_files[@]:1}"; do
    other_node="$(basename "$f" -packages.txt)"
    node_rc=0
    diff -q "$reference" "$f" >/dev/null 2>&1 || node_rc=$?
    if [[ "$node_rc" -gt 1 ]]; then
        echo "ERROR: diff failed (rc=${node_rc}) comparing ${reference} and ${f}" >&2
        exit 1
    fi
    if [[ "$node_rc" -eq 1 ]]; then
        echo "WARNING: ${other_node} package set differs from ${reference_node}"
        node_mismatch+=$'\n'"### \`${reference_node}\` vs \`${other_node}\`"$'\n\n```diff\n'
        du_rc=0
        diff -u "$reference" "$f" > "$WORK/node-diff.raw" || du_rc=$?
        if [[ "$du_rc" -gt 1 ]]; then
            echo "ERROR: diff -u failed (rc=${du_rc}) for ${f}" >&2
            exit 1
        fi
        # No pipe: `tail | head` dies on SIGPIPE under pipefail exactly like the
        # `echo | head` this file already fixed once. Two file steps instead.
        tail -n +3 "$WORK/node-diff.raw" > "$WORK/node-diff.body"
        node_mismatch+="$(head -n 200 "$WORK/node-diff.body")"
        if [[ "$(wc -l < "$WORK/node-diff.raw")" -gt 202 ]]; then
            node_mismatch+=$'\n… truncated; see the run log for the full diff'
        fi
        node_mismatch+=$'\n```\n'
    fi
done

# ── Fetch the recorded baseline from the data branch ────────────────
baseline="$WORK/baseline.txt"
have_baseline=0
branch_sha=""

# ls-remote --exit-code is deterministic: 0 = the ref exists, 2 = no such ref,
# anything else = transport or auth failure. The alternative — matching git's
# stderr text — breaks on a locale change or a reworded message, and would turn
# a legitimate first run into a hard failure.
lsr_rc=0
git ls-remote --exit-code --heads origin "$BASELINE_BRANCH" >/dev/null 2>"$WORK/ls.err" || lsr_rc=$?
case "$lsr_rc" in
    0) : ;;
    2) echo "Data branch ${BASELINE_BRANCH} does not exist yet." ;;
    *) echo "ERROR: could not reach origin for ${BASELINE_BRANCH} (rc=${lsr_rc}):" >&2
       cat "$WORK/ls.err" >&2
       exit 1 ;;
esac

if [[ "$lsr_rc" -eq 0 ]]; then
    git fetch --quiet origin "$BASELINE_BRANCH"
    branch_sha="$(git rev-parse FETCH_HEAD)"
    # Branch present but file absent is an inconsistent state, not a first run.
    # Treating it as "seeded" would skip the drift diff and then build a
    # parentless commit whose push is rejected — wedging the lane every week.
    if ! git cat-file -e "${branch_sha}:${BASELINE_FILE}" 2>/dev/null; then
        echo "ERROR: ${BASELINE_BRANCH} exists but has no ${BASELINE_FILE}" >&2
        exit 1
    fi
    git show "${branch_sha}:${BASELINE_FILE}" > "$baseline"
    have_baseline=1
fi

if [[ "$have_baseline" -eq 0 ]]; then
    echo "No baseline on ${BASELINE_BRANCH} — seeding it from ${reference_node}."
    baseline_status="seeded"
    package_diff=""
else
    # rc 0 = same, 1 = differs, anything else = diff itself failed. Treating 2
    # as "differs" would overwrite a good baseline from a failed compare.
    diff_rc=0
    diff -q "$baseline" "$reference" >/dev/null 2>&1 || diff_rc=$?
    case "$diff_rc" in
        0) baseline_status="unchanged"; package_diff="" ;;
        1) baseline_status="changed"
           # Capture rc rather than `|| true`: a bare `|| true` would swallow a
           # read error here and produce an empty diff plus a baseline update.
           du_rc=0
           diff -u "$baseline" "$reference" > "$WORK/pkg-diff.raw" || du_rc=$?
           if [[ "$du_rc" -gt 1 ]]; then
               echo "ERROR: diff -u failed (rc=${du_rc}) rendering the package diff" >&2
               exit 1
           fi
           package_diff="$(tail -n +3 "$WORK/pkg-diff.raw")" ;;
        *) echo "ERROR: diff failed (rc=${diff_rc}) comparing the baseline and ${reference}" >&2
           exit 1 ;;
    esac
fi

echo "Baseline status: ${baseline_status}"

if [[ "$baseline_status" == "unchanged" && -z "$node_mismatch" ]]; then
    echo "No drift and no node mismatch. Nothing to report."
    exit 0
fi

# ── Build the report body ───────────────────────────────────────────
body="$WORK/body.md"
{
    echo "Automated report from the PVE package-currency lane (\`package-currency.yml\`)."
    echo
    echo "- Run: ${GITHUB_SERVER_URL:-https://github.com}/${GITHUB_REPOSITORY:-}/actions/runs/${GITHUB_RUN_ID:-local}"
    echo "- Integration suite against upgraded nodes: **${SUITE_OUTCOME}**"
    echo "- Reference node: \`${reference_node}\`"
    echo "- Baseline: \`${BASELINE_BRANCH}\` / \`${BASELINE_FILE}\`"
    echo
    if [[ "$SUITE_OUTCOME" == "failure" ]]; then
        echo "> The suite failed against current PVE. This lane is report-only, so the"
        echo "> workflow is green — the failure is here, not in the check status."
        echo
    elif [[ "$SUITE_OUTCOME" == "inconclusive" ]]; then
        echo "> The suite did not produce a valid result — it failed before testing"
        echo "> anything (an unreachable node, for example). Nothing below says"
        echo "> whether the module works against current PVE."
        echo
    elif [[ "$SUITE_OUTCOME" == "not-run" ]]; then
        echo "> The suite did not run, so nothing is known about behaviour against"
        echo "> current PVE. Only the package set below is trustworthy."
        echo
    fi
    if [[ -n "$node_mismatch" ]]; then
        echo "## Nodes disagree"
        echo
        echo "The two nested nodes installed from the same ISO ended up with different"
        echo "package sets. This is the shape of the failure that previously left a node"
        echo "unclustered, and is worth investigating on its own."
        echo "$node_mismatch"
    fi
    if [[ "$baseline_status" == "changed" ]]; then
        echo "## Package drift since the last recorded baseline"
        echo
        echo '```diff'
        # Herestring, not a pipe: `echo "$big" | head` dies on SIGPIPE under
        # `set -o pipefail`, which killed the run on exactly the large drift
        # this section exists to report.
        head -n 300 <<< "$package_diff"
        echo '```'
        echo
        echo "Lane 1 stays pinned to the ISO; this lane does not bump it."
        echo "The recorded baseline on \`${BASELINE_BRANCH}\` has been updated to match."
    elif [[ "$baseline_status" == "seeded" ]]; then
        echo "## Baseline seeded"
        echo
        echo "No baseline existed, so this run recorded one on \`${BASELINE_BRANCH}\`."
    fi
} > "$body"

# GitHub rejects an issue body over 65536 characters with a 422, which under
# set -e would kill the run and produce no report at all — on exactly the large
# drift or full node divergence worth reporting.
#
# This is a backstop, deliberately redundant with the per-section caps above:
# with two nodes those caps already bound the body to roughly 30 KB, so neither
# guard is individually observable in a mutation test. It earns its place if the
# node count grows or a cap is raised.
if [[ "$(wc -c < "$body")" -gt 60000 ]]; then
    echo "Report body over 60000 bytes; truncating."
    head -c 60000 "$body" > "$body.trunc"
    printf '\n\n… truncated; see the run log for the full detail.\n' >> "$body.trunc"
    mv "$body.trunc" "$body"
fi

echo "--- report body ---"
cat "$body"
echo "-------------------"

# ── Update the data branch ──────────────────────────────────────────
if [[ "$baseline_status" != "unchanged" ]]; then
    # Plumbing rather than checkout/commit: this builds a one-file tree and a commit
    # on the branch's current tip without touching the job's working tree, so the
    # data branch stays a single-file history and nothing here can disturb main's
    # checkout. It is also idempotent — no local branch is created, so a re-run
    # cannot collide with itself.
    blob="$(git hash-object -w "$reference")"
    tree="$(printf '100644 blob %s\t%s\n' "$blob" "$BASELINE_FILE" | git mktree)"

    parent_args=()
    if [[ -n "$branch_sha" ]]; then
        parent_args=(-p "$branch_sha")
    fi

    commit_msg="ci: record PVE package baseline from ${reference_node}

Run: ${GITHUB_RUN_ID:-local}
Suite against current PVE: ${SUITE_OUTCOME}"

    # github-actions[bot] is the identity the workflow token acts as, and the one
    # with access to push this branch. Set explicitly rather than relying on git
    # config: an agent session exports GIT_AUTHOR_*/GIT_COMMITTER_* for its own bot
    # identity, and that must not leak into a CI-authored data commit.
    commit="$(
        GIT_AUTHOR_NAME="github-actions[bot]" \
        GIT_AUTHOR_EMAIL="41898282+github-actions[bot]@users.noreply.github.com" \
        GIT_COMMITTER_NAME="github-actions[bot]" \
        GIT_COMMITTER_EMAIL="41898282+github-actions[bot]@users.noreply.github.com" \
        git commit-tree "$tree" ${parent_args[@]+"${parent_args[@]}"} -m "$commit_msg"
    )"
    echo "Built baseline commit ${commit} for ${BASELINE_BRANCH}"

    run git push origin "${commit}:refs/heads/${BASELINE_BRANCH}"
    echo "Baseline recorded on ${BASELINE_BRANCH}."
fi

# ── Upsert the rolling issue ────────────────────────────────────────
# One issue, edited in place. A new issue per weekly tick would be noise.
if [[ "$baseline_status" == "changed" || -n "$node_mismatch" ]]; then
    # A failed query must not be read as "no issue exists" — that turns the
    # upsert into an append and quietly breaks the one-issue invariant.
    if ! existing="$(gh issue list --label "$ISSUE_LABEL" --state open --limit 1 --json number --jq '.[0].number // empty')"; then
        echo "ERROR: could not query open ${ISSUE_LABEL} issues" >&2
        exit 1
    fi
    if [[ -n "$existing" && ! "$existing" =~ ^[0-9]+$ ]]; then
        echo "ERROR: unexpected issue number from gh: ${existing}" >&2
        exit 1
    fi
    if [[ -n "$existing" ]]; then
        echo "Updating rolling issue #${existing}"
        run gh issue edit "$existing" --body-file "$body"
        run gh issue comment "$existing" --body "Refreshed by run ${GITHUB_RUN_ID:-local} — suite: ${SUITE_OUTCOME}, baseline: ${baseline_status}."
    else
        echo "Creating rolling issue"
        run gh issue create --title "$ISSUE_TITLE" --label "$ISSUE_LABEL" --body-file "$body"
    fi
fi
