# AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests

This project contains Playwright-based REST API tests for the AAS TwinEngine Plugin TestPlugin.

## Overview

The tests are organized to match the Bruno API collection structure and cover the following areas:

- **AAS Repository**: Tests for shell operations, asset information, and submodel references
- **Submodel Repository**: Tests for submodels and submodel elements
- **Registry**: Tests for AAS and Submodel descriptors
- **Serialization**: Tests for appropriate serialization endpoints

## Prerequisites

1. .NET 8.0 SDK
2. Playwright for .NET
3. Running instance of the DataEngine service (default: http://localhost:8085)

## Installation

First, restore the NuGet packages:

```powershell
dotnet restore
```

Build:

```powershell
dotnet build
```

## Running Tests

### Run all tests

```powershell
dotnet test
```

### Run specific test class

```powershell
dotnet test --filter FullyQualifiedName~AasRepositoryTests
```

### Run with different base URL

Set the environment variable before running tests:

```powershell
$env:BASE_URL="http://localhost:8085"
dotnet test
```

## Project Structure

```
AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests/
├── ApiTestBase.cs                  # Base class for all API tests
├── AasRegistry/
│   └── AasRegistryTests.cs        # Tests for AAS Registry endpoints
├── AasRepository/
│   └── AasRepositoryTests.cs      # Tests for AAS Repository endpoints
├── DataEngine/
│   └── HealthTests.cs             # Tests for Data Engine health endpoint
├── SubmodelRegistry/
│   └── SubmodelRegistryTests.cs   # Tests for Submodel Registry endpoints
└── SubmodelRepository/
    ├── SubmodelTests.cs           # Tests for Submodel endpoints
    ├── SubmodelElementTests.cs    # Tests for Submodel Element endpoints
    └── SerializationTests.cs      # Tests for Serialization endpoints
```

## Test Structure

### Base Classes

- **ApiTestBase.cs**: Base class providing common functionality for all API tests
  - Initializes Playwright API request context
  - Provides Base64 URL encoding for identifiers
  - Contains assertion helpers
  - Manages configuration including base URL and test identifiers

### Test Classes

1. **AasRepository/AasRepositoryTests.cs**: Tests for AAS Repository endpoints
   - GetShellById
   - GetAssetInformationById
   - GetSubmodelRefById

2. **SubmodelRepository/SubmodelTests.cs**: Tests for Submodel endpoints
   - GetSubmodel for Nameplate, ContactInfo, and Reliability

3. **SubmodelRepository/SubmodelElementTests.cs**: Tests for Submodel Element endpoints
   - GetSubmodelElement for various element types and submodels

4. **DataEngine/HealthTests.cs**: Tests for Data Engine health endpoint
   - GetHealth

5. **AasRegistry/AasRegistryTests.cs**: Tests for AAS Registry endpoints
   - GetAllShellDescriptors (with and without pagination)
   - GetShellDescriptorById

6. **SubmodelRegistry/SubmodelRegistryTests.cs**: Tests for Submodel Registry endpoints
   - GetSubmodelDescriptorById for various submodels

7. **SubmodelRepository/SerializationTests.cs**: Tests for Serialization endpoints
   - GetAppropriateSerialization with multiple submodels

## Configuration

The tests use the following default configuration:

- **Base URL**: `http://localhost:8085`
- **AAS Identifier**: `https://mm-software.com/ids/aas/000-001`
- **Submodel Identifiers**:
  - ContactInformation: `https://mm-software.com/submodel/000-001/ContactInformation`
  - Nameplate: `https://mm-software.com/submodel/000-001/Nameplate`
  - Reliability: `https://mm-software.com/submodel/000-001/Reliability`

All identifiers are automatically Base64 URL encoded in the tests.

## Example Test

```csharp
[Fact]
    public async Task GetShellById_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        // Verify it's valid JSON
        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetShellById_Expected.json"));
    }
```

## Notes

- All tests inherit from `ApiTestBase` which implements `IAsyncLifetime` for proper setup and teardown
- Identifiers are Base64 URL encoded as required by the API
- JSON responses are validated to ensure they are well-formed
