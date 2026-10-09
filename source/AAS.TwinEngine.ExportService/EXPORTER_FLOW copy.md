I have what I need. Here's a complete, practical step‑by‑step guide to test the Export Service — from the fastest check (unit tests) to a full live end‑to‑end run that exercises the new **deletion verification** and **parallelism** features.

Note: your `appsettings.json` currently has only **Submodels** enabled on both source *and* target (every other kind has its target disabled), so a live run will sync **submodels only**. That's perfect for a focused test. `MaxDegreeOfParallelism` is `10`.

---

## Level 1 — Unit tests (fastest, no infrastructure)

This validates all the logic you changed: create/update/skip, verified delete (404 → delete, still‑present → skip, transient → fail), and bounded parallelism.

```powershell
cd C:\Projects\t6\AAS.TwinEngine.DataEngine
dotnet test "source\AAS.TwinEngine.ExportService.UnitTests\AAS.TwinEngine.ExportService.UnitTests.csproj" --logger "console;verbosity=minimal"
```

Run just the new behavior:

```powershell
dotnet test "source\AAS.TwinEngine.ExportService.UnitTests\AAS.TwinEngine.ExportService.UnitTests.csproj" `
  --filter "FullyQualifiedName~PhaseExecutorTests|FullyQualifiedName~SourceEntityHttpClientTests"
```

Expect **188 passed**. These prove the behavior deterministically — if you only want to confirm correctness, you can stop here.

---

## Level 2 — Live end‑to‑end run

This runs the real service against a real PostgreSQL state store, a **source** AAS repo, and a **target** AAS repo, then watches it sync.

### Step 0 — Prerequisites
- .NET 10 SDK (`dotnet --version`)
- Docker Desktop running
- A REST tool — PowerShell's `Invoke-RestMethod` is used below

### Step 1 — Start dependencies (Postgres + source + target)

Eclipse BaSyx repos speak exactly the API the exporter expects (`/submodels`, cursor paging, `GET/PUT/DELETE /submodels/{base64url-id}`). Save this as `test-deps.yml` somewhere temporary:

```yaml
services:
  exportstate-db:
    image: postgres:16
    environment:
      POSTGRES_DB: exportstate
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports: ["5432:5432"]

  source-repo:
    image: eclipsebasyx/submodel-repository:2.0.0-milestone-05
    environment:
      BASYX_BACKEND: InMemory
    ports: ["8085:8081"]   # SOURCE on host :8085

  target-repo:
    image: eclipsebasyx/submodel-repository:2.0.0-milestone-05
    environment:
      BASYX_BACKEND: InMemory
    ports: ["8081:8081"]   # TARGET on host :8081
```

```powershell
docker compose -f test-deps.yml up -d
```

This matches your config: source Submodels → `http://localhost:8085`, target Submodels → `http://localhost:8081`.

### Step 2 — Point the state store at that Postgres and trigger quickly

Your connection string has empty credentials and the cron is every 3–5 minutes. Override both with environment variables so you don't edit files (ASP.NET Core maps `__` to nested keys):

```powershell
$env:ExportService__StateStore__ConnectionString = "Host=localhost;Port=5432;Database=exportstate;Username=postgres;Password=postgres"
$env:ExportService__Scheduler__CronExpression = "* * * * *"   # every minute for testing
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

### Step 3 — Run the service

```powershell
cd C:\Projects\t6\AAS.TwinEngine.DataEngine\source\AAS.TwinEngine.ExportService
dotnet run
```

Watch the console. Within a minute you'll see `Export run started…`, `Starting export phase for Submodel`, and `Phase Submodel finished. Created=0 Updated=0 …` (nothing to sync yet). Health check: open another terminal →

```powershell
Invoke-RestMethod http://localhost:5090/healthz   # → Healthy
```

---

## Level 3 — Exercise each operation and observe it

Keep the service running. Use a second PowerShell window.

### Test A — Create
Seed two submodels into the **source**:

```powershell
$h = @{ "Content-Type" = "application/json" }
'{ "id":"https://acme.com/sm/A", "modelType":"Submodel", "idShort":"A" }' |
  % { Invoke-RestMethod -Method Post -Uri http://localhost:8085/submodels -Headers $h -Body $_ }
'{ "id":"https://acme.com/sm/B", "modelType":"Submodel", "idShort":"B" }' |
  % { Invoke-RestMethod -Method Post -Uri http://localhost:8085/submodels -Headers $h -Body $_ }
```

Wait for the next tick. Logs show `Created=2`. Verify the **target** now has them:

```powershell
Invoke-RestMethod http://localhost:8081/submodels | % result | Select id, idShort
```

Verify the **ownership ledger**:

```powershell
docker exec -it $(docker ps -qf name=exportstate-db) `
  psql -U postgres -d exportstate -c "select entity_kind, identifier, left(content_hash,12) as hash from export_service.exported_entities;"
```

### Test B — Skip (unchanged)
Do nothing and wait one tick → logs show `Created=0 Updated=0 Skipped=2`. No HTTP writes happen for unchanged entities (hash match).

### Test C — Update
Change submodel A in the source (new `idShort`):

```powershell
$idA = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("https://acme.com/sm/A")).TrimEnd('=').Replace('+','-').Replace('/','_')
'{ "id":"https://acme.com/sm/A", "modelType":"Submodel", "idShort":"A_v2" }' |
  % { Invoke-RestMethod -Method Put -Uri "http://localhost:8085/submodels/$idA" -Headers $h -Body $_ }
```

Next tick → `Updated=1 Skipped=1`. The target's A becomes `A_v2` and its `content_hash` in the ledger changes.

### Test D — Verified delete (the key new behavior — 404 path)
Delete submodel B from the **source**:

```powershell
$idB = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("https://acme.com/sm/B")).TrimEnd('=').Replace('+','-').Replace('/','_')
Invoke-RestMethod -Method Delete -Uri "http://localhost:8085/submodels/$idB"
```

Next tick, the logs show the **verification flow** in order:
```
Verifying deletion candidate Submodel https://acme.com/sm/B via source GET-by-id.
Deletion verification … source confirmed the entity is deleted. Deleting from target.
Deletion complete … removed from target and ownership state.
```
`Deleted=1`. B disappears from the target **and** from the ledger table.

### Test E — Safety path (entity still exists → NOT deleted)
This proves the exporter won't delete on a pagination glitch. Temporarily shrink the source page size so the paged read can "miss" entities while GET‑by‑ID still finds them — the simplest way to simulate is: stop the service, set the source submodels `Limit` very low and add more than one page of entities, but the cleanest demonstration is the unit test `ExecuteDeletionsAsync_WhenSourceStillReturnsEntity_SkipsDeletionAndKeepsState`. In a live run you'd see:
```
Deletion verification … source still reports the entity as present. Skipping target deletion and keeping ownership state…
```
and the row stays in both target and ledger (`Skipped`, not `Deleted`).

### Test F — Parallelism / scale
Seed many submodels and watch throughput:

```powershell
0..199 | ForEach-Object {
  $id = "https://acme.com/sm/bulk-$_"
  "{ ""id"":""$id"", ""modelType"":""Submodel"", ""idShort"":""bulk$_"" }" |
    % { Invoke-RestMethod -Method Post -Uri http://localhost:8085/submodels -Headers $h -Body $_ }
}
```

Next tick → `Created=200` applied with up to 10 concurrent writes. To compare, restart the service with `$env:ExportService__Performance__MaxDegreeOfParallelism = "1"` and observe the phase takes noticeably longer for the same batch.

---

## Step 4 — Cleanup

```powershell
# stop the service with Ctrl+C in its window, then:
docker compose -f test-deps.yml down -v
Remove-Item Env:ExportService__* , Env:ASPNETCORE_ENVIRONMENT
```

---

### Quick reference — what each result means

| You see in logs | Meaning |
|---|---|
| `Created=N` | New source entities written to target + ledger |
| `Updated=N` | Hash changed → target updated |
| `Skipped=N` | Hash identical, or a delete candidate that **still exists** in source (safely not deleted) |
| `Deleted=N` | Source 404‑confirmed → removed from target + ledger |
| `Failed=N` | Per‑entity error (incl. GET‑by‑ID transient failure) → retried next run, state kept |
| `status Aborted` | Source read failed mid‑pagination → **no deletions** (mass‑delete protection) |

If you'd like, I can drop the `test-deps.yml` compose file into the repo (e.g. under `example`) and add a short seed script so this is one command to run — just say the word.