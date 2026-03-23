# Plan: Integration Test Workflow Refactoring

**Status**: Planned (not started)
**Created**: 2026-03-23
**Priority**: Medium — improves CI speed and reliability but not blocking

## Goals

1. **Parallel VM provisioning** — provision PVE 8 and PVE 9 VMs simultaneously instead of sequentially
2. **File caching** — cache ISOs, cloud images, and OVA files on the self-hosted runner
3. **Zero-touch runner setup** — runner no longer needs manual ISO provisioning
4. **Extensibility** — easy to add cluster peers, storage VMs in the future

## Proposed Job Graph

```
build ─────────────┐
                    ├──→ provision ──→ test-pve8 ──┐
container-image ───┘     (all VMs      test-pve9 ──┤──→ cleanup
                          parallel)    test-cluster─┘    (always)
                                       (future)
```

## Changes Required

### 1. Terraform: for_each multi-VM provisioning
- Refactor main.tf from single-VM to `pve_instances` map variable
- Both ISOs upload and both VMs create in a single `terraform apply`
- Update variables.tf and outputs.tf

### 2. New caching scripts
- `ensure-base-iso.sh` — download PVE base ISO if not cached in /opt/pve-isos
- `ensure-cloud-images.sh` — download cloud image + OVA with ETag-based 7-day TTL
- Modify `prepare-auto-iso.sh` — add `--cache-dir` flag with hash-based skip

### 3. New `provision` job (self-hosted)
- Preflight cleanup for ALL VM IDs
- Prepare both auto-install ISOs (cached)
- Download cloud images once (cached)
- Single `terraform apply`
- Wait for both PVE installs + create API tokens
- Expose connection details as job outputs

### 4. Test jobs consume provision outputs
- Matrix with `max-parallel: 2`
- Tests get PVETEST_HOST, PVETEST_APITOKEN from `needs.provision.outputs.*`
- No provisioning in test jobs

### 5. Cleanup job with `if: always()`
- API-only cleanup via preflight-cleanup.sh (stateless)
- Terraform state as artifact for belt-and-suspenders

## Estimated Time Savings

| Phase | Current | Proposed |
|---|---|---|
| Build + container | 5-10 min | 5-10 min |
| Provision (sequential → parallel) | 20-30 min | 10-15 min |
| Tests (sequential → parallel if runner allows) | 10-20 min | 5-10 min |
| Teardown | 4-6 min | 3-5 min |
| **Total** | **~40-60 min** | **~25-40 min** |

## Dependabot / CI Isolation Lessons (2026-03-23)

During scan-6 we discovered two issues that this refactoring must account for:

### 1. cleanup-images must be gated against dependabot

The `cleanup-images` job uses `actions/delete-package-versions` with `if: always()` to
prune old GHCR container images. When a dependabot PR ran, the `integration` job was
skipped (correctly), but `cleanup-images` still fired and **deleted the container image
that the main branch integration run was actively using**, causing PVE 8 to fail with
"image not found".

**Fix already applied**: All jobs now have `if: github.actor != 'dependabot[bot]'`.

**For the refactoring**: The new `cleanup` job (Terraform destroy + VM cleanup) must
also be gated. Use `if: always() && github.actor != 'dependabot[bot]'` on all jobs
that touch shared resources (self-hosted runner, GHCR, PVE host).

### 2. Container image tags must survive concurrent cleanup

The current `cleanup-images` job deletes all but 1 container image version. If two
workflow runs overlap (e.g., a push to main while a prior run is still testing), the
cleanup from the first run can delete the image needed by the second.

**For the refactoring**: Consider one of:
- **Tag images by run ID** instead of commit SHA, and only delete images older than
  the current run
- **Pin `min-versions-to-keep: 3`** to survive overlapping runs
- **Move cleanup to a scheduled workflow** (weekly) instead of per-run

### 3. Self-hosted runner disk space

The runner ran out of disk space during an integration test run, leaving it in a broken
state that required manual rebuilding. The caching strategy must account for this:
- Set a maximum cache size or file count in `/opt/pve-isos`
- Auto-prune ISOs not referenced by the current workflow matrix
- The cleanup job should clean up uploaded ISOs from the PVE host, not just VMs
- Consider a periodic runner maintenance script that frees disk space

## Implementation Order

1. Refactor Terraform (main.tf, variables.tf, outputs.tf) → for_each
2. Create ensure-base-iso.sh and ensure-cloud-images.sh
3. Add --cache-dir to prepare-auto-iso.sh
4. Create parallel wait wrapper for create-api-token.sh
5. Restructure workflow into provision → test → cleanup jobs
6. Update prepare-test-environment.sh for cache dir
7. Ensure all jobs gated with `github.actor != 'dependabot[bot]'`
8. Address container image cleanup race condition
9. Test on runner via workflow_dispatch

## Files to Modify

- .github/workflows/integration-tests.yml
- tests/infrastructure/main.tf
- tests/infrastructure/variables.tf
- tests/infrastructure/outputs.tf
- tests/infrastructure/scripts/prepare-auto-iso.sh
- tests/infrastructure/scripts/prepare-test-environment.sh
- tests/infrastructure/scripts/create-api-token.sh (wrapper)
- New: tests/infrastructure/scripts/ensure-base-iso.sh
- New: tests/infrastructure/scripts/ensure-cloud-images.sh
