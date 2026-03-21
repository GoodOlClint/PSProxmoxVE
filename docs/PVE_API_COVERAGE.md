# Proxmox VE API Coverage

This document tracks which PVE API areas are implemented in PSProxmoxVE and which are planned for future releases.

**Last updated:** 2026-03-21
**Module version:** 0.1.0-preview
**Total cmdlets:** 169

## Implemented

| Area | Cmdlets | API Endpoints |
|------|---------|---------------|
| **Connection** | 3 | `POST /access/ticket`, `DELETE /access/ticket` |
| **Nodes** | 2 | `GET /nodes`, `GET /nodes/{node}/status` |
| **VMs (QEMU)** | 19 | `/nodes/{node}/qemu/*` (CRUD, lifecycle, clone, migrate, resize, disk import, config, guest agent) |
| **Containers (LXC)** | 14 | `/nodes/{node}/lxc/*` (CRUD, lifecycle, clone, migrate, config, snapshots) |
| **Storage** | 6 | `/storage`, `/nodes/{node}/storage/{storage}/*` (CRUD, content, upload, download) |
| **Snapshots** | 4 | `/nodes/{node}/qemu/{vmid}/snapshot/*` (CRUD, rollback) |
| **Container Snapshots** | 4 | `/nodes/{node}/lxc/{vmid}/snapshot/*` (CRUD, rollback) |
| **Networking** | 5 | `/nodes/{node}/network/*` (CRUD, apply) |
| **SDN Zones** | 3 | `/cluster/sdn/zones/*` (CRUD) |
| **SDN VNets** | 3 | `/cluster/sdn/vnets/*` (CRUD) |
| **SDN Subnets** | 3 | `/cluster/sdn/vnets/{vnet}/subnets/*` (CRUD) |
| **Users** | 4 | `/access/users/*` (CRUD) |
| **Roles** | 3 | `/access/roles/*` (CRUD) |
| **Permissions** | 2 | `/access/acl` (get, set) |
| **API Tokens** | 3 | `/access/users/{userid}/tokens/*` (CRUD) |
| **Templates** | 4 | VM template conversion, listing, cloning, removal |
| **Cloud-Init** | 3 | `/nodes/{node}/qemu/{vmid}/config` (cloud-init fields), `/nodes/{node}/qemu/{vmid}/cloudinit/regenerate` |
| **Tasks** | 4 | `/nodes/{node}/tasks` (list, get status, stop, wait) |
| **Firewall** | 21 | `/cluster/firewall/*`, `/nodes/{node}/firewall/*`, `/nodes/{node}/qemu/{vmid}/firewall/*`, `/nodes/{node}/lxc/{vmid}/firewall/*` (rules, groups, aliases, IP sets, options, refs) |
| **Backup** | 7 | `/nodes/{node}/vzdump`, `/cluster/backup/*`, `/cluster/backup-info/not-backed-up` |
| **SDN Zones** | 4 | `/cluster/sdn/zones/*` (CRUD + update) |
| **SDN VNets** | 4 | `/cluster/sdn/vnets/*` (CRUD + update) |
| **SDN Subnets** | 4 | `/cluster/sdn/vnets/{vnet}/subnets/*` (CRUD + update) |
| **SDN IPAM** | 4 | `/cluster/sdn/ipams/*` (CRUD + update) |
| **SDN DNS** | 4 | `/cluster/sdn/dns/*` (CRUD + update) |
| **SDN Controllers** | 4 | `/cluster/sdn/controllers/*` (CRUD + update) |
| **SDN Apply** | 1 | `PUT /cluster/sdn` (apply pending changes) |
| **Cluster** | 1 | `GET /cluster/resources` (cluster-wide inventory) |
| **Pools** | 4 | `/pools/*` (CRUD) |
| **VM Disk Ops** | 2 | `move_disk`, `unlink` |
| **Guest Agent (ext)** | 6 | `get-osinfo`, `get-fsinfo`, `file-read`, `file-write`, `set-user-password`, `fstrim` |
| **Container Ops** | 6 | `suspend`, `resume`, `resize`, `template`, `move_volume`, `interfaces` |
| **Storage Content** | 4 | `status`, `content` DELETE/PUT/POST |
| **Node Ops** | 6 | `config`, `dns`, `startall`, `stopall` |
| **Access Groups** | 4 | `/access/groups/*` (CRUD) |
| **Access Domains** | 4 | `/access/domains/*` (CRUD) |
| **Access Password** | 1 | `PUT /access/password` |

## Not Yet Implemented

### Gaps

| Area | Key Endpoints | Notes | Priority |
|------|--------------|-------|----------|

### Lower Priority Gaps

| Area | Key Endpoints | Notes |
|------|--------------|-------|
| **Ceph** | `/nodes/{node}/ceph/*` | OSD, monitor, pool, and MDS management |
| **HA (High Availability)** | `/cluster/ha/*` | HA groups, resources, and fencing configuration |
| **Replication** | `/cluster/replication/*` | ZFS replication between nodes |
| **Access Groups** | `/access/groups/*` | User group management |
| **Access Domains/Realms** | `/access/domains/*` | LDAP, AD, OpenID realm configuration |
| **PBS Integration** | N/A (separate API) | Proxmox Backup Server operations |
| **Cluster Config** | `/cluster/config/*` | Cluster join, node management, totem config |
| **ACME / Certificates** | `/cluster/acme/*`, `/nodes/{node}/certificates/*` | Let's Encrypt certificate automation |
| **Node Management** | `/nodes/{node}/apt/*`, `/nodes/{node}/disks/*`, `/nodes/{node}/services/*` | Package updates, disk management, service control |
| **Metrics** | `/cluster/metrics/*` | External metrics server configuration |
| **Additional VM Agent** | `/nodes/{node}/qemu/{vmid}/agent/*` | File read/write, OS info, suspend/resume via agent |

## Contributing New Cmdlets

See [CONTRIBUTING.md](../CONTRIBUTING.md) for the full guide. In short:

1. Create service methods in `PSProxmoxVE.Core/Services/`
2. Create model classes in `PSProxmoxVE.Core/Models/` if needed
3. Create cmdlet classes in `PSProxmoxVE/Cmdlets/`
4. Add cmdlet names to `CmdletsToExport` in the manifest
5. Add Pester tests
6. Update this document and the README cmdlet reference
