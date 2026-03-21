# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).

## [Unreleased]

### Added

- Firewall management cmdlets (21): rules, security groups, aliases, IP sets, options at cluster/node/VM/container levels
- Backup/vzdump cmdlets (5): ad-hoc backup creation and scheduled backup job CRUD
- SDN IPAM cmdlets (3): Get/New/Remove-PveSdnIpam for IPAM plugin management
- SDN DNS cmdlets (3): Get/New/Remove-PveSdnDns for DNS plugin management
- SDN Controller cmdlets (3): Get/New/Remove-PveSdnController for controller management
- PSGallery version badge in README
- Integration tests for firewall rules, aliases, IP sets, backup jobs, and OVA import

### Fixed

- `Remove-PveRole` now has `ConfirmImpact.High` for safety
- URL-encode snapshot names in API paths for defense-in-depth
- Extract auth header magic strings to named constants in PveHttpClient

## [0.1.0-preview] - 2026-03-19

### Added

- Initial project structure and solution setup
- Ticket and API token authentication with session management
- HTTP client with manual multipart ISO upload (bugzilla 7389 workaround)
- Typed response models for PVE 8.x and 9.x API resources
- Service layer for all resource domains
- 66 PowerShell cmdlets for VMs, containers, storage, networking, SDN, users, roles, permissions, API tokens, templates, cloud-init, snapshots, and tasks
- QEMU guest agent cmdlets (Test-PveVmGuestAgent, Get-PveVmGuestNetwork, Invoke-PveVmGuestExec)
- xUnit unit tests for core library
- Pester 5 cmdlet tests across OS/PS version matrix (Windows PS 5.1, PS 7.5 on Windows/Linux/macOS)
- Integration tests against live PVE 8 and PVE 9 instances via Terraform-provisioned nested VMs
- GitHub Actions CI/CD workflows (build, unit tests, integration tests)
- Format definitions for default table output on all PS versions
