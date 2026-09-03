# Handling `contentSpecificationIds` in the DPP Metadata Submodel

## 1. Overview

The Digital Product Passport (DPP) Metadata Submodel contains the `contentSpecificationIds` element. This element is intended to contain the semantic identifiers of the other Submodels associated with the same Asset Administration Shell (AAS).

In the DPP Metadata Submodel template, `contentSpecificationIds` is represented as a `SubmodelElementList`. The list contains `Property` elements with the semantic ID `contentSpecificationId`.

This should have multiple `contentSpecificationId` values. For example, the list contains references for:

* Nameplate
* Maintenance Instructions
* Additional content specifications
* Carbon Footprint

Each value identifies another Submodel or content specification associated with the DPP.
Therefore, the `contentSpecificationIds` element is different from a normal DataElement because its purpose is to represent a collection of semantic references.

---

# 2. Problem Statement

The current DataEngine/plugin processing model does not handle this use case cleanly.

The main problem is:

> `contentSpecificationIds` needs to contain the semantic IDs of all relevant Submodels belonging to the same AAS, while the current plugin processing and DataEngine model expects supported DataElements to be processed according to the template definition.

The DPP Metadata Submodel therefore introduces a special case.

When the DPP Metadata Submodel is processed from a template, validation can fail because `contentSpecificationIds` is a SubmodelElementList containing a DataElement type/cardinality combination that is currently not fully supported by the plugin/DataEngine processing model.

The template explicitly demonstrates that the list can contain multiple `Property` elements. Each `Property` has the same `idShort` (`contentSpecificationId`) and uses `OneToMany` cardinality.

---

# 3. Expected Behaviour

For an AAS containing multiple Submodels, the `contentSpecificationIds` element should represent the Submodels associated with that AAS.

Conceptually:

```text
AAS
 ├── DPP Metadata Submodel
 │    └── contentSpecificationIds
 │         ├── Submodel A semantic ID
 │         ├── Submodel B semantic ID
 │         ├── Submodel C semantic ID
 │         └── Submodel D semantic ID
 │
 ├── Submodel A
 ├── Submodel B
 ├── Submodel C
 └── Submodel D
```

The values should therefore correspond to the semantic identifiers of the associated Submodels/content specifications.

The example template contains multiple `contentSpecificationId` properties, including values such as:

```text
https://admin-shell.io/idta/digitalproductpassport/Nameplate/1

https://admin-shell.io/idta/SubmodelTemplate/MaintenanceInstructions/1/0

0173-1#01-AHF578#003

0173-1#01-AHX837#002

https://admin-shell.io/idta/CarbonFootprint/CarbonFootprint/1/0
```

---

# 4. Implementation Options

Three possible approaches have been identified.

## Option 1 – Provide the values through the template

### Description

The `contentSpecificationIds` values could be defined directly in the DPP Metadata Submodel template.

The DataEngine would then process the template in the same way as other SubmodelElements.

### Problem

This approach creates a validation problem.

The `contentSpecificationIds` element contains a list of multiple DataElements. In particular, the list contains multiple `Property` elements with the same semantic meaning and `OneToMany` cardinality.

The current plugin processing model does not fully support this structure.

As a result:

```text
Template
   ↓
Template validation
   ↓
contentSpecificationIds
   ↓
Unsupported DataElement/cardinality
   ↓
Validation / processing failure
```

### Possible solution

Introduce a special qualifier/configuration mechanism that tells the processing pipeline to skip a particular SubmodelElement during normal plugin processing.

For example:

```text
contentSpecificationIds
        |
        +-- special qualifier
                |
                +-- skip normal plugin processing
```

The DataEngine would recognize this special qualifier and exclude `contentSpecificationIds` from normal template/plugin validation and processing.

### Advantages

* Keeps the information in the template.
* Does not require the DataEngine to discover all Submodels.
* Avoids introducing a completely separate processing path.
* Can be implemented as an explicit exception for this special case.

### Disadvantages

* Introduces a special-case qualifier.
* The DataEngine and template processing need to understand this qualifier.
* The value is not dynamically generated from the actual Submodels.
* The template becomes responsible for information that may depend on the actual AAS instance.

### Assessment

This approach is technically possible, but it introduces a special exception into the normal template processing flow.

---

# 5. Option 2 – DataEngine generates `contentSpecificationIds`

## Description

In this approach, the DataEngine itself generates the response for `contentSpecificationIds`.

Instead of processing the element like a normal DataElement, the DataEngine would recognize it as a special DPP Metadata requirement.

The processing could look like:

```text
Request for DPP Metadata
          |
          v
      DataEngine
          |
          v
Find AAS
          |
          v
Get all associated Submodels
          |
          v
For each Submodel:
    extract Submodel semantic ID
          |
          v
Create contentSpecificationIds
          |
          v
Return response
```

### Example

If an AAS contains:

```text
Submodel A → semanticId = A
Submodel B → semanticId = B
Submodel C → semanticId = C
```

the DataEngine would generate:

```text
contentSpecificationIds:
    A
    B
    C
```

### Advantages

* Values are generated dynamically.
* The response represents the actual Submodels attached to the AAS.
* No need to maintain the list manually in the template.
* Does not depend on plugin support for the special list structure.

### Disadvantages

This requires the DataEngine to know how to resolve the complete AAS structure.

The DataEngine would need to:

1. Resolve the requested AAS.
2. Retrieve all associated Submodels.
3. Retrieve each Submodel's semantic ID.
4. Build the `contentSpecificationIds` response.
5. Handle Submodels that cannot be resolved.
6. Handle Submodels without a semantic ID.
7. Potentially make multiple backend calls.

This makes `contentSpecificationIds` a special DataEngine-specific implementation.

It also creates additional coupling between the DPP Metadata Submodel and the DataEngine.

### Assessment

This option is **not recommended as the preferred general solution** because it requires the DataEngine to perform special traversal and aggregation logic for one specific SubmodelElement.

It is also potentially difficult depending on how the AAS/Submodel data is exposed by the current architecture.

---

# 6. Option 3 – Plugin generates the response

## Description

In this approach, the plugin responsible for the relevant DPP Metadata processing generates the `contentSpecificationIds` value.

The plugin would have access to the required information and return multiple `contentSpecificationId` values.

Conceptually:

```text
AAS
 |
 +-- DPP Metadata
 |
 +-- Submodel A
 +-- Submodel B
 +-- Submodel C
 |
 v
DPP Metadata Plugin
 |
 +-- contentSpecificationId = A
 +-- contentSpecificationId = B
 +-- contentSpecificationId = C
```

This keeps the special domain-specific logic within the plugin rather than adding DPP-specific behaviour to the DataEngine.

### Required capability

The main technical limitation is that the current DataEngine/plugin model does not support multiple values for the same DataElement in the required way.

The DPP template contains:

```text
contentSpecificationIds
    ├── contentSpecificationId
    ├── contentSpecificationId
    ├── contentSpecificationId
    └── contentSpecificationId
```

The required cardinality is effectively:

```text
contentSpecificationId → OneToMany
```

The example template explicitly uses the `SMT/Cardinality` qualifier with the value `OneToMany` for the individual `contentSpecificationId` properties.

### Required change

The DataEngine and plugins would need to support `OneToMany` cardinality for DataElement values.

This is already identified as a required capability in:

**Support OneToMany Cardinality In DataElement Values – Issue #740** (https://github.com/AAS-TwinEngine/AAS.TwinEngine.PM/issues/740)

Once this capability is available, the plugin could return multiple values for `contentSpecificationId` without requiring a DPP-specific DataEngine implementation.

### Advantages

* Keeps DPP-specific behaviour in the plugin.
* Avoids hard-coding DPP behaviour into the DataEngine.
* Supports dynamic generation of the values.
* Provides a generic solution that can also support other OneToMany DataElements.
* Aligns better with the semantic/cardinality definition of the template.

### Disadvantages

* Requires support for OneToMany cardinality.
* Requires changes to the DataEngine/plugin contract.
* May require changes to validation, mapping and response generation.

### Assessment

This is the **preferred long-term approach**, provided that generic OneToMany DataElement support is implemented.

--- 
