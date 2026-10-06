DO $$
DECLARE
    truncate_statement text;
BEGIN
    SELECT INTO truncate_statement
        'TRUNCATE TABLE ' ||
        string_agg(format('%I.%I', schemaname, tablename), ', ' ORDER BY tablename) ||
        ' RESTART IDENTITY CASCADE;'
    FROM pg_tables
    WHERE schemaname = 'public';

    IF truncate_statement IS NOT NULL THEN
        EXECUTE truncate_statement;
    END IF;
END $$;