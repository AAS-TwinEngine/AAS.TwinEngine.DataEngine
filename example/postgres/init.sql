-- ============================================================
-- DPP Plugin Database Initialization Script
-- ============================================================
-- This is the main orchestration file that executes database
-- initialization scripts in two phases:
--   1) table creation
--   2) optional dummy data seeding
--
-- To skip dummy data for a clean environment, comment or remove
-- the include line for orchestration/init_data.sql below.
--
-- ============================================================

\echo 'Executing: orchestration/init_tables.sql - Creating database tables...'
\i /docker-entrypoint-initdb.d/orchestration/init_tables.sql

\echo 'Executing: orchestration/init_data.sql - Seeding dummy data (optional)...'
\i /docker-entrypoint-initdb.d/orchestration/init_data.sql

\echo 'Database initialization completed successfully!'
