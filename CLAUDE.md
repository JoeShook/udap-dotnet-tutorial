# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A local UDAP (Unified Data Access Profiles) playground and tutorial for the [udap-dotnet](https://github.com/udap-tools/udap-dotnet) library. Implements the HL7 "Security for Scalable Registration, Authentication, and Authorization" IG on FHIR. Licensed CC BY-NC-ND 4.0.

## Build & Run

```bash
# Build entire solution
dotnet build udap.dotnet.tutorial.sln

# Run with Docker Compose (pre-built images)
docker-compose -f docker-compose-github.yml up

# Run with Docker Compose (build locally)
docker-compose up

# Run with .NET Aspire (see dotnetAspire.md for prerequisites)
# Start from aspire.AppHost project

# Generate PKI certificates (run first if no CertificateStore exists)
dotnet run --project udap.pki.devdays
```

There are no test projects in this solution.

## Architecture

Eight projects in the solution, all targeting .NET 10.0:

### Core Services

- **udap.pki.devdays** — CLI tool that generates all PKI (root CA, sub CA, leaf certificates) for three communities: Community1 (RSA), Community2 (ECDSA), Community3 (Revoked RSA). Outputs to `CertificateStore/`.
- **udap.certificates.server.devdays** — Static HTTP server (port 5034) serving intermediate certificates and CRLs from wwwroot.
- **udap.fhirserver.devdays** — FHIR R4 resource server (HTTPS port 7017, path `/fhir/r4`) using Brian Postlethwaite's DemoFileSystemFhirServer. Hosts UDAP metadata for all three communities. Authenticates via JWT bearer tokens from the auth server.
- **udap.authserver.devdays** — Data Holder's Authorization Server (HTTPS port 5102). Duende IdentityServer with SQLite + UDAP extensions for Dynamic Client Registration and Tiered OAuth.
- **udap.idp.server.devdays** — Identity Provider (HTTPS port 5202). Duende IdentityServer configured as a UDAP-enabled OpenID Connect IdP for Community1.

### Orchestration

- **aspire.AppHost** — .NET Aspire orchestrator. Configures all services and adds UdapEd container.
- **aspire.ServiceDefaults** — Shared library adding OpenTelemetry, health checks, and service discovery to all services.

### Authentication Flow

1. Client performs UDAP Dynamic Client Registration (DCR) with Auth Server
2. Client requests authorization via OAuth 2.0 authorization code flow
3. Auth Server can delegate to IDP Server via Tiered OAuth
4. FHIR Server validates bearer tokens issued by Auth Server

## Key Patterns

**Middleware ordering matters.** Each server has a specific pipeline order:
- FHIR Server: `UsePathBase` → `UseUdapMetadataServer` → `UseRouting` → `UseAuthentication` → `UseAuthorization`
- Auth Server: `UseUdapServer` must come before `UseIdentityServer`
- IDP Server: `UseUdapMetadataServer` → `UseUdapIdPServer` → `UseIdentityServer`

**Certificate stores** are configured via `UdapFileCertStoreManifest` sections in each project's `appsettings.json`. The `IPrivateCertificateStore` interface can be implemented for secure storage (HSM).

**Databases**: Auth Server and IDP Server each use SQLite via EF Core (files: `udap.authserver.devdays.EntityFramework.db`, `udap.idp.server.devdays.EntityFramework.db`). Delete the DB file and restart if certificates are regenerated.

**Networking**: All services use `host.docker.internal` as hostname for container-to-host and container-to-container communication. TLS cert at `CertificateStore/udap-tutorial-dev-tls-cert.pfx` (password: `udap-test`) is generated from `DevDaysCA_1.crt`.

## Key Dependencies

- `Udap.Server`, `Udap.Metadata.Server`, `Udap.UI` — UDAP protocol implementation (from udap-dotnet)
- `Duende.IdentityServer` 7.4.6 — OAuth/OIDC server framework
- `brianpos.Fhir.R4B.DemoFileSystemFhirServer` — FHIR server implementation
- `Portable.BouncyCastle` — Certificate generation in PKI project
- `Serilog` — Structured logging across all services

## Important Caveats

- After regenerating certificates with `udap.pki.devdays`, delete the auth server SQLite database and restart. On Windows, also clear cached intermediate certs via MMC (Certificates snap-in → Intermediate Certification Authorities → delete DevDaysSubCA_1).
- Windows and Linux cache CRL requests and may cache intermediate certificates, which can cause confusing validation failures during development.
