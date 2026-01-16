-- ============================================================
-- Core Data Insertion
-- ============================================================
-- This file contains CREATE and INSERT statements for core business entities for NamePlate Submodel:
-- - Markings
-- - Asset-Marking relationships
-- - Product Classification Items
-- - Carbon Footprint data
-- -Asset-ProductClassificationItem relationships
-- ============================================================

CREATE TABLE "Marking" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "MarkingName" TEXT,
    "DesignationOfCertificateOrApproval" TEXT,
    "IssueDate" DATE,
    "ExpiryDate" DATE,
    "MarkingAdditionalText" TEXT,
    "MarkingFile" TEXT
);

CREATE TABLE "AssetMarking" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT NOT NULL REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "MarkingId" INT NOT NULL REFERENCES "Marking"("Id") ON DELETE CASCADE
);

-- ============================================================
-- Product Classification Tables
-- ============================================================

CREATE TABLE "ProductClassificationItem" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "ProductClassificationSystem" TEXT,
    "ClassificationSystemVersion" TEXT,
    "ProductClassId" TEXT
);

CREATE TABLE "AssetProductClassificationItem" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "ProductClassificationItemId" INT REFERENCES "ProductClassificationItem"("Id") ON DELETE CASCADE
);

CREATE TABLE "ProductOrSectorSpecificCarbonFootprint" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "PcfCalculationMethod" TEXT,
    "PcfRuleOperator" TEXT,
    "PcfRuleName" TEXT,
    "PcfRuleVersion" TEXT,
    "PcfRuleOnlineReference" TEXT,
    "PcfApiEndpoint" TEXT,
    "PcfApiQuery" TEXT
);

-- ============================================================
-- Marking Data
-- ============================================================

INSERT INTO "Marking" ("Index","MarkingName","DesignationOfCertificateOrApproval","IssueDate","ExpiryDate","MarkingAdditionalText","MarkingFile") VALUES
(0,'0173-1#07-DAA603#004','KEMA99IECEX1105/128','2022-01-01','2030-01-01','additional information on the marking - 00',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/checkmark.png'),
(0,'0173-1#07-DAA603#005','KEMA99IECEX1105/129','2022-02-01','2030-02-01','additional information on the marking - 01',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/checkmark.png'),
(1,'0173-1#07-DAA603#006','KEMA99IECEX1105/130','2022-03-01','2030-03-01','additional information on the marking - 02',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/checkmark.png'),
(0,'0173-1#07-DAA603#007','KEMA99IECEX1105/131','2022-04-01','2030-04-01','additional information on the marking - 03',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/checkmark.png'),
(1,'0173-1#07-DAA603#008','KEMA99IECEX1105/132','2022-05-01','2030-05-01','additional information on the marking - 04',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/checkmark.png');

-- ============================================================
-- Asset-Marking Relationships
-- ============================================================

INSERT INTO "AssetMarking" ("AssetId","MarkingId") VALUES
(1,1),
(1,3),
(2,2),
(2,3),
(3,4),
(3,5);

-- ============================================================
-- Product Classification
-- ============================================================

INSERT INTO "ProductClassificationItem" ("Index","ProductClassificationSystem","ClassificationSystemVersion","ProductClassId") VALUES
(0,'ECLASS','14','19-01-01-01'),
(1,'IEC CDD','2024-09','IEC-CDD-AAA124'),
(2,'UNSPSC','23.0301','UNSPSC-43211503'),
(0,'ISO 13584','2023','ISO13584-XYZ789'),
(0,'ECLASS','5','27-02-03-05'),
(1,'IEC CDD','2024-09','IEC-CDD-AAA123');

INSERT INTO "AssetProductClassificationItem" ("AssetId","ProductClassificationItemId") VALUES
(1,1),
(1,2),
(1,3),
(2,4),
(3,5),
(3,6);

-- ============================================================
-- Carbon Footprint Data
-- ============================================================

INSERT INTO "ProductOrSectorSpecificCarbonFootprint" (
  "AssetId","PcfCalculationMethod","PcfRuleOperator","PcfRuleName","PcfRuleVersion","PcfRuleOnlineReference","PcfApiEndpoint","PcfApiQuery"
) VALUES
(1,'IEC TS 63058','GHG Protocol','GHG Protocol Product Standard','1.1','https://ghgprotocol.org/standards/product-standard','https://api.carbonfootprint.org/v1/calculate','?productId=12345&unit=kgCO2e&scope=cradle-to-gate'),
(2,'EN 15804','ISO 14067','ISO 14067','2.1','https://www.iso.org/standard/43278.html','https://api.iso14067.org/v2/emissions','?sector=electronics&region=EU&year=2025'),
(3,'PACT v2.0.0','PAS 2050','PAS 2050','0.9','https://www.bsigroup.com/en-GB/PAS-2050-Carbon-Footprint/','https://api.pas2050.com/v1/footprint','?material=steel&quantity=1000kg&method=ISO14067');
