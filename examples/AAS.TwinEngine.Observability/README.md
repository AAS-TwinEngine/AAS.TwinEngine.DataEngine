# TwinEngine Observability Example

This example runs TwinEngine DataEngine with the BaSyx Go services and a local observability stack. It includes the relational database plugin, PostgreSQL, pgAdmin, Grafana, Prometheus, OpenTelemetry, Docker resource metrics, and the BaSyx web UI behind Nginx.

## Prerequisites

- Docker Desktop with Docker Compose
- Ports `8080`, `8081`, `3000`, `9090`, and `9999` available
- At least 4 GB of memory available for the default resource limits

For local image builds, clone the DataEngine and DPP plugin repositories next to each other:

```text
TwinEngine/
  AAS.TwinEngine.DataEngine/
  AAS.TwinEngine.Plugin.DPP/
```

## Start the Stack

From this directory:

```powershell
docker compose up -d
```

To pull current images and rebuild local services:

```powershell
docker compose up -d --pull always --build
```

Check the service state:

```powershell
docker compose ps
```

Stop the stack:

```powershell
docker compose down
```

To remove the database volume as well, use `docker compose down -v`. This permanently removes local database data.

## Endpoints

| Endpoint                                  | Purpose                                 |
| ----------------------------------------- | --------------------------------------- |
| `http://localhost:8080/`                  | DataEngine gateway; redirects to the UI |
| `http://localhost:8080/aas-ui/`           | BaSyx web UI                            |
| `http://localhost:8080/shell-descriptors` | AAS descriptor API                      |
| `http://localhost:8081`                   | pgAdmin                                 |
| `http://localhost:3000`                   | Grafana and LGTM UI                     |
| `http://localhost:9090`                   | Prometheus                              |
| `localhost:9999`                          | PostgreSQL host port                    |

The default pgAdmin credentials are:

- Email: `admin@example.com`
- Password: `admin`

Inside the Compose network, PostgreSQL is available at host `postgres`, port `5432`, user `postgres`, and password `admin`. The plugin uses database `twinengine`; BaSyx configuration uses `basyxTestDB`.

## Services

| Service                        | Image or build                                     | Purpose                                              |
| ------------------------------ | -------------------------------------------------- | ---------------------------------------------------- |
| `nginx`                        | `nginx:1.31.2`                                     | API gateway and UI proxy                             |
| `twinengine-dataengine`        | Local build or GHCR image                          | TwinEngine DataEngine with OpenTelemetry export      |
| `dpp-plugin`                   | Local build or GHCR image                          | Relational database plugin with OpenTelemetry export |
| `template-repository-registry` | `eclipsebasyx/aasenvironment-go:1.0.12`            | BaSyx Go AAS environment and repositories            |
| `basyx_configuration`          | `eclipsebasyx/basyxconfigurationservice-go:1.0.12` | BaSyx Go database configuration initialization       |
| `postgres`                     | `postgres:16-alpine`                               | Plugin and BaSyx persistence                         |
| `aas-web-ui`                   | `eclipsebasyx/aas-gui:v2-260801`                   | BaSyx web UI                                         |
| `otel-lgtm`                    | `grafana/otel-lgtm:0.29.0`                         | Grafana, Prometheus, and OTLP backends               |
| `otel-docker-stats-collector`  | OpenTelemetry Collector                            | Docker container metrics collection                  |
| `docker-resource-exporter`     | Local build                                        | Docker resource metrics exporter                     |
| `pgadmin`                      | `dpage/pgadmin4:9.16`                              | PostgreSQL administration                            |

BaSyx Go configuration service version `1.0.12` requires PostgreSQL 16 or newer. The Compose file uses PostgreSQL 16 accordingly.

## Database Bulk Loader

The `databaseBulkDataLoader` folder contains a separate one-shot PostgreSQL client container. It does not start a database server and requires an already initialized schema.

Create `databaseBulkDataLoader/.env`:

```env
PG_CONN_STRING=postgresql://postgres:admin@localhost:9999/twinengine
ASSET_COUNT=1000
BATCH_SIZE=100
```

Run it from the loader directory:

```powershell
Set-Location databaseBulkDataLoader
docker compose up
```

The loader checks the schema, truncates existing data, and loads the requested number of assets. It exits with code `0` on success.

## K6 Performance Tests

The K6 scripts are under `script`. K6 must be installed locally. Create `script/.env` using the settings documented in [script/README.md](script/README.md), then run:

```powershell
Set-Location script
k6 run main.js
```

The default test base URL is `http://localhost:8080`. Results are written to `script/results`.

To generate the HTML dashboard report in PowerShell:

```powershell
$env:K6_WEB_DASHBOARD="true"
$env:K6_WEB_DASHBOARD_EXPORT="results/k6-summary-report.html"
k6 run main.js
```

## Resource Configuration

CPU and memory limits can be set in `.env` in this directory:

```env
TWINENGINE_CPU=0.5
TWINENGINE_MEMORY=1g
DPP_PLUGIN_CPU=0.5
DPP_PLUGIN_MEMORY=1g
POSTGRES_CPU=2
POSTGRES_MEMORY=4g
```

These values are used by the `twinengine-dataengine`, `dpp-plugin`, and `postgres` services.

## Troubleshooting

Inspect the BaSyx and database startup logs:

```powershell
docker compose logs basyx_configuration
docker compose logs postgres
docker compose logs template-repository-registry
```

If the configuration service reports that PostgreSQL 16 is required, verify that the Compose file uses `postgres:16-alpine`. If an existing data volume was created with PostgreSQL 14 or another major version, use a new volume or migrate the data before starting PostgreSQL 16. Do not delete a volume that contains data you need.

If the UI does not load, check that Nginx is running and that its template points to `template-repository-registry:8082`:

```powershell
docker compose ps nginx template-repository-registry
docker compose logs nginx
```

Do not run the Minimal and Observability examples at the same time without changing their fixed container names and host ports.

The default credentials and exposed ports are for local development only.
