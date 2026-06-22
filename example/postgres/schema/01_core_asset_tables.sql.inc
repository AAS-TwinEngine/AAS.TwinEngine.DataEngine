-- ============================================================
-- Core Asset Tables
-- ============================================================

CREATE TABLE "Asset" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ProductId" VARCHAR(50),
    "IdShort" VARCHAR(100),
    "GlobalAssetId" TEXT,
    "AasId" TEXT,
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

CREATE TABLE "SpecificAssetIds" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT NOT NULL REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "Name" TEXT,
    "Value" TEXT
);

