# TwinEngine Demonstrator Setup

## Overview

This project provides a basic setup to demonstrate how **TwinEngine** can be integrated and run locally. It provides a complete environment for managing Asset Administration Shells (AAS) and related components.

This example includes three submodels for each existing product (`000-001`, `000-002`, and `000-003`):

- ContactInformation
- CustomSubmodel
- HandoverDocumentation

---

## Default configuration

- [aas/](aas/) contains the existing submodel templates and shell template.
- `plugin/`— contain JSON files mounted into the plugin containers:
  Changes to these JSON files on the host not visible to the running containers, you must restart the container.

- Two services are built from local sources in the repo:

- twinengine-dataengine (built from local DataEngine source)
- twinengine-plugin (plugin build)

## Template Value Resolution: Manual QA

The valid scenarios below are distributed across the existing templates. No new products, submodel families, or invalid-template examples are introduced. Invalid templates remain a separate manual test activity. Unit tests, module tests, and Playwright tests are not changed by these fixture updates.

DataEngine should retain populated template values and request only unresolved values. Missing values, empty strings, whitespace-only strings, and case-insensitive `null` strings are unresolved; `0` and `false` are valid values.

### ContactInformation

Template: [ContactInformationAAS.aas.xml](aas/ContactInformationAAS.aas.xml).

| Element path | Scenario | Expected result |
| --- | --- | --- |
| `ContactInformation*.RoleOfContactPerson` | Populated property inside a dynamic collection prototype | Keep `0173-1#07-AAS931#001` in every generated contact, even though plugin data contains a different role. |
| `ContactInformation*.Company` | Partially populated multi-language property | Keep the existing English `M&M software`; request German only, returning `M&M Software aus dem Plugin`. The conflicting English plugin value must not replace the template text. |
| `ContactInformation*.Department`, `NameOfContact`, and nested `Phone` | Missing language values and nested dynamic collections | Plugin values populate the missing fields; contact/phone counts come from plugin data. Products `000-001` and `000-002` have two contacts; `000-003` has three. |
| `CorporateContact.Company` | Fully populated single collection | Keep `M&M Software`; do not request this collection from the plugin. |
| `StaticContact01.Name`, `StaticContact02.Name` | Multiple fully populated occurrences sharing a collection semantic ID | Keep `John Doe` and `Jane Smith` as two distinct template-owned entries, in template order. |

### CustomSubmodel

Template: [CustomAas.xml](aas/CustomAas.xml). Paths starting with `TemplateValueProperties` are grouped in the existing submodel to keep the example navigable.

| Element path | Scenario | Expected result |
| --- | --- | --- |
| `TemplateValueProperties.Zero`, `False` | Valid zero and boolean false | Keep `0` and `false`; do not request either value, despite conflicting plugin values `99` and `true`. |
| `TemplateValueProperties.Empty`, `Missing`, `Whitespace`, `NullPlaceholder` | Empty, omitted, whitespace-only, and mixed-case padded ` NuLl ` values | Request these values. For product `000-001`, results are `000-001 empty resolved`, `000-001 missing resolved`, `000-001 whitespace resolved`, and `000-001 null resolved`; the other products use their own identifier prefix. |
| `ArbitraryRange` | Minimum populated, maximum missing | Keep minimum `0.5`; request maximum only. |
| `OperatingConditionsOfReliabilityCharacteristics*.ArbitraryRangeDate` | Maximum populated, minimum missing | Keep maximum `2030-12-31`; request minimum only. |
| `Markings[*].Marking*.ArbitraryRangeString` | Both Range endpoints missing | Request both endpoints from the plugin. |
| `TemplateValueProperties.ResolvedRange` | Both Range endpoints populated | Keep `0` and `100`; request neither endpoint. |
| `OperatingConditionsOfReliabilityCharacteristics*.ProductClassName` | All declared languages populated | Preserve both template texts without requesting those language values. |
| `Markings[*].Marking*.ClassName` | No template language values | Resolve configured default languages, if configured. With no default languages configured, there are no language IDs to request. |
| `Target_model` | Complete model reference | Keep the template Submodel key unchanged. |
| `OperatingConditionsOfReliabilityCharacteristics*.ModelReference` | Partial model reference | Request the missing Submodel key, preserving the populated `TechnicalPropertyAreas` and `0` keys. |
| `Markings[*].Marking*.ReferenceElementWithOutValue` | Reference with no value | Preserve the absent reference; there are no reference keys to request. |
| `EntryNode.SameAs`, `TemplateValueProperties.ExternalReference` | External references | Preserve template references without requesting their keys. |
| `EntryNode.IsPartOf`, `EntryNode.HasPart` | Partial and complete relationship references | Fill only missing keys in `IsPartOf`; preserve populated keys and the complete `HasPart` references. |
| `EntryNode` and nested `Node` entities | Missing self-managed global asset IDs | Resolve missing asset IDs from the plugin. |
| `TemplateValueProperties.ResolvedEntity` | Fully populated self-managed entity | Preserve the global asset ID and manufacturer specific asset ID; no plugin values are needed. |
| `TemplateValueProperties.MixedEntity` | Static and dynamic entity identifiers/statements | Preserve its global asset ID, `Manufacturer = M&M Software`, and unmapped identifier. Fill `SerialNumber` as `<product>-SERIAL` and `Statement` as `<product> plugin statement`. |
| `TemplateValueProperties.CoManagedEntity` | Co-managed entity with a populated statement | Preserve the statement; do not create or request a global asset ID. |
| `Header`, `OperatingConditionsOfReliabilityCharacteristics*.Paragraph` | Populated Blobs | Keep the template bytes instead of requesting replacement content. |
| `TemplateValueProperties.UnresolvedBlob` | Missing Blob bytes | Populate base64 `UGx1Z2luIGJsb2I=` (text `Plugin blob`) from the plugin. |
| `TemplateValueProperties.ResolvedFile` | Populated File | Keep the template URL and `image/jpeg` content type; do not request a replacement path. |
| `Markings[*].Marking*.MarkingFile`, `OperatingConditionsOfReliabilityCharacteristics*.CompanyLogo` | Missing File paths | Fill paths from plugin data, retaining template content types. |
| `Markings[*].Marking*.ExpiryDate` | Static value inside plugin-expanded list collections | Preserve `2032-01-01` in every generated marking; plugin data continues to supply the dynamic fields and counts. |
| `TemplateValueProperties.EmptyCollection`, `EmptyList` | Containers with no children | Preserve empty containers; do not invent child semantic IDs to request. |

`*` denotes a generated collection occurrence in this table. Inspect the returned `idShort` values for the exact generated names before making an element-path request.

### CustomSubmodel: Fully Populated Lists

The following four scenarios also belong to [CustomAas.xml](aas/CustomAas.xml), alongside its existing dynamic `Markings` list. They are not part of HandoverDocumentation.

| Element path | Scenario | Expected result |
| --- | --- | --- |
| `SingleTemplateLanguage` | One resolved Property in a list | Keep one item, `en`; no plugin request for this list. |
| `FiveTemplateLanguages` | Five resolved Properties sharing the item semantic ID | Keep exactly `en`, `de`, `fr`, `es`, `it`, in that order; no plugin request for this list. |
| `SingleTemplateMarking` | One fully populated collection in a list | Keep one `MarkingName = CE` item without requesting it. |
| `MultipleTemplateMarkings` | Multiple fully populated collections sharing the item semantic ID | Keep two entries, `CE` then `UKCA`, without requesting them. |

### HandoverDocumentation

Template: [HandoverDocumentation.xml](aas/HandoverDocumentation.xml). This template retains its existing document structures; the added static-list scenarios are in CustomSubmodel.

| Element path | Scenario | Expected result |
| --- | --- | --- |
| `Documents` and nested `DocumentIds`, `DocumentClassifications`, `DocumentVersions`, `Language`, `DigitalFiles`, and `Entities` | Unresolved list prototypes and nested list expansion | Continue using the existing product-specific plugin data for values and instance counts. |

### Running the Checks

1. Reload/import the modified XML into the template repository used by this setup. A repository with persisted templates may not overwrite them merely because its container restarts; confirm that the repository serves the updated template before testing.
2. Restart `twinengine-plugin` after updating its mounted JSON data. Docker uses [plugin/mock-submodel-data.json](plugin/mock-submodel-data.json); local runs use [../Data/mock-submodel-data.json](../Data/mock-submodel-data.json). These copies contain the same scenario data.
3. In the existing [API collection](apiCollection/README.md), retrieve each existing product's ContactInformation, CustomSubmodel, and HandoverDocumentation submodels. Inspect the fields and counts from the tables above.
4. Repeat with `GET /submodels` to compare batch retrieval with individual submodel retrieval. Confirm the added static entries retain their relative template order.
5. Retrieve a fully populated element, for example `FiveTemplateLanguages` from CustomSubmodel or `CorporateContact` from ContactInformation, through `GET /submodels/{submodelIdentifier}/submodel-elements/{idShortPath}`. No plugin data request is needed for those elements.
6. Inspect the schema requests sent to TestPlugin: resolved IDs such as `0173-1#02-AAW001#001_en` and `https://mm-software.com/template-values/zero` must be absent. German company IDs and unresolved property/entity IDs must be present in their structural context. A mixed submodel still needs a plugin request for its unresolved fields.

Existing Playwright requests can exercise these scenarios after the setup is reloaded. Full-response snapshots must be reviewed against the new fields, language entries, and static list items before they are refreshed; this change does not update those snapshots or add test code.

---

## Rebuild images (when you change code)

Rebuild all images then restart:

```bash
# rebuild images
docker-compose build --no-cache

# restart the stack
docker-compose up -d
```

---

### Troubleshooting

- If http://localhost:8080/aas-ui/ doesn't load:

  Check docker-compose logs nginx for errors

  Make sure port **8080, 8085, 8086** is not used by another service.

- If a container fails to start because of bind port, stop whatever uses that port or change the mapping in `docker-compose.yml`.

- If you edit plugin JSON files of twinengine-testplugin, make sure you restart that cotainerApp.
  ```bash
  docker-compose restart <container-app-name>
  ```

  - Verify the host path for the mounted volume is correct relative to the example folder.

---
