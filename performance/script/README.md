# K6 Script (Short Guide)

This folder contains the K6 test script for DataEngine endpoints.

## Files

- `main.js`: K6 entry script
- `helpers/config.js`: reads `.env` and builds runtime config
- `helpers/setup.js`: discovers shell/submodel IDs
- `helpers/scenarios.js`: endpoint definitions and metric names
- `helpers/requests.js`: request execution and custom metrics
- `.env`: test configuration values
- `results/summary.json`: JSON summary output
- `results/results.csv`: CSV summary output
- `results/k6-summary-report.html`: HTML dashboard export (when enabled via terminal env)

## Run

From this folder:

```powershell
k6 run main.js
```

## Run With K6 Summary HTML

Set dashboard env vars in terminal, then run:

```powershell
$env:K6_WEB_DASHBOARD = "true"
$env:K6_WEB_DASHBOARD_EXPORT = "results/k6-summary-report.html"
k6 run main.js
```

CMD equivalent:

```cmd
set K6_WEB_DASHBOARD=true
set K6_WEB_DASHBOARD_EXPORT=results/k6-summary-report.html
k6 run main.js
```

## Output

- CSV: `results/results.csv`
- JSON: `results/summary.json`
- HTML: `results/k6-summary-report.html` (only when dashboard env vars are set)
