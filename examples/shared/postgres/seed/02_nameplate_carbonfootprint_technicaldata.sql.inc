INSERT INTO "Marking"
("Index","MarkingName","DesignationOfCertificateOrApproval","IssueDate","ExpiryDate",
 "MarkingAdditionalText","MarkingFile")
VALUES
(0,'0173-1#07-DAA603#004','KEMA99IECEX1105/128','2022-01-01','2030-01-01','additional information on the marking - 00',
 ('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
    'main/examples/shared/data/checkmark.png')),
(0,'0173-1#07-DAA603#005','KEMA99IECEX1105/129','2022-02-01','2030-02-01','additional information on the marking - 01',
 ('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
    'main/examples/shared/data/checkmark.png')),
(1,'0173-1#07-DAA603#006','KEMA99IECEX1105/130','2022-03-01','2030-03-01','additional information on the marking - 02',
 ('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
    'main/examples/shared/data/checkmark.png')),
(0,'0173-1#07-DAA603#007','KEMA99IECEX1105/131','2022-04-01','2030-04-01','additional information on the marking - 03',
 ('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
    'main/examples/shared/data/checkmark.png')),
(1,'0173-1#07-DAA603#008','KEMA99IECEX1105/132','2022-05-01','2030-05-01','additional information on the marking - 04',
 ('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
    'main/examples/shared/data/checkmark.png'));

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
-- INSERT : ProductImage
-- ============================================================

INSERT INTO "ProductImage" (
    "Id",
    "Index",
    "ImageFile",
    "ImageNote_en",
    "ImageNote_de"
)
VALUES

(
    1,
    0,
    ('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
    'main/examples/shared/data/product1.jpg'),
    'Front view of the machine showing external and structure',
    'Frontansicht der Maschine, die äußere und strukturelle Ansicht zeigt'
),

(
    2,
    1,
    ('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
    'develop/examples/shared/data/product1-1.png'),
    'Side view of the machine showing external and structure',
    'Seitenansicht der Maschine, die äußere und strukturelle Ansicht zeigt'
),

(
    3,
    0,
    ('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
    'main/examples/shared/data/product2.jpg'),
    'Front view of the machine showing external and structure',
    'Frontansicht der Maschine, die äußere und strukturelle Ansicht zeigt'
),

(
    4,
    1,
    ('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
    'develop/examples/shared/data/product3-1.png'),
    'Side view of the machine showing external and structure',
    'Seitenansicht der Maschine, die äußere und strukturelle Ansicht zeigt'
),

(
    5,
    0,
    ('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
    'main/examples/shared/data/product3.jpg'),
    'Front view of the machine showing external and structure',
    'Frontansicht der Maschine, die äußere und strukturelle Ansicht zeigt'
);

-- ============================================================
-- INSERT : AssetProductImage
-- ============================================================

INSERT INTO "AssetProductImage" (
    "Id",
    "AssetId",
    "ProductImageId"
)
VALUES

(1, 1, 1),
(2, 1, 2),
(3, 2, 3),
(4, 3, 5),
(5, 3, 4);

-- ============================================================
-- Product Classification
-- ============================================================

INSERT INTO "ProductClassifications"
(
    "Index",
    "ClassificationSystem",
    "ClassificationSystemVersion",
    "ClassificationSystemUrl",
    "ProductClassId",
    "ProductClassCodedName",
    "ProductClassName_en",
    "ProductClassName_de"
)
VALUES
(
    0,
    'ECLASS',
    '14',
    'https://eclass.eu',
    '19-01-01-01',
    'ECLASS-CAM-001',
    'Digital Camera',
    'Digitalkamera'
),
(
    1,
    'IEC CDD',
    '2024-09',
    'https://cdd.iec.ch',
    'IEC-CDD-AAA124',
    'IEC-SENSOR-124',
    'Industrial Sensor',
    'Industriesensor'
),
(
    2,
    'UNSPSC',
    '23.0301',
    'https://www.unspsc.org',
    'UNSPSC-43211503',
    'UNSPSC-COMPUTE-003',
    'Portable Computer Device',
    'Tragbares Computergerät'
),
(
    0,
    'ISO 13584',
    '2023',
    'https://www.iso.org/standard/13584.html',
    'ISO13584-XYZ789',
    'ISO-PUMP-789',
    'Hydraulic Pump',
    'Hydraulikpumpe'
),
(
    0,
    'ECLASS',
    '5',
    'https://eclass.eu',
    '27-02-03-05',
    'ECLASS-PERFUME-005',
    'Luxury Perfume',
    'Luxusparfüm'
),
(
    1,
    'IEC CDD',
    '2024-09',
    'https://cdd.iec.ch',
    'IEC-CDD-AAA123',
    'IEC-CONTROLLER-123',
    'PLC Controller',
    'PLC-Steuerung'
);

INSERT INTO "AssetProductClassifications" ("AssetId","ProductClassificationsId") VALUES
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
    "AssetId","PcfCalculationMethod","PcfRuleOperator","PcfRuleName","PcfRuleVersion",
    "PcfRuleOnlineReference","PcfApiEndpoint","PcfApiQuery"
) VALUES
(
    1,'IEC TS 63058','GHG Protocol','GHG Protocol Product Standard','1.1',
    'https://ghgprotocol.org/standards/product-standard',
    'https://api.carbonfootprint.org/v1/calculate',
    '?productId=12345&unit=kgCO2e&scope=cradle-to-gate'
),
(
    2,'EN 15804','ISO 14067','ISO 14067','2.1',
    'https://www.iso.org/standard/43278.html',
    'https://api.iso14067.org/v2/emissions',
    '?sector=electronics&region=EU&year=2025'
),
(
    3,'PACT v2.0.0','PAS 2050','PAS 2050','0.9',
    'https://www.bsigroup.com/en-GB/PAS-2050-Carbon-Footprint/',
    'https://api.pas2050.com/v1/footprint',
    '?material=steel&quantity=1000kg&method=ISO14067'
);
