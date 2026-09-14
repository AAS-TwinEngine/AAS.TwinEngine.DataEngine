-- ============================================================
-- Core Asset Tables
-- ============================================================

CREATE TABLE IF NOT EXISTS "Asset" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ProductId" VARCHAR(50) UNIQUE,
    "IdShort" VARCHAR(100),
    "GlobalAssetId" TEXT,
    "AasId" TEXT,
    "AssetKind" VARCHAR(50) NOT NULL,
    "AssetType" VARCHAR(100),
    "ThumbnailContentType" VARCHAR(50),
    "ThumbnailPath" TEXT,
    "MaintenanceFreeAsset" BOOLEAN,
    "UriOfTheProduct" TEXT,
    "ManufacturerProductType" TEXT,
    "OrderCodeOfManufacturer" TEXT,
    "ProductArticleNumberOfManufacturer" TEXT,
    "SerialNumber" TEXT,
    "YearOfConstruction" INT,
    "DateOfManufacture" DATE,
    "HardwareVersion" TEXT,
    "FirmwareVersion" TEXT,
    "SoftwareVersion" TEXT,
    "CountryOfOrigin" TEXT,
    "UniqueFacilityIdentifier" TEXT,
    "ManufacturerName" TEXT,
    "ManufacturerProductDesignation_en" TEXT,
    "ManufacturerProductDesignation_de" TEXT,
    "ManufacturerProductRoot_en" TEXT,
    "ManufacturerProductRoot_de" TEXT,
    "ManufacturerProductFamily_en" TEXT,
    "ManufacturerProductFamily_de" TEXT,
    "CompanyLogo" TEXT,
    "ManufacturerArticleNumber" TEXT,
    "ManufacturerOrderCode" TEXT,
    "ProductImage" TEXT,
    "ManufacturerLogo" TEXT,
    "TextStatement_en" TEXT,
    "TextStatement_de" TEXT,
    "ValidDate" DATE,
    "PcfCalculationMethod" TEXT,
    "LifeCyclePhase" TEXT,
    "PcfCO2eq" NUMERIC,
    "ReferenceImpactUnitForCalculation" TEXT,
    "QuantityOfMeasureForCalculation" NUMERIC,
    "PublicationDate" TIMESTAMP,
    "ExpirationDate" TIMESTAMP,
    "ExplanatoryStatement" TEXT
);

CREATE TABLE IF NOT EXISTS "SpecificAssetIds" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ProductId" VARCHAR(50) NOT NULL REFERENCES "Asset"("ProductId") ON DELETE CASCADE,
    "Name" TEXT,
    "Value" TEXT
);

CREATE INDEX idx_asset_productid
ON "Asset"("ProductId");
