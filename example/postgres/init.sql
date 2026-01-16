-- ============================================================
-- DPP Plugin Database Initialization Script
-- ============================================================
-- This is the main orchestration file that executes all database
-- initialization scripts in the correct hierarchical order.
--
-- Execution Order:
--   1. 01_core_asset_tables.sql.inc    - Create core asset database schema
--   2. 02_nameplate_carbonfootprint_technicaldata.sql.inc -  Create Nameplate, Carbon Footprint, Technical Data tables and insert data
--   3. 03_contactinformations.sql.inc - Create Contact information and relationships table and insert data
--   4. 04_handoverdocumentation.sql.inc - Create Handover Documentation table and insert data
--
-- ============================================================

\echo 'Executing: 01_core_asset_tables.sql.inc - Creating core asset database schema...'
\i /docker-entrypoint-initdb.d/01_core_asset_tables.sql.inc

\echo 'Executing: 02_nameplate_carbonfootprint_technicaldata.sql.inc - Inserting  data...'
\i /docker-entrypoint-initdb.d/02_nameplate_carbonfootprint_technicaldata.sql.inc

\echo 'Executing: 03_contactinformations.sql.inc - Inserting contact information...'
\i /docker-entrypoint-initdb.d/03_contactinformations.sql.inc

\echo 'Executing: 04_handoverdocumentation.sql.inc - Inserting document metadata...'
\i /docker-entrypoint-initdb.d/04_handoverdocumentation.sql.inc

\echo 'Database initialization completed successfully!'
