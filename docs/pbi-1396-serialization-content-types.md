# PBI 1396 — Support IDTA Serialization API Content Types

> Implementation summary for [AAS-TwinEngine/AAS.TwinEngine.PM#1396](https://github.com/AAS-TwinEngine/AAS.TwinEngine.PM/issues/1396)
> (parent: [#2200 — AAS Read-Only API Compliance Completion (SSP-002 Follow-on)](https://github.com/AAS-TwinEngine/AAS.TwinEngine.PM/issues/2200))

---

## 1. What the PBI requires

### User story

> **As a** DataEngine consumer
> **I want to** request serialized representations using IDTA-defined media types
> **So that** DataEngine is compliant with the IDTA Serialization API specification and interoperable with other AAS ecosystem components.

### Acceptance criteria

| # | Criterion | Status |
|---|---|---|
| AC1 | DataEngine serialization endpoints support IDTA-defined content types via the `Accept` header | ✅ Done |
| AC2 | AAS serialization supports `application/asset-administration-shell-package+xml` | ✅ Done |
| AC3 | AAS serialization supports `application/json` | ✅ Done |
| AC4 | AAS serialization supports `application/xml` | ✅ Done |
| AC5 | OpenAPI/Swagger documentation advertises all supported serialization formats | ✅ Done |

### Problem statement

The IDTA Serialization API defines `GET /serialization` as a content-negotiated endpoint: the client
selects the representation via the `Accept` header. Before this change DataEngine ignored the `Accept`
header entirely and **always** returned an AASX package, so neither plain JSON nor plain XML
serializations were reachable and the OpenAPI document advertised only `application/octet-stream`.

### Reference specification

- IDTA Submodel Repository Service Specification **V3.1.2 SSP-001** — Serialization API (`GET /serialization`)
- Cross-checked against the vendored V3.2 contract in
  `.aas-specs/aas-specs-api/SubmodelRepositoryServiceSpecification/V3.2_SSP-001.yaml`, which confirms
  that the JSON and XML representations are the AAS `Environment` schema and that the documented error
  codes are `400 / 401 / 403 / 500` (no `406`).

> **Note on versions:** V3.1.2 names the package media type
> `application/asset-administration-shell-package+xml`; V3.2 renames it to `application/aas+zip`.
> This PBI explicitly targets V3.1.2, so the former name is implemented. The V3.2 alias is **out of
> scope** here and is a candidate follow-up under the parent epic.

---

## 2. What was implemented

### 2.1 Content negotiation behaviour

`GET /serialization` now resolves the response format from the `Accept` header:

| `Accept` header | Format returned | `Content-Type` | Download name |
|---|---|---|---|
| `application/asset-administration-shell-package+xml` | AASX package | `application/asset-administration-shell-package+xml` | `{aasId}.aasx` |
| `application/json` | AAS `Environment` as JSON | `application/json` | `{aasId}.json` |
| `application/xml` | AAS `Environment` as XML | `application/xml` | `{aasId}.xml` |
| *absent*, `*/*`, `application/*` | AASX package (default) | `application/asset-administration-shell-package+xml` | `{aasId}.aasx` |
| anything else (e.g. `text/csv`) | — | — | `400 Bad Request` |

Negotiation details:

- **Quality factors are honoured.** `Accept: application/xml;q=0.4, application/json;q=0.9` yields JSON.
- **Ties keep client order.** `Accept: application/json, application/xml` yields JSON.
- **`q=0` is a rejection.** `application/json;q=0, application/xml` yields XML.
- **Matching is case-insensitive** and tolerates parameters (`application/json; charset=utf-8`).
- **Unsupported wildcards are not silently accepted.** `text/*` does not match any supported type and
  is rejected; only `*/*` and `application/*` fall back to the default.
- **Backwards compatible.** Clients that send no `Accept` header (or only wildcards) still receive an
  AASX package exactly as before.

### 2.2 Error contract

An `Accept` header that cannot be satisfied raises `InvalidRequestHeaderException`
(a `BadRequestException`), which the existing `GlobalExceptionHandler` maps to **`400 Bad Request`**
with the standard `ServiceErrorResponse` body and `traceId`.

**Design decision — 400 instead of 406.** Strict HTTP semantics would suggest `406 Not Acceptable`,
but the IDTA specification documents only `400 / 401 / 403 / 500` for this operation, and the repository
has no `406` exception mapping. `400` keeps the error contract aligned with the specification and with
every other DataEngine endpoint. The response message enumerates the supported media types so the
client can self-correct.

### 2.3 OpenAPI / Swagger

`[ProducesResponseType(..., contentTypes)]` on its own was **not sufficient**: NSwag collapses any
binary/file response down to a single `application/octet-stream` entry, which hid the negotiated
formats. A generic NSwag operation processor was added that re-applies the content types declared on
`ProducesResponseTypeAttribute` to the matching response.

The generated document now contains:

```jsonc
"/serialization": {
  "get": {
    "responses": {
      "200": {
        "content": {
          "application/asset-administration-shell-package+xml": { "schema": { "type": "string", "format": "binary" } },
          "application/json":                                   { "schema": { "type": "string", "format": "binary" } },
          "application/xml":                                    { "schema": { "type": "string", "format": "binary" } }
        }
      }
      // 400 / 404 / 500 -> application/json (ServiceErrorResponse)
    }
  }
}
```

The processor is endpoint-agnostic: any future action that declares content types on
`ProducesResponseType` gets them advertised automatically.

---

## 3. Architecture and layering

The change follows the existing **Controller → Handler → Service** pattern and respects the Onion
Architecture dependency direction.

```
SerializationController          (Api)              reads the raw Accept header only — no logic
        │
        ▼
SerializationHandler             (Api/Handler)      resolves format, validates + decodes identifiers,
        │                                           maps to FileStreamResult
        │   uses SerializationFormatResolver        (Api/Shared)  Accept header -> SerializationFormat
        │   uses SerializationMediaTypes            (Api/Shared)  media type <-> format <-> extension
        ▼
ISerializationService            (ApplicationLogic) builds the AAS Environment once, then serializes
SerializationService                                to AASX / JSON / XML
```

- HTTP concerns (`Accept` parsing, media types, file names) stay in the **Api** layer.
- The **ApplicationLogic** layer only knows the `SerializationFormat` enum — no HTTP types leak in.
- The `Environment` is assembled **once** and then serialized, so adding a format costs one branch and
  no extra plugin/registry round-trips.

---

## 4. Files changed

### New files

| File | Purpose |
|---|---|
| `source/AAS.TwinEngine.DataEngine/ApplicationLogic/Services/SubmodelRepository/SerializationFormat.cs` | `AasxPackage` / `Json` / `Xml` enum |
| `source/AAS.TwinEngine.DataEngine/Api/Shared/SerializationMediaTypes.cs` | Media-type constants, default format, format ⇄ media type ⇄ file extension mapping |
| `source/AAS.TwinEngine.DataEngine/Api/Shared/SerializationFormatResolver.cs` | `Accept` header → `SerializationFormat`, with q-factor and wildcard handling |
| `source/AAS.TwinEngine.DataEngine/Api/SubmodelRepository/Requests/SerializationRequest.cs` | Replaces `SerializeAasxRequest`; adds the raw `AcceptHeader` |
| `source/AAS.TwinEngine.DataEngine/ServiceConfiguration/OpenApi/ResponseContentTypesOperationProcessor.cs` | NSwag processor that advertises declared response content types |
| `source/AAS.TwinEngine.DataEngine.UnitTests/Api/Shared/SerializationFormatResolverTests.cs` | Resolver unit tests |

### Modified files

| File | Change |
|---|---|
| `Api/SubmodelRepository/SerializationController.cs` | Forwards the `Accept` header; declares the three media types for `200`; documents negotiation in XML docs |
| `Api/SubmodelRepository/Handler/ISerializationHandler.cs` / `SerializationHandler.cs` | `GetAasxFileAsync` → `GetSerializationAsync`; resolves format and sets content type + file extension |
| `ApplicationLogic/Services/SubmodelRepository/ISerializationService.cs` / `SerializationService.cs` | `GetAasxFileStreamAsync` → `GetSerializationStreamAsync(..., SerializationFormat, ...)`; added `BuildJsonStream` / `BuildXmlStream` |
| `Program.cs` | Registers `ResponseContentTypesOperationProcessor` with the OpenAPI document |
| `examples/shared/apiCollection/.../Get appropriate serialization - Product1.bru` | Explicit `Accept: application/asset-administration-shell-package+xml` |
| `examples/shared/apiCollection/.../Get appropriate serialization - Product1 - JSON.bru` *(new)* | JSON example request |
| `examples/shared/apiCollection/.../Get appropriate serialization - Product1 - XML.bru` *(new)* | XML example request |

### Removed files

| File | Reason |
|---|---|
| `Api/SubmodelRepository/Requests/SerializeAasxRequest.cs` | Replaced by `SerializationRequest` — the name no longer described the endpoint, which is now format-agnostic |

---

## 5. Test evidence

| Suite | Result |
|---|---|
| `dotnet build AAS.TwinEngine.DataEngine.sln` | **0 errors** |
| `dotnet test AAS.TwinEngine.DataEngine.UnitTests` | **1469 passed**, 0 failed |
| `dotnet test AAS.TwinEngine.DataEngine.ModuleTests` | **468 passed**, 0 failed |

### Unit tests

- `SerializationFormatResolverTests` — wildcards, missing header, casing, media-type parameters,
  q-factor ordering, `q=0` rejection, and unsupported-type rejection.
- `SerializationHandlerTests` — format selection per `Accept` value, resulting content type and file
  extension, `400` on unsupported header, plus the pre-existing identifier validation and
  Base64URL/injection guard cases.
- `SerializationServiceTests` — JSON output is a parsable `Environment` containing
  `assetAdministrationShells` and `submodels`; XML output starts with an XML declaration and contains
  the environment element; existing AASX packaging behaviour unchanged.
- `SerializationControllerTests` — the raw `Accept` header is forwarded to the handler unmodified.

### Module (integration) tests

Executed through the real ASP.NET pipeline, including `HeaderSanitizationMiddleware` and
`GlobalExceptionHandler`, against both the `v1-config` and `v2-config` configuration formats:

- `SerializeAsync_ReturnsRequestedContentType_WhenAcceptHeaderIsSupportedAsync` — theory over all three
  media types plus `*/*` and a q-factor list, asserting the response `Content-Type`.
- `SerializeAsync_ReturnsDeserializableEnvironment_WhenJsonIsRequestedAsync` — the JSON body parses and
  exposes `assetAdministrationShells` / `submodels`.
- `SerializeAsync_Returns400_WhenAcceptHeaderIsNotSupportedAsync` — `text/csv`, `application/pdf`.
- `OpenApiDocument_AdvertisesAllSupportedSerializationMediaTypesAsync` — fetches
  `/swagger/1.0/swagger.json` and asserts the `200` response lists all three media types **(AC5)**.
- All pre-existing serialization tests (no `Accept` header) still pass, proving backwards compatibility.

### End-to-end (Playwright)

`AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests/SubmodelRepository/SerializationTests.cs`:

- AASX case asserts the OPC relationship markup is present.
- New JSON case asserts the content type and a parsable `Environment`.
- New XML case asserts the content type and XML declaration.
- New `text/csv` case asserts `400`.

> ⚠️ **Regression caught during implementation:** `ApiTestBase` sets a global
> `Accept: application/json` header for every Playwright request. Once negotiation went live, the
> existing AASX assertions would have silently started receiving JSON. The serialization tests now set
> their `Accept` header explicitly per request.

---

## 6. How to verify manually

```bash
AAS_ID=$(printf 'https://mm-software.com/ids/aas/000-001' | base64 -w0)
SM_ID=$(printf 'https://mm-software.com/submodel/000-001/ContactInformation' | base64 -w0)
URL="http://localhost:8085/serialization?aasIds=$AAS_ID&submodelIds=$SM_ID&includeConceptDescriptions=false"

# AASX package (also the default when no Accept header is sent)
curl -s -D- -o product.aasx -H 'Accept: application/asset-administration-shell-package+xml' "$URL"

# JSON environment
curl -s -H 'Accept: application/json' "$URL" | jq '.assetAdministrationShells | length'

# XML environment
curl -s -H 'Accept: application/xml' "$URL" | head -3

# Unsupported media type -> 400
curl -s -o /dev/null -w '%{http_code}\n' -H 'Accept: text/csv' "$URL"
```

Swagger UI: `http://localhost:8085/swagger` → **Serialization API** → `GET /serialization` → the
**Media type** dropdown on the `200` response now lists all three formats.

The Bruno collection under
`examples/shared/apiCollection/Submodel Repository/Serialization/` contains ready-made requests for
each format.

---

## 7. Risk assessment

| Risk | Severity | Mitigation |
|---|---|---|
| Existing clients sending `Accept: application/json` now receive JSON instead of AASX | Medium | Intended behaviour per the PBI. Clients that send no `Accept` header or wildcards are unaffected. Caught and fixed in the Playwright suite; no internal consumer of `/serialization` exists (verified across DataEngine and ExportService). |
| Large environments materialised in memory for JSON/XML | Low | Same data volume already materialised for the AASX path; AASX continues to stream from a temp file with `FileOptions.DeleteOnClose`. |
| NSwag upgrade changes file-response handling | Low | The operation processor only rewrites responses that explicitly declare content types; it is a no-op otherwise, and the module test on `swagger.json` fails loudly if the output regresses. |
| `406` expected by a strict HTTP client | Low | Documented decision (§2.2); error body enumerates the supported media types. |

---

## 8. Out of scope / follow-ups

- `application/aas+zip` (the V3.2 alias for the AASX package media type).
- `406 Not Acceptable` instead of `400`, should strict HTTP semantics be preferred later.
- Serialization endpoints beyond `GET /serialization`.
- A richer OpenAPI schema for the JSON/XML responses (currently `string/binary`; the IDTA V3.2
  contract references the `Environment` schema).
