WITH required_tables AS (
    SELECT unnest(ARRAY[
        'Asset',
        'SpecificAssetIds',
        'Marking',
        'ProductImage',
        'ProductClassifications',
        'ProductOrSectorSpecificCarbonFootprint',
        'MaintenanceInstructionsForSpecificInterval',
        'Alarm',
        'ContactForMaintenanceAuthorization',
        'Email',
        'Phone',
        'Fax',
        'MaintenanceStep',
        'MaintenanceTool',
        'MaintenanceConsumable',
        'MaintenanceSparePart',
        'DocumentId',
        'DocumentClassification',
        'DocumentVersion',
        'Languages',
        'AssetMarking',
        'AssetProductClassifications',
        'AssetProductImage',
        'Document',
        'AssetDocument',
        'DocumentDocumentId',
        'DocumentDocumentClassification',
        'DocumentDocumentVersion',
        'DocumentVersionLanguages',
        'AssetMaintenanceInstruction',
        'MaintenanceInstructionAlarm',
        'MaintenanceInstructionContactForMaintenanceAuthorization',
        'MaintenanceInstructionsForSpecificIntervalMaintenanceStep',
        'AssetMaintenanceTool',
        'AssetMaintenanceConsumable',
        'AssetMaintenanceSparePart'
    ]) AS table_name
),
missing_tables AS (
    SELECT r.table_name
    FROM required_tables r
    LEFT JOIN information_schema.tables t
      ON t.table_schema = 'public'
     AND t.table_type = 'BASE TABLE'
     AND t.table_name = r.table_name
    WHERE t.table_name IS NULL
)
SELECT
    CASE
        WHEN COUNT(*) = 0 THEN 'OK'
        ELSE 'MISSING|' || string_agg(table_name, ',' ORDER BY table_name)
    END AS schema_status
FROM missing_tables;