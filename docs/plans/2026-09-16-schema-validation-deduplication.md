# Schema Validation Deduplication Implementation Plan

> **REQUIRED:** Read and follow .github/skills/executing-plans/SKILL.md to implement this plan task-by-task.

**Goal:** Validate each distinct generated JSON Schema shape once while preparing a batch of submodel requests.

**Architecture:** Keep schema generation and grouping in `PluginDataHandler`. Use the serialized generated schema as the stable shape identity, cache successful validations by that identity, and preserve per-response validation. This remains plugin-agnostic and does not change the plugin contract.

**Tech Stack:** .NET 10, C#, xUnit, NSubstitute, `Json.Schema`.

---

### Task 1: Add regression coverage

**Files:**
- Modify: `source/AAS.TwinEngine.DataEngine.UnitTests/Infrastructure/Providers/PluginDataProvider/Services/PluginDataHandlerTests.cs`

**Steps:**
1. Add a batch test with multiple requests producing the same schema shape.
2. Configure the validator substitute and assert `ValidateRequestSchema` is called once for the repeated shape.
3. Run the focused test and confirm the new assertion fails against the current implementation when the semantic keys differ.

### Task 2: Deduplicate request-schema validation

**Files:**
- Modify: `source/AAS.TwinEngine.DataEngine/Infrastructure/Providers/PluginDataProvider/Services/PluginDataHandler.cs`

**Steps:**
1. Use the generated schema's canonical serialized representation as the validation cache key.
2. Validate only when a schema key is first encountered.
3. Preserve schema reuse for request grouping and keep response validation unchanged.
4. Run the focused test and confirm it passes.

### Task 3: Full focused validation

**Files:**
- No additional files.

**Steps:**
1. Run all `PluginDataHandlerTests`.
2. Run the existing `TemplateProviderTests` regression suite.
3. Confirm no unrelated files are staged.

### Task 4: Performance comparison

**Files:**
- No source files.

**Steps:**
1. Build the changed DataEngine and its parent `9851fbd`.
2. Run both against `AAS.TwinEngine.Plugin.DPP` branch `query-update`, commit `4188bf1`.
3. Measure warm `GET /submodels?limit=250` responses with identical environment and record status, payload bytes, result count, and latency percentiles.
4. Report the measured delta and limitations.
