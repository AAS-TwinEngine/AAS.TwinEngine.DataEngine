# Bruno API Testing Setup – DataEngine (.NET Backend)

## Overview

This directory contains the Bruno collection and instructions to test the **AAS.TwinEngine.DataEngine** .NET API using Bruno. The collection includes pre-configured requests and environments to exercise the DataEngine API and its plugin-based data sources.

---

## Quick Summary

| Item                     | Description                                         |
|--------------------------|-----------------------------------------------------|
| **API**                  | `AAS.TwinEngine.DataEngine` (.NET)                  |
| **Testing Tool**         | [Bruno](https://www.usebruno.com/downloads)         |
| **Default API URL**      | `http://localhost:8080`                            |
| **SDK Required**         | .NET 8 (recommended)                                |
| **Run docker compose file**           |  Run `docker-compose-up` [form AasTwin.DataEngine](../README.md)                |

---

## Prerequisites

1. **Install Bruno**

   * Download: [https://www.usebruno.com/downloads](https://www.usebruno.com/downloads)
   * Platforms: Windows, macOS, Linux

2. **Install .NET SDK**

   * Recommended: **.NET 8** (install from Microsoft docs)

3. **Install docker**

---

## Running the services


### 1. Run docker compose file 

Before starting , run twinengine environmnet with dpp-plugin.
[click here for getting starated with docker-compose](../README.md)


## Bruno Collection — Quick Start

1. Open Bruno
2. `Collection -> Open Collection` and choose the Bruno collection folder (`apiCollection`) from the AasTwin.DataEngine repository
3. From the top-right environment dropdown select an environment: `local`
4. Expand folders to find requests, select a request and click **Send**
5. Inspect the request/response in the right panel

---

## Bruno environment & collection variables

The collection includes a set of environment/collection variables you can edit to point the requests at your local or dev instance.
**Enter these variables in plain text — the collection’s Pre-request script will automatically change value to  Base64-encode.**

| Variable name                               | Purpose                                                    | Example value                                                                                      |
| ------------------------------------------- | ---------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `DataEngineBaseUrl`                         | Base URL for DataEngine API                                | `http://localhost:8080`                                                                            |
| `productId-1`                               | Product ID for first asset                                 | `000-001`                                                                                          |
| `productId-2`                               | Product ID for second asset                                | `000-002`                                                                                          |
| `productId-3`                               | Product ID for third asset                                 | `001-001`                                                                                          |
| `aasIdentifier-1`                           | AAS identifier (auto-encoded to Base64 by script)          | `https://mm-software.com/ids/aas/000-001`                                                          |
| `aasIdentifier-2`                           | AAS identifier (auto-encoded to Base64 by script)          | `https://mm-software.com/ids/aas/000-002`                                                          |
| `aasIdentifier-3`                           | AAS identifier (auto-encoded to Base64 by script)          | `https://mm-software.com/ids/aas/001-001`                                                          |
| `submodelIdentifierContact-1`               | Submodel identifier for ContactInformation (auto-encoded)  | `https://mm-software.com/submodel/000-001/ContactInformation`                                      |
| `submodelIdentifierNameplate-1`             | Submodel identifier for Nameplate (auto-encoded)           | `https://mm-software.com/submodel/000-001/Nameplate`                                               |
| `submodelIdentifierTechnicalData-1`         | Submodel identifier for TechnicalData (auto-encoded)       | `https://mm-software.com/submodel/000-001/TechnicalData`                                           |
| `submodelIdentifierCarbonFootprint-1`       | Submodel identifier for CarbonFootprint (auto-encoded)     | `https://mm-software.com/submodel/000-001/CarbonFootprint`                                         |
| `submodelIdentifierHandoverDocumentation-1` | Submodel identifier for HandoverDocumentation (auto-encoded) | `https://mm-software.com/submodel/000-001/HandoverDocumentation`                                   |

**Note:** All identifier variables (aasIdentifier-*, submodelIdentifier-*) are automatically Base64-encoded by the collection's pre-request script. Enter plain URLs as shown above.

---

## Default api-test configuration

* The default configuration includes four shell descriptors with these IDs:

  * `https://mm-software.com/ids/aas/000-001`
  * `https://mm-software.com/ids/aas/000-002`
  * `https://mm-software.com/ids/aas/001-001`

* Default submodel templates (under `../aas`):

  * `ContactInformation`
  * `Nameplate`
  * `HandoverDocumentation`
  * `CarbonFootprint`
  * `TechnicalData`

* Default shell template used by all 5 shells:

```json
{
  "id": "https://mm-software.com/aas/aasTemplate",
  "assetInformation": {
    "assetKind": "Instance"
  },
  "submodels": [
    {
      "type": "ModelReference",
      "keys": [
        { "type": "Submodel", "value": "Nameplate" }
      ]
    },
    {
      "type": "ModelReference",
      "keys": [
        { "type": "Submodel", "value": "ContactInformation" }
      ]
    },
    {
      "type": "ModelReference",
      "keys": [
        { "type": "Submodel", "value": "HandoverDocumentation" }
      ]
    },
    {
      "type": "ModelReference",
      "keys": [
        { "type": "Submodel", "value": "CarbonFootprint" }
      ]
    },
    {
      "type": "ModelReference",
      "keys": [
        { "type": "Submodel", "value": "TechnicalData" }
      ]
    }
  ],
  "modelType": "AssetAdministrationShell"
}
```

---

## Useful requests & folders

* **Aas Registry** — endpoints to get all ShellDescriptors and ShellDescriptor by id
* **Aas Repository** — endpoints to get Shell by id, SubmodelRef by id, Asset Information by id
* **Submodel Registry** — endpoints to get SubmodelDescriptor by id
* **Submodel Repository** — endpoints to get submodel, submodelElement, and serialization

(Each Bruno request contains example payloads.)

---

## Troubleshooting

#### Bruno shows `SSL/TLS handshake failed`

- Run `dotnet dev-certs https --trust`
- Ensure plugin and API endpoints match port and schema (`https://`)


---
