# tfsaggregator-rest

A REST-based modernization fork of TFS Aggregator for Azure DevOps Server 2022.2.

This project replaces the deprecated Work Item Tracking (WIT) Object Model / SOAP dependencies used by the original TFS Aggregator with Azure DevOps REST API calls while preserving the original in-process plugin execution model and existing rule DSL where practical.

---

## Why This Fork Exists

The original TFS Aggregator v2 relies on the legacy WIT Object Model (`WorkItemStore`, `WorkItem`, etc.), which is deprecated / incompatible with modern Azure DevOps Server environments.

This fork provides a migration path for teams that:

- Still rely on TFS Aggregator-style rule processing
- Need compatibility with Azure DevOps Server 2022.2
- Prefer to retain the original server plugin execution model

---

## Why Not Aggregator v3?

The TFS Aggregator project evolved toward an external execution model using Azure Functions / Docker and Azure DevOps Service Hooks.

While that architecture is appropriate for many environments, it is not always practical for tightly controlled on-premises Azure DevOps Server deployments.

This fork is intended for environments where:

- Azure Functions / Docker hosting is unavailable or undesirable
- In-process plugin execution is preferred
- Centralized deployment on the Azure DevOps Server host is desired
- Per-project webhook / service hook configuration is operationally burdensome
- Full control of the on-premises application tier makes server plugin deployment viable

---

## Current Supported Scope

This fork currently supports the primary scenarios needed for rollup / automation-style rules:

- Child → Parent rollups
- Parent / Child traversal (`self.Parent`, `parent.Children`)
- Field read / write operations
- Existing rule DSL compatibility for supported scenarios
- Azure DevOps Server 2022.2

---

## Current Limitations

This fork does **NOT** currently provide full parity with the original TFS Aggregator WIT Object Model implementation.

Not currently supported:

- WIQL / query-based rule scenarios
- Work item creation
- Full workflow / state transition metadata
- Arbitrary link manipulation beyond parent / child hierarchy
- Attachments / hyperlinks
- Global lists
- Full revision / history parity
- Complete WIT Object Model behavioral parity

---

## Architecture Overview

Execution Flow:

Azure DevOps Server Plugin  
→ Work Item Changed Event  
→ Aggregator Rule Engine  
→ REST-backed Work Item Repository  
→ Azure DevOps Server REST API

Key modernization changes:

- Replaced `WorkItemStore` / `WorkItem` SOAP APIs with REST API calls
- Preserved plugin-based execution model
- Preserved existing rule DSL where possible
- Added Azure DevOps Server 2022.2 build and installer support

---

## Relationship to Original Project

This project is based on the original TFS Aggregator:

https://github.com/tfsaggregator/tfsaggregator

Credit for the original design and implementation goes to the original TFS Aggregator contributors.

---

## Project Status

**Current Release:** `v0.1.0-alpha`

This project should currently be considered:

> **Focused / Partial Compatibility Fork**

It is intended for practical modernization scenarios rather than as a full drop-in replacement for every original TFS Aggregator feature.

---

## Contributing

Contributions are welcome, particularly for expanding REST-based feature parity while maintaining Azure DevOps Server compatibility.

---

## License

Licensed under the original MIT License.