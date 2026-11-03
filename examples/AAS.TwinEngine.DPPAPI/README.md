# TwinEngine DPP API Example

## Overview

This example extends the [Minimal.Export example](../AAS.TwinEngine.Minimal.Export/README.md)
with the BaSyx DPP API. The Export Service copies AAS data from DataEngine to
the target BaSyx environment. The DPP API reads the exported data from the
target environment's PostgreSQL database.

## Quick Start

### Prerequisites

- Docker and Docker Compose 2.20.0 or newer.
- Ports `8080`, `8081`, `3000`, `8083`, `8092`, and `9090` available.
- To build locally, clone `AAS.TwinEngine.Plugin.DPP` beside this repository:

  ```text
  TwinEngine/
    AAS.TwinEngine.DataEngine/
    AAS.TwinEngine.Plugin.DPP/
  ```

### Running the Demonstrator

From this folder, start the services:

```powershell
docker compose up -d --build
docker compose ps
```

The first export runs on a three-minute schedule. Check its logs before using
the DPP API:

```powershell
docker compose logs --tail 50 export-service
```

Open Swagger at [http://localhost:8083/swagger](http://localhost:8083/swagger)
or the AAS UI at [http://localhost:8080/aas-ui/](http://localhost:8080/aas-ui/).
Select `Product1` in the UI to view its AAS ID and available submodels.

Stop the services with:

```powershell
docker compose down
```

The database volume is preserved. `docker compose down --volumes` removes the
example data. Do not run another TwinEngine example at the same time because
the examples share container names and ports.

## Architecture & Services

The Compose file includes the Minimal.Export services and adds the DPP API.

| Service | Port | Purpose |
| --- | --- | --- |
| `nginx` | `8080` | Target AAS gateway and UI |
| `target-registry-repository` | `8092` | BaSyx repository and precomputed registries |
| `export-service` | - | Copies data from DataEngine to the target |
| `dpp-api` | `8083` | Provides DPP retrieval operations |
| `postgres` | - | Stores source and target data |
| `pgadmin` | `8081` | PostgreSQL administration |
| `otel-lgtm` | `3000`, `9090` | Grafana and Prometheus |

The DPP API reads from the target database; it does not call DataEngine for
each request.

## DPP API

Use Swagger to call `ReadDPPById` and `ReadDPPByProductId`. Product identifiers
in the shared seed include:

- `https://mm-software.com/ids/aas/000-001`
- `https://mm-software.com/ids/aas/000-002`
- `https://mm-software.com/ids/aas/001-001`

For successful retrieval, the DPP metadata, shell IDs, and referenced submodels
must match the plugin and API mappings. The shared seed currently uses
`https://mm-software.com/ids/assets/000-001` as the global asset ID and
`https://mm-software.com/ids/aas/000-001` as the shell ID. No example-specific
SQL rewrite is applied.

## Troubleshooting

| Issue | Resolution |
| --- | --- |
| DPP retrieval returns `404` | Check that the first export completed and that the DPP metadata, shell IDs, and submodels match the configured mappings. |
| Swagger reports CORS or `Failed to fetch` | In Swagger, use the server at `http://localhost:8083`. Port `8080` is the AAS gateway. |
| Target repository is unhealthy | Check `docker compose logs target-registry-repository basyx_configuration_target postgres` and wait for database initialization. |
| Compose reports an `include` error | Use Docker Compose 2.20.0 or newer. |
