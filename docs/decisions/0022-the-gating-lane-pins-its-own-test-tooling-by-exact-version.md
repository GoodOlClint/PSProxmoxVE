# ADR 0022 — The gating lane pins its own test tooling by exact version

- **Status:** Accepted
- **Date:** 2026-09-01
- **Deciders:** operator + agent
- **Context source:** recorded as an amendment to D017 on 2026-09-01; split into its own record during the ADR migration, [ADR 0023](0023-decisions-live-in-docs-decisions-in-house-adr-format.md).

## Context

[ADR 0017](0017-ci-runs-two-lanes-a-pinned-gating-lane-and-a-report-only-currency-lane.md) pins the nested PVE packages so the merge gate has no moving inputs. The lane's own tooling was not pinned, which is the same defect one layer up.

Both Pester install sites used `-MinimumVersion 5.0` with no ceiling. The image is rebuilt on every CI run and Pester is installed fresh on every unit-test run, so PSGallery decided the version: a new major could reach the merge gate with no commit to this repository, surfacing as unexplained test breakage on whichever PR happened to run next.

It had already happened silently. Steps named "Install Pester 5" were resolving 6.1.0 on both the PowerShell 5.1 and 7.x legs, because Pester 6 declares `PowerShellVersion 5.1` and so installs on Windows PowerShell too. Nothing broke — the suite uses only constructs common to 5 and 6 — but nobody chose it.

## Decision

The gating lane's test tooling is pinned by exact version: `Pester` in `tests/Dockerfile.test` (`ARG PESTER_VERSION`) and in `.github/workflows/unit-tests.yml` (`env.PESTER_VERSION`), installed and imported with `-RequiredVersion` at every site, including the suite's own import inside the container.

The Dockerfile promotes the ARG to `ENV` so the version is discoverable at runtime. Both files must name the same version, and `shell-selfchecks` asserts it.

Bumping is a deliberate commit that changes both files together.

## Rejected alternatives

`-MinimumVersion 5.0`, or any floor without a ceiling. It reads as a pin and is not one: the resolved version is whatever PSGallery published most recently, so the merge gate changes without a commit.

Pinning only the Dockerfile. The unit-test workflow installs Pester independently, so the two would drift and the drift would be invisible — which is why `shell-selfchecks` asserts they agree rather than trusting convention.

## Consequences

Two files must change together for every bump, and a check exists solely to enforce that.

The pin is on the version, not on the gallery — a yanked or unavailable version fails the build loudly, which is the intended behaviour for a gate.
