INSERT INTO "MaintenanceInstructionsForSpecificInterval" (
    "Index",
    "MaintenanceID",
    "NameOfMaintenance_en",
    "NameOfMaintenance_de",
    "SourceOfMaintenanceInstructions_en",
    "SourceOfMaintenanceInstructions_de",
    "RelatedStandardsLawsRegulations_en",
    "RelatedStandardsLawsRegulations_de",
    "SafetyRegulationsToBeObserved_en",
    "SafetyRegulationsToBeObserved_de",
    "MaintenanceIntervalValue",
    "MaintenanceIntervalUnit",
    "FlowChartOfMaintenanceSteps",
    "NumberOfRequiredTechnicians",
    "RequiredQualification_en",
    "RequiredQualification_de",
    "ValueTotalEstimatedWorkingTime",
    "UnitValueTotalEstimatedWorkingTime"
) VALUES

(0, 'MNT-001',
'Oil change after 6000 km',
'Ölwechsel nach 6000 km',
'manufacturer',
'Fahrzeughersteller',
'ISO 9001 maintenance guidelines',
'ISO 9001 Wartungsrichtlinien',
'Disconnect power; Secure against restart; Check no voltage',
'Stromversorgung trennen; Gegen Wiedereinschalten sichern; Spannungsfreiheit prüfen',
6,'months',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
2,
'Qualified maintenance technician required',
'Qualifizierter Wartungstechniker erforderlich',
2,'hour'),

(1, 'MNT-002',
'Filter replacement every 12 months',
'Filterwechsel alle 12 Monate',
'Component supplier',
'Komponentenlieferant',
'DIN EN 13306 Maintenance terminology',
'DIN EN 13306 Wartungsbegriffe',
'Wear gloves; Disconnect system; Dispose filters safely',
'Schutzhandschuhe tragen; System trennen; Filter entsorgen',
12,'months',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
1,
'Certified technician required',
'Zertifizierter Techniker erforderlich',
4,'hour'),

(0, 'MNT-003',
'Brake inspection every 10,000 km',
'Bremseninspektion alle 10.000 km',
'Authorized service center',
'Autorisierte Servicestelle',
'TRBS 1112 Part 1 Explosion hazards',
'TRBS 1112 Teil 1 Explosionsgefährdung',
'Secure vehicle; Use safety stands; Wear goggles',
'Fahrzeug sichern; Unterstellböcke verwenden; Schutzbrille tragen',
3,'months',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
0,
'IEC/EN 60079-14 knowledge required',
'IEC/EN 60079-14 Kenntnisse erforderlich',
5,'hour'),

(0, 'MNT-004',
'Coolant level check every 6 months',
'Kühlmittelstand alle 6 Monate prüfen',
'Machine manufacturer',
'Maschinenhersteller',
'EU Machinery Directive 2006/42/EC',
'EU-Maschinenrichtlinie 2006/42/EG',
'Allow cooling; Open cap slowly; Avoid contact',
'Motor abkühlen lassen; Deckel langsam öffnen; Kontakt vermeiden',
4,'months',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
1,
'IEC/EN 60079-19 knowledge required',
'IEC/EN 60079-19 Kenntnisse erforderlich',
1,'hour'),

(1, 'MNT-005',
'Battery inspection annually',
'Batterieprüfung jährlich',
'Maintenance authority',
'Wartungsbehörde',
'IEC 60079 Explosive atmospheres standard',
'IEC 60079 Explosionsschutz',
'Disconnect battery; Avoid sparks; Wear gloves',
'Batterie trennen; Funken vermeiden; Handschuhe tragen',
9,'months',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
2,
'IEC/EN 61009-1 knowledge required',
'IEC/EN 61009-1 Kenntnisse erforderlich',
30,'minute');

INSERT INTO "AssetMaintenanceInstruction"
("AssetId", "MaintenanceInstructionId")
VALUES
(1,1),
(1,2),
(2,3),
(3,4),
(3,5);

INSERT INTO "Alarm" (
    "Index",
    "AlarmName_en",
    "AlarmName_de",
    "WarningLimitRelativeValue",
    "WarningLimitSeverity"
) VALUES
(0, 'Pre-alarm at 300 operating hours', 'Voralarm bei 300 Betriebsstunden', 80, 'Warning'),
(0, 'Maintenance alert at 500 operating hours', 'Wartungswarnung bei 500 Betriebsstunden', 85, 'Warning'),
(0, 'Critical maintenance threshold reached', 'Kritischer Wartungsschwellenwert erreicht', 95, 'Critical'),
(0, 'Filter replacement warning', 'Warnung für Filterwechsel', 75, 'Warning'),
(0, 'Overdue maintenance alarm', 'Wartung überfällig Alarm', 100, 'Critical');

INSERT INTO "MaintenanceInstructionAlarm"
("MaintenanceInstructionId", "AlarmId")
VALUES
(1,1),
(2,2),
(3,3),
(4,4),
(5,5);

INSERT INTO "ContactForMaintenanceAuthorization" (
"Index","Company_en","Company_de","Department_en","Department_de",
"Title_en","Title_de","AcademicTitle_en","AcademicTitle_de",
"NameOfContact_en","NameOfContact_de",
"FirstName_en","FirstName_de",
"MiddleNames_en","MiddleNames_de",
"Street_en","Street_de",
"Zipcode_en","Zipcode_de",
"CityTown_en","CityTown_de",
"NationalCode_en","NationalCode_de",
"StateCounty_en","StateCounty_de",
"FurtherDetailsOfContact_en","FurtherDetailsOfContact_de",
"RoleOfContactPerson"
) VALUES

(0,'M&M Software Development Center India','M&M Software Development Center India',
'Human Resources','Human Resources',
'Mr.','Herr','Dr.','Dr.',
'Aarav Sharma','Aarav Sharma',
'Aarav','Aarav','Sharma','Sharma',
'221B Baker Street','221B Baker Street',
'110001','110001',
'Mumbai','München',
'IN','IN',
'Delhi','Delhi',
'Responsible for B2B sales in region-1',
'Responsible for B2B sales in region-1',
'0173-1#07-AAS927#001'),

(1,'M&M Germany','M&M Germany',
'Finance','Finance',
'Mr.','Herr','Dr.','Dr.',
'Lukas Mueller','Lukas Müller',
'Lukas','Lukas','Mueller','Müller',
'Hauptstraße 15','Hauptstraße 16',
'10115','10115',
'Berlin','Berlin',
'DE','DE',
'Berlin','Berlin',
'Responsible for B2B sales in region-2',
'Responsible for B2B sales in region-2',
'0173-1#07-AAS928#001'),

(0,'M&M Software Development Center India','M&M Software Development Center India',
'Operations','Operations',
'Ms','Frau','Prof.','Prof.',
'Priya Mehta','Priya Mehta',
'Priya','Priya','Mehta','Mehta',
'221B Baker Street','221B Baker Street',
'110001','110001',
'Mumbai','München',
'IN','IN',
'Delhi','Delhi',
'Responsible for B2B sales in region-3',
'Responsible for B2B sales in region-3',
'0173-1#07-AAS931#001'),

(1,'M&M Germany','M&M Germany',
'Human Resources','Human Resources',
'Ms','Frau','Prof.','Prof.',
'Anna Schneider','Anna Schneider',
'Anna','Anna','Schneider','Schneider',
'Hauptstraße 15','Hauptstraße 16',
'10115','10115',
'Berlin','Berlin',
'DE','DE',
'Berlin','Berlin',
'Responsible for B2B sales in region-4',
'Responsible for B2B sales in region-4',
'0173-1#07-AAS930#001'),

(2,'M&M China','M&M China',
'Finance','Finance',
'Mr.','Herr','Dr.','Dr.',
'Anna Müller','Wei Li',
'Li','Li','Wei','Wei',
'88 Nanjing Road','89 Nanjing Road',
'200001','200001',
'Beijing','Beijing',
'CN','CN',
'Shanghai Municipality','Shanghai Municipality',
'Responsible for B2B sales in region-5',
'Responsible for B2B sales in region-5',
'0173-1#07-AAS929#001'),

(0,'M&M Germany','M&M Germany',
'Operations','Operations',
'Ms','Frau','Prof.','Prof.',
'Johann Becker','Johann Becker',
'Johann','Johann','Becker','Becker',
'Hauptstraße 15','Hauptstraße 16',
'10115','10115',
'Berlin','Berlin',
'DE','DE',
'Berlin','Berlin',
'Responsible for B2B sales in region-6',
'Responsible for B2B sales in region-6',
'0173-1#07-AAS928#001');

INSERT INTO "Email"
("ContactForMaintenanceAuthorizationId","EmailAddress","TypeOfEmailAddress",
 "PublicKey_en","PublicKey_de",
 "TypeOfPublicKey_en","TypeOfPublicKey_de")
VALUES
(1,'aarav.sharma@examples/shared.in','0173-1#07-AAS754#001',
 'A1B2C3D4E5F67890ABCDEF1234567890ABCDEF12','A1B2C3D4E5F67890ABCDEF1234567890ABCDEF13',
 'RSA Encryption','RSA-Verschlüsselung'),
(2,'lukas.mueller@examples/shared.de','0173-1#07-AAS756#001',
 'B2C3D4E5F67890ABCDEF1234567890ABCDEF1234','B2C3D4E5F67890ABCDEF1234567890ABCDEF1235',
 'ECC Encryption','ECC-Verschlüsselung'),
(3,'priya.mehta@examples/shared.in','0173-1#07-AAS757#001',
 'C3D4E5F67890ABCDEF1234567890ABCDEF123456','C3D4E5F67890ABCDEF1234567890ABCDEF123457',
 'DSA Signature','DSA-Signatur'),
(4,'anna.schneider@examples/shared.de','0173-1#07-AAS758#001',
 'E5F67890ABCDEF1234567890ABCDEF1234567890','E5F67890ABCDEF1234567890ABCDEF1234567891',
 'EdDSA Signature','EdDSA-Signatur'),
(5,'wei.li@examples/shared.cn','0173-1#07-AAS754#001',
 'F67890ABCDEF1234567890ABCDEF1234567890AB','F67890ABCDEF1234567890ABCDEF1234567890AB',
 'RSA Encryption','RSA-Verschlüsselung'),
(6,'johann.becker@examples/shared.de','0173-1#07-AAS756#001',
 'D4E5F67890ABCDEF1234567890ABCDEF12345678','D4E5F67890ABCDEF1234567890ABCDEF12345679',
 'ECC Encryption','ECC-Verschlüsselung');

INSERT INTO "Fax" VALUES
(DEFAULT,1,'+49 151 23456789','+49 151 23456790','0173-1#07-AAS754#001'),
(DEFAULT,2,'+91 98765 43210','+91 98765 43211','0173-1#07-AAS756#001'),
(DEFAULT,3,'+49 160 98765432','+49 160 98765433','0173-1#07-AAS756#001'),
(DEFAULT,4,'+91 91234 56789','+91 91234 56790','0173-1#07-AAS754#002'),
(DEFAULT,5,'+86 138 1234 5678','+86 138 1234 5679','0173-1#07-AAS756#002'),
(DEFAULT,6,'+49 170 12345678','+49 170 12345679','0173-1#07-AAS756#002');

INSERT INTO "Phone" (
    "ContactForMaintenanceAuthorizationId",
    "TelephoneNumber_en",
    "TelephoneNumber_de",
    "AvailableTime_en",
    "AvailableTime_de",
    "TypeOfTelephone"
)
VALUES
(
    1,
    '+49 151 23456789',
    '+49 151 23456790',
    'Monday – Friday 08:00 to 16:00',
    'Montag – Freitag 08:00 bis 16:00',
    '0173-1#07-AAS754#001'
),
(
    2,
    '+91 98765 43210',
    '+91 98765 43211',
    'Monday – Thursday 09:00 to 17:00',
    'Montag – Donnerstag 09:00 bis 17:00',
    '0173-1#07-AAS755#001'
),
(
    3,
    '+49 160 98765432',
    '+49 160 98765433',
    'Tuesday – Saturday 07:30 to 15:30',
    'Dienstag – Samstag 07:30 bis 15:30',
    '0173-1#07-AAS756#001'
),
(
    4,
    '+91 91234 56789',
    '+91 91234 56790',
    'Monday – Friday 10:00 to 18:00',
    'Montag – Freitag 10:00 bis 18:00',
    '0173-1#07-AAS757#001'
),
(
    5,
    '+86 138 1234 5678',
    '+86 138 1234 5679',
    'Wednesday – Sunday 08:00 to 14:00',
    'Mittwoch – Sonntag 08:00 bis 14:00',
    '0173-1#07-AAS758#001'
),
(
    6,
    '+49 170 12345678',
    '+49 170 12345679',
    'Monday – Friday 06:00 to 12:00',
    'Montag – Freitag 06:00 bis 12:00',
    '0173-1#07-AAS759#001'
);

INSERT INTO "MaintenanceInstructionContactForMaintenanceAuthorization" VALUES
(DEFAULT,1,1),
(DEFAULT,1,2),
(DEFAULT,2,3),
(DEFAULT,3,3),
(DEFAULT,3,4),
(DEFAULT,3,5),
(DEFAULT,4,6),
(DEFAULT,4,2),
(DEFAULT,5,1),
(DEFAULT,5,4);

INSERT INTO "MaintenanceStep" (
"Index","MaintenanceStepID",
"QuantityOfSparePartForMaintenanceStep",
"QuantityOfConsumablesForMaintenanceStep",
"UnitForQuantityOfConsumablesForMaintenanceStep",
"QuantityOfToolsForMaintenanceStep",
"DocumentationSignatureMandatory",
"EndOfMaintenance",
"MaintenanceStepName_en","MaintenanceStepName_de",
"LocalizationDescription_en","LocalizationDescription_de",
"InstructionMaintenanceStep_en","InstructionMaintenanceStep_de",
"ConditionForNextMaintenanceStep_en","ConditionForNextMaintenanceStep_de",
"ConditionForAlternativeNextStep_en","ConditionForAlternativeNextStep_de",
"RelatedDocumentOrFileMaintenanceStep",
"ValueEstimatedDurationTimeMaintenanceStep",
"UnitEstimatedDurationTimeMaintenanceStep"
)
VALUES
(0,'MS-001',0,0,NULL,1,TRUE,FALSE,
'Start maintenance','Wartung starten',
'Main plant – entry point','Hauptanlage – Eingang',
'Disconnect system power','Systemstrom trennen',
'System is powered off','System ist ausgeschaltet',
'Power still active','Strom noch aktiv',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
5,'minutes'),
(1,'MS-002',0,0,NULL,2,TRUE,FALSE,
'Apply safety lockout','Sicherheitsverriegelung anwenden',
'Control panel area','Schaltschrankbereich',
'Apply lockout/tagout procedure','Lockout/Tagout Verfahren anwenden',
'Lockout applied','Verriegelung aktiv',
'Lockout failed','Verriegelung fehlgeschlagen',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
10,'minutes'),
(0,'MS-003',1,0,NULL,2,TRUE,FALSE,
'Inspect equipment','Gerät prüfen',
'Machine section A','Maschinenbereich A',
'Check visible damage','Sichtprüfung durchführen',
'No damage found','Keine Schäden gefunden',
'Damage detected','Schaden festgestellt',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
15,'minutes'),
(0,'MS-004',2,20,'ml',3,TRUE,FALSE,
'Replace filter','Filter austauschen',
'Filter housing','Filtergehäuse',
'Remove and install filter','Filter ersetzen',
'Filter OK','Filter in Ordnung',
'Fit issue','Problem erkannt',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
20,'minutes'),
(1,'MS-005',1,50,'ml',1,TRUE,FALSE,
'Add lubricant','Schmiermittel hinzufügen',
'Lubrication unit','Schmierstation',
'Add lubricant','Schmiermittel hinzufügen',
'Lubrication OK','Schmierung OK',
'Leak found','Leck erkannt',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
10,'minutes'),
(2,'MS-006',0,0,NULL,2,TRUE,FALSE,
'Test system','System testen',
'Control panel','Steuerung',
'Run test','Test ausführen',
'Test passed','Test bestanden',
'Test failed','Test fehlgeschlagen',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
15,'minutes'),
(1,'MS-007',0,0,NULL,1,TRUE,FALSE,
'Reconnect power','Stromversorgung wiederherstellen',
'Power unit','Stromeinheit',
'Reconnect','Verbinden',
'Power OK','Strom OK',
'Failure','Fehler',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
5,'minutes'),
(1,'MS-008',0,0,NULL,1,TRUE,TRUE,
'End maintenance','Wartung beenden',
'Main system','Hauptsystem',
'Finalize','Abschließen',
'Completed','Abgeschlossen',
'-','-',
('https://raw.githubusercontent.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/refs/heads/' ||
'main/examples/shared/data/dummy_document.pdf'),
5,'minutes');

INSERT INTO "MaintenanceInstructionsForSpecificIntervalMaintenanceStep"
("MaintenanceInstructionsForSpecificIntervalId", "MaintenanceStepId")
VALUES

(1,1),
(1,2),
(2,3),
(3,4),
(3,5),
(3,6),
(4,3),
(4,7),
(5,1),
(5,8);

INSERT INTO "MaintenanceTool" (
"Index","ToolID","OrderCodeOfManufacturer","AddressOfAdditionalLink",
"ToolName_en","ToolName_de",
"CompanyNameToolSupplier_en","CompanyNameToolSupplier_de",
"ToolDescription_en","ToolDescription_de",
"MaxQuantityOfTool"
)
VALUES
(0,'TL-001','BOSCH-TRX-T20','https://tools.examples/shared.com/t20',
'Torx Screwdriver T20','Torx Schraubendreher T20',
'Bosch Tools','Bosch Werkzeuge',
'Torx screwdriver size T20, non-insulated, 100 mm length',
'Torx Schraubendreher Größe T20, nicht isoliert, 100 mm Länge',
2),

(1,'TL-002','SKF-WRENCH-SET','https://tools.examples/shared.com/wrench-set',
'Wrench Set','Schraubenschlüssel Set',
'SKF Industrial Tools','SKF Industrie Werkzeuge',
'Set of metric wrenches from 8 mm to 24 mm',
'Satz metrischer Schraubenschlüssel von 8 mm bis 24 mm',
1),
(0,'TL-003','FLUKE-MULTI-115','https://tools.examples/shared.com/multimeter',
'Digital Multimeter','Digitales Multimeter',
'Fluke Corporation','Fluke Unternehmen',
'Digital multimeter for voltage, resistance, and continuity testing',
'Digitalmultimeter für Spannung, Widerstand und Durchgangsprüfung',
1),
(0,'TL-004','FACOM-PLIERS-SET','https://tools.examples/shared.com/pliers',
'Pliers Set','Zangenset',
'Facom Tools','Facom Werkzeuge',
'Set of combination and needle-nose pliers for general maintenance',
'Satz aus Kombi- und Spitzzangen für allgemeine Wartung',
2),
(1,'TL-005','MAKITA-DRILL-18V','https://tools.examples/shared.com/drill',
'Cordless Drill','Akku-Bohrmaschine',
'Makita Corporation','Makita Unternehmen',
'18V cordless drill with adjustable torque and battery system',
'18V Akku-Bohrmaschine mit Drehmomentregelung und Batteriesystem',
1),

(2,'TL-006','STAHLWILLE-TORQUE-50','https://tools.examples/shared.com/torque-wrench',
'Torque Wrench','Drehmomentschlüssel',
'Stahlwille GmbH','Stahlwille GmbH',
'Torque wrench up to 50 Nm for precise tightening',
'Drehmomentschlüssel bis 50 Nm für präzises Anziehen',
2);

INSERT INTO "AssetMaintenanceTool"
("AssetId", "MaintenanceToolId")
VALUES

(1,1),
(1,2),
(2,3),
(3,4),
(3,5),
(3,6);

INSERT INTO "MaintenanceConsumable" (
"Index","ConsumableID",
"UnitMaxQuantityOfConsumable",
"OrderCodeOfManufacturer",
"AddressOfAdditionalLink",
"ConsumableName_en","ConsumableName_de",
"CompanyNameSupplierConsumable_en","CompanyNameSupplierConsumable_de",
"ConsumableDescription_en","ConsumableDescription_de",
"DisposalInstructionsForConsumable_en","DisposalInstructionsForConsumable_de",
"QuantityOfConsumable"
)
VALUES
(0,'CS-001','ml','CASTROL-OIL-80W','https://consumables.examples/shared.com/oil',
'Lubricating Oil','Schmieröl',
'Castrol Ltd','Castrol GmbH',
'High-performance mineral gear oil, SAE 80W, reduces wear and ensures smooth operation',
'Hochleistungs-Getriebeöl, SAE 80W, reduziert Verschleiß und sorgt für reibungslosen Betrieb',
'Dispose via certified waste company. Avoid release into environment.',
'Entsorgung nur durch Fachbetrieb. Freisetzung in Umwelt vermeiden.',
50),
(1,'CS-002','pcs','KIMBERLY-CLOTH-BLUE','https://consumables.examples/shared.com/cloth',
'Cleaning Cloth','Reinigungstuch',
'Kimberly-Clark','Kimberly-Clark GmbH',
'Blue industrial cleaning cloth, 380x380 mm, highly absorbent',
'Blaues Industriereinigungstuch, 380x380 mm, sehr saugfähig',
'Dispose as industrial waste. Do not reuse if contaminated.',
'Als Industrieabfall entsorgen. Nicht wiederverwenden bei Verschmutzung.',
20),
(2,'CS-003','ml','LOCTITE-THREADLOCK-243','https://consumables.examples/shared.com/loctite',
'Threadlocker Fluid','Schraubensicherungsmittel',
'Henkel AG','Henkel AG',
'Medium-strength threadlocker for securing bolts',
'Mittelfeste Schraubensicherung für Schrauben',
'Dispose as hazardous chemical waste. Avoid skin contact.',
'Als Sondermüll entsorgen. Hautkontakt vermeiden.',
10),
(0,'CS-004','g','MOLYKOTE-GREASE-HP','https://consumables.examples/shared.com/grease',
'Industrial Grease','Industrieschmierfett',
'Dow Chemical','Dow Chemical GmbH',
'High-performance grease for bearings',
'Hochleistungsfett für Lager',
'Dispose per hazardous waste regulations.',
'Entsorgung gemäß Vorschriften für gefährliche Abfälle.',
30),
(0,'CS-005','ml','DISTILLED-WATER-5L','https://consumables.examples/shared.com/water',
'Distilled Water','Destilliertes Wasser',
'AquaPure Supplies','AquaPure Lieferanten',
'Purified water for cooling and cleaning',
'Gereinigtes Wasser für Kühlung und Reinigung',
'Dispose via drainage if uncontaminated.',
'Kann über Abwasser entsorgt werden, wenn nicht kontaminiert.',
100);

INSERT INTO "AssetMaintenanceConsumable"
("AssetId", "MaintenanceConsumableId")
VALUES

(1,1),
(1,2),
(2,3),
(3,4),
(3,5);


INSERT INTO "MaintenanceSparePart" (
"Index","SparePartID",
"OrderCodeOfManufacturer",
"AddressOfAdditionalLink",
"SparePartName_en","SparePartName_de",
"CompanyNameSupplierSparePart_en","CompanyNameSupplierSparePart_de",
"SparePartDescription_en","SparePartDescription_de",
"DisposalInstructionsForSparePart_en","DisposalInstructionsForSparePart_de",
"QuantityOfSparePart"
)
VALUES
(0,'SP-001','BOSCH-BEARING-6205','https://spareparts.examples/shared.com/bearing',
'Ball Bearing 6205','Kugellager 6205',
'Bosch Industrial','Bosch Industrie',
'Deep groove ball bearing, size 6205, high durability',
'Rillenkugellager Größe 6205, hohe Haltbarkeit',
'Dispose via metal recycling.',
'Über Metallrecycling entsorgen.',
2),
(0,'SP-002','SKF-SEAL-RING-45','https://spareparts.examples/shared.com/seal',
'Seal Ring 45 mm','Dichtungsring 45 mm',
'SKF Group','SKF Gruppe',
'Rubber sealing ring 45 mm diameter',
'Gummidichtungsring 45 mm Durchmesser',
'Dispose as rubber waste.',
'Als Gummiabfall entsorgen.',
1),
(0,'SP-003','SIEMENS-SENSOR-TEMP','https://spareparts.examples/shared.com/sensor',
'Temperature Sensor','Temperatursensor',
'Siemens AG','Siemens AG',
'Industrial temperature sensor',
'Industrieller Temperatursensor',
'Dispose as electronic waste.',
'Als Elektroschrott entsorgen.',
1),
(1,'SP-004','PANASONIC-BATTERY-CR2032','https://spareparts.examples/shared.com/battery',
'Button Cell Battery','Knopfzelle Batterie',
'Panasonic Corporation','Panasonic Unternehmen',
'Lithium button cell CR2032, 3V',
'Lithium-Knopfzelle CR2032, 3V',
'Dispose at battery collection point.',
'Bei Sammelstellen entsorgen.',
1);

INSERT INTO "AssetMaintenanceSparePart"
("AssetId", "MaintenanceSparePartId")
VALUES

(1,1),
(1,2),
(2,3),
(3,4);
