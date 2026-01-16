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
    "en" TEXT,
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

CREATE TABLE "DocumentDocumentVersion" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "DocumentId" INT REFERENCES "Document"("Id") ON DELETE CASCADE,
    "DocumentVersionId" INT REFERENCES "DocumentVersion"("Id") ON DELETE CASCADE
);

-- ============================================================
-- Documents
-- ============================================================

INSERT INTO "Document" ("Index") VALUES
(0),(1),(2),(0),(1),(0),(1);

-- ============================================================
-- Document Identifiers
-- ============================================================

INSERT INTO "DocumentId" ("Index","DocumentDomainId","DocumentIdentifier","DocumentIsPrimary") VALUES
(0,'a3f1c2b4-9d81-4e5a-b6f2-01ac9e11d001','DOC2025A001',TRUE),
(1,'b7e92d10-3c45-4a8f-9f21-02bc9e11d002','CERTX94B21',FALSE),
(0,'c81a4f22-6d91-43bb-a812-03cd9e11d003','MANUAL8F2Q7',TRUE),
(0,'a3f1c2b4-9d81-4e5a-b6f2-01ac9e11d002','DOC2025A002',TRUE),
(0,'b7e92d10-3c45-4a8f-9f21-02bc9e11d003','CERTX94B22',TRUE),
(0,'c81a4f22-6d91-43bb-a812-03cd9e11d004','MANUAL8F2Q8',TRUE),
(1,'a3f1c2b4-9d81-4e5a-b6f2-01ac9e11d003','DOC2025A003',TRUE),
(2,'b7e92d10-3c45-4a8f-9f21-02bc9e11d004','CERTX94B23',FALSE),
(0,'c81a4f22-6d91-43bb-a812-03cd9e11d005','MANUAL8F2Q9',FALSE);

-- ============================================================
-- Asset-Document Relationships
-- ============================================================

INSERT INTO "AssetDocument" ("AssetId","DocumentId") VALUES
(1,1),
(1,2),
(1,3),
(2,4),
(2,5),
(3,6),
(3,7);

-- ============================================================
-- Document-DocumentId Relationships
-- ============================================================

INSERT INTO "DocumentDocumentId" ("DocumentId","DocumentIdentifierId") VALUES
(1,1),
(1,2),
(2,3),
(3,4),
(4,5),
(5,6),
(6,6),
(7,9),
(7,7),
(7,8);

-- ============================================================
-- Document Classifications
-- ============================================================

INSERT INTO "DocumentClassification" ("Index","ClassId","ClassificationSystem","ClassName_en","ClassName_de") VALUES
(0,'CLS-001','IEC-61360','Electrical Components','Elektrische Bauteile'),
(1,'CLS-002','ISO-13584','Hydraulic Pumps','Hydraulikpumpen'),
(0,'CLS-003','ECLASS-13.0','Industrial Sensors','Industrielle Sensoren'),
(0,'CLS-004','UNSPSC','Fasteners','Befestigungselemente'),
(0,'CLS-005','ISO-81346','Bearings','Lager'),
(0,'CLS-006','IEC-61131','PLC Controllers','PLC-Steuerungen'),
(1,'CLS-007','ECLASS-13.0','Safety Equipment','Sicherheitsausrüstung'),
(2,'CLS-008','GS1','Packaging Materials','Verpackungsmaterialien'),
(0,'CLS-009','ISO-13584','Cooling Systems','Kühlsysteme');

-- ============================================================
-- Document-Classification Relationships
-- ============================================================

INSERT INTO "DocumentDocumentClassification" ("DocumentId","DocumentClassificationId") VALUES
(1,1),
(1,2),
(2,3),
(3,4),
(4,5),
(5,6),
(6,6),
(7,9),
(7,7),
(7,8);

-- ============================================================
-- Document Versions
-- ============================================================

INSERT INTO "DocumentVersion" (
  "Index","en","DigitalFile","Version","StatusSetDate","StatusValue","OrganizationShortName",
  "OrganizationOfficialName","Title_en","Title_de","Subtitle_en","Subtitle_de","Description_en","Description_de",
  "KeyWords_en","KeyWords_de","PreviewFile"
) VALUES
(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf','1','2023-01-01','Released','M&M','M&M Germany',
 'User Guide – DSLR Camera Model X100','Benutzerhandbuch – DSLR-Kamera Modell X100','Complete Instructions for Professional Photography','Vollständige Anleitung für professionelle Fotografie',
 'Detailed instructions for operating the X100 DSLR camera, including setup and troubleshooting.','Detaillierte Anweisungen zur Bedienung der DSLR-Kamera X100, einschließlich Einrichtung und Fehlerbehebung',
 'DSLR, Camera, Photography, User Guide, Setup','DSLR, Kamera, Fotografie, Benutzerhandbuch, Einrichtung','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(1,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf','1.1','2024-05-05','InReview','M&M','M&M Software mainment Center India',
 'Technical Specification – Mirrorless Camera Z-Series','Technische Spezifikation – Systemkamera der Z-Serie','Detailed Specs for Advanced Imaging','Detaillierte Spezifikationen für fortschrittliche Bildgebung',
 'Comprehensive technical details of the Z-Series mirrorless camera, covering sensor and performance.','Umfassende technische Details der spiegellosen Kamera Z-Serie, einschließlich Sensor und Leistung.','Mirrorless, Camera, Specs, Imaging, Performance','Spiegellos, Kamera, Spezifikationen, Bildgebung, Leistung','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf','2.1','2026-01-01','Released','M&M','M&M China',
 'Maintenance Manual – Professional Camera Lens 50mm','Wartungshandbuch – Professionelles Kameraobjektiv 50 mm','Care and Cleaning Procedures','Pflege und Reinigungsverfahren',
 'Guidelines for cleaning and maintaining the 50mm professional lens for optimal performance.','Richtlinien zur Reinigung und Wartung des professionellen 50-mm-Objektivs für optimale Leistung.','Lens, Maintenance, Cleaning, Professional, Care','Objektiv, Wartung, Reinigung, Professionell, Pflege','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf','2.3','2025-10-10','InReview','M&M','M&M Germany',
 'Installation Guide – Wide-Angle Lens Kit','Installationsanleitung – Weitwinkel-Objektiv-Kit','Step-by-Step Setup Instructions','Schritt-für-Schritt-Installationsanleitung',
 'Step-by-step instructions for installing and configuring the wide-angle lens kit.','Schritt-für-Schritt-Anleitung zur Installation und Konfiguration des Weitwinkel-Objektivsets.','Wide-Angle, Lens, Installation, Setup, Kit','Weitwinkel, Objektiv, Installation, Einrichtung, Set','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf','0.9','2024-01-01','Released','M&M','M&M Software mainment Center India',
 'Product Data Sheet – Telephoto Lens 200mm','Produktdatenblatt – Teleobjektiv 200 mm','Technical Data and Performance Metrics','Technische Daten und Leistungskennzahlen',
 'Technical data and compatibility details for the 200mm telephoto lens.','Technische Daten und Kompatibilitätsdetails für das 200-mm-Teleobjektiv.','Telephoto, Lens, Data Sheet, Specifications, Optics','Teleobjektiv, Objektiv, Datenblatt, Spezifikationen, Optik','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf','1.2','2024-03-03','InReview','M&M','M&M China',
 'Safety Instructions – Digital Camera Accessories','Sicherheitsanweisungen – Zubehör für Digitalkameras','Guidelines for Safe Usage','Richtlinien für sichere Verwendung',
 'Safety guidelines for handling batteries, chargers, and other camera accessories.','Sicherheitsrichtlinien für den Umgang mit Batterien, Ladegeräten und anderem Kamera-Zubehör.','Safety, Camera, Accessories, Guidelines, Handling','Sicherheit, Kamera, Zubehör, Richtlinien, Handhabung','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(1,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf','1.4','2023-01-01','Released','M&M','M&M Germany',
 'Perfume Catalog – Luxury Fragrance Collection 2025','Parfümkatalog – Luxusduftkollektion 2025','Explore Elegant Scents for Every Occasion','Entdecken Sie elegante Düfte für jeden Anlass',
 'A curated catalog showcasing premium perfumes with scent profiles and packaging details.','Ein kuratierter Katalog mit Premium-Parfums, Duftprofilen und Verpackungsdetails.','Perfume, Fragrance, Luxury, Catalog, Collection','Parfum, Duft, Luxus, Katalog, Kollektion','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(2,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf','2','2024-03-03','InReview','M&M','M&M Software mainment Center India',
 'Quality Assurance Report – Eau de Parfum Series A','Qualitätssicherungsbericht – Eau de Parfum Serie A','Verified Standards and Testing Results','Geprüfte Standards und Testergebnisse',
 'Report detailing quality checks and compliance standards for Series A perfumes.','Bericht mit Qualitätsprüfungen und Konformitätsstandards für Parfums der Serie A.','Packaging, Perfume, Bottles, Caps, Compliance','Verpackung, Parfum, Flaschen, Verschlüsse, Konformität','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/main/example/data/dummy_document.pdf','1','2022-02-02','Released','M&M','M&M Germany',
 'Packaging Standards – Perfume Bottles and Caps','Verpackungsstandards – Parfümflaschen und Verschlüsse','Design and Material Compliance Guidelines','Richtlinien für Design und Materialkonformität',
 'Design and material compliance guidelines for perfume packaging.','Richtlinien für Design und Materialkonformität bei Parfumverpackungen.','Perfume, Fragrance, Luxury, Catalog, Collection','Parfum, Duft, Luxus, Katalog, Kollektion','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)');

-- ============================================================
-- Document-Version Relationships
-- ============================================================

INSERT INTO "DocumentDocumentVersion" ("DocumentId","DocumentVersionId") VALUES
(1,1),
(1,2),
(2,3),
(3,4),
(4,5),
(5,6),
(6,6),
(7,9),
(7,7),
(7,8);
