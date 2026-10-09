-- Additional databases for this example, created in the same Postgres instance as `twinengine`.
-- basyxSourceDB  : backs the source BaSyx Go registry+repository (pre-seeded with templates).
-- basyxTargetDB  : backs the target BaSyx Go registry+repository (starts empty; the export target).
-- exportstate    : ownership ledger used by the Export Service to diff source vs. target.
SELECT 'CREATE DATABASE "basyxSourceDB"'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'basyxSourceDB'
)\gexec

SELECT 'CREATE DATABASE "basyxTargetDB"'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'basyxTargetDB'
)\gexec

SELECT 'CREATE DATABASE "exportstate"'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'exportstate'
)\gexec
