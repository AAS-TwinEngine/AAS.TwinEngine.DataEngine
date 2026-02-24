# AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.TypeScript

This project contains Playwright-based REST API tests written in TypeScript for the AAS TwinEngine Plugin TestPlugin.

## Overview

The tests are organized to match the existing C# Playwright test structure and cover the following areas:

- **AAS Repository**: Tests for shell operations, asset information, and submodel references
- **Submodel Repository**: Tests for submodels, submodel elements, and serialization
- **AAS Registry**: Tests for AAS descriptors
- **Submodel Registry**: Tests for Submodel descriptors
- **Data Engine**: Tests for health endpoint

## Prerequisites

1. Node.js 20 or later
2. npm
3. Running instance of the DataEngine service (default: http://localhost:8085)

## Installation

Install the npm dependencies:

```bash
npm install
```

## Running Tests

### Run all tests

```bash
npx playwright test
```

### Run specific test file

```bash
npx playwright test tests/aas-repository/aas-repository.spec.ts
```

### Run tests with a specific name pattern

```bash
npx playwright test -g "GetShellById"
```

### Run with different base URL

Set the environment variable before running tests:

```bash
BASE_URL="http://localhost:8085" npx playwright test
```

### List all tests without executing

```bash
npx playwright test --list
```

### View test report

```bash
npx playwright show-report
```

## Project Structure

```
AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.TypeScript/
├── playwright.config.ts                     # Playwright configuration
├── tsconfig.json                            # TypeScript configuration
├── package.json                             # Node.js project configuration
├── tests/
│   ├── api-test-base.ts                     # Shared utilities and test helpers
│   ├── aas-registry/
│   │   ├── aas-registry.spec.ts             # Tests for AAS Registry endpoints
│   │   └── test-data/                       # Expected JSON responses
│   ├── aas-repository/
│   │   ├── aas-repository.spec.ts           # Tests for AAS Repository endpoints
│   │   └── test-data/                       # Expected JSON responses
│   ├── data-engine/
│   │   └── health.spec.ts                   # Tests for Data Engine health endpoint
│   ├── submodel-registry/
│   │   ├── submodel-registry.spec.ts        # Tests for Submodel Registry endpoints
│   │   └── test-data/                       # Expected JSON responses
│   └── submodel-repository/
│       ├── submodel.spec.ts                 # Tests for Submodel endpoints
│       ├── submodel-element.spec.ts         # Tests for Submodel Element endpoints
│       ├── serialization.spec.ts            # Tests for Serialization endpoints
│       └── test-data/                       # Expected JSON responses
```

## Test Helpers

### `api-test-base.ts`

Provides shared functionality for all API tests:

- **`base64EncodeUrl(str)`**: Base64 URL encodes a string for use in API paths
- **`assertSuccessResponse(response)`**: Asserts that an API response has a 2xx status code
- **`compareJson(actual, filePath)`**: Compares actual JSON response with expected JSON from a test data file
- **`testDataPath(dir, ...segments)`**: Resolves a test data file path relative to a test directory
- Pre-encoded identifiers: `aasIdentifier`, `submodelIdentifierContact`, `submodelIdentifierNameplate`, `submodelIdentifierReliability`

## Test Cases

| Test File | Tests | Description |
|-----------|-------|-------------|
| `aas-registry.spec.ts` | 3 | GetAllShellDescriptors, pagination, GetShellDescriptorById |
| `aas-repository.spec.ts` | 3 | GetShellById, GetAssetInformationById, GetSubmodelRefById |
| `health.spec.ts` | 1 | GetHealth returns "Healthy" |
| `submodel-registry.spec.ts` | 3 | GetSubmodelDescriptorById for Contact, Nameplate, Reliability |
| `submodel.spec.ts` | 3 | GetSubmodel for Nameplate, ContactInfo, Reliability |
| `submodel-element.spec.ts` | 4 | GetSubmodelElement for various element types |
| `serialization.spec.ts` | 1 | GetAppropriateSerialization with multiple submodels |
| **Total** | **18** | |

## Configuration

The tests use the following default configuration:

- **Base URL**: `http://localhost:8085` (configurable via `BASE_URL` environment variable)
- **AAS Identifier**: `https://mm-software.com/ids/aas/000-001`
- **Submodel Identifiers**:
  - ContactInformation: `https://mm-software.com/submodel/000-001/ContactInformation`
  - Nameplate: `https://mm-software.com/submodel/000-001/Nameplate`
  - Reliability: `https://mm-software.com/submodel/000-001/Reliability`

All identifiers are automatically Base64 URL encoded in the tests.

## CI/CD

The tests run via GitHub Actions using the `playwright-tests-typescript.yml` workflow, which:

1. Sets up Node.js 20
2. Installs npm dependencies
3. Starts the Docker Compose environment
4. Waits for services to be healthy
5. Runs the Playwright tests
6. Uploads the HTML test report as an artifact
7. Cleans up Docker Compose services
