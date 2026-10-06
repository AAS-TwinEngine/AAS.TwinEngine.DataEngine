# Bruno API Testing Setup – DataEngine (.NET Backend)

## Overview

This directory contains the Bruno collection and instructions to test the **AAS.TwinEngine.DataEngine** .NET API using Bruno. The collection includes pre-configured requests and environments to exercise the DataEngine API and its plugin-based data sources.

---

## Quick Summary

| Item                        | Description                                                     |
| --------------------------- | --------------------------------------------------------------- |
| **API**                     | `AAS.TwinEngine.DataEngine` (.NET)                              |
| **Testing Tool**            | [Bruno](https://www.usebruno.com/downloads)                     |
| **Default API URL**         | `http://localhost:8080`                                         |
| **SDK Required**            | .NET 8 (recommended)                                            |
| **Run docker compose file** | Run `docker-compose-up` [form AasTwin.DataEngine](../README.md) |

---

## Prerequisites

1. **Install Bruno**
   - Download: [https://www.usebruno.com/downloads](https://www.usebruno.com/downloads)
   - Platforms: Windows, macOS, Linux

2. **Install .NET SDK**
   - Recommended: **.NET 8** (install from Microsoft docs)

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

Selecting an environment only loads that environment's variables. It does not change the collection authentication mode.

---

## Bruno environment & collection variables

The collection includes a set of environment/collection variables you can edit to point the requests at your local or dev instance.
**Enter these variables in plain text — the collection’s Pre-request script will automatically change value to Base64-encode.**

| Variable name                                 | Purpose                                                        | Example value                                                      |
| --------------------------------------------- | -------------------------------------------------------------- | ------------------------------------------------------------------ |
| `DataEngineBaseUrl`                           | Base URL for DataEngine API                                    | `http://localhost:8080`                                            |
| `productId-1`                                 | Product ID for first asset                                     | `000-001`                                                          |
| `productId-2`                                 | Product ID for second asset                                    | `000-002`                                                          |
| `productId-3`                                 | Product ID for third asset                                     | `001-001`                                                          |
| `aasIdentifier-1`                             | AAS identifier (auto-encoded to Base64 by script)              | `https://mm-software.com/ids/aas/000-001`                          |
| `aasIdentifier-2`                             | AAS identifier (auto-encoded to Base64 by script)              | `https://mm-software.com/ids/aas/000-002`                          |
| `aasIdentifier-3`                             | AAS identifier (auto-encoded to Base64 by script)              | `https://mm-software.com/ids/aas/001-001`                          |
| `submodelIdentifierMaintenanceInstructions-1` | Submodel identifier for MaintenanceInstructions (auto-encoded) | `https://mm-software.com/submodel/000-001/MaintenanceInstructions` |
| `submodelIdentifierNameplate-1`               | Submodel identifier for Nameplate (auto-encoded)               | `https://mm-software.com/submodel/000-001/Nameplate`               |
| `submodelIdentifierTechnicalData-1`           | Submodel identifier for TechnicalData (auto-encoded)           | `https://mm-software.com/submodel/000-001/TechnicalData`           |
| `submodelIdentifierCarbonFootprint-1`         | Submodel identifier for CarbonFootprint (auto-encoded)         | `https://mm-software.com/submodel/000-001/CarbonFootprint`         |
| `submodelIdentifierHandoverDocumentation-1`   | Submodel identifier for HandoverDocumentation (auto-encoded)   | `https://mm-software.com/submodel/000-001/HandoverDocumentation`   |

**Note:** All identifier variables (aasIdentifier-_, submodelIdentifier-_) are automatically Base64-encoded by the collection's pre-request script. Enter plain URLs as shown above.

---

## Authentication (Keycloak) for the Secured example

The collection includes the OAuth 2.0 (Resource Owner Password) settings for the Keycloak instance of the [AAS.TwinEngine.Secured](../../AAS.TwinEngine.Secured/README.md) example. OAuth 2.0 is enabled in the collection by default. The OAuth fields are stored in `collection.bru`; if Bruno does not display them after an external file change, reload or reopen the collection.

### Use it

1. Start the secured example (`docker compose up -d` in `examples/AAS.TwinEngine.Secured`).
2. In Bruno select the `secured` environment.
3. Confirm the collection auth mode is **OAuth 2.0**. If Bruno still shows **No Auth**, reload or reopen the collection so it reads the current `collection.bru` file.

Selecting `secured` makes the variables below available to Bruno. Bruno resolves the OAuth mappings, fetches the token, caches it, refreshes it automatically, and injects `Authorization: Bearer <token>` into all downstream requests. Every folder and request uses `auth: inherit`.

The `secured` environment must be selected before sending requests so the Keycloak variables resolve correctly.

### Auth variables (`environments/secured.bru`)

| Variable name      | Purpose                 | Example value                                                               |
| ------------------ | ----------------------- | --------------------------------------------------------------------------- |
| `keycloakTokenUrl` | Keycloak token endpoint | `http://localhost:9090/realms/basyx/protocol/openid-connect/token` |
| `authClientId`     | Public Keycloak client  | `basyx-ui`                                                                  |
| `authUsername`     | Realm user              | `admin` (full access) or `usera` (viewer)                                   |
| `authPassword`     | Realm user password     | `pwd`                                                                       |

These are local demo credentials only. For anything non-local, move `authPassword` into Bruno's secret variables instead of committing it.

### Keycloak OAuth variable mapping

The collection stores the following OAuth 2.0 mappings:

| Bruno OAuth 2.0 field       | Value to enter             | Resolved value in the secured environment                              |
| --------------------------- | -------------------------- | ----------------------------------------------------------------------- |
| Grant type                  | `Password Credentials`     | `Password Credentials`                                                  |
| Access token URL            | `{{keycloakTokenUrl}}`     | Keycloak realm token endpoint                                           |
| Refresh token URL           | `{{keycloakTokenUrl}}`     | Keycloak realm token endpoint                                           |
| Username                    | `{{authUsername}}`         | `usera`                                                                 |
| Password                    | `{{authPassword}}`         | `pwd`                                                                   |
| Client ID                   | `{{authClientId}}`         | `basyx-ui`                                                              |
| Client secret               | Leave empty                | Empty; the demo client is public                                       |
| Scope                       | `openid profile`           | `openid profile`                                                        |
| Credentials placement       | `Body`                     | `Body`                                                                  |
| Token placement             | `Header`                   | `Header`                                                                |
| Token header prefix         | `Bearer`                   | `Bearer`                                                                |

### Switching users

Change `authUsername` in the `secured` environment, then clear the cached token via **Settings -> Auth -> Clear Cache** (or just re-send; Bruno re-fetches when the token expires).

### Turning it off

Set the collection auth mode back to **No Auth**, or set a single request's auth to **No Auth** to bypass the token for that request only.

---

## Default api-test configuration

- The default configuration includes four shell descriptors with these IDs:
  - `https://mm-software.com/ids/aas/000-001`
  - `https://mm-software.com/ids/aas/000-002`
  - `https://mm-software.com/ids/aas/001-001`

- Default submodel templates (under `../aas`):
  - `MaintenanceInstructions`
  - `Nameplate`
  - `HandoverDocumentation`
  - `CarbonFootprint`
  - `TechnicalData`

- Default shell template used by all 5 shells:

```json
{
  "id": "https://mm-software.com/aas/aasTemplate",
  "assetInformation": {
    "assetKind": "Instance"
  },
  "submodels": [
    {
      "type": "ModelReference",
      "keys": [{ "type": "Submodel", "value": "Nameplate" }]
    },
    {
      "type": "ModelReference",
      "keys": [{ "type": "Submodel", "value": "MaintenanceInstructions" }]
    },
    {
      "type": "ModelReference",
      "keys": [{ "type": "Submodel", "value": "HandoverDocumentation" }]
    },
    {
      "type": "ModelReference",
      "keys": [{ "type": "Submodel", "value": "CarbonFootprint" }]
    },
    {
      "type": "ModelReference",
      "keys": [{ "type": "Submodel", "value": "TechnicalData" }]
    }
  ],
  "modelType": "AssetAdministrationShell"
}
```

---

## Useful requests & folders

- **Aas Registry** — endpoints to get all ShellDescriptors and ShellDescriptor by id
- **Aas Repository** — endpoints to get Shell by id, SubmodelRef by id, Asset Information by id
- **Submodel Registry** — endpoints to get SubmodelDescriptor by id
- **Submodel Repository** — endpoints to get submodel, submodelElement, and serialization

(Each Bruno request contains example payloads.)

---

## Troubleshooting

#### Bruno shows `SSL/TLS handshake failed`

- Run `dotnet dev-certs https --trust`
- Ensure plugin and API endpoints match port and schema (`https://`)

---
