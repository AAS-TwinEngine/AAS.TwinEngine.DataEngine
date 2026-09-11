# TwinEngine DataEngine with Secured BaSyx

## Overview

This example combines the DataEngine minimal example with the secured BaSyx setup pattern. It keeps the DataEngine DPP plugin, PostgreSQL seed data, and shared template files from the DataEngine examples, and enables Keycloak/OIDC plus BaSyx ABAC on the BaSyx template repository/registry service.

The setup is intended for local integration testing of DataEngine header forwarding into a secured BaSyx environment.

DataEngine is built from the local `../../source` tree so this example includes local fixes to the outbound HTTP pipeline.

## What This Combines

From the DataEngine minimal example:

- `twinengine-dataengine` as the orchestration API
- `dpp-plugin` as the DPP data provider
- `postgres` seeded from `../shared/postgres`
- DPP template preconfiguration from `../shared/aas`
- nginx as the single public entry point on `http://localhost:8080`
- AAS UI served behind nginx at `http://localhost:8080/aas-ui/`

From the secured BaSyx example:

- Keycloak realm import and test users
- BaSyx ABAC enabled on the template repository/registry service
- OIDC trustlist mounted into BaSyx
- ABAC policy file mounted into BaSyx
- Keycloak readiness gating before secured BaSyx and DataEngine start

## Ports

| Service | URL | Purpose |
| --- | --- | --- |
| nginx | `http://localhost:8080` | Public gateway for DataEngine and UI routes |
| template-repository-registry | `http://localhost:8082` | Direct BaSyx AAS environment endpoint |
| Keycloak | `http://keycloak.localhost:9090` | OIDC issuer and admin console |
| PGAdmin | `http://localhost:8081` | PostgreSQL administration |

## Start The Example

From this folder:

```powershell
docker compose up -d
```

Stop containers:

```powershell
docker compose down
```

Reset persisted BaSyx/Postgres state:

```powershell
docker compose down -v
```

## Authentication

The example reuses the Keycloak realm from `../securedExample/keycloak/realm`.

Default credentials:

| User | Password | Role | Purpose |
| --- | --- | --- | --- |
| `admin` | `pwd` | `admin` | Full access |
| `usera` | `pwd` | `viewer` | Read access to configured secured templates |

The BaSyx trustlist is configured in `security_env/trustlist.json` and expects tokens issued by:

```text
http://keycloak.localhost:9090/realms/basyx
```

## Header Forwarding

DataEngine does not validate access tokens. It forwards mapped headers to downstream services.

The DataEngine override file `dataengine-env.json` configures:

- `Authorization` -> `Authorization` for BaSyx template repository and registry calls
- `Authorization` -> `X-Auth-Token` for plugin calls
- `X-Organization-Id` -> `X-Tenant-Context` for plugin calls
- `X-Correlation-Id` -> `X-Request-Id` for plugin calls

The local nginx config in `nginx/default.conf.template` forwards `Authorization` into DataEngine for all DataEngine-routed paths. This is required because the shared nginx config only forwards `Authorization` for the UI route.

## Verify Header Forwarding

This request uses a deliberately fake token. If the token reaches BaSyx, BaSyx should reject it as an invalid JWT with `401`. If the token is missing, BaSyx usually returns an ABAC `403`.

```powershell
$submodelId = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('https://mm-software.com/submodel/000-001/TechnicalData')).TrimEnd('=')
curl.exe -i -H "Authorization: Bearer ABC" "http://localhost:8080/submodels/$submodelId"
```

For packet-level confirmation, capture DataEngine traffic to BaSyx:

```powershell
docker run --rm --network container:twinengine-dataengine nicolaka/netshoot tcpdump -A -s 0 -i any "tcp port 8082"
```

Then send the request above. The captured outbound request should contain:

```http
Authorization: Bearer ABC
```

## Configuration Notes

- `dataengine-env.json` is mounted to `/config/dataengine-env.json`, which DataEngine loads at startup.
- BaSyx ABAC uses `security_env/access-rules.json`.
- `ABAC_POLICY_FILE_IMPORT=always` keeps the local JSON policy authoritative during local restarts.
- The ABAC rules are aligned to the DataEngine DPP template IDs, not the standalone BaSyx DriveMotor AASX IDs.
- Keycloak runs on host port `9090` so nginx can keep port `8080` as the DataEngine gateway.

## Troubleshooting

**Header reaches DataEngine but not BaSyx:** Check `dataengine-env.json` mappings and set a breakpoint in `RequestHeaderMapper.ApplyMappings`.

If the mappings are loaded but the outbound request still has no `Authorization` header, rebuild the DataEngine image used by this compose file. Older published `develop` images can run the forwarding handler inside the resilience pipeline, where the request context is not available.

**Header does not reach DataEngine:** Check `nginx/default.conf.template` for `proxy_set_header Authorization $http_authorization;` on the relevant route.

**BaSyx returns `401`:** The token reached BaSyx but is not a valid trusted JWT.

**BaSyx returns `403`:** The token is absent or valid but not allowed by `security_env/access-rules.json`.

**Policy changes do not apply:** Run `docker compose down -v` and restart, or keep `ABAC_POLICY_FILE_IMPORT=always` enabled.
