# Commercial Licensing

> DRAFT — REQUIRES PROFESSIONAL LEGAL REVIEW

This document describes the current vendor-controlled commercial licensing process. It is not a final commercial contract, does not grant rights by itself, and has not been approved by professional legal counsel.

## Overview

Production or live use requires vendor-approved commercial authorization and an appropriate vendor-issued signed license. Commercial terms are agreed outside the application.

## Current Licensing Model

- offline signed technical entitlement
- vendor-controlled private signing key kept outside the application and repository
- server-side verification using trusted public keys only
- startup license loading and validation
- Community, Professional, and Enterprise technical editions
- explicit signed features remain authoritative
- missing or invalid licenses enter restricted Community behavior
- machine binding is not implemented; it remains deferred
- no online activation, customer portal, automated billing, or licensing server is currently implemented
- no online revocation is implemented

## Customer Process

1. Customer contacts the vendor.
2. Vendor reviews requirements, edition, features, limits, and intended use.
3. Commercial terms are agreed through the approved business process.
4. An authorized vendor operator issues the signed license offline.
5. The license is delivered through an approved controlled channel.
6. The customer installs it at the configured `Licensing:LicenseFilePath` location.
7. The application is restarted or reloaded according to the approved operational procedure.
8. The application validates the license at startup.
9. Support, renewal, replacement, and recovery follow the approved vendor/customer process.

License replacement currently requires an application restart/reload. Hot reload and per-request license revalidation are not implemented.

## No Online Activation

Online activation, online revocation, automated billing, customer self-service, and a licensing server are not currently implemented or approved by Phase 5.2. The current model remains offline-first.

## Evaluation vs Production

See [Evaluation Use](Evaluation-Use.md) for the draft evaluation policy. Evaluation access does not itself authorize production, live, customer-facing, commercial-service, redistribution, sublicensing, resale, competing-product, or general modification use.

See [Phase 5.3 — Commercial Licensing and Customer Workflow](plans/Phase-5/Phase-5.3-Commercial-Licensing-and-Customer-Workflow.md) for the draft vendor-side request, approval, issuance, delivery, installation, replacement, renewal, support, and offboarding workflow. That document is an operational draft, not a legal contract.

## Official Build Trust

Official releases should be accompanied by a vendor-controlled record containing the product version, Git commit SHA, build timestamp, CI evidence/reference, release manifest, artifact filename, SHA-256 checksum, and release owner/operator record. A checksum alone is not proof of publisher identity unless the manifest and artifact record are authenticated separately.

## Contact

For commercial requirements and licensing requests, use the approved vendor process: `<COMMERCIAL_CONTACT>`.

The contact placeholder requires business completion and is not an approved contact address.
