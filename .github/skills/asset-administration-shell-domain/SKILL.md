---
name: asset-administration-shell-domain
description: Guidance for working with Asset Administration Shell (AAS) concepts, structures, and JSON representations when generating code or APIs
---

## 1. Core Concepts

### Asset Administration Shell (AAS)
- Represents the **digital twin of an asset**.
- Key fields:
  - `id` (globally unique identifier)
  - `idShort` (human-readable identifier)
  - `assetInformation`
  - `submodels`

Each asset can contain multiple **submodels** describing different aspects.

---

## 2. Submodels
- Define specific aspects of an asset:
  - Technical data
  - Operational data
  - Identification
  - Documentation
  - Condition monitoring

**Structure:**
- `id`
- `idShort`
- `semanticId`
- `submodelElements`

---

## 3. Submodel Elements
Submodel elements represent structured data inside a submodel.

**Types include:**
- Property
- MultiLanguageProperty
- Range
- File
- Blob
- ReferenceElement
- RelationshipElement
- SubmodelElementCollection
- SubmodelElementList

Elements can be **nested recursively**.

---

## 4. Identifiers
- **id** → Globally unique identifier for AAS or Submodel.
- **idShort** → Human-readable identifier within the AAS structure.
- **semanticId** → Reference defining the semantic meaning of an element (often linked to IEC CDD or ECLASS dictionaries).

---

## 5. References
Used to connect elements:
- **ModelReference** → Points to other AAS objects.
- **GlobalReference** → Points to external definitions.

---

## 6. JSON Representation
AAS data is commonly exchanged in JSON.

**Characteristics:**
- Deeply nested objects
- Arrays of submodel elements
- Semantic ID references
- Structured data types


---

## 7. Coding Guidelines
When generating **C# or API code** for AAS:
- Use strongly typed models (avoid dynamic objects).
- Respect hierarchical structure:
  - AssetAdministrationShell → Submodel → SubmodelElement
- Handle nested collections correctly.
- Use `System.Text.Json` for serialization.
- Preserve semantics of `semanticId` and `idShort`.

---

## 8. API Design Guidelines
For APIs handling AAS data:
- Use RESTful endpoints.
- Return structured JSON consistent with AAS.
- Support retrieval of:
  - AAS
  - Submodels
  - Submodel elements
- Validate identifiers and semantic references.

---

## 9. References
For deeper study of AAS specifications:
- [Details of the Asset Administration Shell Part 1 V3.0RC02 (PDF)](https://industrialdigitaltwin.org/wp-content/uploads/2022/06/DetailsOfTheAssetAdministrationShell_Part1_V3.0RC02_Final1.pdf)
- [IDTA Specification v3.1.1 – Submodel Elements](https://industrialdigitaltwin.io/aas-specifications/IDTA-01001/v3.1.1/spec-metamodel/submodel-elements.html#entity-attributes)