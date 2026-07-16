-- ============================================================
-- Bulk Seed Data Generator
-- ============================================================
-- HOW TO USE:
--   Change the value of p_asset_count (line ~20) to any positive
--   integer. Re-run this script and it will INSERT that many
--   assets plus ALL related data with zero duplicate IDs.
--
-- PER ASSET this script seeds:
--   3  SpecificAssetIds
--   2  Markings            → AssetMarking (junction)
--   2  ProductImages       → AssetProductImage (junction)  [explicit IDs]
--   2  ProductClassifications → AssetProductClassifications (junction)
--   1  ProductOrSectorSpecificCarbonFootprint
--   2  MaintenanceInstructions → AssetMaintenanceInstruction (junction)
--      └─ 1 Alarm each                  → MaintenanceInstructionAlarm
--      └─ 2 Contacts each (Email+Phone+Fax)
--             → MaintenanceInstructionContactForMaintenanceAuthorization
--      └─ 2 MaintenanceSteps each
--             → MaintenanceInstructionsForSpecificIntervalMaintenanceStep
--   1  MaintenanceTool     → AssetMaintenanceTool (junction)
--   1  MaintenanceConsumable → AssetMaintenanceConsumable (junction)
--   1  MaintenanceSparePart  → AssetMaintenanceSparePart (junction)
--   2  Documents each with:
--      └─ 1 DocumentId       → DocumentDocumentId
--      └─ 1 DocumentClassification → DocumentDocumentClassification
--      └─ 1 DocumentVersion  → DocumentDocumentVersion
--         └─ 1 Language      → DocumentVersionLanguages
--
-- Asset ProductIds are generated as: 000-001, 000-002, ... 000-999,
-- then 001-000, 001-001, ... for continuous six-digit sequencing.
-- All junction IDs follow their parent sequences; no manual ID
-- bookkeeping is required beyond ProductImage / AssetProductImage
-- which have explicit (non-identity) primary keys.
-- ============================================================

DO $$
DECLARE
    -- Value is supplied at runtime via the app.asset_count session GUC.
    -- Fallback: defaults to 3 when no value is provided by the loader.
    p_asset_count CONSTANT INT := COALESCE(
        NULLIF(current_setting('app.asset_count', true), '')::INT,
        1000
    );

    -- loop counter
    i INT;

    -- captured auto-generated IDs
    asset_id          INT;
    marking_id_1      INT;
    marking_id_2      INT;
    pc_id_1           INT;   -- ProductClassifications
    pc_id_2           INT;
    maint_id_1        INT;   -- MaintenanceInstructionsForSpecificInterval
    maint_id_2        INT;
    alarm_id_1        INT;
    alarm_id_2        INT;
    contact_id_1      INT;
    contact_id_2      INT;
    step_id_1         INT;
    step_id_2         INT;
    step_id_3         INT;
    step_id_4         INT;
    tool_id           INT;
    consumable_id     INT;
    spare_id          INT;
    doc_id_1          INT;
    doc_id_2          INT;
    doc_ident_id_1    INT;
    doc_ident_id_2    INT;
    doc_class_id_1    INT;
    doc_class_id_2    INT;
    doc_ver_id_1      INT;
    doc_ver_id_2      INT;
    lang_id_1         INT;
    lang_id_2         INT;

    -- explicit-ID counters for tables that use INT PRIMARY KEY (not identity)
    pi_counter        INT;    -- ProductImage
    api_counter       INT;    -- AssetProductImage

    pid               VARCHAR(50); -- formatted product id, e.g. '000-001'
    asset_sequence    INT;
    asset_offset      INT;
    pid_group         INT;
    pid_sequence      INT;

BEGIN
    IF p_asset_count < 1 THEN
        RAISE EXCEPTION 'app.asset_count must be a positive integer';
    END IF;

    PERFORM set_config('synchronous_commit', 'off', true);

    -- Continue ProductId numbering across reruns on the same database.
    SELECT COALESCE(MAX("Id"), 0) INTO asset_offset FROM "Asset";

    SELECT COALESCE(MAX("Id"), 0) INTO pi_counter FROM "ProductImage";
    SELECT COALESCE(MAX("Id"), 0) INTO api_counter FROM "AssetProductImage";

    SELECT "Id"
    INTO lang_id_1
    FROM "Languages"
    WHERE "Language" = 'en'
    ORDER BY "Id"
    LIMIT 1;

    IF lang_id_1 IS NULL THEN
        INSERT INTO "Languages" ("Index","Language")
        VALUES (0, 'en')
        RETURNING "Id" INTO lang_id_1;
    END IF;

    SELECT "Id"
    INTO lang_id_2
    FROM "Languages"
    WHERE "Language" = 'de'
    ORDER BY "Id"
    LIMIT 1;

    IF lang_id_2 IS NULL THEN
        INSERT INTO "Languages" ("Index","Language")
        VALUES (0, 'de')
        RETURNING "Id" INTO lang_id_2;
    END IF;

    FOR i IN 1..p_asset_count LOOP

        asset_sequence := asset_offset + i;
        pid_group := asset_sequence / 1000;
        pid_sequence := asset_sequence % 1000;
        pid := lpad(pid_group::text, 3, '0') || '-' || lpad(pid_sequence::text, 3, '0');

        -- ============================================================
        -- ASSET
        -- ============================================================
        INSERT INTO "Asset" (
            "ProductId","IdShort","GlobalAssetId","AasId",
            "ThumbnailContentType","ThumbnailPath","MaintenanceFreeAsset",
            "UriOfTheProduct","ManufacturerProductType","OrderCodeOfManufacturer",
            "ProductArticleNumberOfManufacturer","SerialNumber","YearOfConstruction",
            "DateOfManufacture","HardwareVersion","FirmwareVersion","SoftwareVersion",
            "CountryOfOrigin","UniqueFacilityIdentifier","ManufacturerName",
            "ManufacturerProductDesignation_en","ManufacturerProductDesignation_de",
            "ManufacturerProductRoot_en","ManufacturerProductRoot_de",
            "ManufacturerProductFamily_en","ManufacturerProductFamily_de",
            "CompanyLogo","ManufacturerArticleNumber","ManufacturerOrderCode",
            "ProductImage","ManufacturerLogo","TextStatement_en","TextStatement_de",
            "ValidDate","PcfCalculationMethod","LifeCyclePhase","PcfCO2eq",
            "ReferenceImpactUnitForCalculation","QuantityOfMeasureForCalculation",
            "PublicationDate","ExpirationDate","ExplanatoryStatement"
        ) VALUES (
            pid,
            'Product' || asset_sequence,
            'https://mm-software.com/ids/assets/' || pid,
            'https://mm-software.com/ids/aas/' || pid,
            'image/jpeg',
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/product1.jpg',
            (i % 2 = 1),
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/product1.jpg',
            'FM-ABC-' || lpad(asset_sequence::text, 4, '0'),
            'FMABC' || lpad(asset_sequence::text, 4, '0'),
            'FM11-ABC22-' || lpad(asset_sequence::text, 6, '0'),
            '9804' || lpad(asset_sequence::text, 3, '0'),
            2022 + (asset_sequence % 4),
            ('2022-01-01'::DATE + make_interval(years => (asset_sequence - 1) % 4))::DATE,
            '1.0.' || asset_sequence,
            '1.0.' || asset_sequence,
            '1.0.' || asset_sequence,
            CASE asset_sequence % 3 WHEN 1 THEN 'DE' WHEN 2 THEN 'IN' ELSE 'CN' END,
            lpad((987654321 + asset_sequence)::text, 9, '0'),
            CASE asset_sequence % 3 WHEN 1 THEN 'M&M Germany' WHEN 2 THEN 'M&M India' ELSE 'M&M China' END,
            'Product-' || pid,
            'Produkt-' || pid,
            CASE asset_sequence % 2 WHEN 1 THEN 'Camera' ELSE 'Perfume' END,
            CASE asset_sequence % 2 WHEN 1 THEN 'Kamera' ELSE 'Parfuem' END,
            CASE asset_sequence % 2 WHEN 1 THEN 'Electronics' ELSE 'Cosmetics' END,
            CASE asset_sequence % 2 WHEN 1 THEN 'Elektronik' ELSE 'Kosmetika' END,
            'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
            lpad(asset_sequence::text, 6, '0'),
            'EEA-EX-200-S/47-Q' || asset_sequence,
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/product1.jpg',
            'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
            'Restricted use',
            'Eingeschraenkte Nutzung',
            ('2035-01-01'::DATE + make_interval(months => asset_sequence % 12))::DATE,
            CASE asset_sequence % 3 WHEN 1 THEN 'ISO 14067' WHEN 2 THEN 'EN 15804' ELSE 'PACT v2.0.0' END,
            CASE asset_sequence % 3 WHEN 1 THEN 'C4 - landfill' WHEN 2 THEN 'A5 - Installation' ELSE 'C3 - recycling' END,
            round((5 + (asset_sequence % 15))::numeric, 1),
            CASE asset_sequence % 3 WHEN 1 THEN 'ml' WHEN 2 THEN 'cbm' ELSE 'piece' END,
            round((1 + (asset_sequence % 9))::numeric, 1),
            '2025-12-24T14:30:00Z'::TIMESTAMPTZ,
            '2035-12-24T14:30:00Z'::TIMESTAMPTZ,
            'https://docs.google.com/viewer?url=https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.pdf'
        ) RETURNING "Id" INTO asset_id;

        -- ============================================================
        -- SPECIFIC ASSET IDS  (3 per asset)
        -- ============================================================
        INSERT INTO "SpecificAssetIds" ("AssetId","Name","Value") VALUES
            (asset_id, 'SerialNumber', 'SN-' || lpad(i::text, 6, '0') || '-' || pid),
            (asset_id, 'BatchId',      'BATCH-' || (2022 + i % 4) || '-' || lpad(i::text, 3, '0')),
            (asset_id, 'LotNumber',    'LOT-' || CASE i%3 WHEN 1 THEN 'DE' WHEN 2 THEN 'IN' ELSE 'CN' END || '-' || lpad(i::text, 5, '0'));

        -- ============================================================
        -- MARKINGS  (2 per asset)
        -- ============================================================
        INSERT INTO "Marking" (
            "Index","MarkingName","DesignationOfCertificateOrApproval",
            "IssueDate","ExpiryDate","MarkingAdditionalText","MarkingFile"
        ) VALUES (
            0,
            '0173-1#07-DAA603#' || lpad(i::text, 3, '0'),
            'KEMA99IECEX1105/' || lpad(i::text, 3, '0'),
            ('2022-01-01'::DATE + make_interval(months => (i - 1) % 12))::DATE,
            '2030-12-31',
            'Marking information - ' || lpad(i::text, 2, '0'),
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/checkmark.png'
        ) RETURNING "Id" INTO marking_id_1;

        -- second marking uses an offset to keep MarkingName values distinct across all rows
        INSERT INTO "Marking" (
            "Index","MarkingName","DesignationOfCertificateOrApproval",
            "IssueDate","ExpiryDate","MarkingAdditionalText","MarkingFile"
        ) VALUES (
            1,
            '0173-1#07-DAB603#' || lpad(i::text, 3, '0'),
            'KEMA99IECEX2105/' || lpad(i::text, 3, '0'),
            ('2022-02-01'::DATE + make_interval(months => (i - 1) % 12))::DATE,
            '2031-12-31',
            'Marking information B - ' || lpad(i::text, 2, '0'),
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/checkmark.png'
        ) RETURNING "Id" INTO marking_id_2;

        INSERT INTO "AssetMarking" ("AssetId","MarkingId") VALUES (asset_id, marking_id_1);
        INSERT INTO "AssetMarking" ("AssetId","MarkingId") VALUES (asset_id, marking_id_2);

        -- ============================================================
        -- PRODUCT IMAGES  (2 per asset)
        -- ProductImage and AssetProductImage use explicit INT PRIMARY KEY
        -- so pi_counter / api_counter track the next available ID.
        -- ============================================================
        pi_counter  := pi_counter  + 1;
        api_counter := api_counter + 1;
        INSERT INTO "ProductImage" ("Id","Index","ImageFile","ImageNote_en","ImageNote_de")
        VALUES (
            pi_counter, 0,
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/product1.jpg',
            'Front view of product ' || i,
            'Frontansicht von Produkt ' || i
        );
        INSERT INTO "AssetProductImage" ("Id","AssetId","ProductImageId")
        VALUES (api_counter, asset_id, pi_counter);

        pi_counter  := pi_counter  + 1;
        api_counter := api_counter + 1;
        INSERT INTO "ProductImage" ("Id","Index","ImageFile","ImageNote_en","ImageNote_de")
        VALUES (
            pi_counter, 1,
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/product1.jpg',
            'Side view of product ' || i,
            'Seitenansicht von Produkt ' || i
        );
        INSERT INTO "AssetProductImage" ("Id","AssetId","ProductImageId")
        VALUES (api_counter, asset_id, pi_counter);

        -- ============================================================
        -- PRODUCT CLASSIFICATIONS  (2 per asset)
        -- ============================================================
        INSERT INTO "ProductClassifications" (
            "Index","ClassificationSystem","ClassificationSystemVersion",
            "ClassificationSystemUrl","ProductClassId","ProductClassCodedName",
            "ProductClassName_en","ProductClassName_de"
        ) VALUES (
            0,
            CASE i % 4 WHEN 1 THEN 'ECLASS' WHEN 2 THEN 'IEC CDD' WHEN 3 THEN 'UNSPSC' ELSE 'ISO 13584' END,
            CASE i % 4 WHEN 1 THEN '14' WHEN 2 THEN '2024-09' WHEN 3 THEN '23.0301' ELSE '2023' END,
            CASE i % 4 WHEN 1 THEN 'https://eclass.eu' WHEN 2 THEN 'https://cdd.iec.ch' WHEN 3 THEN 'https://www.unspsc.org' ELSE 'https://www.iso.org' END,
            'PC-' || lpad(i::text, 4, '0') || '-A',
            'CODE-' || lpad(i::text, 4, '0') || '-A',
            'Product Class A - ' || i,
            'Produktklasse A - ' || i
        ) RETURNING "Id" INTO pc_id_1;

        INSERT INTO "ProductClassifications" (
            "Index","ClassificationSystem","ClassificationSystemVersion",
            "ClassificationSystemUrl","ProductClassId","ProductClassCodedName",
            "ProductClassName_en","ProductClassName_de"
        ) VALUES (
            1,
            CASE (i + 1) % 4 WHEN 1 THEN 'ECLASS' WHEN 2 THEN 'IEC CDD' WHEN 3 THEN 'UNSPSC' ELSE 'ISO 13584' END,
            CASE (i + 1) % 4 WHEN 1 THEN '14' WHEN 2 THEN '2024-09' WHEN 3 THEN '23.0301' ELSE '2023' END,
            CASE (i + 1) % 4 WHEN 1 THEN 'https://eclass.eu' WHEN 2 THEN 'https://cdd.iec.ch' WHEN 3 THEN 'https://www.unspsc.org' ELSE 'https://www.iso.org' END,
            'PC-' || lpad(i::text, 4, '0') || '-B',
            'CODE-' || lpad(i::text, 4, '0') || '-B',
            'Product Class B - ' || i,
            'Produktklasse B - ' || i
        ) RETURNING "Id" INTO pc_id_2;

        INSERT INTO "AssetProductClassifications" ("AssetId","ProductClassificationsId") VALUES (asset_id, pc_id_1);
        INSERT INTO "AssetProductClassifications" ("AssetId","ProductClassificationsId") VALUES (asset_id, pc_id_2);

        -- ============================================================
        -- CARBON FOOTPRINT  (1 per asset)
        -- ============================================================
        INSERT INTO "ProductOrSectorSpecificCarbonFootprint" (
            "AssetId","PcfCalculationMethod","PcfRuleOperator","PcfRuleName","PcfRuleVersion",
            "PcfRuleOnlineReference","PcfApiEndpoint","PcfApiQuery"
        ) VALUES (
            asset_id,
            CASE i % 3 WHEN 1 THEN 'IEC TS 63058' WHEN 2 THEN 'EN 15804' ELSE 'PACT v2.0.0' END,
            CASE i % 3 WHEN 1 THEN 'GHG Protocol' WHEN 2 THEN 'ISO 14067' ELSE 'PAS 2050' END,
            CASE i % 3 WHEN 1 THEN 'GHG Protocol Product Standard' WHEN 2 THEN 'ISO 14067' ELSE 'PAS 2050' END,
            CASE i % 3 WHEN 1 THEN '1.1' WHEN 2 THEN '2.1' ELSE '0.9' END,
            CASE i % 3 WHEN 1 THEN 'https://ghgprotocol.org/standards/product-standard'
                       WHEN 2 THEN 'https://www.iso.org/standard/43278.html'
                       ELSE          'https://www.bsigroup.com/en-GB/PAS-2050-Carbon-Footprint/' END,
            'https://api.carbonfootprint.org/v1/calculate',
            '?productId=' || pid || '&unit=kgCO2e'
        );

        -- ============================================================
        -- MAINTENANCE INSTRUCTIONS  (2 per asset)
        -- ============================================================
        INSERT INTO "MaintenanceInstructionsForSpecificInterval" (
            "Index","MaintenanceID",
            "NameOfMaintenance_en","NameOfMaintenance_de",
            "SourceOfMaintenanceInstructions_en","SourceOfMaintenanceInstructions_de",
            "RelatedStandardsLawsRegulations_en","RelatedStandardsLawsRegulations_de",
            "SafetyRegulationsToBeObserved_en","SafetyRegulationsToBeObserved_de",
            "MaintenanceIntervalValue","MaintenanceIntervalUnit",
            "FlowChartOfMaintenanceSteps",
            "NumberOfRequiredTechnicians",
            "RequiredQualification_en","RequiredQualification_de",
            "ValueTotalEstimatedWorkingTime","UnitValueTotalEstimatedWorkingTime"
        ) VALUES (
            0, 'MNT-' || pid || '-A',
            'Maintenance A for product ' || i, 'Wartung A fuer Produkt ' || i,
            'Manufacturer', 'Hersteller',
            'ISO 9001 maintenance guidelines', 'ISO 9001 Wartungsrichtlinien',
            'Disconnect power; Secure against restart', 'Stromversorgung trennen; Gegen Wiedereinschalten sichern',
            6, 'months',
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.pdf',
            2,
            'Qualified maintenance technician required', 'Qualifizierter Wartungstechniker erforderlich',
            2, 'hour'
        ) RETURNING "Id" INTO maint_id_1;

        INSERT INTO "MaintenanceInstructionsForSpecificInterval" (
            "Index","MaintenanceID",
            "NameOfMaintenance_en","NameOfMaintenance_de",
            "SourceOfMaintenanceInstructions_en","SourceOfMaintenanceInstructions_de",
            "RelatedStandardsLawsRegulations_en","RelatedStandardsLawsRegulations_de",
            "SafetyRegulationsToBeObserved_en","SafetyRegulationsToBeObserved_de",
            "MaintenanceIntervalValue","MaintenanceIntervalUnit",
            "FlowChartOfMaintenanceSteps",
            "NumberOfRequiredTechnicians",
            "RequiredQualification_en","RequiredQualification_de",
            "ValueTotalEstimatedWorkingTime","UnitValueTotalEstimatedWorkingTime"
        ) VALUES (
            1, 'MNT-' || pid || '-B',
            'Maintenance B for product ' || i, 'Wartung B fuer Produkt ' || i,
            'Component supplier', 'Komponentenlieferant',
            'DIN EN 13306 maintenance terminology', 'DIN EN 13306 Wartungsbegriffe',
            'Wear gloves; Disconnect system', 'Schutzhandschuhe tragen; System trennen',
            12, 'months',
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.pdf',
            1,
            'Certified technician required', 'Zertifizierter Techniker erforderlich',
            4, 'hour'
        ) RETURNING "Id" INTO maint_id_2;

        INSERT INTO "AssetMaintenanceInstruction" ("AssetId","MaintenanceInstructionId") VALUES (asset_id, maint_id_1);
        INSERT INTO "AssetMaintenanceInstruction" ("AssetId","MaintenanceInstructionId") VALUES (asset_id, maint_id_2);

        -- ============================================================
        -- ALARMS  (1 per maintenance instruction → 2 per asset)
        -- ============================================================
        INSERT INTO "Alarm" ("Index","AlarmName_en","AlarmName_de","WarningLimitRelativeValue","WarningLimitSeverity")
        VALUES (0, 'Pre-alarm ' || pid || '-A', 'Voralarm ' || pid || '-A', 80, 'Warning')
        RETURNING "Id" INTO alarm_id_1;

        INSERT INTO "Alarm" ("Index","AlarmName_en","AlarmName_de","WarningLimitRelativeValue","WarningLimitSeverity")
        VALUES (0, 'Critical alarm ' || pid || '-B', 'Kritischer Alarm ' || pid || '-B', 95, 'Critical')
        RETURNING "Id" INTO alarm_id_2;

        INSERT INTO "MaintenanceInstructionAlarm" ("MaintenanceInstructionId","AlarmId") VALUES (maint_id_1, alarm_id_1);
        INSERT INTO "MaintenanceInstructionAlarm" ("MaintenanceInstructionId","AlarmId") VALUES (maint_id_2, alarm_id_2);

        -- ============================================================
        -- CONTACTS  (2 per asset; each gets 1 Email + 1 Phone + 1 Fax)
        -- Email/Phone/Fax have UNIQUE constraint on ContactId FK.
        -- ============================================================
        INSERT INTO "ContactForMaintenanceAuthorization" (
            "Index","Company_en","Company_de","Department_en","Department_de",
            "Title_en","Title_de","AcademicTitle_en","AcademicTitle_de",
            "NameOfContact_en","NameOfContact_de","FirstName_en","FirstName_de",
            "MiddleNames_en","MiddleNames_de","Street_en","Street_de",
            "Zipcode_en","Zipcode_de","CityTown_en","CityTown_de",
            "NationalCode_en","NationalCode_de","StateCounty_en","StateCounty_de",
            "FurtherDetailsOfContact_en","FurtherDetailsOfContact_de","RoleOfContactPerson"
        ) VALUES (
            0,
            CASE i % 3 WHEN 1 THEN 'M&M Germany' WHEN 2 THEN 'M&M India' ELSE 'M&M China' END,
            CASE i % 3 WHEN 1 THEN 'M&M Germany' WHEN 2 THEN 'M&M India' ELSE 'M&M China' END,
            'Maintenance', 'Wartung',
            'Mr.', 'Herr', 'Dr.', 'Dr.',
            'Contact-' || pid || '-A', 'Kontakt-' || pid || '-A',
            'FirstA-' || i, 'FirstA-' || i,
            'LastA-' || i, 'LastA-' || i,
            lpad(i::text, 3, '0') || ' Example Street', lpad(i::text, 3, '0') || ' Musterstrasse',
            lpad(i::text, 5, '0'), lpad(i::text, 5, '0'),
            CASE i % 3 WHEN 1 THEN 'Berlin' WHEN 2 THEN 'Mumbai' ELSE 'Beijing' END,
            CASE i % 3 WHEN 1 THEN 'Berlin' WHEN 2 THEN 'Mumbai' ELSE 'Beijing' END,
            CASE i % 3 WHEN 1 THEN 'DE' WHEN 2 THEN 'IN' ELSE 'CN' END,
            CASE i % 3 WHEN 1 THEN 'DE' WHEN 2 THEN 'IN' ELSE 'CN' END,
            CASE i % 3 WHEN 1 THEN 'Berlin' WHEN 2 THEN 'Delhi' ELSE 'Shanghai' END,
            CASE i % 3 WHEN 1 THEN 'Berlin' WHEN 2 THEN 'Delhi' ELSE 'Shanghai' END,
            'Responsible for region ' || i || '-A',
            'Verantwortlich fuer Region ' || i || '-A',
            '0173-1#07-AAS927#001'
        ) RETURNING "Id" INTO contact_id_1;

        INSERT INTO "ContactForMaintenanceAuthorization" (
            "Index","Company_en","Company_de","Department_en","Department_de",
            "Title_en","Title_de","AcademicTitle_en","AcademicTitle_de",
            "NameOfContact_en","NameOfContact_de","FirstName_en","FirstName_de",
            "MiddleNames_en","MiddleNames_de","Street_en","Street_de",
            "Zipcode_en","Zipcode_de","CityTown_en","CityTown_de",
            "NationalCode_en","NationalCode_de","StateCounty_en","StateCounty_de",
            "FurtherDetailsOfContact_en","FurtherDetailsOfContact_de","RoleOfContactPerson"
        ) VALUES (
            1,
            CASE (i + 1) % 3 WHEN 1 THEN 'M&M Germany' WHEN 2 THEN 'M&M India' ELSE 'M&M China' END,
            CASE (i + 1) % 3 WHEN 1 THEN 'M&M Germany' WHEN 2 THEN 'M&M India' ELSE 'M&M China' END,
            'Finance', 'Finanzen',
            'Ms', 'Frau', 'Prof.', 'Prof.',
            'Contact-' || pid || '-B', 'Kontakt-' || pid || '-B',
            'FirstB-' || i, 'FirstB-' || i,
            'LastB-' || i, 'LastB-' || i,
            lpad(i::text, 3, '0') || ' Second Street', lpad(i::text, 3, '0') || ' Zweitestrasse',
            lpad((i + 1)::text, 5, '0'), lpad((i + 1)::text, 5, '0'),
            CASE (i + 1) % 3 WHEN 1 THEN 'Hamburg' WHEN 2 THEN 'Chennai' ELSE 'Shanghai' END,
            CASE (i + 1) % 3 WHEN 1 THEN 'Hamburg' WHEN 2 THEN 'Chennai' ELSE 'Shanghai' END,
            CASE (i + 1) % 3 WHEN 1 THEN 'DE' WHEN 2 THEN 'IN' ELSE 'CN' END,
            CASE (i + 1) % 3 WHEN 1 THEN 'DE' WHEN 2 THEN 'IN' ELSE 'CN' END,
            CASE (i + 1) % 3 WHEN 1 THEN 'Hamburg' WHEN 2 THEN 'Tamil Nadu' ELSE 'Shanghai' END,
            CASE (i + 1) % 3 WHEN 1 THEN 'Hamburg' WHEN 2 THEN 'Tamil Nadu' ELSE 'Shanghai' END,
            'Responsible for region ' || i || '-B',
            'Verantwortlich fuer Region ' || i || '-B',
            '0173-1#07-AAS928#001'
        ) RETURNING "Id" INTO contact_id_2;

        -- Email (UNIQUE per ContactId)
        INSERT INTO "Email" (
            "ContactForMaintenanceAuthorizationId","EmailAddress","TypeOfEmailAddress",
            "PublicKey_en","PublicKey_de","TypeOfPublicKey_en","TypeOfPublicKey_de"
        ) VALUES (
            contact_id_1,
            'contact.' || i || '.a@example.com',
            '0173-1#07-AAS754#001',
            md5('pubkey-en-' || contact_id_1::text),
            md5('pubkey-de-' || contact_id_1::text),
            'RSA Encryption', 'RSA-Verschluesselung'
        );

        INSERT INTO "Email" (
            "ContactForMaintenanceAuthorizationId","EmailAddress","TypeOfEmailAddress",
            "PublicKey_en","PublicKey_de","TypeOfPublicKey_en","TypeOfPublicKey_de"
        ) VALUES (
            contact_id_2,
            'contact.' || i || '.b@example.com',
            '0173-1#07-AAS756#001',
            md5('pubkey-en-' || contact_id_2::text),
            md5('pubkey-de-' || contact_id_2::text),
            'ECC Encryption', 'ECC-Verschluesselung'
        );

        -- Phone (UNIQUE per ContactId)
        INSERT INTO "Phone" (
            "ContactForMaintenanceAuthorizationId",
            "TelephoneNumber_en","TelephoneNumber_de",
            "AvailableTime_en","AvailableTime_de","TypeOfTelephone"
        ) VALUES (
            contact_id_1,
            '+49 ' || lpad(i::text, 3, '0') || ' 1000001',
            '+49 ' || lpad(i::text, 3, '0') || ' 1000002',
            'Monday - Friday 08:00 to 17:00',
            'Montag - Freitag 08:00 bis 17:00',
            '0173-1#07-AAS754#001'
        );

        INSERT INTO "Phone" (
            "ContactForMaintenanceAuthorizationId",
            "TelephoneNumber_en","TelephoneNumber_de",
            "AvailableTime_en","AvailableTime_de","TypeOfTelephone"
        ) VALUES (
            contact_id_2,
            '+49 ' || lpad(i::text, 3, '0') || ' 2000001',
            '+49 ' || lpad(i::text, 3, '0') || ' 2000002',
            'Monday - Friday 09:00 to 18:00',
            'Montag - Freitag 09:00 bis 18:00',
            '0173-1#07-AAS755#001'
        );

        -- Fax (UNIQUE per ContactId)
        INSERT INTO "Fax" (
            "ContactForMaintenanceAuthorizationId",
            "FaxNumber_en", "FaxNumber_de", "TypeOfFaxNumber"
        ) VALUES (
            contact_id_1,
            '+49 ' || lpad(i::text, 3, '0') || ' 3000001',
            '+49 ' || lpad(i::text, 3, '0') || ' 3000002',
            '0173-1#07-AAS754#001'
        );

        INSERT INTO "Fax" (
            "ContactForMaintenanceAuthorizationId",
            "FaxNumber_en", "FaxNumber_de", "TypeOfFaxNumber"
        ) VALUES (
            contact_id_2,
            '+49 ' || lpad(i::text, 3, '0') || ' 4000001',
            '+49 ' || lpad(i::text, 3, '0') || ' 4000002',
            '0173-1#07-AAS756#001'
        );

        -- MaintenanceInstruction <-> Contact (each instruction linked to both contacts)
        INSERT INTO "MaintenanceInstructionContactForMaintenanceAuthorization" (
            "MaintenanceInstructionId",
            "ContactForMaintenanceAuthorizationId"
        ) VALUES
            (maint_id_1, contact_id_1),
            (maint_id_1, contact_id_2),
            (maint_id_2, contact_id_1),
            (maint_id_2, contact_id_2);

        -- ============================================================
        -- MAINTENANCE STEPS  (2 per instruction → 4 per asset)
        -- ============================================================
        INSERT INTO "MaintenanceStep" (
            "Index","MaintenanceStepID",
            "QuantityOfSparePartForMaintenanceStep","QuantityOfConsumablesForMaintenanceStep",
            "UnitForQuantityOfConsumablesForMaintenanceStep","QuantityOfToolsForMaintenanceStep",
            "DocumentationSignatureMandatory","EndOfMaintenance",
            "MaintenanceStepName_en","MaintenanceStepName_de",
            "LocalizationDescription_en","LocalizationDescription_de",
            "InstructionMaintenanceStep_en","InstructionMaintenanceStep_de",
            "ConditionForNextMaintenanceStep_en","ConditionForNextMaintenanceStep_de",
            "ConditionForAlternativeNextStep_en","ConditionForAlternativeNextStep_de",
            "RelatedDocumentOrFileMaintenanceStep",
            "ValueEstimatedDurationTimeMaintenanceStep","UnitEstimatedDurationTimeMaintenanceStep"
        ) VALUES (
            0, 'MS-' || pid || '-01',
            0, 0, NULL, 1, TRUE, FALSE,
            'Start maintenance ' || pid, 'Wartung starten ' || pid,
            'Entry point', 'Eintrittspunkt',
            'Disconnect system power', 'Systemstrom trennen',
            'System is powered off', 'System ist ausgeschaltet',
            'Power still active', 'Strom noch aktiv',
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.pdf',
            5, 'minutes'
        ) RETURNING "Id" INTO step_id_1;

        INSERT INTO "MaintenanceStep" (
            "Index","MaintenanceStepID",
            "QuantityOfSparePartForMaintenanceStep","QuantityOfConsumablesForMaintenanceStep",
            "UnitForQuantityOfConsumablesForMaintenanceStep","QuantityOfToolsForMaintenanceStep",
            "DocumentationSignatureMandatory","EndOfMaintenance",
            "MaintenanceStepName_en","MaintenanceStepName_de",
            "LocalizationDescription_en","LocalizationDescription_de",
            "InstructionMaintenanceStep_en","InstructionMaintenanceStep_de",
            "ConditionForNextMaintenanceStep_en","ConditionForNextMaintenanceStep_de",
            "ConditionForAlternativeNextStep_en","ConditionForAlternativeNextStep_de",
            "RelatedDocumentOrFileMaintenanceStep",
            "ValueEstimatedDurationTimeMaintenanceStep","UnitEstimatedDurationTimeMaintenanceStep"
        ) VALUES (
            1, 'MS-' || pid || '-02',
            0, 0, NULL, 1, TRUE, TRUE,
            'End maintenance ' || pid, 'Wartung beenden ' || pid,
            'Exit point', 'Ausgangspunkt',
            'Reconnect power', 'Stromversorgung wiederherstellen',
            'System online', 'System online',
            'Reconnect failed', 'Verbindung fehlgeschlagen',
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.pdf',
            5, 'minutes'
        ) RETURNING "Id" INTO step_id_2;

        INSERT INTO "MaintenanceStep" (
            "Index","MaintenanceStepID",
            "QuantityOfSparePartForMaintenanceStep","QuantityOfConsumablesForMaintenanceStep",
            "UnitForQuantityOfConsumablesForMaintenanceStep","QuantityOfToolsForMaintenanceStep",
            "DocumentationSignatureMandatory","EndOfMaintenance",
            "MaintenanceStepName_en","MaintenanceStepName_de",
            "LocalizationDescription_en","LocalizationDescription_de",
            "InstructionMaintenanceStep_en","InstructionMaintenanceStep_de",
            "ConditionForNextMaintenanceStep_en","ConditionForNextMaintenanceStep_de",
            "ConditionForAlternativeNextStep_en","ConditionForAlternativeNextStep_de",
            "RelatedDocumentOrFileMaintenanceStep",
            "ValueEstimatedDurationTimeMaintenanceStep","UnitEstimatedDurationTimeMaintenanceStep"
        ) VALUES (
            0, 'MS-' || pid || '-03',
            1, 10, 'ml', 2, TRUE, FALSE,
            'Inspect ' || pid, 'Pruefen ' || pid,
            'Inspection zone', 'Pruefzone',
            'Check components', 'Komponenten pruefen',
            'No defects found', 'Keine Maengel gefunden',
            'Defects detected', 'Maengel festgestellt',
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.pdf',
            15, 'minutes'
        ) RETURNING "Id" INTO step_id_3;

        INSERT INTO "MaintenanceStep" (
            "Index","MaintenanceStepID",
            "QuantityOfSparePartForMaintenanceStep","QuantityOfConsumablesForMaintenanceStep",
            "UnitForQuantityOfConsumablesForMaintenanceStep","QuantityOfToolsForMaintenanceStep",
            "DocumentationSignatureMandatory","EndOfMaintenance",
            "MaintenanceStepName_en","MaintenanceStepName_de",
            "LocalizationDescription_en","LocalizationDescription_de",
            "InstructionMaintenanceStep_en","InstructionMaintenanceStep_de",
            "ConditionForNextMaintenanceStep_en","ConditionForNextMaintenanceStep_de",
            "ConditionForAlternativeNextStep_en","ConditionForAlternativeNextStep_de",
            "RelatedDocumentOrFileMaintenanceStep",
            "ValueEstimatedDurationTimeMaintenanceStep","UnitEstimatedDurationTimeMaintenanceStep"
        ) VALUES (
            1, 'MS-' || pid || '-04',
            0, 0, NULL, 1, TRUE, TRUE,
            'Finalize ' || pid, 'Abschliessen ' || pid,
            'Final check area', 'Abschlusskontrolle',
            'Close up and sign off', 'Abdecken und abzeichnen',
            'Completed', 'Abgeschlossen',
            'Not completed', 'Nicht abgeschlossen',
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.pdf',
            5, 'minutes'
        ) RETURNING "Id" INTO step_id_4;

        INSERT INTO "MaintenanceInstructionsForSpecificIntervalMaintenanceStep" (
            "MaintenanceInstructionsForSpecificIntervalId",
            "MaintenanceStepId"
        ) VALUES
            (maint_id_1, step_id_1),
            (maint_id_1, step_id_2),
            (maint_id_2, step_id_3),
            (maint_id_2, step_id_4);

        -- ============================================================
        -- MAINTENANCE TOOL  (1 per asset)
        -- ============================================================
        INSERT INTO "MaintenanceTool" (
            "Index","ToolID","OrderCodeOfManufacturer","AddressOfAdditionalLink",
            "ToolName_en","ToolName_de",
            "CompanyNameToolSupplier_en","CompanyNameToolSupplier_de",
            "ToolDescription_en","ToolDescription_de","MaxQuantityOfTool"
        ) VALUES (
            0,
            'TL-' || lpad(i::text, 4, '0'),
            'ORD-TOOL-' || lpad(i::text, 4, '0'),
            'https://tools.example.com/t' || i,
            'Maintenance Tool ' || i, 'Wartungswerkzeug ' || i,
            'Tool Supplier ' || i, 'Werkzeuglieferant ' || i,
            'General purpose maintenance tool for product ' || i,
            'Allgemeines Wartungswerkzeug fuer Produkt ' || i,
            2
        ) RETURNING "Id" INTO tool_id;

        INSERT INTO "AssetMaintenanceTool" ("AssetId","MaintenanceToolId") VALUES (asset_id, tool_id);

        -- ============================================================
        -- MAINTENANCE CONSUMABLE  (1 per asset)
        -- ============================================================
        INSERT INTO "MaintenanceConsumable" (
            "Index","ConsumableID","UnitMaxQuantityOfConsumable","OrderCodeOfManufacturer",
            "AddressOfAdditionalLink",
            "ConsumableName_en","ConsumableName_de",
            "CompanyNameSupplierConsumable_en","CompanyNameSupplierConsumable_de",
            "ConsumableDescription_en","ConsumableDescription_de",
            "DisposalInstructionsForConsumable_en","DisposalInstructionsForConsumable_de",
            "QuantityOfConsumable"
        ) VALUES (
            0,
            'CS-' || lpad(i::text, 4, '0'),
            'ml',
            'ORD-CONS-' || lpad(i::text, 4, '0'),
            'https://consumables.example.com/c' || i,
            'Consumable ' || i, 'Verbrauchsmaterial ' || i,
            'Consumable Supplier ' || i, 'Verbrauchsmateriallieferant ' || i,
            'Consumable for product ' || i, 'Verbrauchsmaterial fuer Produkt ' || i,
            'Dispose via certified waste company.',
            'Entsorgung nur durch Fachbetrieb.',
            50
        ) RETURNING "Id" INTO consumable_id;

        INSERT INTO "AssetMaintenanceConsumable" ("AssetId","MaintenanceConsumableId") VALUES (asset_id, consumable_id);

        -- ============================================================
        -- MAINTENANCE SPARE PART  (1 per asset)
        -- ============================================================
        INSERT INTO "MaintenanceSparePart" (
            "Index","SparePartID","OrderCodeOfManufacturer","AddressOfAdditionalLink",
            "SparePartName_en","SparePartName_de",
            "CompanyNameSupplierSparePart_en","CompanyNameSupplierSparePart_de",
            "SparePartDescription_en","SparePartDescription_de",
            "DisposalInstructionsForSparePart_en","DisposalInstructionsForSparePart_de",
            "QuantityOfSparePart"
        ) VALUES (
            0,
            'SP-' || lpad(i::text, 4, '0'),
            'ORD-SPARE-' || lpad(i::text, 4, '0'),
            'https://spareparts.example.com/sp' || i,
            'Spare Part ' || i, 'Ersatzteil ' || i,
            'Spare Supplier ' || i, 'Ersatzteillieferant ' || i,
            'Spare part for product ' || i, 'Ersatzteil fuer Produkt ' || i,
            'Dispose via metal recycling.',
            'Ueber Metallrecycling entsorgen.',
            1
        ) RETURNING "Id" INTO spare_id;

        INSERT INTO "AssetMaintenanceSparePart" ("AssetId","MaintenanceSparePartId") VALUES (asset_id, spare_id);

        -- ============================================================
        -- DOCUMENTS  (2 per asset)
        -- Each document gets: DocumentId, DocumentClassification,
        --                      DocumentVersion, Language
        -- ============================================================

        -- --- Document 1 ---
        INSERT INTO "Document" ("Index") VALUES (0) RETURNING "Id" INTO doc_id_1;

        INSERT INTO "DocumentId" ("Index","DocumentDomainId","DocumentIdentifier","DocumentIsPrimary")
        VALUES (
            0,
            (
                substr(md5(pid || '-doc-1'), 1, 8) || '-' ||
                substr(md5(pid || '-doc-1'), 9, 4) || '-' ||
                substr(md5(pid || '-doc-1'), 13, 4) || '-' ||
                substr(md5(pid || '-doc-1'), 17, 4) || '-' ||
                substr(md5(pid || '-doc-1'), 21, 12)
            )::uuid,
            'DOC-' || pid || '-001',
            TRUE
        )
        RETURNING "Id" INTO doc_ident_id_1;

        INSERT INTO "DocumentClassification" ("Index","ClassId","ClassificationSystem","ClassName_en","ClassName_de")
        VALUES (
            0,
            'CLS-' || lpad(i::text, 3, '0') || '-A',
            CASE i % 4 WHEN 1 THEN 'IEC-61360' WHEN 2 THEN 'ISO-13584' WHEN 3 THEN 'ECLASS-13.0' ELSE 'UNSPSC' END,
            'Document Class A - ' || i,
            'Dokumentklasse A - ' || i
        ) RETURNING "Id" INTO doc_class_id_1;

        INSERT INTO "DocumentVersion" (
            "Index","DigitalFile","Version","StatusSetDate","StatusValue",
            "OrganizationShortName","OrganizationOfficialName",
            "Title_en","Title_de","Subtitle_en","Subtitle_de",
            "Description_en","Description_de","KeyWords_en","KeyWords_de","PreviewFile"
        ) VALUES (
            0,
            'https://docs.google.com/viewer?url=https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.pdf',
            '1.' || i::text,
            ('2023-01-01'::DATE + make_interval(months => (i - 1) % 12))::DATE,
            CASE i % 2 WHEN 1 THEN 'Released' ELSE 'InReview' END,
            'M&M',
            CASE i % 3 WHEN 1 THEN 'M&M Germany' WHEN 2 THEN 'M&M India' ELSE 'M&M China' END,
            concat('Document A for ', pid),
            concat('Dokument A fuer ', pid),
            'Technical document',
            'Technisches Dokument',
            concat('Documentation for product ', i::text),
            concat('Dokumentation fuer Produkt ', i::text),
            'product, manual, guide',
            'Produkt, Handbuch, Leitfaden',
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.jpg'
        ) RETURNING "Id" INTO doc_ver_id_1;

        INSERT INTO "AssetDocument"                   ("AssetId","DocumentId")                VALUES (asset_id,    doc_id_1);
        INSERT INTO "DocumentDocumentId"              ("DocumentId","DocumentIdentifierId")    VALUES (doc_id_1,    doc_ident_id_1);
        INSERT INTO "DocumentDocumentClassification"  ("DocumentId","DocumentClassificationId") VALUES (doc_id_1,  doc_class_id_1);
        INSERT INTO "DocumentVersionLanguages"        ("DocumentVersionId","LanguageId")       VALUES (doc_ver_id_1, lang_id_1);
        INSERT INTO "DocumentDocumentVersion"         ("DocumentId","DocumentVersionId")       VALUES (doc_id_1,    doc_ver_id_1);

        -- --- Document 2 ---
        INSERT INTO "Document" ("Index") VALUES (1) RETURNING "Id" INTO doc_id_2;

        INSERT INTO "DocumentId" ("Index","DocumentDomainId","DocumentIdentifier","DocumentIsPrimary")
        VALUES (
            0,
            (
                substr(md5(pid || '-doc-2'), 1, 8) || '-' ||
                substr(md5(pid || '-doc-2'), 9, 4) || '-' ||
                substr(md5(pid || '-doc-2'), 13, 4) || '-' ||
                substr(md5(pid || '-doc-2'), 17, 4) || '-' ||
                substr(md5(pid || '-doc-2'), 21, 12)
            )::uuid,
            'DOC-' || pid || '-002',
            TRUE
        )
        RETURNING "Id" INTO doc_ident_id_2;

        INSERT INTO "DocumentClassification" ("Index","ClassId","ClassificationSystem","ClassName_en","ClassName_de")
        VALUES (
            1,
            'CLS-' || lpad(i::text, 3, '0') || '-B',
            CASE (i + 1) % 4 WHEN 1 THEN 'IEC-61360' WHEN 2 THEN 'ISO-13584' WHEN 3 THEN 'ECLASS-13.0' ELSE 'UNSPSC' END,
            'Document Class B - ' || i,
            'Dokumentklasse B - ' || i
        ) RETURNING "Id" INTO doc_class_id_2;

        INSERT INTO "DocumentVersion" (
            "Index","DigitalFile","Version","StatusSetDate","StatusValue",
            "OrganizationShortName","OrganizationOfficialName",
            "Title_en","Title_de","Subtitle_en","Subtitle_de",
            "Description_en","Description_de","KeyWords_en","KeyWords_de","PreviewFile"
        ) VALUES (
            1,
            'https://docs.google.com/viewer?url=https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.pdf',
            '2.' || i::text,
            ('2024-01-01'::DATE + make_interval(months => (i - 1) % 12))::DATE,
            CASE (i + 1) % 2 WHEN 1 THEN 'Released' ELSE 'InReview' END,
            'M&M',
            CASE (i + 1) % 3 WHEN 1 THEN 'M&M Germany' WHEN 2 THEN 'M&M India' ELSE 'M&M China' END,
            concat('Document B for ', pid),
            concat('Dokument B fuer ', pid),
            'Compliance document',
            'Konformitaetsdokument',
            concat('Compliance record for product ', i::text),
            concat('Konformitaetsnachweis fuer Produkt ', i::text),
            'compliance, certification',
            'Konformitaet, Zertifizierung',
            'https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/main/example/data/dummy_document.jpg'
        ) RETURNING "Id" INTO doc_ver_id_2;

        INSERT INTO "AssetDocument"                   ("AssetId","DocumentId")                VALUES (asset_id,    doc_id_2);
        INSERT INTO "DocumentDocumentId"              ("DocumentId","DocumentIdentifierId")    VALUES (doc_id_2,    doc_ident_id_2);
        INSERT INTO "DocumentDocumentClassification"  ("DocumentId","DocumentClassificationId") VALUES (doc_id_2,  doc_class_id_2);
        INSERT INTO "DocumentVersionLanguages"        ("DocumentVersionId","LanguageId")       VALUES (doc_ver_id_2, lang_id_2);
        INSERT INTO "DocumentDocumentVersion"         ("DocumentId","DocumentVersionId")       VALUES (doc_id_2,    doc_ver_id_2);

    END LOOP;

END $$;
