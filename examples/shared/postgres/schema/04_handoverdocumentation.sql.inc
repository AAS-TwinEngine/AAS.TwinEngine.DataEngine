-- ============================================================
-- Classification and Document Data Insertion
-- ============================================================
-- This file contains CREATE and INSERT statements for:
-- - Documents and Document metadata
-- - Document Classifications
-- - Document Versions
-- - All related relationships
-- ============================================================

-- ============================================================
-- Document Management Tables
-- ============================================================

CREATE TABLE "Document" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT
);

CREATE TABLE "DocumentId" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "DocumentDomainId" UUID,
    "DocumentIdentifier" TEXT,
    "DocumentIsPrimary" BOOLEAN
);

CREATE TABLE "AssetDocument" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "DocumentId" INT REFERENCES "Document"("Id") ON DELETE CASCADE
);

CREATE TABLE "DocumentDocumentId" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "DocumentId" INT REFERENCES "Document"("Id") ON DELETE CASCADE,
    "DocumentIdentifierId" INT REFERENCES "DocumentId"("Id") ON DELETE CASCADE
);

CREATE TABLE "DocumentClassification" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "ClassId" TEXT,
    "ClassificationSystem" TEXT,
    "ClassName_en" TEXT,
    "ClassName_de" TEXT
);

CREATE TABLE "DocumentDocumentClassification" (
    "Id" INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    "DocumentId" INT REFERENCES "Document"("Id") ON DELETE CASCADE,
    "DocumentClassificationId" INT REFERENCES "DocumentClassification"("Id") ON DELETE CASCADE
);

CREATE TABLE "DocumentVersion" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "DigitalFile" TEXT,
    "Version" TEXT,
    "StatusSetDate" DATE,
    "StatusValue" TEXT,
    "OrganizationShortName" TEXT,
    "OrganizationOfficialName" TEXT,
    "Title_en" TEXT,
    "Title_de" TEXT,
    "Subtitle_en" TEXT,
    "Subtitle_de" TEXT,
    "Description_en" TEXT,
    "Description_de" TEXT,
    "KeyWords_en" TEXT,
    "KeyWords_de" TEXT,
    "PreviewFile" TEXT
);

CREATE TABLE "Languages" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "Language" TEXT
);

CREATE TABLE "DocumentVersionLanguages" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "DocumentVersionId" INT REFERENCES "DocumentVersion"("Id") ON DELETE CASCADE,
    "LanguageId" INT REFERENCES "Languages"("Id") ON DELETE CASCADE
);

CREATE TABLE "DocumentDocumentVersion" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "DocumentId" INT REFERENCES "Document"("Id") ON DELETE CASCADE,
    "DocumentVersionId" INT REFERENCES "DocumentVersion"("Id") ON DELETE CASCADE
);



