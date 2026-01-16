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

INSERT INTO "Asset" (
    "ProductId","IdShort","GlobalAssetId","AasId","ThumbnailContentType","ThumbnailPath",
    "UriOfTheProduct","ManufacturerProductType","OrderCodeOfManufacturer","ProductArticleNumberOfManufacturer",
    "SerialNumber","YearOfConstruction","DateOfManufacture","HardwareVersion","FirmwareVersion","SoftwareVersion",
    "CountryOfOrigin","UniqueFacilityIdentifier","ManufacturerName","ManufacturerProductDesignation_en",
    "ManufacturerProductDesignation_de","ManufacturerProductRoot_en","ManufacturerProductRoot_de",
    "ManufacturerProductFamily_en","ManufacturerProductFamily_de","CompanyLogo","ManufacturerArticleNumber",
    "ManufacturerOrderCode","ProductImage","ManufacturerLogo","TextStatement_en","TextStatement_de",
    "ValidDate","PcfCalculationMethod","LifeCyclePhase","PcfCO2eq","ReferenceImpactUnitForCalculation",
    "QuantityOfMeasureForCalculation","PublicationDate","ExpirationDate","ExplanatoryStatement"
) VALUES
('000-001','Product1','https://mm-software.com/ids/assets/000-001','https://mm-software.com/ids/aas/000-001','image/jpeg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/product1.jpg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/product1.jpg',
 'FM-ABC-1234','FMABC1234','FM11-ABC22-123456','9804820',2022,'2022-01-01',
 '1.0.0','1.0.0','1.0.0','DE','987654321','M&M Germany',
 'FM-ABC-1234','ABC-123','Camera','Kamera','Electronics','Elektronik',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 '123456','EEA-EX-200-S/47-Q3',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/product1.jpg',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 'Restricted use','Eingeschränkte Nutzung','2035-05-05',
 'ISO 14067','C4 - landfill',17.2,'ml',5,
 '2025-12-24T14:30:00Z','2035-12-24T14:30:00Z',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf'),
('000-002','Product2','https://mm-software.com/ids/assets/000-002','https://mm-software.com/ids/aas/000-002','image/jpeg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/product2.jpg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/product2.jpg',
 'FM-ABC-1235','FMABC1238','FM11-ABC22-123458','9804821',2023,'2023-01-01',
 '1.0.1','1.0.1','1.0.1','IN','123567890','M&M Software mainment Center India',
 'FM-ABC-123','ABC-123','Camera','Kamera','Electronics','Elektronik',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 '123456','EEA-EX-200-S/47-Q4',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/product2.jpg',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 'Restricted use','Eingeschränkte Nutzung','2035-06-05',
 'EN 15804','A5 - Installation',6.2,'cbm',2.2999999999999998,
 '2026-01-15T09:15:00+05:30','2036-01-15T09:15:00+05:31',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf'),
('001-001','Product3','https://mm-software.com/ids/assets/001-001','https://mm-software.com/ids/aas/001-001','image/jpeg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/product3.jpg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/product3.jpg',
 'TM-ABC-1680','TMABC1680','TM11-ABC22-123456','9804760',2024,'2024-01-01',
 '2.0.0','1.0.0','1.0.2','CN','874512451','M&M China',
 'TM-ABC-1234','ABC-1234','perfume','Parfüm','Cosmetics','Kosmetika',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 '123456','EEA-EX-200-S/47-Q5',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/product3.jpg',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 'Restricted use','Eingeschränkte Nutzung','2035-07-05',
 'PACT v2.0.0','C3 - recycling, waste treatment',2.2999999999999998,'piece',7.8,
 '2024-07-01T18:45:00-04:00','2034-07-01T18:45:00-04:01',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf');

-- ============================================================
-- Specific Asset IDs
-- ============================================================

INSERT INTO "SpecificAssetIds" ("AssetId","Name","Value") VALUES
(1,'product1.0','M&M - 001'),
(1,'product1.1','M&M - 002'),
(2,'product2','M&M - 003');
