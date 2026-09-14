-- ============================================================
-- Handover Documentation Schema (Table Creation)
-- ============================================================
-- This file contains CREATE TABLE statements for:
-- - Documents and Document metadata
-- - Document Classifications
-- - Document Versions
-- - All related relationships
-- ============================================================

-- ============================================================
-- Document Management Tables
-- ============================================================

CREATE TABLE IF NOT EXISTS "Document" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT
);

CREATE TABLE IF NOT EXISTS "DocumentId" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "DocumentDomainId" UUID,
    "DocumentIdentifier" TEXT,
    "DocumentIsPrimary" BOOLEAN
);

CREATE TABLE IF NOT EXISTS "AssetDocument" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "DocumentId" INT REFERENCES "Document"("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "DocumentDocumentId" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "DocumentId" INT REFERENCES "Document"("Id") ON DELETE CASCADE,
    "DocumentIdentifierId" INT REFERENCES "DocumentId"("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "DocumentClassification" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "ClassId" TEXT,
    "ClassificationSystem" TEXT,
    "ClassName_en" TEXT,
    "ClassName_de" TEXT
);

CREATE TABLE IF NOT EXISTS "DocumentDocumentClassification" (
    "Id" INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    "DocumentId" INT REFERENCES "Document"("Id") ON DELETE CASCADE,
    "DocumentClassificationId" INT REFERENCES "DocumentClassification"("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "DocumentVersion" (
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

CREATE TABLE IF NOT EXISTS "Languages" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "Language" TEXT
);

CREATE TABLE IF NOT EXISTS "DocumentVersionLanguages" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "DocumentVersionId" INT REFERENCES "DocumentVersion"("Id") ON DELETE CASCADE,
    "LanguageId" INT REFERENCES "Languages"("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "DocumentDocumentVersion" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "DocumentId" INT REFERENCES "Document"("Id") ON DELETE CASCADE,
    "DocumentVersionId" INT REFERENCES "DocumentVersion"("Id") ON DELETE CASCADE
);



