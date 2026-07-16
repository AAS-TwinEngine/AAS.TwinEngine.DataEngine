# Database Bulk Data Loader

This folder contains a Docker Compose runner for the PostgreSQL bulk loader.

The runner uses the official `postgres:16-alpine` image only as a client container to execute [load.sh](load.sh). It does not start a PostgreSQL server.

## What Is Required

1. Docker with Compose support.
2. A reachable PostgreSQL instance (local or remote).
3. Valid environment values in `.env`.

## Configuration

The loader reads only environment variables:

1. `PG_CONN_STRING` (required)
2. `ASSET_COUNT` (optional, defaults to `1000`)
3. `BATCH_SIZE` (optional, defaults to `100`)

Create your runtime file:

```bash
cp .env.example .env
```

Then update at least `PG_CONN_STRING`.

## Run

From this folder:

```bash
docker compose up --build
```

The container will:

1. Start.
2. Execute `sh /workspace/load.sh`.
3. Exit with code `0` on success.
4. Exit non-zero if any SQL step fails (`set -e` and `ON_ERROR_STOP=1`).

## Connection Setup

Connection string pattern:

```text
postgres://username:password@host:port/database
```

### Local PostgreSQL from the `example` stack

The `example/docker-compose.yml` PostgreSQL service already exposes this mapping:

```yaml
ports:
  - "9999:5432"
```

Set `PG_CONN_STRING` in `.env` to:

```text
PG_CONN_STRING=postgres://postgres:admin@localhost:9999/twinengine
```

### Azure PostgreSQL

Use the full Azure connection string directly in `PG_CONN_STRING`.

Example:

```text
PG_CONN_STRING=postgres://username:password@your-server.postgres.database.azure.com:5432/your_database
```

## Notes

1. No extra PostgreSQL container is created in this folder.
2. Loader SQL and `.sql.inc` schema includes are preserved and processed as before.
3. The setup is independent from the `example` Compose project, except it reads schema include files from `example/postgres/schema`.
