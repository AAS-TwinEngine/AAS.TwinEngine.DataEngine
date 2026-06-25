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
-- ProductImage
-- ============================================================

CREATE TABLE "ProductImage" (
    "Id" INT PRIMARY KEY,
    "Index" INT,
    "ImageFile" TEXT,
    "ImageNote_en" TEXT,
    "ImageNote_de" TEXT
);

-- ============================================================
-- Asset ↔ ProductImage Mapping
-- ============================================================

CREATE TABLE "AssetProductImage" (
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

CREATE TABLE "ProductClassifications" (
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

CREATE TABLE "AssetProductClassifications" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "ProductClassificationsId" INT REFERENCES "ProductClassifications"("Id") ON DELETE CASCADE
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


