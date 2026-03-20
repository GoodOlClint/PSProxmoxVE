# Proxmox VE API Coverage

This document tracks which PVE API areas are implemented in PSProxmoxVE and which are planned for future releases.

**Last updated:** 2026-03-20
**Module version:** 0.1.0-preview
**Total cmdlets:** 75

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
| **Tasks** | 2 | `/nodes/{node}/tasks/{upid}/status` (get, wait) |

## Not Yet Implemented

### High-Value Gaps

These are the most impactful areas for real-world automation that are not yet covered.

| Area | Key Endpoints | Use Case | Priority |
|------|--------------|----------|----------|
| **Firewall** | `/cluster/firewall/*`, `/nodes/{node}/firewall/*`, `/nodes/{node}/qemu/{vmid}/firewall/*` | Security automation, rule management, IP sets, aliases | High |
| **Backup / vzdump** | `/nodes/{node}/vzdump`, `/cluster/backup/*` | Disaster recovery, scheduled backups, backup job management | High |
| **Pool Management** | `/pools/*` | Multi-tenant environments, resource grouping | Medium |

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
| **SDN IPAM** | `/cluster/sdn/ipams/*` | IP address management for SDN |
| **SDN DNS** | `/cluster/sdn/dns/*` | DNS integration for SDN |
| **SDN Controllers** | `/cluster/sdn/controllers/*` | SDN controller configuration |

## Contributing New Cmdlets

See [CONTRIBUTING.md](../CONTRIBUTING.md) for the full guide. In short:

1. Create service methods in `PSProxmoxVE.Core/Services/`
2. Create model classes in `PSProxmoxVE.Core/Models/` if needed
3. Create cmdlet classes in `PSProxmoxVE/Cmdlets/`
4. Add cmdlet names to `CmdletsToExport` in the manifest
5. Add Pester tests
6. Update this document and the README cmdlet reference
