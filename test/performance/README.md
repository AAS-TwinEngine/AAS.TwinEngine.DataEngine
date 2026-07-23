# DataEngine Performance Test

This folder contains a Docker Compose setup for running a full DataEngine performance environment with:

1. TwinEngine DataEngine
2. DPP plugin
3. PostgreSQL
4. Registry and template services
5. LGTM observability stack (Grafana, Prometheus, OTLP)

## What Is Included

The stack in `docker-compose.yml` starts these main services:

1. `twinengine-dataengine`
2. `dpp-plugin`
3. `postgres`
4. `pgadmin`
5. `otel-lgtm`
6. `otel-docker-stats-collector`
7. `docker-resource-exporter`
8. Supporting services (`nginx`, `mongo`, template repositories, registries, UI)

## What Is Required

1. Docker with Compose support.
2. Access to local container ports listed below.
3. Valid resource settings in `.env`.

## Environment Configuration

This performance stack reads CPU and memory settings from `.env`:

```env
TWINENGINE_CPU=0.5
TWINENGINE_MEMORY=1g

DPP_PLUGIN_CPU=0.5
DPP_PLUGIN_MEMORY=1g

POSTGRES_CPU=4
POSTGRES_MEMORY=8g
```

These values are applied through `cpus` and `mem_limit` in `docker-compose.yml`.

## CPU and Memory Sizing Examples

You can tune container resource usage in `docker-compose.yml`.

Example service settings:

```yaml
services:
  twinengine-dataengine:
    cpus: ${TWINENGINE_CPU:-1}
    mem_limit: ${TWINENGINE_MEMORY:-1g}
```

CPU examples:

```yaml
cpus: 1      # 1 core
cpus: 2      # 2 cores
cpus: 4      # 4 cores
cpus: 0.5    # Half a core
cpus: 1.25   # 1¼ cores
```

Memory examples:

```yaml
mem_limit: 512m   # 512 MB
mem_limit: 1g     # 1 GB
mem_limit: 2g     # 2 GB
mem_limit: 4g     # 4 GB
```

## Run

From this folder:

```bash
docker compose up -d
```

To stop and remove containers/volumes:

```bash
docker compose down -v
```

## Common Endpoints

1. DataEngine entry via nginx: `http://localhost:8080`
2. pgAdmin: `http://localhost:8081`
3. Grafana (LGTM): `http://localhost:3000`
4. Prometheus (LGTM): `http://localhost:9090`
5. PostgreSQL: `localhost:9999`

## Notes

1. This folder runs the complete local performance stack, not just the bulk loader.
2. For database-only loading, use the `databaseBulkDataLoader` subfolder.
3. If you change CPU/memory values, restart the stack to apply updates.
