-- Create an additional database for BaSyx Go components in the same Postgres instance.
SELECT 'CREATE DATABASE "basyxTestDB"'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'basyxTestDB'
)\gexec
