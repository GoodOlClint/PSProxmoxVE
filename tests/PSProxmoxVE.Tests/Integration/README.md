# Integration Tests

This directory contains Pester 5 integration tests for PSProxmoxVE that exercise the module
against a **real, live Proxmox VE API endpoint**. They are skipped by default and must be
opted into explicitly.

---

## Prerequisites

### Dedicated test node — never production

Integration tests **create and destroy real resources** (VMs, snapshots, ISO uploads, network
objects, user accounts). You must use a dedicated PVE test cluster or standalone node running
Proxmox VE **8.x or 9.x**. Never point these tests at a production cluster.

Recommended minimum:
- Single-node cluster or standalone host
- At least one storage pool with `images`, `iso`, and `rootdir` content types enabled
- Network access from the machine running Pester to the PVE API port (default 8006)

---

## Required API Token

Create a dedicated API token on the test node with the permissions listed below.
Using a scoped token (rather than the root password) limits blast radius if credentials leak.

```
pveum user add pester@pve
pveum acl modify / --users pester@pve --roles PVEAdmin
pveum user token add pester@pve pester-ci
```

### Minimum permissions per domain

| Test area      | Required privilege(s)                                          |
|----------------|----------------------------------------------------------------|
| Connection     | `Sys.Audit`                                                    |
| Nodes          | `Sys.Audit`                                                    |
| VMs (read)     | `VM.Audit`                                                     |
| VMs (create)   | `VM.Allocate`, `VM.Config.Disk`, `VM.Config.Memory`, `VM.Config.Network` |
| VMs (delete)   | `VM.Allocate`                                                  |
| VMs (power)    | `VM.PowerMgmt`                                                 |
| VMs (clone)    | `VM.Clone`                                                     |
| Storage (read) | `Datastore.Audit`                                              |
| Storage (ISO)  | `Datastore.AllocateSpace`, `Datastore.AllocateTemplate`        |
| Snapshots      | `VM.Snapshot`, `VM.Snapshot.Rollback`                          |
| Network        | `Sys.Modify`                                                   |
| Users          | `User.Modify`                                                  |
| Templates      | `VM.Allocate`, `VM.Clone`                                      |
| Cloud-Init     | `VM.Config.CloudInit`                                          |

---

## Environment Variables

Set these before running the integration suite. The first six are required; any missing
required variable causes every integration test to be skipped with a clear reason message.

| Variable                 | Required | Description                                                           | Example value                                       |
|--------------------------|----------|-----------------------------------------------------------------------|-----------------------------------------------------|
| `PVETEST_HOST`           | Yes      | Hostname or IP address of the test PVE node                          | `192.168.1.10` or `pve-test.internal`               |
| `PVETEST_PORT`           | Yes      | PVE API port                                                          | `8006`                                              |
| `PVETEST_APITOKEN`       | Yes      | API token in `USER@REALM!TOKENID=UUID` format                        | `pester@pve!pester-ci=xxxxxxxx-xxxx-xxxx-xxxx-xxxx` |
| `PVETEST_NODE`           | Yes      | Node name as it appears in `pvesh get /nodes`                        | `pve-test1`                                         |
| `PVETEST_STORAGE`        | Yes      | Storage pool to use for disk and ISO operations                       | `local`                                             |
| `PVETEST_ISO_PATH`       | Yes      | Local path to a small `.iso` file used for upload tests               | `/tmp/tinycorelinux.iso`                            |
| `PVETEST_PASSWORD`       | No       | Root password for cloud-init Linux VM provisioning                    | `Testpass123!`                                      |
| `PVETEST_CLOUD_IMAGE_URL`| No      | URL for cloud image download (defaults to Ubuntu Noble)               | `https://cloud-images.ubuntu.com/noble/current/noble-server-cloudimg-amd64.img` |
| `PVETEST_PVE_VERSION`    | No       | Expected PVE major version (8 or 9)                                   | `9`                                                 |

### Setting variables (Bash / zsh)

```bash
export PVETEST_HOST="192.168.1.10"
export PVETEST_PORT="8006"
export PVETEST_APITOKEN="pester@pve!pester-ci=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
export PVETEST_NODE="pve-test1"
export PVETEST_STORAGE="local"
export PVETEST_ISO_PATH="/tmp/tinycorelinux.iso"
# Optional — required for Linux VM provisioning tests
export PVETEST_PASSWORD="Testpass123!"
```

### Setting variables (PowerShell)

```powershell
$env:PVETEST_HOST      = '192.168.1.10'
$env:PVETEST_PORT      = '8006'
$env:PVETEST_APITOKEN  = 'pester@pve!pester-ci=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
$env:PVETEST_NODE      = 'pve-test1'
$env:PVETEST_STORAGE   = 'local'
$env:PVETEST_ISO_PATH  = '/tmp/tinycorelinux.iso'
# Optional — required for Linux VM provisioning tests
$env:PVETEST_PASSWORD  = 'Testpass123!'
```

### GitHub Actions / CI

Add the variables as repository **Actions secrets** and expose them as environment variables
in your workflow:

```yaml
env:
  PVETEST_HOST:      ${{ secrets.PVETEST_HOST }}
  PVETEST_PORT:      ${{ secrets.PVETEST_PORT }}
  PVETEST_APITOKEN:  ${{ secrets.PVETEST_APITOKEN }}
  PVETEST_NODE:      ${{ secrets.PVETEST_NODE }}
  PVETEST_STORAGE:   ${{ secrets.PVETEST_STORAGE }}
  PVETEST_ISO_PATH:  ${{ secrets.PVETEST_ISO_PATH }}
  PVETEST_PASSWORD:  ${{ secrets.PVETEST_PASSWORD }}
```

---

## How to Run

The integration suite is tagged `Integration`. Use the `-Tag` filter so that the unit
tests and integration tests can be run independently.

### Via the project helper script (recommended)

```powershell
./Invoke-Tests.ps1 -Tier Integration
```

### Directly with Invoke-Pester

```powershell
# Run integration tests only
Invoke-Pester -Path ./tests/PSProxmoxVE.Tests -Tag Integration -Output Detailed

# Run everything (unit + integration)
Invoke-Pester -Path ./tests/PSProxmoxVE.Tests -Output Detailed

# Run unit tests only (exclude integration)
Invoke-Pester -Path ./tests/PSProxmoxVE.Tests -ExcludeTag Integration -Output Detailed
```

---

## Warning — Real Resources Are Created and Destroyed

The integration tests:

- **Create VMs** (named `pester-test-vm`, `pester-clone-vm`, `pester-linux-vm`) on `PVETEST_NODE`
- **Delete** those VMs after the test completes (via `AfterAll` cleanup)
- **Provision a Linux VM** with cloud-init, guest agent, and disk import (when `PVETEST_PASSWORD` is set)
- **Start and stop** VMs, including graceful ACPI shutdown via guest agent
- **Create and delete a snapshot** on an existing stopped VM
- **Upload an ISO** to `PVETEST_STORAGE`
- **Download a cloud image** to PVE storage (when `PVETEST_PASSWORD` is set)

The `AfterAll` block performs best-effort cleanup. If the test run is interrupted, leftover
VMs named `pester-*` may remain on the test node and should be removed manually.

**Always confirm you are pointing at the correct, isolated test node before running.**

---

## Planned Test Coverage

| Domain         | Current integration tests                                                      | Planned additions                                              |
|----------------|--------------------------------------------------------------------------------|----------------------------------------------------------------|
| Connection     | Connect via API token, detect server version                                    | Connect via credential (ticket auth), session expiry handling  |
| Nodes          | List nodes, get node status                                                     | Node resource usage metrics                                    |
| VMs            | List, create, delete, start, stop, clone                                        | Migrate, resize disk, Get/Set-PveVmConfig, Move-PveVm          |
| Storage        | List, upload ISO                                                                | Get-PveStorageContent, Invoke-PveStorageDownload, Remove ISO   |
| Snapshots      | Create, list, delete snapshot on stopped VM                                     | Restore snapshot, snapshots on running VM (with vmstate)       |
| Network        | List node networks                                                              | Create bridge, Set-PveNetwork, Invoke-PveNetworkApply          |
| SDN            | _(none yet — requires SDN plugin enabled on test node)_                        | Get/New/Remove zone and vnet                                   |
| Users          | List users, verify root@pam present                                             | Create/remove user, assign role, set permission                |
| Templates      | List templates (count not asserted)                                             | Convert VM to template, deploy VM from template                |
| Cloud-Init     | Get cloud-init config from a stopped VM (no-throw assertion)                   | Set cloud-init fields, verify propagation via VM config        |
| Tasks          | _(implicitly exercised via -Wait on lifecycle cmdlets)_                        | Get-PveTask, Wait-PveTask with custom timeout                  |
