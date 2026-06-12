-- ============================================================
-- DPP Plugin Database Initialization - Dummy Data Seeding
-- ============================================================
-- This script inserts sample/demo data only.
-- If you do not want dummy data, simply delete or rename this file.
-- ============================================================
-- Note: Runs after 01_init_tables.sql (alphabetical order)

\echo 'Executing: add-data/01_core_asset_tables.sql.inc - Seeding core asset data...'
\i /docker-entrypoint-initdb.d/add-data/01_core_asset_tables.sql.inc

\echo 'Executing: add-data/02_nameplate_carbonfootprint_technicaldata.sql.inc - Seeding nameplate/carbon/technical data...'
\i /docker-entrypoint-initdb.d/add-data/02_nameplate_carbonfootprint_technicaldata.sql.inc

\echo 'Executing: add-data/03_MaintenanceInstructions.sql.inc - Seeding maintenance data...'
\i /docker-entrypoint-initdb.d/add-data/03_MaintenanceInstructions.sql.inc

\echo 'Executing: add-data/04_handoverdocumentation.sql.inc - Seeding handover documentation data...'
\i /docker-entrypoint-initdb.d/add-data/04_handoverdocumentation.sql.inc

\echo 'Dummy data seeding completed successfully!'
