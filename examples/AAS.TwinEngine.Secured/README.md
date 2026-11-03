# TwinEngine Example with Secured AAS Environment

## Overview

This example combines the DataEngine minimal example with a secured AAS environment setup pattern. It keeps the DataEngine DPP plugin, PostgreSQL seed data, and shared template files from the DataEngine examples, and enables Keycloak/OIDC plus ABAC on the template repository/registry service.

The setup is intended for local integration testing of DataEngine header forwarding into a secured AAS environment.

> **Note:** This example uses the BaSyx Go standard AAS APIs for the AAS environment (registry and repository).

DataEngine is built from the local `../../source` tree so this example includes local fixes to the outbound HTTP pipeline.

## What This Combines

From the DataEngine minimal example:

- `twinengine-dataengine` as the orchestration API
- `dpp-plugin` as the DPP data provider
- `postgres` seeded from `../shared/postgres`
- DPP template preconfiguration from `../shared/aas`
- nginx as the single public entry point on `http://localhost:8080`
- AAS UI served behind nginx at `http://localhost:8080/aas-ui/`

From the secured AAS environment example:

- Keycloak realm import and test users
- ABAC enabled on the template repository/registry service
- OIDC trustlist mounted into the AAS environment
- ABAC policy file mounted into the AAS environment
- Keycloak readiness gating before the secured AAS environment and DataEngine start

## Ports

| Service | URL | Purpose |
| --- | --- | --- |
| nginx | `http://localhost:8080` | Public gateway for DataEngine and UI routes |
| template-repository-registry | `http://localhost:8082` | Direct AAS environment endpoint |
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

Reset persisted AAS environment/Postgres state:

```powershell
docker compose down -v
```

## Authentication

The example imports the local Keycloak realm from `./keycloak/realm`.

Default credentials:

| User | Password | Role | Purpose |
| --- | --- | --- | --- |
| `admin` | `pwd` | `admin` | Full access |
| `usera` | `pwd` | `viewer` | Read access to configured secured templates |

The trustlist is configured in `security_env/trustlist.json` and expects tokens issued by:

```text
http://keycloak.localhost:9090/realms/basyx
```

## Header Forwarding

DataEngine does not validate access tokens. It forwards mapped headers to downstream services.

The DataEngine environment variables in `docker-compose.yml` configure:

- `Authorization` -> `Authorization` for template repository and registry calls
- `Authorization` -> `X-Auth-Token` for plugin calls
- `X-Organization-Id` -> `X-Tenant-Context` for plugin calls
- `X-Correlation-Id` -> `X-Request-Id` for plugin calls

The local nginx config in `nginx/default.conf.template` forwards `Authorization` into DataEngine for all DataEngine-routed paths. This is required because the shared nginx config only forwards `Authorization` for the UI route.

## Verify Header Forwarding

This request uses a deliberately fake token. If the token reaches the AAS environment, it should reject it as an invalid JWT with `401`. If the token is missing, it usually returns an ABAC `403`.

```powershell
$submodelId = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('https://mm-software.com/submodel/000-001/TechnicalData')).TrimEnd('=')
curl.exe -i -H "Authorization: Bearer ABC" "http://localhost:8080/submodels/$submodelId"
```

For packet-level confirmation, capture DataEngine traffic to the AAS environment:

```powershell
docker run --rm --network container:twinengine-dataengine nicolaka/netshoot tcpdump -A -s 0 -i any "tcp port 8082"
```

Then send the request above. The captured outbound request should contain:

```http
Authorization: Bearer ABC
```

## Configuration Notes

- DataEngine configuration is defined directly in the `twinengine-dataengine` service environment.
- ABAC uses `security_env/access-rules.json`.
- `ABAC_POLICY_FILE_IMPORT=always` keeps the local JSON policy authoritative during local restarts.
- The ABAC rules are aligned to the DataEngine DPP template IDs, not the IDs of the standalone sample AASX files.
- Keycloak runs on host port `9090` so nginx can keep port `8080` as the DataEngine gateway.

## Troubleshooting

**Header reaches DataEngine but not the AAS environment:** Check the `TemplateManagement__*__headerMappings` variables and set a breakpoint in `RequestHeaderMapper.ApplyMappings`.

If the mappings are loaded but the outbound request still has no `Authorization` header, rebuild the DataEngine image used by this compose file. Older published `develop` images can run the forwarding handler inside the resilience pipeline, where the request context is not available.

**Header does not reach DataEngine:** Check `nginx/default.conf.template` for `proxy_set_header Authorization $http_authorization;` on the relevant route.

**The AAS environment returns `401`:** The token reached it but is not a valid trusted JWT.

**The AAS environment returns `403`:** The token is absent or valid but not allowed by `security_env/access-rules.json`.

**Policy changes do not apply:** Run `docker compose down -v` and restart, or keep `ABAC_POLICY_FILE_IMPORT=always` enabled.
