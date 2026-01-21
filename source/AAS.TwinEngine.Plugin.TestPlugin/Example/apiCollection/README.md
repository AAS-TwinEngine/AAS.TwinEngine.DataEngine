# Bruno API Testing Setup – DataEngine (.NET Backend)

## Overview

This directory contains the Bruno collection and instructions to test the **AAS.TwinEngine.DataEngine** .NET API using Bruno. The collection includes pre-configured requests and environments to exercise the DataEngine API and its plugin-based data sources.

---

## Quick Summary

| Item                     | Description                                         |
|--------------------------|-----------------------------------------------------|
| **API**                  | `AAS.TwinEngine.DataEngine` (.NET)                  |
| **Testing Tool**         | [Bruno](https://www.usebruno.com/downloads)         |
| **Default API URL**      | `local` `http://localhost:8085` <br> - started with Docker compose  <br>   `local_net` `https://localhost:5059` <br> - started with devleopment environment (dotnet run)                        |
| **SDK Required**         | .NET 8 (recommended)                                |
| **Run docker compose file**           |  Run `docker-compose up -d` from AAS.TwinEngine.Plugin.TestPlugin Example folder              |

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

Before starting , run twinengine environmnet with plugin.
[click here for getting starated with docker-compose](../README.md)

### 2. Start the DataEngine .NET API

**Annotation:** To avoid complications you should deactivate the running DataEngine started with Docker compose. 

Run the API:

```bash
cd source\AAS.TwinEngine.DataEngine
dotnet run
```

By default the API listens at `https://localhost:5059` unless overridden by environment settings or `launchSettings.json`.

---

## Bruno Collection — Quick Start

1. Open Bruno
2. `Collection -> Open Collection` and choose the Bruno collection folder (`apiCollection`) from the AasTwin.DataEngine repository
3. From the top-right environment dropdown select an environment: `local` or `local_net` (use `local_net` for local testing with development environment)
4. Expand folders to find requests, select a request and click **Send**
5. Inspect the request/response in the right panel

---

## Bruno environment & collection variables

The collection includes a set of environment/collection variables you can edit to point the requests at your local or dev instance.
**Enter these variables in plain text — the collection’s Pre-request script will automatically change value to  Base64-encode.**

| Variable name                   | Purpose                                                                     | Example value                                                                      |
| ------------------------------- | --------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| `DataEngineBaseUrl`             | Base URL for DataEngine API                                                 | `https://localhost:5059`                                                           |
| `aasIdentifier`                 | Base64-encoded AAS identifier | `https://mm-software.com/ids/aas/000-001`(this will be convered to base64 by script in bruno)                             |
| `submodelIdentifierContact`     | Base64-encoded submodel identifier for ContactInformation        | `https://mm-software.com/submodels/000-001/ContactInformation`((this will be convered to base64 by script in bruno)) |
| `submodelIdentifierNameplate`   | Base64-encoded submodel identifier for Nameplate    | `https://mm-software.com/submodels/000-001/Nameplate`(this will be convered to base64 by script in bruno)            |
| `submodelIdentifierReliability` | Base64-encoded submodel identifier for Reliability| `https://mm-software.com/submodels/000-001/Reliability`(this will be convered to base64 by script in bruno)         |

Default values are set as shown.

---

## Default api-test configuration

* The default configuration includes three shell descriptors with these IDs:

  * `https://mm-software.com/ids/aas/000-001`
  * `https://mm-software.com/ids/aas/000-002`
  * `https://mm-software.com/ids/aas/000-003`

* Default submodel templates (under `/Example/aas`):

  * `ContactInformationAAS`
  * `DigitalNameplateAAS`
  * `Reliability`

* Default shell template used by all four shells:

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
        { "type": "Submodel", "value": "Reliability" }
      ]
    }
  ],
  "modelType": "AssetAdministrationShell"
}
```

By default, DataEngine requests the dev Template Repository, Submodel Registry, and AAS Registry (Azure-backed in the `dev` environment).

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
