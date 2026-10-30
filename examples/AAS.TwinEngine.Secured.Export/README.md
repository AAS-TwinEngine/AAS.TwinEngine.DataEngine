# TwinEngine Export Service Demonstrator

## Overview

This example includes services from [`AAS.TwinEngine.Minimal.Export`](../AAS.TwinEngine.Minimal.Export/README.md)
and adds secured settings using the realm from
[`AAS.TwinEngine.Secured`](../AAS.TwinEngine.Secured/README.md). Both the source and the independent
target BaSyx environments enable ABAC. The **Export Service** obtains OAuth client-credentials tokens
for source reads and target writes. It demonstrates
the full export flow end-to-end: data created, updated, or deleted at the source is reflected in the
target on a time-based schedule — observable live in two separate AAS Web UIs.

```
SOURCE                                            TARGET
┌───────────────────────────────┐                 ┌──────────────────────────────┐
│ nginx :8080 (aas-ui-source)   │                 │ aas-ui-target :8090          │
│  └─ twinengine-dataengine     │   OAuth bearer  │  └─ target-registry-         │
│      └─ dpp-plugin            │ ───────────────▶│      repository :8092        │
│      └─ secured BaSyx :8082   │   (cron, */3min) │      (secured)               │
│          + Keycloak :9090    │                 └──────────────────────────────┘
└───────────────────────────────┘
```

## Included Submodel Templates

Inherited from Minimal.Export: **Nameplate**, **MaintenanceInstructions**, **TechnicalData**,
**CarbonFootprint**, **HandoverDocumentation**, and **DppMetadata**. The target's access policy has
separate allowlists; inheriting a template does not automatically grant anonymous or viewer access.

## Compose Reuse

All secured settings are in [`docker-compose.yml`](docker-compose.yml); there is no separate override
file. A single top-level `include` reuses
`../AAS.TwinEngine.Minimal.Export/docker-compose.yml`, including images, database initialization,
template mappings, the export schedule, health checks, and telemetry.

Local service settings customize the included services. The same file adds Keycloak, Authorization header forwarding, source and target ABAC settings,
OAuth client credentials, and the two UI entrypoints. Local bind mounts resolve from this example;
included bind mounts resolve from Minimal.Export via `project_directory`. The source gateway replaces the
minimal target gateway, and Prometheus uses port `9091`, leaving `9090` for Keycloak.
Minimal.Export itself is unchanged.

## Quick Start

### Prerequisites

- **Docker** and **Docker Compose v5.1.4+** (validated with top-level `include`, local service
  customization, and `!override`).
- **Available Ports**:
  - `8080` — Source API Gateway (nginx) + source AAS UI (`/aas-ui/`)
  - `8081` — PGAdmin
  - `8082` — Source BaSyx Go registry/repository (debug access)
  - `8085` — DataEngine (debug access)
  - `8087` — Export Service (health check only, no public API)
  - `8090` — Target AAS UI
  - `8092` — Target BaSyx Go registry/repository
  - `9090` — Keycloak for both secured environments
  - `3000` — Grafana
  - `9091` — Prometheus

The DPP Plugin is internal-only. Ensure `keycloak.localhost` resolves to `127.0.0.1` in your browser.
If it does not, add that mapping to your hosts file. The browser issuer URL must match the shared
OIDC trustlist: `http://keycloak.localhost:9090/realms/basyx`.

### Repository Structure

Clone both repositories into the same root directory, as in the Minimal example:

```
TwinEngine/
├── AAS.TwinEngine.DataEngine/
│   ├── source/
│   └── examples/
│       └── AAS.TwinEngine.Secured.Export/
│           └── docker-compose.yml
└── AAS.TwinEngine.Plugin.DPP/
    └── source/
```

### Running the Demonstrator

From this example's folder, load the development `aas-env` client secret from the imported realm.
If the realm was already customized in Keycloak, use its current secret instead.

```powershell
cd AAS.TwinEngine.DataEngine\examples\AAS.TwinEngine.Secured.Export
$realm = Get-Content -Raw ..\AAS.TwinEngine.Secured\keycloak\realm\basyx-realm.json |
    ConvertFrom-Json
$env:KEYCLOAK_AAS_ENV_CLIENT_SECRET = (
    $realm.clients | Where-Object { $_.clientId -eq 'aas-env' }
).secret
docker compose config --quiet
docker compose up -d --build
```

The client secret is required; Compose reports an explicit error if it is missing. Do not commit
secrets or rendered Compose output. Stop another running TwinEngine example before starting this
one: the examples share container names, ports, and a network name. Do not delete database volumes
just to switch examples.

The `--build` flag is required here (unlike the Minimal example) because the Export Service has no
published container image yet — it is always built from local source.

### Watching the Export Happen

1. Open the **source** UI: `http://localhost:8080/aas-ui/` — populated immediately, same data as the
   Minimal example, with role-based access through the secured source template repository.
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

This target runs BaSyx with registry integration enabled, so it creates the shell and submodel
descriptors itself while handling the exporter's shell and submodel writes. The exporter's own
descriptor create then returns `409` for a descriptor that belongs to an entity it owns. Because an
AAS descriptor carries the same identifier as the shell or submodel it describes, the exporter
reconciles that descriptor with the source and records ownership of it. The adoption applies only to
`ShellDescriptor` and `SubmodelDescriptor`, and only when the described `Shell` or `Submodel` is
already in the ownership ledger. Shells, submodels, concept descriptions, and descriptors of
unmanaged entities are still never modified on conflict.

In the logs a reconciled descriptor appears as an `Updated` operation:

```
Create for ShellDescriptor https://mm-software.com/ids/aas/000-001 conflicted with a descriptor
the target generated for a shell or submodel this exporter owns. Reconciling it with the source
and recording ownership.
Phase ShellDescriptor finished. Created=0 Updated=3 Deleted=0 Skipped=0 Failed=0
```

The next unchanged run reports `Skipped` for the same descriptors.

Run statuses distinguish:

- `Success` — no entity operations failed.
- `PartialFailure` — at least one entity operation succeeded and at least one failed.
- `FullFailure` — entity operations failed and none succeeded.
- `Aborted` — a source or unexpected phase failure stopped the run to protect data integrity.

### Changing the Schedule

The default schedule is every 3 minutes (`*/3 * * * *`), inherited from Minimal.Export. To make
the demo more responsive, add `ExportService__Scheduler__CronExpression: "*/1 * * * *"` to
`export-service.environment` in [`docker-compose.yml`](docker-compose.yml), then:

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
| **dpp-plugin**                  | -     | Source: Digital Product Passport plugin (relational DB backed)      |
| **source-registry-repository**  | 8082  | Secured source: BaSyx Go environment, pre-seeded with shared templates |
| **basyx_configuration_source**  | -     | One-shot schema setup for the source BaSyx Go environment            |
| **aas-ui-source**                | 8080  | Source AAS UI (served through nginx at `/aas-ui/`)                   |
| **export-service**               | 8087  | Reads from source, writes to target, on a cron schedule (`/healthz`) |
| **target-registry-repository**  | 8092  | Target: independent BaSyx Go environment, pre-seeded with a target-only shell/submodel pair |
| **basyx_configuration_target**  | -     | One-shot schema setup for the target BaSyx Go environment             |
| **aas-ui-target**                | 8090  | Target AAS UI, points directly at the target registry/repository     |
| **keycloak**                     | 9090  | OIDC issuer for both secured BaSyx environments                      |
| **otel-lgtm**                    | 3000 / 9091 | Grafana / Prometheus; telemetry inherited from Minimal.Export |
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

### Authentication

Both BaSyx environments enable ABAC and share the OIDC trustlist and Keycloak realm from
[`AAS.TwinEngine.Secured`](../AAS.TwinEngine.Secured/README.md). The target uses its own
[`security_env/access-rules.json`](security_env/access-rules.json). The Compose file mounts this
policy without changing the shared secured example. Export Service obtains an
OAuth client-credentials token for the `aas-env` service account for all source and target
endpoints. As in Minimal.Export, it reads shells and submodels directly from DataEngine and
concept descriptions from the source BaSyx repository. DataEngine forwards Authorization to its
template repositories and registries; its own `/healthz` remains the exporter readiness endpoint.

The target policy matches exported instance IDs, not template IDs:

- Anonymous: read the three exported product shells and their Nameplate and CarbonFootprint
   submodels (six submodels total), including their registry descriptors.
- `usera` / `pwd` (`viewer`): read all five exported submodel types for each product (15 total),
   including public content and registry descriptors. Writes are denied.
- `admin` / `pwd` and the export service account: full API access, including target-owned entities.

The allowlist covers products `000-001`, `000-002`, and `001-001`. Add corresponding shell, submodel,
and descriptor entries when introducing products. The target-owned preconfigured entities are not
included in the anonymous or viewer allowlists.

Open the target UI at `http://localhost:8090/`; its repository API uses `http://localhost:8092`.
Both UIs use Keycloak at `http://keycloak.localhost:9090`.
After editing the target policy, reload it without deleting database volumes:

```powershell
docker compose restart target-registry-repository
```

These instance allowlists protect the target API. Source access is governed by the shared source
template policy, not the target's instance allowlist. Exported descriptor endpoints can resolve
content through the source gateway, so both policies matter. This configuration enables ABAC at the
BaSyx layer; it does not add an independent authentication middleware to DataEngine or the DPP Plugin.

Set `KEYCLOAK_AAS_ENV_CLIENT_SECRET` in the environment as shown above, using the secret matching
the imported realm. The example does not embed a default client secret in its Compose file.
These credentials are for local demonstration only.

## Resetting the Demo

To reset the target and Export Service state (e.g. after changing the schedule or testing from
scratch), remove the volumes. The pre-existing target shell and submodel are then seeded again from
`target-preconfiguration/`:

```bash
docker compose down -v
docker compose up -d --build
```

This deletes all databases in this example's PostgreSQL volume, including source data, the Keycloak
realm state, and the Export Service's ownership ledger. The next run treats exports as fresh creates.
Do not use this reset when you need to preserve demo updates or history.

## Troubleshooting

**Compose reports `KEYCLOAK_AAS_ENV_CLIENT_SECRET` is missing:** Run the realm-secret loading
commands in [Running the Demonstrator](#running-the-demonstrator) in the same PowerShell terminal
before invoking Compose. Environment variables set in another terminal do not carry over.

**Export Service restarts with an invalid `AuthKind`:** The supported OAuth value is
`OAuth2ClientCredentials`, not `OAuthClientCredentials`. Use it for every source and target
`Auth__Kind` setting, then recreate the exporter with `docker compose up -d export-service`.
Check `http://localhost:8087/healthz` and wait for an export log ending in `with status Success`.

**Expected entities are missing:** Check `docker compose logs export-service` for `Failed to Create` / `failed
with status` errors. Common causes are documented in the main
[EXPORTER_FLOW.md](../../source/EXPORTER_FLOW.md).

**Export Service won't start:** `docker compose ps` — ensure `postgres` is healthy and
`target-registry-repository` has started before `export-service` (compose `depends_on` handles this,
but a slow first-time image pull can still race).

**Port conflicts:** `netstat -ano | findstr :8092` (Windows) to find conflicts; adjust the host-side
port mapping in [`docker-compose.yml`](docker-compose.yml).

## Security Note

Default credentials (`postgres`/`admin`, PGAdmin `admin@example.com`/`admin`) are for **development
only**. Do not use this Docker Compose configuration in production. See the Minimal example's README
for the full security notice.
