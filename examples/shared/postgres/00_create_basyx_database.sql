-- BaSyx services and Keycloak use a separate database from the DPP plugin.
SELECT 'CREATE DATABASE basyxTestDB'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'basyxTestDB')\gexec