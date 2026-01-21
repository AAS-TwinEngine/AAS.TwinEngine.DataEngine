# Core Concepts 

This document explains the core concepts of **DataEngine**, how it interacts with **template services** and the **Plugin**, and how the four AAS-aligned APIs (AAS Registry, AAS Repository, Submodel Registry, Submodel Repository) work together.

---

## High-Level Idea

**DataEngine** is a .NET-based API that dynamically generates complete **Asset Administration Shell (AAS)** submodels.

It does this by:
- Fetching **submodel templates** (without values) from a **template AAS environment**.
- Requesting **values for semantic IDs** from a separate **Plugin API** using a **JSON Schema–based contract**.
- Filling those values into the template and returning a fully populated, IDTA-compliant submodel to the caller.

DataEngine’s HTTP endpoints are designed to follow the **IDTA / AAS specification structure**:
- When a user asks for a **shell descriptor**, **shell**, **submodel descriptor** or **submodel / submodel element**, the URL structure and payloads are aligned with the AAS standard.

---

## Architecture Overview


### Component Roles

- **AAS Viewer / UI**: Visualizes AAS, shells, submodels and submodel elements for users.
- **API Gateway**: Single entry point (e.g. Nginx) that forwards HTTP calls to DataEngine.
- **DataEngine**: Core service that exposes AAS-aligned endpoints and orchestrates templates + plugin data.
- **Plugin**: Separate API that holds / accesses the **actual values** (in Plugin DB) and responds to DataEngine with data according to JSON Schema.
- **Plugin DB**: Database storing the customer’s real submodel/submodel-element values.
- **Template Registry / Template Repo**: Provide template shells/submodels and registry information that DataEngine uses as the structural basis.

---

Below sections correspond to the `/Api` folders in the DataEngine project:

- [DataEngine](#dataengine)
- [Submodel Repository](#submodel-repository)
- [Submodel Registry](#submodel-registry)
- [AAS Registry](#aas-registry)
- [AAS Repository](#aas-repository)
- [Plugin](#plugin)
- [Multi-Plugin](#multi-plugin)

Each of these is implemented in `source/AAS.TwinEngine.DataEngine/Api/...` and aligned with the IDTA / AAS specifications.

---

## DataEngine

- DataEngine is **.NET API** exposing multiple HTTP endpoints that follow **AAS / IDTA naming and structure**.
- It does **not store business data itself**.
- Instead it:
  - Understands AAS templates and semantic IDs.
  - Knows where to get **templates** (template repository / registry).
  - Knows where to get **values** (Plugin API backed by Plugin DB).
  - Combines both to answer AAS-compliant requests.

### Request Processing Flow (Submodel Example)

1. **Client request**
   - User (via UI or API client) calls a **Submodel Repository** endpoint on DataEngine, e.g. `GET /submodels/{submodelId}`.

2. **Template retrieval**
   - DataEngine resolves which **template AAS / submodel** must be used.
   - It calls the **template AAS environment / template submodel repository** to fetch the **template submodel**.
   - This template contains **semantic IDs** but **no values**.

3. **JSON Schema request to Plugin**
   - DataEngine builds a **JSON Schema** representing the semantic IDs and expected structure.
   - It sends a request to the **Plugin API** containing:
     - Which **submodel / submodel elements** it needs.
     - The **semantic IDs** and structure as JSON Schema.

4. **Plugin logic + DB**
   - Plugin checks its **Plugin DB** for values corresponding to those semantic IDs.
   - If values exist, Plugin returns them in a JSON structure that matches the agreed **schema**.

5. **Template filling**
   - DataEngine validates the Plugin response against the JSON Schema.
   - It **fills the values** into the previously fetched template submodel.

6. **Response to client**
   - DataEngine returns a **fully populated AAS submodel** that is ready to be consumed by UIs or other APIs.

### Plugin Responsibilities

- Plugin is a **separate API** maintained by customer.
- It:
  - Exposes an HTTP API that follows the **JSON Schema contract** provided by DataEngine.
  - Describes its **database structures as JSON Schema** (for submodels and submodel elements).
  - Stores the **actual values** in Plugin DB.
- **TwinEngine team** provides the **schema and contracts** for the Plugin, not the data itself.

---

## Submodel Repository

> Folder: `source/AAS.TwinEngine.DataEngine/Api/SubmodelRepository`

### Purpose

The **Submodel Repository** API provides **read access** to **submodels and submodel elements**, following the AAS/IDTA patterns. It is the main entry point for
clients that want **values** for a specific submodel.

### Main Endpoints

The actual route attributes are defined in the controllers, but conceptually they follow the IDTA standard and look like:

- `GET /submodels/{submodelId}`
  - Returns a full AAS **Submodel** with all populated elements.

- `GET /submodels/{submodelId}/submodel-elements/{idShortPath}`
  - Returns a single **SubmodelElement** (e.g. a Property, Range, Collection) identified by `idShortPath`.

These endpoints:
- Accept IDs compatible with AAS ID formats.
- Return JSON representations aligned with **AAS 3.0 / IDTA** models.

Visit below page to see the supported submodel elements and response.
---LINK---

### Template Requirements (from Template Repository)

For DataEngine to work correctly, the **template submodels** in the template repository must:

- Follow the **AAS / IDTA** representation of submodels.
- Define **semantic IDs** for all relevant submodel elements.
- Provide correct **idShort** values matching what UI / clients expect.
- Not contain concrete values – they are **templates only**.

DataEngine expects at least:
- A template **Submodel** with:
  - `idShort` = logical name (e.g. `Nameplate`, `Reliability`, `ContactInformation`).
  - `semanticId` = template semantic reference (IDTA template ID).
- For each submodel element:
  - A valid `semanticId` which the Plugin understands and can map to data fields.

### AAS Template Repository

For submodels, the **AAS Template Repository** stores **submodel templates** :

DataEngine never hardcodes templates; instead it always resolves and fetches them from this repository using a **templateId**.

### Mapping Templates by Submodel ID

When a client calls `GET /submodels/{submodelId}`, DataEngine must efficiently find the right **templateId**:

1. It reads **mapping rules** from configuration (for example `appsettings.json`).
2. Each rule links a `templateId` to one or more **regex patterns** that match submodel IDs:

```json
[
  {
    "templateId": "nameplateTemplateId",
    "pattern": ["Nameplate$", "DigitalNameplate$"]
  },
  {
    "templateId": "contactInfoTemplateId",
    "pattern": ["ContactInformation$", "MoreContact$"]
  }
]
```

3. For an incoming `submodelId`, DataEngine selects the first rule whose `pattern` regex matches and uses the corresponding `templateId`.
4. With that `templateId`, it calls the BaSyx template repository, for example:
   - `GET /submodels/{templateId}` – to fetch the submodel template JSON.

If **no rule matches** the provided `submodelId`, DataEngine throws a **NotFound** error (for example a `NotFoundException`) to indicate that
no template is configured for that submodel.

> When mappings change, the engine must be restarted or the configuration reloaded so that new template rules are applied.

### Template Repository Endpoints Used

Conceptually, DataEngine needs endpoints (from BaSyx template environment) such as:

- `GET /shells/{templateShellId}` – to find which submodels belong to a template shell.
- `GET /submodels/{templateSubmodelId}` – to get the actual **template submodel structure**.

The exact URLs depend on your BaSyx deployment but are configured via
`AasEnvironment` settings in DataEngine’s configuration.

---

## Submodel Registry

> Folder: `source/AAS.TwinEngine.DataEngine/Api/SubmodelRegistry`

### Purpose

The **Submodel Registry** API allows clients to **discover which submodels exist**, their IDs, and semantics. It does **not** return submodel values, only
**descriptors**.

### Main Endpoints (Conceptual)

- `GET /submodel-descriptors`
  - Query Parameters:
    - limit: int
    - cursor: string
  - Supports paging parameters like `limit` and `cursor`.
  - Returns a collection of **SubmodelDescriptor** objects.

- `GET /submodel-descriptors/{submodelIdentifier}`
  - Returns a single **SubmodelDescriptor**.

### Expected Response Format

A typical **SubmodelDescriptor** includes:
- `id` – global identifier of the submodel.
- `idShort` – human-readable short ID.
- `semanticId` – semantic reference for the submodel.
- Information about endpoints where the actual submodel can be fetched.

### Specifications

- Follows the **AAS 3.0 / IDTA** standard for **SubmodelDescriptor** objects.
- Includes necessary metadata for client tools (like AAS Viewer) to
  know **where** and **how** to retrieve submodels.

### Template Registry Endpoints Required

DataEngine itself may rely on a **template-oriented registry** (BaSyx) for template descriptors, using endpoints such as:

- `GET /shell-descriptors` – for template shells.
- `GET /submodel-descriptors` – for template submodels.

These are configured via the `AasEnvironment` settings and are used internally to
know which templates exist and how to resolve them by ID / semantic.

---

## AAS Registry

> Folder: `source/AAS.TwinEngine.DataEngine/Api/AasRegistry`

### Purpose

The **AAS Registry** API exposes **Shell Descriptors**. It allows clients to:
- Discover which **Asset Administration Shells** exist.
- Retrieve descriptors for specific shells.
- Follow AAS-aligned patterns similar to BaSyx.

### Main Endpoints (Conceptual)

- `GET /shell-descriptors`
  - Returns a paginated list of shell descriptors.

- `GET /shell-descriptors/{aasIdentifier}`
  - Returns a single shell descriptor.

### Expected Response Format

A typical **ShellDescriptor** includes:
- `id` – global AAS identifier.
- `idShort` – human-readable short name.
- `description` – multilingual descriptions.
- `submodelDescriptors[]` – links to registered submodels.

### Shell Descriptor Template Requirements

Template shells (in the template registry) must:
- Have stable **AAS IDs** and **idShorts**.
- Reference template submodels for each relevant aspect (e.g. Nameplate, Reliability).
- Follow the AAS 3.0 structure so DataEngine can map them consistently.

### Shell Descriptor Creation Modes 

DataEngine supports two ways of creating and keeping Shell Descriptors up to date:

- **On Request**
  - When a client calls `GET /shell-descriptors` or `GET /shell-descriptors/{aasIdentifier}`,
    DataEngine asks the **Plugin** for shell metadata (`/shells-metadata`),
    fills a Shell Descriptor **template** from the template repository with that metadata,
    and returns the computed descriptor(s) directly to the caller.

- **Precomputed**
  - A background scheduler in DataEngine runs on a configured **cron** schedule.
  - It calls the Plugin (`GET /shells-metadata`), transforms the metadata into Shell Descriptors
    using the same template, and then synchronizes them with the **AAS Registry** via
    `POST /shell-descriptors`, `PUT /shell-descriptors/{aasIdentifier}` and
    `DELETE /shell-descriptors/{aasIdentifier}`.
  - This keeps the external AAS Registry in sync with the Plugin data without user interaction.

### Template Registry Endpoints Used

For working with shell templates, DataEngine expects BaSyx-like endpoints such as:

- `GET /shell-descriptors` – to list shell templates.
- `GET /shell-descriptors/{aasIdentifier}` – to get a specific shell template descriptor.

These are again configurable via `AasEnvironment` options.

---

## AAS Repository

> Folder: `source/AAS.TwinEngine.DataEngine/Api/AasRepository`

### Purpose

The **AAS Repository** API provides access to complete **Asset Administration Shells** and their **asset information**. It ties together:
- Shell metadata (from registry / templates).
- Submodel references / descriptors.
- Populated submodel data fetched via the Submodel Repository and Plugin.

### Main Endpoints (Conceptual)

- `GET /shells/{aasIdentifier}`
  - Returns the full AAS object, including references to submodels.

- `GET /shells/{aasIdentifier}/asset-information`
  - Returns the AssetInformation section of the AAS.

These follow the AAS 3.0 conventions and integrate with BaSyx-style endpoints.

### Expected Response Format

- `GET /shells/{aasIdentifier}` returns an **AssetAdministrationShell** JSON containing:
  - `id`, `idShort`
  - `assetInformation`
  - `submodels[]` – references / links to submodels.

- `GET /shells/{aasIdentifier}/asset-information` returns:
  - `assetKind`
  - `globalAssetId` / `specificAssetIds`
  - Other AAS-compliant asset metadata.

### Template Requirements

- Template shells must be defined in the template repository with:
  - Proper references to their template submodels.
  - Correct semantic IDs for cross-linking.
- DataEngine then uses these to build a full runtime AAS by combining:
  - Shell descriptor and shell template from BaSyx.
  - Populated submodels that were filled using Plugin data.

### Shell Retrieval Flow (Short)

When a client (for example an AAS Viewer) calls `GET /shells/{shellIdentifier}`, DataEngine does **not** return a stored shell. Instead it
**constructs the shell on the fly** based on configuration, templates and Plugin metadata:

1. **Extract Asset Reference**  
  DataEngine applies configured `AasIdExtractionRules` to the incoming `shellIdentifier` (pattern, separator, index) to derive an internal
  asset reference.

2. **Resolve Shell Template**  
  Using configured shell template mapping rules (for example a single `"*"` pattern mapping to one templateId), DataEngine resolves which
  **shell template** to use. That template lives in the **AAS Template Repository** and typically only contains submodel references
  (idShort + semanticId), not values.

3. **Fetch Template & Asset Metadata**  
  DataEngine:
  - Calls the Template Repository to fetch the shell template by `templateId`.
  - Calls the Plugin service (for example `GET /metadata/assets/{shellIdentifier}`) to retrieve **assetInformation metadata** for that shell.

4. **Assemble Shell**  
  DataEngine merges **template + asset information** into a complete `AssetAdministrationShell` JSON and returns it on `/shells/{shellIdentifier}`.

This allows you to define lightweight, reusable **shell templates** (with just submodel names/semanticIds) in the template repository, while all
runtime-specific data (asset information and submodel contents) comes from the Plugin.

### Template Repository Endpoints Used

DataEngine typically uses endpoints such as (BaSyx style):

- `GET /shells/{templateAasId}` – to get the template shell.
- `GET /shells/{templateAasId}/submodels` – to discover attached template submodels.

These are configured via `AasEnvironment` in DataEngine.

---

## Plugin

### Purpose

The **Plugin** is a separate API and deployment that acts as the **primary data source** for DataEngine:

- Holds or connects to the **actual data storage** (Plugin DB).
- Exposes HTTP endpoints that understand **semantic IDs** and **JSON Schema** sent by DataEngine.
- Returns data and metadata in a shape that DataEngine can map into AAS submodels and submodel elements.

DataEngine itself remains stateless with respect to business values; it only orchestrates between **templates** and **Plugin data**.

### Endpoints (Conceptual)

>The concrete URL structure lives in the Plugin implementation (for example the DPP-Plugin), but conceptually a Plugin exposes endpoints that:

- Accept a **semantic ID** (or set of semantic IDs).
- Accept a **JSON Schema** (or schema reference) describing the expected structure.
- Return **values** for submodel and submodel elements matching that schema.

Typical patterns are:

- `POST /data` or `POST /submodels/{submodelId}`
  - Request body contains:
    - The target **submodel / AAS context** (for example aasId, submodelId).
    - A list of **semantic IDs** and structure (JSON Schema).
  - Response body contains:
    - Values for each requested semanticId.
    - Optional metadata such as timestamps, quality, units.

### Expected Response (Conceptual)

The Plugin response should:

- Conform to the **JSON Schema** provided by DataEngine.
- Include **typed values** for each semantic ID.
- Provide **metadata** such as timestamps, units, quality flags, and source information, depending on the use case.

### Plugin Manifest

Each Plugin exposes its capabilities to DataEngine via a **Plugin Manifest**. The manifest tells DataEngine:

- Which **semantic IDs** the plugin can provide values for.
- Whether the plugin can provide **Shell Descriptors** and/or **Asset Information**.
- Where the plugin is hosted (URL) and how it should be addressed.

Conceptually, a manifest entry contains at least the following fields:

```json
{
  "supportedSemanticIds": [
    "http://example.com/idta/digital-nameplate/thumbnail",
    "http://example.com/idta/digital-nameplate/contact-name",
    "http://example.com/idta/digital-nameplate/email"
  ],
  "capabilities": {
    "hasShellDescriptor": true,
    "hasAssetInformation": false
  }
}
```

#### Field Overview

- `supportedSemanticIds`  
  List of semantic IDs that this plugin can serve. DataEngine uses this information to decide **which plugin(s)** to call for a given
  submodel or submodel element.

- `capabilities`  
  Flags describing what kind of data the plugin can provide:
  - `hasShellDescriptor` – plugin can provide metadata required to build **Shell Descriptors**.
  - `hasAssetInformation` – plugin can provide **assetInformation** content for shells.

#### How DataEngine Uses the Manifest

- At startup, DataEngine loads one or more Plugin Manifests.
- For each request, it builds a **semantic tree** of required IDs and uses the manifests to:
  - Select the appropriate plugin(s) that support those semantic IDs.
  - Apply **Multi-Plugin** conflict rules when multiple plugins claim the same semantic ID.
- This allows adding or replacing plugins **without changing DataEngine code**—only the manifest configuration needs to be updated.

### Relation to DPP-Plugin

- A concrete implementation of this concept is the **DPP-Plugin** (Digital Product Passport Plugin).
- DPP-Plugin implements:
  - The **Plugin contract** expected by DataEngine.
  - A concrete database schema for storing submodel / submodel element values.
- See the separate **DPP-Plugin** repository for:
  - LINK

---

## Multi-Plugin

### Purpose

In many scenarios, you may want to split data across multiple plugins (for example one for sensor data, one for maintenance, one for configuration).

**Multi-Plugin support** allows DataEngine to:

- Call **more than one Plugin** for the same request.
- Merge their responses.
- Resolve conflicts deterministically.

Configuration is done via `PluginConfig.Plugins` and `MultiPluginConflictOption` in the DataEngine configuration.

### How Multiple Plugins Work Together

1. **Configured plugins**
   - In configuration you define multiple plugin entries, each with:
     - `PluginName`
     - `PluginUrl`

2. **Request fan-out**
   - When DataEngine needs values for a submodel / semantic IDs, it can:
     - Call one specific plugin, or
     - **Fan out** the same request to multiple plugins.

3. **Aggregation**
   - DataEngine collects all responses and builds a combined data set, keyed by semantic IDs or element identifiers.

### Orchestration & Conflict Resolution

When multiple plugins return values for the **same semantic ID** or submodel element, DataEngine uses the configured conflict strategy (from
`MultiPluginConflictOption.HandlingMode`):

- `TakeFirst`
  - Take the value from the **first plugin** that provided a value.
  - Remaining values for the same ID are ignored.

- `SkipConflictingIds`
  - If more than one plugin returns a value for the same ID, **drop that ID entirely** from the combined result.
  - Non-conflicting IDs are still used.

- `ThrowError`
  - If multiple plugins provide a value for the same ID, DataEngine returns an **error**.
  - The client can then decide how to handle or reconfigure plugins.

### Typical Patterns

- **Functional separation**
  - One plugin for **master data** (nameplate, contact information).
  - Another plugin for **operational data** (temperatures, usage hours).

- **Organizational separation**
  - Different business units own different plugins but the **same DataEngine**.

In all cases, DataEngine keeps a single, AAS-compliant interface for clients, hiding the complexity of multiple underlying plugins.

---