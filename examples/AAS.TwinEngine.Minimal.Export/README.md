# TwinEngine Export Service Demonstrator

## Overview

This example extends [`AAS.TwinEngine.Minimal`](../AAS.TwinEngine.Minimal/README.md) with a **second,
independent BaSyx Go environment** acting as an external AAS Registry/Repository, and runs the
**Export Service** against it. It demonstrates the full export flow end-to-end: data created, updated,
or deleted at the source is reflected in the target on a time-based schedule — observable live in two
separate AAS Web UIs.

```
SOURCE                                            TARGET
┌───────────────────────────────┐                 ┌──────────────────────────────┐
│ nginx :8080 (aas-ui-source)   │                 │ aas-ui-target :8090          │
│  └─ twinengine-dataengine     │   export-service │  └─ target-registry-         │
│      └─ dpp-plugin            │ ───────────────▶│      repository :8092        │
│      └─ source-registry-      │   (cron, */3min) │      (pre-seeded pair)       │
│          repository :8082     │                 └──────────────────────────────┘
└───────────────────────────────┘
```

## Included Submodel Templates

Same as the Minimal example — 5 Digital Product Passport for Industry 4.0 templates: **Nameplate**,
**MaintenanceInstructions**, **TechnicalData**, **CarbonFootprint**, **HandoverDocumentation**.

## Quick Start

### Prerequisites

- **Docker** (v20.10+) and **Docker Compose** (v1.29+)
- **Available Ports**:
  - `8080` — Source API Gateway (nginx) + source AAS UI (`/aas-ui/`)
  - `8081` — PGAdmin
  - `8082` — Source BaSyx Go registry/repository (debug access)
  - `8085` — DataEngine (debug access)
  - `8086` — DPP Plugin (debug access)
  - `8087` — Export Service (health check only, no public API)
  - `8090` — Target AAS UI
  - `8092` — Target BaSyx Go registry/repository

### Repository Structure

Clone both repositories into the same root directory, as in the Minimal example:

```
TwinEngine/
├── AAS.TwinEngine.DataEngine/
│   ├── source/
│   └── examples/
│       └── AAS.TwinEngine.Minimal.Export/
│           └── docker-compose.yml
└── AAS.TwinEngine.Plugin.DPP/
    └── source/
```

### Running the Demonstrator

```bash
cd AAS.TwinEngine.DataEngine/examples/AAS.TwinEngine.Minimal.Export
docker compose up -d --build
```

The `--build` flag is required here (unlike the Minimal example) because the Export Service has no
published container image yet — it is always built from local source.

### Watching the Export Happen

1. Open the **source** UI: `http://localhost:8080/aas-ui/` — populated immediately, same data as the
   Minimal example.
2. Open the **target** UI: `http://localhost:8090/` — it already contains the
   `PreExistingTargetAsset` shell with its `PreExistingTargetDetails` and `PreExistingTargetNameplate`
   submodels. The `OwnershipMarker` value in the details submodel identifies it as target-owned.
3. Wait for the Export Service's scheduled run (every 3 minutes by default — see
   [Changing the schedule](#changing-the-schedule) to speed this up for a demo).
4. Refresh the target UI — source shells, submodels, and their registry descriptors appear alongside
   the target-owned pair. Confirm the marker value and target-owned shell remain present.
5. Edit or delete data at the source (e.g. via PGAdmin, see the Minimal README) and wait for the next
   run — the change is reflected in the target, while the target-owned pair remains untouched.

You can also watch it happen in the logs:

```bash
docker compose logs -f export-service
```

Look for lines like:

```
Export run started at ...
Phase ConceptDescription finished. Created=X Updated=0 Deleted=0 Skipped=0 Failed=0
Phase Submodel finished. Created=X Updated=0 Deleted=0 Skipped=0 Failed=0
Phase SubmodelDescriptor finished. Created=X Updated=0 Deleted=0 Skipped=0 Failed=0
Phase Shell finished. Created=X Updated=0 Deleted=0 Skipped=0 Failed=0
Phase ShellDescriptor finished. Created=X Updated=0 Deleted=0 Skipped=0 Failed=0
Export run finished at ... with status Success.
```

### Export Ownership and Run Outcomes

The Export Service updates or deletes only entities recorded as exporter-managed in its PostgreSQL
state store. If a create request receives `409 Conflict` for an entity that is not in that ownership
ledger, the export is reported as failed for that entity; the service does not fall back to `PUT` or
overwrite the target-owned entity.

Run statuses distinguish:

- `Success` — no entity operations failed.
- `PartialFailure` — at least one entity operation succeeded and at least one failed.
- `FullFailure` — entity operations failed and none succeeded.
- `Aborted` — a source or unexpected phase failure stopped the run to protect data integrity.

### Changing the Schedule

The default schedule is every 3 minutes (`*/3 * * * *`). To make the demo more responsive, edit the
`ExportService__Scheduler__CronExpression` environment variable for the `export-service` service in
`docker-compose.yml`, e.g. `*/1 * * * *` for every minute, then:

```bash
docker compose up -d export-service
```

HTTP retry and timeout settings are configured under `ExportService:Resilience`. The demo sets a
30-second per-request timeout and retries transient failures using
`InitialDelaySeconds * BackoffMultiplier^(retry attempt - 1)`, with jitter to reduce synchronized
retries. For example, the default 2-second initial delay and multiplier of 2 produce base retry
delays of 2, 4, and 8 seconds before jitter.

## Architecture & Services

| Service                        | Port  | Role                                                               |
| ------------------------------- | ----- | ------------------------------------------------------------------- |
| **nginx**                       | 8080  | Source API gateway & source AAS UI proxy                            |
| **twinengine-dataengine**       | 8085  | Source: aggregates plugin + template data into one API              |
| **dpp-plugin**                  | 8086  | Source: Digital Product Passport plugin (relational DB backed)      |
| **source-registry-repository**  | 8082  | Source: BaSyx Go environment, pre-seeded with the 5 templates        |
| **basyx_configuration_source**  | -     | One-shot schema setup for the source BaSyx Go environment            |
| **aas-ui-source**                | 8080  | Source AAS UI (served through nginx at `/aas-ui/`)                   |
| **export-service**               | 8087  | Reads from source, writes to target, on a cron schedule (`/healthz`) |
| **target-registry-repository**  | 8092  | Target: independent BaSyx Go environment, pre-seeded with a target-only shell/submodel pair |
| **basyx_configuration_target**  | -     | One-shot schema setup for the target BaSyx Go environment             |
| **aas-ui-target**                | 8090  | Target AAS UI, points directly at the target registry/repository     |
| **postgres**                     | -     | Single Postgres instance hosting `twinengine`, `basyxSourceDB`, `basyxTargetDB`, and `exportstate` as separate databases |
| **pgadmin**                      | 8081  | Web UI for managing all four databases                               |

### Why the descriptor hrefs still point at :8080

The Export Service forwards shell/submodel/concept-description payloads **verbatim** — it does not
rewrite `endpoints[].protocolInformation.href`. So descriptors created in the target registry still
reference the source gateway (`http://localhost:8080/...`) for resolving actual AAS/Submodel content,
even though the registry *entry* itself now lives in the target. This mirrors how real AAS registry
federation works (a registry only stores *where* to find data, not the data itself) and is why
`aas-ui-target` can still successfully open shells/submodels it lists, even though they were primarily
exported for their registry entries.

## Resetting the Demo

To reset the target and Export Service state (e.g. after changing the schedule or testing from
scratch), remove the volumes. The pre-existing target shell and submodel are then seeded again from
`target-preconfiguration/`:

```bash
docker compose down -v
docker compose up -d --build
```

This also resets the Export Service's state store, so the next run treats everything as a fresh create.

## Troubleshooting

**Expected entities are missing:** Check `docker compose logs export-service` for `Failed to Create` / `failed
with status` errors. Common causes are documented in the main
[EXPORTER_FLOW.md](../../source/EXPORTER_FLOW.md).

**Export Service won't start:** `docker compose ps` — ensure `postgres` is healthy and
`target-registry-repository` has started before `export-service` (compose `depends_on` handles this,
but a slow first-time image pull can still race).

**Port conflicts:** `netstat -ano | findstr :8092` (Windows) to find conflicts; adjust the host-side
port mapping in `docker-compose.yml`.

## Security Note

Default credentials (`postgres`/`admin`, PGAdmin `admin@example.com`/`admin`) are for **development
only**. Do not use this Docker Compose configuration in production. See the Minimal example's README
for the full security notice.
