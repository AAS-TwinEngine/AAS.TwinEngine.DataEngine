-- ============================================================
-- Nameplate / Carbon Footprint / Technical Data Schema (Table Creation)
-- ============================================================
-- This file contains CREATE TABLE statements for core business entities for the Nameplate submodel:
-- - Markings
-- - Asset-Marking relationships
-- - Product Classification Items
-- - Carbon Footprint data
-- -Asset-ProductClassifications relationships
-- ============================================================


-- ============================================================
-- Marking Data
-- ============================================================

CREATE TABLE IF NOT EXISTS "TechnicalPropertyArea" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT NOT NULL REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "Index" INT NOT NULL,
    "Length" NUMERIC,
    "Width" NUMERIC,
    "Height" NUMERIC,
    UNIQUE ("AssetId", "Index")
);

CREATE INDEX IF NOT EXISTS ix_technical_property_area_asset
ON "TechnicalPropertyArea" ("AssetId", "Index");


CREATE TABLE IF NOT EXISTS "Marking" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "MarkingName" TEXT,
    "DesignationOfCertificateOrApproval" TEXT,
    "IssueDate" DATE,
    "ExpiryDate" DATE,
    "MarkingAdditionalText" TEXT,
    "MarkingFile" TEXT
);

CREATE TABLE IF NOT EXISTS "AssetMarking" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT NOT NULL REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "MarkingId" INT NOT NULL REFERENCES "Marking"("Id") ON DELETE CASCADE
);

-- ============================================================
-- ProductImage
-- ============================================================

CREATE TABLE IF NOT EXISTS "ProductImage" (
    "Id" INT PRIMARY KEY,
    "Index" INT,
    "ImageFile" TEXT,
    "ImageNote_en" TEXT,
    "ImageNote_de" TEXT
);

-- ============================================================
-- Asset ↔ ProductImage Mapping
-- ============================================================

CREATE TABLE IF NOT EXISTS "AssetProductImage" (
    "Id" INT PRIMARY KEY,

    "AssetId" INT NOT NULL
        REFERENCES "Asset"("Id")
        ON DELETE CASCADE,

    "ProductImageId" INT NOT NULL
        REFERENCES "ProductImage"("Id")
        ON DELETE CASCADE
);

-- ============================================================
-- Product Classification Tables
-- ============================================================

CREATE TABLE IF NOT EXISTS "ProductClassifications" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "ClassificationSystem" TEXT,
    "ClassificationSystemVersion" TEXT,
    "ClassificationSystemUrl"   TEXT,
    "ProductClassId" TEXT,
    "ProductClassCodedName"   TEXT,
    "ProductClassName_en" TEXT,
    "ProductClassName_de" TEXT
);

CREATE TABLE IF NOT EXISTS "AssetProductClassifications" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "ProductClassificationsId" INT REFERENCES "ProductClassifications"("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "ProductOrSectorSpecificCarbonFootprint" (
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

CREATE INDEX IF NOT EXISTS ix_asset_marking_asset_marking
ON "AssetMarking" ("AssetId", "MarkingId");

CREATE INDEX IF NOT EXISTS ix_asset_product_image_asset_image
ON "AssetProductImage" ("AssetId", "ProductImageId");

CREATE INDEX IF NOT EXISTS ix_asset_product_classifications_asset_classification
ON "AssetProductClassifications" ("AssetId", "ProductClassificationsId");

CREATE INDEX IF NOT EXISTS ix_product_sector_pcf_asset
ON "ProductOrSectorSpecificCarbonFootprint" ("AssetId");


