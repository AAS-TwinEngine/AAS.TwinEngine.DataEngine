# K6 Load Test Guide

This folder contains the K6 script suite for DataEngine API endpoint testing.

## Files

- `main.js`: Entry point. Builds K6 scenarios from config and executes requests.
- `config.js`: Central configuration for load behavior, endpoints, discovery, and env overrides.
- `scenarios.js`: Endpoint catalog (which endpoint key maps to which URL path).
- `setup.js`: Discovers shell and submodel IDs used by ID-based endpoints.
- `requests.js`: Executes HTTP calls, checks status, and records custom duration metrics.
- `summary.json`: Generated output summary from the latest run.
- `results.csv`: Generated CSV metric summary from the latest run.

## How Configuration Works

All defaults are in `config.js` under `defaultConfig`. Values can also be overridden by environment variables.

### 1) Base URL

- Config key: `baseUrl`
- Purpose: Target API host.
- Env override: `BASE_URL`

Example:

```js
baseUrl: "http://localhost:8080"
```

### 2) Load Settings

- Config key: `load.vus`
- Purpose: Number of VUs used by each endpoint scenario.
- Env override: `VUS`

- Config key: `load.maxDuration`
- Purpose: Hard timeout for each endpoint scenario.
- Env override: `MAX_DURATION`

- Config key: `load.gracefulStop`
- Purpose: Extra time K6 gives in-flight iterations to finish after stop.
- Env override: `GRACEFUL_STOP`

Example:

```js
load: {
	vus: 4,
	maxDuration: "10m",
	gracefulStop: "30s"
}
```

### 3) Request Logging

- Config key: `logRequests`
- Purpose: Logs successful and failed requests in console.
- Env override: `LOG_REQUESTS`

Accepted booleans: `true/false`, `1/0`, `yes/no`, `on/off`.

### 4) Discovery Settings

Used by `setup.js` to discover IDs before test execution.

- `discovery.shellsPath` (env: `DISCOVER_SHELLS_PATH`)
- `discovery.submodelDescriptorsPath` (env: `DISCOVER_SUBMODEL_DESCRIPTORS_PATH`)
- `discovery.maxDiscoveredIds` (env: `MAX_DISCOVERED_IDS`)

`maxDiscoveredIds = 0` means no limit.

### 5) Endpoint Scenarios

Endpoint toggles and request volume are defined in `endpoints`.

Each endpoint key supports:

- `enabled`: Whether to run this endpoint scenario.
- `requests`: Total number of requests (iterations) for this endpoint scenario.

Example:

```js
endpoints: {
	getShells: { enabled: true, requests: 10000 },
	getSubmodels: { enabled: false, requests: 10 }
}
```

## How To Set Different Scenarios

You can create different load shapes by changing `enabled` and `requests` per endpoint.

### Smoke scenario (quick)

- Set each enabled endpoint to `requests: 5` or `10`.
- Keep `vus` low (for example `1` or `2`).

### Focused endpoint scenario

- Set one endpoint to high request count.
- Disable others.

Example:

```js
getShells: { enabled: true, requests: 5000 },
getShellById: { enabled: false, requests: 10 }
```

### Mixed scenario

- Keep most endpoints enabled with different request counts based on priority/traffic.

## Environment Variable Overrides

You can override defaults at runtime:

- `BASE_URL`
- `VUS`
- `MAX_DURATION`
- `GRACEFUL_STOP`
- `LOG_REQUESTS`
- `DISCOVER_SHELLS_PATH`
- `DISCOVER_SUBMODEL_DESCRIPTORS_PATH`
- `MAX_DISCOVERED_IDS`
- `ENABLED_ENDPOINTS` (comma-separated keys)
- `DISABLED_ENDPOINTS` (comma-separated keys)
- `ENDPOINT_REQUESTS` (comma-separated `key:value`, for example `getShells:100,getAssetInformation:20`)

### Example (PowerShell)

```powershell
$env:BASE_URL = "http://localhost:8080"
$env:VUS = "2"
$env:GRACEFUL_STOP = "0s"
$env:ENDPOINT_REQUESTS = "getShells:50,getShellById:25"
k6 run main.js
```

## How To Run

From this folder:

```powershell
k6 run main.js
```

Or from repository root:

```powershell
k6 run source/Testing/K6/main.js
```

## Visualize With HTML Dashboard

For a quick visual report (interactive during run + HTML file after run):

```powershell
$env:K6_WEB_DASHBOARD = "true"
$env:K6_WEB_DASHBOARD_EXPORT = "k6-report.html"
k6 run main.js
```

What you get:

- Live dashboard URL in terminal while test is running (example: `http://127.0.0.1:5665`).
- Exported HTML report file after test completes.

Report file location:

- `k6-report.html` in the same folder where the command is executed.

To reset this behavior in the current PowerShell session:

```powershell
Remove-Item Env:K6_WEB_DASHBOARD -ErrorAction SilentlyContinue
Remove-Item Env:K6_WEB_DASHBOARD_EXPORT -ErrorAction SilentlyContinue
```

## Where To See Output

### 1) Terminal Output

K6 prints:

- Scenario plan
- Check pass/fail
- HTTP and custom metrics

### 2) JSON Summary File

- File: `summary.json`
- Location: same folder where `k6 run` is executed for this script (typically this K6 folder).
- Contains full run metrics and setup data.

### 3) CSV Metrics File

- File: `results.csv`
- Location: same as above.
- Contains selected endpoint trend metrics:
	- Avg, Min, Max, P90, P95

## Notes

- If no endpoint scenarios are enabled/configured, the script throws an error.
- ID-based endpoints depend on successful discovery in `setup.js`.
- Graceful stop can be set to `0s` for immediate stop behavior.
