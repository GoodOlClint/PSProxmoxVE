# Self-Hosted GitHub Actions Runner for PSProxmoxVE

## Overview

The PSProxmoxVE integration tests require network access to a live Proxmox VE API. A self-hosted GitHub Actions runner, deployed on or near your Proxmox host, lets pushes to GitHub automatically trigger these tests without exposing your PVE management interface to the public internet.

This directory contains everything needed to set up such a runner.

## Option A: LXC Container (Recommended)

Running the runner inside an LXC container on the Proxmox host itself is the simplest approach. The container has direct network access to the PVE API with no extra networking required.

### 1. Create the LXC container

From the Proxmox host shell (or the web UI):

```bash
# Download a Debian 12 template if you don't already have one
pveam download local debian-12-standard_12.7-1_amd64.tar.zst

# Create a privileged container
pct create 900 local:vztmpl/debian-12-standard_12.7-1_amd64.tar.zst \
    --hostname github-runner \
    --cores 2 \
    --memory 4096 \
    --swap 1024 \
    --rootfs local-lvm:20 \
    --net0 name=eth0,bridge=vmbr0,ip=dhcp \
    --unprivileged 0 \
    --features nesting=1 \
    --start 1
```

Adjust the container ID (900), storage, and network bridge to match your environment.

**Recommended resources:**
- 2 CPU cores
- 4 GB RAM
- 20 GB disk

### 2. Run the setup script

Enter the container and run the setup script:

```bash
pct enter 900

apt-get update && apt-get install -y curl
curl -fsSL https://raw.githubusercontent.com/GoodOlClint/PSProxmoxVE/main/tests/infrastructure/runner/setup-runner.sh \
    -o /tmp/setup-runner.sh
chmod +x /tmp/setup-runner.sh

/tmp/setup-runner.sh \
    --repo GoodOlClint/PSProxmoxVE \
    --token <YOUR_REGISTRATION_TOKEN> \
    --labels self-hosted,proxmox,integration
```

See [GitHub Configuration](#github-configuration) below for how to obtain the registration token.

### 3. Verify

The runner should appear as **Online** in your repository under **Settings > Actions > Runners** within a minute.

## Option B: Docker Container

If you prefer Docker, or want to run the runner on a different machine that has network access to your PVE host:

### 1. Build the image

```bash
cd tests/infrastructure/runner
docker build -t psproxmoxve-runner .
```

### 2. Run the container

```bash
docker run -d \
    -e REPO_URL=https://github.com/GoodOlClint/PSProxmoxVE \
    -e RUNNER_TOKEN=<YOUR_REGISTRATION_TOKEN> \
    -e RUNNER_LABELS=self-hosted,proxmox,integration \
    -e RUNNER_NAME=docker-proxmox-runner \
    --name psproxmoxve-runner \
    --restart unless-stopped \
    psproxmoxve-runner
```

For ephemeral (single-job) mode, add `-e EPHEMERAL=true`. The container will exit after completing one job; combine with `--restart always` to re-register automatically for the next job.

### 3. Verify

```bash
docker logs -f psproxmoxve-runner
```

You should see the runner register and begin listening for jobs.

## GitHub Configuration

### Obtaining a Registration Token

1. Navigate to your repository on GitHub.
2. Go to **Settings > Actions > Runners**.
3. Click **New self-hosted runner**.
4. Copy the registration token shown in the configuration instructions.

> **Note:** Registration tokens expire after one hour. Generate a new one if yours has expired.

### Required Labels

The integration test workflow targets runners with the label `integration`. The setup script applies the following labels by default:

- `self-hosted`
- `proxmox`
- `integration`

You can override these with the `--labels` flag or `RUNNER_LABELS` environment variable.

## Repository Secrets

The integration tests read connection details from GitHub Actions secrets. Configure these in **Settings > Secrets and variables > Actions**:

| Secret | Description | Example |
|---|---|---|
| `PVETEST_HOST` | IP or hostname of the Proxmox VE host (or a nested test PVE instance) | `192.168.1.100` |
| `PVETEST_PORT` | PVE API port | `8006` |
| `PVETEST_APITOKEN` | API token in `user@realm!tokenid=secret` format | `testuser@pve!ci=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `PVETEST_NODE` | Proxmox node name to run tests against | `pve` |
| `PVETEST_STORAGE` | Storage ID for upload/ISO tests | `local` |
| `PVETEST_ISO_PATH` | Path on the runner to a small test ISO file | `/opt/test-assets/test.iso` |

### Creating a Dedicated API Token

It is strongly recommended to create a dedicated, least-privilege API token for testing:

```bash
# On the Proxmox host
pveum user add testuser@pve --password <password>
pveum role add CITestRole --privs "VM.Allocate VM.Audit VM.Config.Disk VM.Config.CPU VM.Config.Memory VM.Config.Network VM.Config.Options VM.PowerMgmt Datastore.AllocateSpace Datastore.Audit SDN.Use"
pveum aclmod / --user testuser@pve --role CITestRole
pveum user token add testuser@pve ci --privsep 0
```

The last command outputs the token secret -- store it as the `PVETEST_APITOKEN` secret.

## Security Considerations

- **Network isolation.** The runner has access to your local network. If possible, place it on a dedicated test VLAN that can only reach the PVE API and the internet (for downloading runner updates and GitHub communication).

- **Dedicated API token.** Use a purpose-built API token with only the permissions the tests need. Never use `root@pam`.

- **Registration token is single-use.** The GitHub registration token is consumed during setup and cannot be reused. A new token is needed only to re-register or remove the runner.

- **Ephemeral runners.** For stronger isolation, use `--ephemeral` (setup script) or `-e EPHEMERAL=true` (Docker). The runner handles one job and then deregisters. This prevents state leakage between workflow runs.

- **Repository scope.** Self-hosted runners registered at the repository level only receive jobs from that repository. Do not register at the organization level unless you understand the implications.

- **Keep the host updated.** Regularly apply OS security patches to the runner container or VM.

## Maintenance

### Runner Auto-Updates

The GitHub Actions runner automatically updates itself when GitHub releases a new version. No manual intervention is required.

### Checking Runner Status

```bash
# LXC / bare metal (systemd service)
systemctl status actions.runner.*

# Docker
docker logs psproxmoxve-runner
```

### Removing the Runner

**LXC / bare metal:**

```bash
cd /opt/github-runner
sudo ./svc.sh stop
sudo ./svc.sh uninstall
./config.sh remove --token <NEW_REMOVAL_TOKEN>
```

Generate a removal token from **Settings > Actions > Runners** by clicking the runner name.

**Docker:**

```bash
docker stop psproxmoxve-runner
docker rm psproxmoxve-runner
```

If the container was not stopped gracefully (which triggers automatic deregistration), remove the runner manually from **Settings > Actions > Runners** in the GitHub UI.

### Reinstalling / Re-registering

If you need to re-register the runner (e.g., after moving it to a new host):

1. Remove the old registration (see above).
2. Generate a new registration token from GitHub.
3. Run the setup script or Docker container again with the new token.
