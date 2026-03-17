# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).

## [Unreleased]

### Added

- Initial project structure and solution setup
- Ticket and API token authentication with session management
- HTTP client with manual multipart ISO upload (bugzilla 7389 workaround)
- Typed response models for PVE 8.x and 9.x API resources
- Service layer for all resource domains
- PowerShell cmdlets for all supported operations
- xUnit unit tests for core library
- Pester 5 cmdlet tests across OS/PS version matrix
- Integration test stubs for future live target testing
- GitHub Actions CI/CD workflows
- Format definitions for default table output on all PS versions
