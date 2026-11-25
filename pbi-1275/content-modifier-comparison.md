# PBI 1275 – Content Modifier Comparison (`$metadata`, `$value`, `$reference`, `$path`)

> **Scope:** Analysis only, with no implementation details.
> **PBI:** [AAS.TwinEngine.PM#1275 – Content modifier route variants](https://github.com/AAS-TwinEngine/AAS.TwinEngine.PM/issues/1275)
> **Captured on:** 2026-10-30

---

## 1. Purpose

This document explains what the AAS content modifier path suffixes mean (`/$metadata`, `/$value`, `/$reference`, `/$path`). It uses a single endpoint as the example and compares two sources:

- **TwinEngine DataEngine**: our implementation.
- **Target Registry/Repository**: an Eclipse BaSyx AAS Environment used as the reference implementation.

The example endpoint is:

```
GET /shells/{aasIdentifier}/submodels/{submodelIdentifier}[/$metadata | /$value | /$reference | /$path]
```

This endpoint was chosen because the IDTA spec allows **all four** content modifiers on a Submodel.

---

## 2. Test Environment

The services come from `examples/AAS.TwinEngine.Minimal.Export/docker-compose.yml`:

| Role | Container | Image | Base URL |
|---|---|---|---|
| DataEngine (ours) | `twinengine-dataengine` | `ghcr.io/aas-twinengine/dataengine:v1.2.0` | `http://localhost:8086` |
| Target Registry/Repository | `target-registry-repository` | `eclipsebasyx/aasenvironment-go:1.0.12` | `http://localhost:8082` |

The Export Service copies the shells and submodels from DataEngine into the target repository, so **both sides hold the same data**. The comparison is therefore like for like.

### Sample identifiers

| Item | Raw identifier | Base64URL (used in path) |
|---|---|---|
| AAS (Product1) | `https://mm-software.com/ids/aas/000-001` | `aHR0cHM6Ly9tbS1zb2Z0d2FyZS5jb20vaWRzL2Fhcy8wMDAtMDAx` |
| Submodel (Nameplate) | `https://mm-software.com/submodel/000-001/Nameplate` | `aHR0cHM6Ly9tbS1zb2Z0d2FyZS5jb20vc3VibW9kZWwvMDAwLTAwMS9OYW1lcGxhdGU` |

### Request rules used

- Every request was sent with **no query parameters**. Only the spec defaults apply (`level=deep`, `extent=withoutBlobValue`).
- `Accept` was left at the default, and all responses were JSON.

---

## 3. What the Content Modifiers Mean (IDTA Spec)

**Spec source:** IDTA-01002 Part 2 (API) **v3.2.0**, from the local clone `.aas-specs/aas-specs-api` (tag `v3.2.0`). For the serialization rules, Part 2 links to IDTA-01001 Part 1 v3.2, *Mappings* chapter.

### 3.1 How the suffix maps to the spec

The spec defines the `Content` serialization modifier as an operation parameter. The HTTP binding expresses it as a path suffix:

> "To ensure type-safe APIs, this parameter is mapped to the path suffixes "/$value", "/$metadata", "/$reference", and "/$path". Content="Normal" is mapped to the path without any "/$<content>" suffix."
> — `http-rest-api/http-rest-api.adoc:48-49`

The route `/shells/{aasId}/submodels/{smId}` is the AAS **superpath** (`http-rest-api.adoc:519`) placed in front of the Submodel Interface operation `GetSubmodel`. That operation allows `path-suffix= $metadata/$value/$reference/$path or no suffix for normal` (`http-rest-api.adoc:386-390`).

### 3.2 Definition of each modifier

Source: `specification/interfaces-operation-parameters.adoc:74-81` (normative table).

| Suffix | Content value | Spec definition | Part 1 serialization format |
|---|---|---|---|
| *(none)* | `Normal` (default) | "The standard serialization of the model element or child elements is applied." | Format "Normal" (JSON/XML) |
| `/$metadata` | `Metadata` | "Only metadata of an element or child elements is returned; the value is not." | Format "Metadata" (Metadata-Serialization) |
| `/$value` | `Value` | "Only the raw value of the model element or child elements is returned; it is commonly referred to as *ValueOnly*-serialization." | Format "Value" (ValueOnly-Serialization in JSON; **JSON only**) |
| `/$reference` | `Reference` | "Only applicable to Referables. Only the reference to the found element is returned; potential child elements are ignored." | Format "Reference" |
| `/$path` | `Path` | "Returns the idShort of the requested element and a list of *idShort* paths to child elements if the requested element is a Submodel, a SubmodelElementCollection, a SubmodelElementList, a AnnotatedRelationshipElement, or an Entity." | Format "Path" (idShortPath serialization in JSON) |

### 3.3 Which modifiers apply to which resource

Source: `interfaces-operation-parameters.adoc:116-162`.

| Resource | Content modifiers allowed |
|---|---|
| Asset Administration Shell | Normal, Reference |
| **Submodel** | **Normal, Metadata, Value, Reference, Path** |
| SubmodelElementCollection / SubmodelElementList / Entity | Normal, Metadata, Value, Reference, Path |
| Property, MLP, Range, File, Blob, ReferenceElement, RelationshipElement, AnnotatedRelationshipElement, BasicEventElement | Normal, Metadata, Value, Reference (**no Path**) |

If a client uses a suffix that is not defined for an element, the server **shall** return `400 Bad Request` (`http-rest-api.adoc:412`).

### 3.4 Modifier constraints (normative)

Source: `http-rest-api.adoc:333-346`.

1. **Value:** The response shall be an **unnamed** JSON object or array. There is no root property carrying the requested object's idShort.
2. **Value + level=core:** Only the direct children are returned, with empty values (`{}` for SMC and `[]` for SML).
3. **Metadata + extent=withBlobValue:** This combination is **not allowed**.
4. **Metadata + level:** "If parameter Content is set to "Metadata" then Level shall not be used." The server shall respond with **400**.
5. **Reference + level=deep:** This combination is **not allowed**.
6. Modifiers are not used for POST. For PUT, `Reference` and `Path` are not possible.

### 3.5 Spec examples (informative)

The informative examples in `annex/serialization-modifier-examples.adoc` cover:
- Metadata at line 111
- Value at line 169
- Reference at line 226
- Path at line 281

Key points from those examples:

- **Metadata** applied to a Submodel returns the Submodel header (`modelType`, `id`, `idShort`, `semanticId`, …) **without `submodelElements`**.
- **Value** applied to a Submodel returns `{ "<childIdShort>": <value>, ... }`.
- **Reference** applied to a Submodel returns a `ModelReference` with a single `Submodel` key. The annex lists Reference under **Core only**; Deep is marked "Not allowed".
- **Path** applied to a Submodel returns a flat array of idShortPaths. "IdShortPaths always start at the first SubmodelElement", so the Submodel's own idShort is **not** included.

---

## 4. Results Overview

| # | Request suffix | DataEngine (`:8086`) | Target BaSyx (`:8082`) | Response size (pretty JSON) |
|---|---|---|---|---|
| 1 | *(none)* | **200 OK**, `application/json; charset=utf-8` | **200 OK**, `application/json; charset=UTF-8` | 53,293 B on both sides |
| 2 | `/$metadata` | **404 Not Found** (empty body) | **200 OK** | Target: 628 B |
| 3 | `/$value` | **404 Not Found** (empty body) | **200 OK** | Target: 2,832 B |
| 4 | `/$reference` | **404 Not Found** (empty body) | **200 OK** | Target: 166 B |
| 5 | `/$path` | **404 Not Found** (empty body) | **200 OK** | Target: 1,240 B |

> DataEngine v1.2.0 exposes **none** of the content modifier routes. The 404 is a routing 404 from Kestrel: `Content-Length: 0`, with no AAS `Result` / `messages` payload. This is the gap PBI 1275 closes.

Full responses are stored in [`responses/`](./responses):

| File | Description |
|---|---|
| [dataengine_base.json](./responses/dataengine_base.json) | DataEngine – no suffix |
| [target_base.json](./responses/target_base.json) | Target – no suffix |
| [target_metadata.json](./responses/target_metadata.json) | Target – `/$metadata` |
| [target_value.json](./responses/target_value.json) | Target – `/$value` |
| [target_reference.json](./responses/target_reference.json) | Target – `/$reference` |
| [target_path.json](./responses/target_path.json) | Target – `/$path` |

---

## 5. Detailed Comparison

DataEngine only answers the base route. Each modifier is therefore compared as **DataEngine Normal response (today)** against **Target response with the modifier (expected behaviour)**.

### 5.1 No suffix (Content = Normal)

**Result: semantically identical.** A deep JSON comparison (`dataengine == target`) returns `True`.

| Aspect | DataEngine | Target | Difference? |
|---|---|---|---|
| Fields present | `idShort, description, administration, id, kind, semanticId, submodelElements, modelType` | same set | No |
| `submodelElements` order | AddressInformation, AssetSpecificProperties, CompanyLogo, … YearOfConstruction | same order | No |
| Values, qualifiers, semanticIds | — | — | No |
| **JSON key order inside objects** | Model order (`idShort` first, `modelType` last) | Alphabetical (`administration` first) | Cosmetic only, because JSON object key order is not significant |
| `Content-Type` charset casing | `utf-8` | `UTF-8` | Cosmetic only |

Trimmed excerpt (DataEngine):

```json
{
  "idShort": "Nameplate",
  "description": [{ "language": "en", "text": "Contains the nameplate information attached to the product" }],
  "administration": { "version": "3", "revision": "0", "templateId": "https://admin-shell.io/idta-02006-3-0" },
  "id": "https://mm-software.com/submodel/000-001/Nameplate",
  "kind": "Instance",
  "semanticId": { "type": "ExternalReference", "keys": [{ "type": "GlobalReference", "value": "https://admin-shell.io/idta/nameplate/3/0/Nameplate" }] },
  "submodelElements": [
    { "idShort": "AddressInformation", "modelType": "SubmodelElementCollection", "value": [ ... ] },
    ...
    { "idShort": "YearOfConstruction", "valueType": "xs:string", "value": "2022", "modelType": "Property" }
  ],
  "modelType": "Submodel"
}
```

---

### 5.2 `/$metadata` (Content = Metadata)

| | DataEngine | Target |
|---|---|---|
| Status | 404 | 200 |
| Body | *(empty)* | Submodel header only |

Target response (complete):

```json
{
  "administration": { "revision": "0", "templateId": "https://admin-shell.io/idta-02006-3-0", "version": "3" },
  "description": [{ "language": "en", "text": "Contains the nameplate information attached to the product" }],
  "id": "https://mm-software.com/submodel/000-001/Nameplate",
  "idShort": "Nameplate",
  "kind": "Instance",
  "modelType": "Submodel",
  "semanticId": { "keys": [{ "type": "GlobalReference", "value": "https://admin-shell.io/idta/nameplate/3/0/Nameplate" }], "type": "ExternalReference" }
}
```

**Compared with the DataEngine Normal response:**

| Field | DataEngine (Normal) | Target `$metadata` |
|---|---|---|
| `idShort`, `id`, `kind`, `modelType` | ✅ | ✅ (same values) |
| `description`, `administration`, `semanticId` | ✅ | ✅ (same values) |
| `submodelElements` | ✅ (20 elements, ~52 KB) | ❌ **removed** |

**Meaning:** `$metadata` returns the Normal response **minus the value-bearing part**. For a Submodel, that part is the whole `submodelElements` array. This matches the spec annex example (`serialization-modifier-examples.adoc:111-130`). For a Property, the spec example keeps `valueType` and drops `value`.

---

### 5.3 `/$value` (Content = Value / ValueOnly)

| | DataEngine | Target |
|---|---|---|
| Status | 404 | 200 |
| Body | *(empty)* | Unnamed JSON object of `idShort → value` |

Trimmed target response:

```json
{
  "AddressInformation": {
    "CityTown":     [{ "de": "Berlin" }, { "en": "Berlin" }],
    "NationalCode": [{ "de": "DE" }, { "en": "DE" }],
    "Street":       [{ "de": "Musterstrasse 1" }, { "en": "Main Street 1" }],
    "Zipcode":      [{ "de": "10115" }, { "en": "10115" }]
  },
  "AssetSpecificProperties": { "GuidelineSpecificProperties": [ {} ] },
  "CompanyLogo": { "contentType": "image/png", "value": "https://raw.githubusercontent.com/.../MM_Logo.svg" },
  "CountryOfOrigin": "DE",
  "DateOfManufacture": "2022-01-01",
  "ManufacturerName": [{ "de": "M&M Software" }, { "en": "M&M Software" }],
  "Markings": [
    {
      "DesignationOfCertificateOrApproval": "KEMA99IECEX1105/128",
      "ExpiryDate": "2030-01-01",
      "IssueDate": "2022-01-01",
      "MarkingAdditionalText": "additional information on the marking - 00",
      "MarkingFile": { "contentType": "image/png", "value": "https://raw.githubusercontent.com/.../checkmark.png" },
      "MarkingName": "0173-1#07-DAA603#004"
    },
    { "...": "Markings[1]" }
  ],
  "YearOfConstruction": "2022"
}
```

**How each element type in Nameplate changes from Normal to Value:**

| Element type (example) | DataEngine Normal shape | Target `$value` shape |
|---|---|---|
| **Submodel** (root) | `{ "idShort": "Nameplate", ..., "submodelElements": [...] }` | Unnamed object `{ ... }`. The `Nameplate` idShort, `id`, and `semanticId` are all gone. |
| **Property** (`CountryOfOrigin`, `xs:string`) | `{ "idShort": "CountryOfOrigin", "valueType": "xs:string", "value": "DE", "modelType": "Property", ... }` | `"CountryOfOrigin": "DE"` |
| **Property** (`DateOfManufacture`, `xs:date`) | `"value": "2022-01-01"` with `valueType: xs:date` | `"DateOfManufacture": "2022-01-01"` (string) |
| **MultiLanguageProperty** (`ManufacturerName`) | `"value": [ { "language": "de", "text": "M&M Software" }, { "language": "en", "text": "M&M Software" } ]` | `"ManufacturerName": [ { "de": "M&M Software" }, { "en": "M&M Software" } ]` (each LangString becomes a `{lang: text}` object) |
| **File** (`CompanyLogo`) | `{ "contentType": "image/png", "value": "https://...", "modelType": "File", ... }` | `"CompanyLogo": { "contentType": "image/png", "value": "https://..." }` |
| **SubmodelElementCollection** (`AddressInformation`) | `{ "modelType": "SubmodelElementCollection", "value": [ {child}, ... ] }` | `"AddressInformation": { "Street": ..., "Zipcode": ... }` (a **named object**) |
| **SubmodelElementList** (`Markings`) | `{ "modelType": "SubmodelElementList", "value": [ {SMC}, {SMC} ] }` | `"Markings": [ { ... }, { ... } ]` (an **array**; items have no idShort) |
| **Empty SMC inside SML** (`GuidelineSpecificProperties[0]` has no children) | SMC with only `semanticId`, `qualifiers`, `modelType` | `[ {} ]` |

**Removed in `$value`:** `modelType`, `idShort` (it becomes the key), `semanticId`, `qualifiers`, `valueType`, `description`, `kind`, `administration`, and `id`.

**Meaning:** `$value` is the most compact view of the actual data, about 5% of the Normal payload for Nameplate. The data **types** are lost (no `valueType`), so the client must already know the model, for example from `$metadata` or the template.

---

### 5.4 `/$reference` (Content = Reference)

| | DataEngine | Target |
|---|---|---|
| Status | 404 | 200 |
| Body | *(empty)* | `ModelReference` to the Submodel |

Target response (complete):

```json
{
  "keys": [
    { "type": "Submodel", "value": "https://mm-software.com/submodel/000-001/Nameplate" }
  ],
  "type": "ModelReference"
}
```

**Compared with the DataEngine Normal response:**

| Aspect | DataEngine (Normal) | Target `$reference` |
|---|---|---|
| Structure | Full Submodel object | `Reference` object (`type` + `keys`) |
| Contents | All metadata and all elements | Only the Submodel `id`, wrapped as a key of type `Submodel` |
| Relation to AAS | `submodels[]` on the shell holds this **same** reference shape | Identical to the matching entry in `GET /shells/{aasId}` → `submodels[]` |

**Meaning:** `$reference` answers the question "how do I point at this element?". For a Submodel, the reference has a single key. For a nested element, the keys run from the Submodel down to the element. For example, `RotationSpeed.MaxRotationSpeed` gives `[Submodel, SubmodelElementCollection, Property]` (`serialization-modifier-examples.adoc:226-278`).

---

### 5.5 `/$path` (Content = Path)

| | DataEngine | Target |
|---|---|---|
| Status | 404 | 200 |
| Body | *(empty)* | Flat JSON array of idShortPath strings (40 entries) |

Trimmed target response:

```json
[
  "AddressInformation",
  "AddressInformation.CityTown",
  "AddressInformation.NationalCode",
  "AddressInformation.Street",
  "AddressInformation.Zipcode",
  "AssetSpecificProperties",
  "AssetSpecificProperties.GuidelineSpecificProperties",
  "AssetSpecificProperties.GuidelineSpecificProperties[0]",
  "CompanyLogo",
  "CountryOfOrigin",
  "...",
  "Markings",
  "Markings[0]",
  "Markings[0].DesignationOfCertificateOrApproval",
  "Markings[0].ExpiryDate",
  "...",
  "Markings[1].MarkingName",
  "...",
  "YearOfConstruction"
]
```

**Compared with the DataEngine Normal response:**

| Aspect | DataEngine (Normal) | Target `$path` |
|---|---|---|
| Structure | Nested object tree | Flat `string[]` |
| Submodel's own idShort (`Nameplate`) | Present | **Not included**, because paths start at the first SubmodelElement (`serialization-modifier-examples.adoc:285`) |
| SMC children | Nested under `value` | `Parent.Child` (dot notation) |
| SML items (no idShort) | Array items in `value` | `List[index]` (bracket notation, for example `Markings[0]`) |
| Leaf-only info (values, types) | Present | None. **Only addresses.** |

**Meaning:** `$path` gives a table of contents of every addressable element in the Submodel. Each entry is a valid `{idShortPath}` for the `/submodel-elements/{idShortPath}` routes.

---

## 6. Summary of Differences

| Suffix | DataEngine today | Target (reference behaviour) | Gap |
|---|---|---|---|
| *(none)* | 200, full Submodel | 200, full Submodel | **None** (semantically identical; key order differs only cosmetically) |
| `/$metadata` | 404, empty body | 200, Submodel without `submodelElements` | Route missing |
| `/$value` | 404, empty body | 200, unnamed ValueOnly JSON object | Route missing |
| `/$reference` | 404, empty body | 200, `ModelReference` with a `Submodel` key | Route missing |
| `/$path` | 404, empty body | 200, flat array of idShortPaths | Route missing |

---

## 7. Observations and Open Points for Refinement

These points came up during the comparison. They should be clarified before acceptance testing.

| # | Observation | Spec position | Why it matters |
|---|---|---|---|
| 1 | **`$reference` without `?level`** returns 200 on BaSyx, although the default level is `deep`. | "The combination of Level="Deep" and Content="Reference" is not allowed." (`http-rest-api.adoc:341`). The annex lists Reference under **Core** only. | Decide whether DataEngine treats a bare `$reference` as implicit `core`, as BaSyx does, or returns 400. This affects the IDTA compliance validator outcome. |
| 2 | **Ordering in `$path` and `$value`.** BaSyx sorts children alphabetically: `AddressInformation.CityTown, .NationalCode, .Street, .Zipcode`. The model order is `Street, Zipcode, CityTown, NationalCode`. | The spec does not define the order. | For `$value`, order inside a JSON object is not significant. For `$path`, an array, the order **is** visible. Agree on model order or sorted order so that comparisons with the target do not produce false diffs. |
| 3 | **Property value typing in `$value`.** All Nameplate properties are `xs:string`, `xs:date`, or `xs:anyURI`, so BaSyx emits JSON strings. | The ValueOnly schema allows `PropertyValue` as string, number, or boolean. The spec example shows `5000` (a number) for `xs:int`. | Nameplate cannot show this case. A submodel with numeric or boolean `valueType` (for example TechnicalData) is needed to check whether numbers and booleans are emitted unquoted. |
| 4 | **Empty SMC inside an SML** is serialized as `[ {} ]` in `$value` and as `...GuidelineSpecificProperties[0]` (with no children) in `$path`. | Consistent with the ValueOnly schema. | This is the expected output for template placeholders that have no plugin data. It is useful as a test case. |
| 5 | **Error payload for unsupported routes.** DataEngine returns a bare 404 with an empty body. | Part 2 expects an AAS `Result` with `messages` for error responses, and 400 for undefined suffix/element combinations (`http-rest-api.adoc:412`). | After PBI 1275, invalid combinations need the spec status code and error body. Examples are `$path` on a Property, and `$metadata` with `?level` or with `?extent=withBlobValue`. |
| 6 | **Metadata + query parameters.** | `$metadata` with `level` → 400; `$metadata` with `extent=withBlobValue` → not allowed (`http-rest-api.adoc:338-339`). | This was not exercised here (default-only run), but it needs to be in the acceptance tests. |
| 7 | **`Content-Type` charset casing** (`utf-8` vs `UTF-8`). | Not specified; header values are case-insensitive. | No action needed. |

---

## 8. References

- IDTA-01002 Part 2 v3.2.0 (local clone `.aas-specs/aas-specs-api`, tag `v3.2.0`)
  - `documentation/IDTA-01002-3/modules/ROOT/pages/http-rest-api/http-rest-api.adoc`: lines 48-49 (suffix mapping), 333-346 (modifier constraints), 386-390 (GetSubmodel), 412 (400 for undefined suffix), 519 (AAS superpath)
  - `documentation/IDTA-01002-3/modules/ROOT/pages/specification/interfaces-operation-parameters.adoc`: lines 57-81 (Content enumeration), 116-162 (applicability table)
  - `documentation/IDTA-01002-3/modules/ROOT/pages/annex/serialization-modifier-examples.adoc`: lines 111 (Metadata), 169 (Value), 226 (Reference), 281 (Path)
- IDTA-01001 Part 1 v3.2, Mappings: <https://industrialdigitaltwin.io/aas-specifications/IDTA-01001/v3.2/mappings/mappings.html>
  - Format Metadata, ValueOnly serialization in JSON (incl. JSON schema), Format Reference, Format Path
