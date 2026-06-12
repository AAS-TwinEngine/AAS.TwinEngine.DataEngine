-- ============================================================
-- DPP Plugin Database Initialization - Table Creation Only
-- ============================================================
-- This script creates database schema objects only.
-- ============================================================

\echo 'Executing: create-tables/01_core_asset_tables.sql.inc - Creating core asset tables...'
\i /docker-entrypoint-initdb.d/create-tables/01_core_asset_tables.sql.inc

\echo 'Executing: create-tables/02_nameplate_carbonfootprint_technicaldata.sql.inc - Creating nameplate/carbon/technical tables...'
\i /docker-entrypoint-initdb.d/create-tables/02_nameplate_carbonfootprint_technicaldata.sql.inc

\echo 'Executing: create-tables/03_MaintenanceInstructions.sql.inc - Creating maintenance tables...'
\i /docker-entrypoint-initdb.d/create-tables/03_MaintenanceInstructions.sql.inc

\echo 'Executing: create-tables/04_handoverdocumentation.sql.inc - Creating handover documentation tables...'
\i /docker-entrypoint-initdb.d/create-tables/04_handoverdocumentation.sql.inc

\echo 'Table creation completed successfully!'
