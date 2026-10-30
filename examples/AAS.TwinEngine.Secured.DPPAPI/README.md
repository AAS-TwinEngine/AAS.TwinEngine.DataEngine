# Secured TwinEngine DPP API Example

This example imports [Secured.Export](../AAS.TwinEngine.Secured.Export/README.md) and adds
a DPP API container with a route-based policy:

| Request | Anonymous | `usera` (viewer) | `admin` / exporter service account |
| --- | --- | --- | --- |
| Current DPP by ID, for the three seeded products | Allowed | Allowed | Allowed |
| DPP by ID and date | Denied | Allowed | Allowed |
| Element and product lookup routes | Denied | Read | Read |
| DPP create, update and delete | Denied | Denied | Allowed |
| Target AAS, Submodel and registry APIs | Denied | Read | Read and write |

Anonymous current reads intentionally expose all selected content in those passports. A date query
does not make the current endpoint historical: use `/v1/dppsByIdAndDate/{id}` for historical reads.
An invalid/expired bearer token is not anonymous access; remove it for a public request.

## Composition

There is one [Compose file](docker-compose.yml). Its top-level `include` imports Secured.Export
(which imports Minimal.Export), reusing Keycloak, Authorization forwarding and OAuth exporter
settings without `extends` blocks. The DPP API container is defined locally and shares the target
database. Local overrides require the exporter secret, enable audit history on the target AAS
Environment, and apply this example's policy to both the target and DPP API.

The [local policy](security_env/access-rules.json) grants anonymous reads only for these exact
current DPP routes (both encoded and decoded policy forms are supplied for URL-style IDs):

- `https://mm-software.com/ids/aas/000-001`
- `https://mm-software.com/ids/aas/000-002`
- `https://mm-software.com/ids/aas/001-001`

Add corresponding exact encoded and decoded `/v1/dpps/{id}` entries in `current_dpp.objects` to publish
another passport. Do not replace these with `/v1/dpps/*`: that wildcard also matches element
subroutes. Historical routes are separate and have no anonymous permission.

The source uses the existing Secured.Export template policy. The target and DPP API use this
example's policy. Direct anonymous target reads are disabled so that AAS history cannot bypass
the protected DPP history route. The target advertises the same `http://localhost:8080` gateway
used by the UI, avoiding a separate trusted-origin requirement for newly created descriptors.
Existing descriptors are not rewritten.

## Start

Requirements:

- Docker Compose v5.1.4 or newer (the include/local customization behavior is validated on v5.1.4).
- The repositories and local build prerequisites described in Minimal.Export.
- Free ports `8080`, `8081`, `8083`, `8092`, `9090`, `9091`, and `3000`.
- `keycloak.localhost` resolves to `127.0.0.1` for browser login. Add it to the hosts file if needed.

Stop other TwinEngine examples first, from their own folders, using `docker compose down`
**without `--volumes`**. Examples share container names, ports and a Docker network name.
This example uses its own `twinengine-secured-dppapi_postgres-data` volume; it does not migrate
data from another example.

From this folder in PowerShell:

```powershell
$realm = Get-Content -Raw ..\AAS.TwinEngine.Secured\keycloak\realm\basyx-realm.json |
    ConvertFrom-Json
$env:KEYCLOAK_AAS_ENV_CLIENT_SECRET = (
    $realm.clients | Where-Object { $_.clientId -eq 'aas-env' }
).secret
docker compose config --quiet
docker compose up -d --build
docker compose ps
docker compose logs --tail 50 export-service
```

Use the current `aas-env` secret if you have changed it in Keycloak. Do not commit the secret
or rendered Compose output. Load the variable again in a new terminal.
Wait for an export ending in `with status Success` (the default schedule is every three minutes).

| URL | Purpose |
| --- | --- |
| <http://localhost:8083/swagger> | DPP API Swagger; use server `http://localhost:8083` |
| <http://localhost:8080/aas-ui/> | Target AAS UI; sign in to browse the protected target |
| <http://keycloak.localhost:9090> | Keycloak |
| <http://localhost:8081> | pgAdmin |
| <http://localhost:3000> | Grafana |
| <http://localhost:9091> | Prometheus; `9090` is reserved for Keycloak |

The demo realm contains `usera` / `pwd` (viewer) and `admin` / `pwd` (admin).
These and all database credentials are local development credentials, not production settings.
DataEngine/plugin authentication is not independently added by this example; source template
access is governed by BaSyx ABAC, with Authorization forwarded by DataEngine.

## Public current DPP, protected history

After the first successful export:

```powershell
$dppId = 'https://mm-software.com/ids/aas/000-001'
$path = [Uri]::EscapeDataString($dppId)
$base = 'http://localhost:8083'

# No bearer token required.
Invoke-RestMethod "$base/v1/dpps/$path"

# Request a viewer token. localhost works in PowerShell even on systems whose
# DNS resolver does not resolve keycloak.localhost; the issuer remains keycloak.localhost.
$token = Invoke-RestMethod -Method Post `
    -Uri 'http://localhost:9090/realms/basyx/protocol/openid-connect/token' `
    -Body @{
        grant_type = 'password'
        client_id = 'basyx-ui'
        username = 'usera'
        password = 'pwd'
    }
$headers = @{ Authorization = "Bearer $($token.access_token)" }
$date = [Uri]::EscapeDataString(
    [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss.ffffffZ')
)
$historyUrl = "$base/v1/dppsByIdAndDate/$($path)?date=$date"

# This request succeeds with the viewer token.
Invoke-RestMethod $historyUrl -Headers $headers

# Without -Headers, the same history request is denied (403).
```

Password grant is included only for a local command-line demonstration. Use an appropriate
interactive OIDC flow for an application; a successful login must carry an allowed role.
For authenticated requests, use the PowerShell example or Postman's **Authorization > Bearer Token**.
The bundled DPP Swagger schema does not expose a bearer authorization control; its unauthenticated
Try it out requests can demonstrate public current reads and denied historical reads.

## Demonstrate before/after content

1. Wait until the public current DPP shows the initial exported values.
2. Save `$beforeDate = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss.ffffffZ')`.
3. Update source data through the existing DataEngine/plugin workflow.
4. Wait for a successful export and confirm current DPP shows the new values.
5. Query the history endpoint with `$beforeDate` and a viewer/admin bearer token.

The selected time must be after initial export and before the update reaches the **target**.
It is not the source edit time or DppMetadata's `lastUpdate`. Each exported Submodel is written
separately, so wait for the full export to finish before capturing an after-update time.
You can also update a content Submodel with an admin bearer token using
`PUT http://localhost:8092/submodels/{base64url-submodel-id}`. Use the direct repository port:
the inherited gateway limits writes, even for an authenticated admin. Content-only AAS updates
are recorded in history without automatically changing DppMetadata's `lastUpdate`.

If you forgot to capture a date, run this in pgAdmin against `basyxTargetDB`:

```sql
SELECT identifier, operation,
       to_char(operation_time AT TIME ZONE 'UTC',
               'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') AS event_date,
       to_char((operation_time - interval '1 microsecond') AT TIME ZONE 'UTC',
               'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') AS immediately_before_event
FROM submodel_history
WHERE identifier LIKE 'https://mm-software.com/submodel/000-001/%'
ORDER BY operation_time DESC, history_id DESC;
```

## Troubleshooting

- **Port 9090 allocated:** this example remaps LGTM's Prometheus to 9091. Stop another stack
  still publishing 9090; do not change the OIDC issuer port independently of its trustlist.
- **403 on historical DPP:** supply a valid viewer/admin token. `userb` has no allowed role.
- **401 with a bearer token:** obtain a fresh token and check the issuer/trustlist.
- **404 on historical DPP:** choose a timestamp after the shell and referenced Submodels existed.
- **Current DPP is 404:** wait for the first export; the shell ID must match metadata's DPP ID.
- **UI shows no shells anonymously:** expected; sign in. The public surface is the current DPP route.
- **Policy edits:** recreate/restart `dpp-api` and `target-registry-repository` to import the file.
- **Export auth binding error:** use `OAuth2ClientCredentials`, not `OAuthClientCredentials`.

Stop with `docker compose down`. Preserve volumes to keep history. `docker compose down --volumes`
irreversibly removes this example's databases, Keycloak state and export ownership ledger.
