# tfsaggregator-rest

REST-based fork of TFS Aggregator that removes dependency on the deprecated Work Item Tracking (WIT) Object Model / SOAP APIs.

This project modernizes the original TFS Aggregator architecture to support Azure DevOps Server 2022.2 by replacing legacy `WorkItemStore` / `WorkItem` usage with Azure DevOps REST API calls while preserving the existing plugin model and rule DSL where practical.

---

## Why This Fork Exists

Azure DevOps Server 2022.x deprecated / removed portions of the legacy WIT Object Model relied upon by the original TFS Aggregator project.

This fork provides a migration path for teams who still depend on TFS Aggregator-style rule processing but need compatibility with newer Azure DevOps Server versions.

---

## Why Not Aggregator v3?

The original TFS Aggregator project evolved toward an external execution model using Azure Functions / Docker-based hosting and Azure DevOps Service Hooks.

While that model is appropriate for many environments, it is not always practical for tightly controlled on-premises Azure DevOps Server deployments.

This fork exists specifically for environments where:

- Azure Functions / Docker hosting is unavailable or undesirable
- In-process server plugin execution is preferred
- Administrators want centralized deployment on the Azure DevOps Server host
- Per-project webhook / service hook configuration is operationally burdensome
- Full control of the on-premises application tier makes server plugin deployment viable

For these scenarios, preserving the original plugin-based execution model remains a practical and efficient approach.

---

## Current Supported Scope

This fork currently supports the primary scenarios needed for rollup / automation-style rules:

- Child → Parent rollups
- Parent / Child traversal (`self.Parent`, `parent.Children`)
- Field read / write operations
- Existing rule DSL syntax compatibility for supported scenarios
- Azure DevOps Server 2022.2

---

## Current Limitations

This fork does **NOT** currently attempt full parity with the original TFS Aggregator WIT Object Model implementation.

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

## Architecture Changes

Key modernization changes include:

- Replaced legacy `WorkItemStore` / `WorkItem` SOAP Object Model usage with Azure DevOps REST APIs
- Preserved Azure DevOps Server plugin integration model
- Preserved existing rule DSL / scripting model where possible
- Added Azure DevOps Server 2022.2 build and installer support

---

## Relationship to Original Project

This project is based on the original TFS Aggregator:

https://github.com/tfsaggregator/tfsaggregator

All credit for the original design and implementation goes to the original project contributors.

---

## Project Status

This fork is actively maintained on a best-effort basis for practical Azure DevOps Server modernization scenarios.

It should currently be considered:

> **Focused / Partial Compatibility Fork**

rather than a drop-in full replacement for every original TFS Aggregator scenario.

---

## Contributing

Contributions are welcome, especially for expanding REST-based feature parity while maintaining Azure DevOps Server compatibility.

---

## License

This project remains licensed under the original MIT license.