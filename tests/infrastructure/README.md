# Nested Proxmox VE Test Infrastructure

This Terraform configuration provisions a throwaway nested Proxmox VE virtual machine on an existing Proxmox host. The nested instance is used as a target for PSProxmoxVE integration tests, providing a real PVE API to test against without risking production infrastructure.

## Prerequisites

- **Terraform** >= 1.5.0
- **Proxmox VE ISO** downloaded from [proxmox.com/en/downloads](https://www.proxmox.com/en/downloads)
- **API token** on the existing Proxmox host with full administrator privileges (Datastore.Allocate, VM.Allocate, VM.Config.*, Sys.Modify, etc.)
- **curl** and **jq** installed on the machine running Terraform (used by provisioner scripts)
- **SSH agent** running with a key that can access the Proxmox host (used by the bpg/proxmox provider for file uploads)
- A **routable IP address** available for the nested PVE instance (see Network section below)
- **Nested virtualization** enabled on the Proxmox host (see Intel vs AMD notes below)

## Quick Start

1. Copy the example variables file and fill in your values:

   ```bash
   cp terraform.tfvars.example terraform.tfvars
   # Edit terraform.tfvars with your Proxmox host details, ISO path, and network config
   ```

2. Initialize Terraform and download the provider:

   ```bash
   terraform init
   ```

3. Review the plan:

   ```bash
   terraform plan
   ```

4. Apply to create the nested PVE VM:

   ```bash
   terraform apply
   ```

   This will:
   - Upload the PVE ISO to the target node
   - Generate an answer file for unattended installation
   - Create and start the nested VM
   - Wait for the PVE API to become responsive (up to 10 minutes)
   - Create an API token (`root@pam!integration`) for integration tests

5. After apply completes, retrieve the test connection details:

   ```bash
   terraform output pve_test_url
   terraform output -raw pve_test_api_token
   ```

## Preparing the Installer ISO (one-time per PVE version)

The standard PVE installer ISO has no automated install mode. Before first use — and again whenever you upgrade to a new PVE release — run the following on the Proxmox host to produce a modified ISO that tells the installer to fetch its answer file from an attached FAT partition labeled `PROXMOX-AIS`:

```bash
ssh root@<proxmox-host>

# Paths are examples — adjust to match your NAS mount points
ORIGINAL_ISO=/mnt/pve/nas-nfs/template/iso/proxmox-ve_9.1-1.iso
MODIFIED_ISO=/mnt/pve/nas-nfs/template/iso/proxmox-ve_9.1-1-auto.iso

proxmox-auto-install-assistant prepare-iso "$ORIGINAL_ISO" \
  --fetch-from partition \
  --partition-label proxmox-ais \
  --output "$MODIFIED_ISO"
```

Update `iso_file_id` in `terraform.tfvars` to point to the new `*-auto.iso` file. The original ISO is left untouched.

## How It Works

### Answer File

The `answer.toml.tftpl` template generates a TOML answer file that automates the Proxmox VE installer. It configures the root password, network settings (static IP), disk layout, and other installation parameters so that no manual interaction is required during installation.

### Wait Script

After the VM is created and booted from the ISO, the `scripts/wait-for-api.sh` script polls the nested PVE API endpoint every 10 seconds for up to 10 minutes. Installation typically takes 3-7 minutes depending on disk and CPU performance. The script exits successfully once the API returns a valid version response.

### API Token Creation

Once the API is responsive, a provisioner script authenticates to the nested PVE using the root password and creates an API token (`root@pam!integration`) with full privileges (privsep=0). The token value is saved to `.api-token` and exposed via the `pve_test_api_token` output for use in integration tests.

## Cleanup

To destroy the nested PVE VM and all associated resources:

```bash
terraform destroy
```

This removes the VM, uploaded ISO, and answer file snippet from the Proxmox host. The `.api-token` file is also cleaned up locally.

## Network Configuration

The nested PVE instance requires a static IP address that is:

- **Routable** from the machine running integration tests (CI runner or developer workstation)
- **Not in use** by any other device on the network
- On the **same subnet** as the network bridge (`vmbr0` by default) on the host

The nested PVE will configure its own `vmbr0` bridge internally, but from the host's perspective it appears as a single VM with the assigned IP address.

If your test environment uses VLANs or an isolated test network, adjust the `network_bridge` variable accordingly and ensure the CI runner has connectivity to that network.

## Resource Requirements

The nested PVE VM requires sufficient resources to run Proxmox VE and potentially host lightweight test VMs inside it:

| Resource | Minimum | Default | Recommended |
|----------|---------|---------|-------------|
| CPU cores | 2 | 4 | 4+ |
| Memory | 4096 MB | 8192 MB | 8192+ MB |
| Disk | 32 GB | 64 GB | 64+ GB |

Ensure the Proxmox host has enough free resources to accommodate these allocations.

## Intel vs AMD Nested Virtualization

Nested virtualization must be enabled on the host for the nested PVE to function as a hypervisor itself. The CPU type is set to `host` to pass through virtualization extensions.

### Intel

Nested virtualization is typically enabled by default on modern Intel CPUs. Verify with:

```bash
cat /sys/module/kvm_intel/parameters/nested
```

If it shows `N`, enable it:

```bash
echo "options kvm_intel nested=1" > /etc/modprobe.d/kvm-intel.conf
modprobe -r kvm_intel && modprobe kvm_intel
```

### AMD

AMD nested virtualization support varies. Check with:

```bash
cat /sys/module/kvm_amd/parameters/nested
```

If it shows `0`, enable it:

```bash
echo "options kvm_amd nested=1" > /etc/modprobe.d/kvm-amd.conf
modprobe -r kvm_amd && modprobe kvm_amd
```

Note that AMD nested virtualization can be less stable than Intel in some configurations. If you encounter issues with nested VMs inside the nested PVE, the integration tests themselves (which test the PVE API, not nested VM creation) will still work -- only tests that attempt to create VMs inside the nested PVE would be affected.

## Files

| File | Purpose |
|------|---------|
| `main.tf` | Provider config, VM resource, provisioners |
| `variables.tf` | Input variable definitions with defaults |
| `outputs.tf` | Test connection details for integration tests |
| `answer.toml.tftpl` | Unattended PVE installer answer file template |
| `scripts/wait-for-api.sh` | Polls PVE API until responsive |
| `terraform.tfvars.example` | Example variable values |
| `.gitignore` | Excludes state, provider cache, secrets |
