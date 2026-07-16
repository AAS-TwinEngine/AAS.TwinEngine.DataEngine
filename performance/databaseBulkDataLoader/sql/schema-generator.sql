-- ============================================================
-- DPP Plugin Database Initialization - Table Creation Only
-- ============================================================
-- This script creates database schema objects only.
-- ============================================================

\echo 'Executing: schema/01_core_asset_tables.sql.inc - Creating core asset tables...'
\ir ../schema/01_core_asset_tables.sql.inc

\echo 'Executing: schema/02_nameplate_carbonfootprint_technicaldata.sql.inc - Creating schema tables...'
\ir ../schema/02_nameplate_carbonfootprint_technicaldata.sql.inc

\echo 'Executing: schema/03_MaintenanceInstructions.sql.inc - Creating maintenance tables...'
\ir ../schema/03_MaintenanceInstructions.sql.inc

\echo 'Executing: schema/04_handoverdocumentation.sql.inc - Creating handover documentation tables...'
\ir ../schema/04_handoverdocumentation.sql.inc

\echo 'Table creation completed successfully!'
