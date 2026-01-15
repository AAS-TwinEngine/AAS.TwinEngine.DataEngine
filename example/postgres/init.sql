
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

CREATE TABLE "ContactInformation" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "RoleOfContactPerson" TEXT,
    "Language" TEXT,
    "TimeZone" TEXT,
    "AddressOfAdditionalLink" TEXT,
    "NationalCode_en" TEXT,
    "NationalCode_de" TEXT,
    "CityTown_en" TEXT,
    "CityTown_de" TEXT,
    "Company_en" TEXT,
    "Company_de" TEXT,
    "Department_en" TEXT,
    "Department_de" TEXT,
    "Street_en" TEXT,
    "Street_de" TEXT,
    "Zipcode_en" TEXT,
    "Zipcode_de" TEXT,
    "POBox_en" TEXT,
    "POBox_de" TEXT,
    "ZipCodeOfPOBox_en" TEXT,
    "ZipCodeOfPOBox_de" TEXT,
    "StateCounty_en" TEXT,
    "StateCounty_de" TEXT,
    "NameOfContact_en" TEXT,
    "NameOfContact_de" TEXT,
    "FirstName_en" TEXT,
    "FirstName_de" TEXT,
    "MiddleNames_en" TEXT,
    "MiddleNames_de" TEXT,
    "Title_en" TEXT,
    "Title_de" TEXT,
    "AcademicTitle_en" TEXT,
    "AcademicTitle_de" TEXT,
    "FurtherDetailsOfContact_en" TEXT,
    "FurtherDetailsOfContact_de" TEXT
);

CREATE TABLE "AssetContactInformation" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT NOT NULL REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "ContactInformationId" INT NOT NULL REFERENCES "ContactInformation"("Id") ON DELETE CASCADE
);

CREATE TABLE "Phone" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ContactInformationId" INT REFERENCES "ContactInformation"("Id") ON DELETE CASCADE,
    "TelephoneNumber_en" TEXT,
    "TelephoneNumber_de" TEXT,
    "AvailableTime_en" TEXT,
    "AvailableTime_de" TEXT,
    "TypeOfTelephone" TEXT
);

CREATE TABLE "Fax" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ContactInformationId" INT REFERENCES "ContactInformation"("Id") ON DELETE CASCADE,
    "FaxNumber_en" TEXT,
    "FaxNumber_de" TEXT,
    "TypeOfFaxNumber" TEXT
);

CREATE TABLE "Email" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ContactInformationId" INT REFERENCES "ContactInformation"("Id") ON DELETE CASCADE,
    "EmailAddress" TEXT,
    "TypeOfEmailAddress" TEXT,
    "PublicKey_en" TEXT,
    "PublicKey_de" TEXT,
    "TypeOfPublicKey_en" TEXT,
    "TypeOfPublicKey_de" TEXT
);

CREATE TABLE "IPCommunication" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "AddressOfAdditionalLink" TEXT,
    "TypeOfCommunication" TEXT,
    "AvailableTime_en" TEXT,
    "AvailableTime_de" TEXT
);

CREATE TABLE "ContactInformationIPCommunication" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ContactInformationId" INT REFERENCES "ContactInformation"("Id") ON DELETE CASCADE,
    "IPCommunicationId" INT REFERENCES "IPCommunication"("Id") ON DELETE CASCADE
);

CREATE TABLE "ProductClassificationItem" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "ProductClassificationSystem" TEXT,
    "ClassificationSystemVersion" TEXT,
    "ProductClassId" TEXT
);

CREATE TABLE "AssetProductClassificationItem" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "ProductClassificationItemId" INT REFERENCES "ProductClassificationItem"("Id") ON DELETE CASCADE
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
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/Camera.jpg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/Camera.jpg',
 'FM-ABC-1234','FMABC1234','FM11-ABC22-123456','9804820',2022,'2022-01-01',
 '1.0.0','1.0.0','1.0.0','DE','987654321','M&M Germany',
 'FM-ABC-1234','ABC-123','Camera','Kamera','Electronics','Elektronik',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 '123456','EEA-EX-200-S/47-Q3',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/Camera.jpg',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 'Restricted use','Eingeschränkte Nutzung','2035-05-05',
 'ISO 14067','C4 - landfill',17.2,'ml',5,
 '2025-12-24T14:30:00Z','2035-12-24T14:30:00Z',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf'),
('000-002','Product2','https://mm-software.com/ids/assets/000-002','https://mm-software.com/ids/aas/000-002','image/jpeg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/camera1.jpg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/camera1.jpg',
 'FM-ABC-1235','FMABC1238','FM11-ABC22-123458','9804821',2023,'2023-01-01',
 '1.0.1','1.0.1','1.0.1','IN','123567890','M&M Software Development Center India',
 'FM-ABC-123','ABC-123','Camera','Kamera','Electronics','Elektronik',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 '123456','EEA-EX-200-S/47-Q4',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/camera1.jpg',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 'Restricted use','Eingeschränkte Nutzung','2035-06-05',
 'EN 15804','A5 - Installation',6.2,'cbm',2.2999999999999998,
 '2026-01-15T09:15:00+05:30','2036-01-15T09:15:00+05:31',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf'),
('001-001','Product3','https://mm-software.com/ids/assets/001-001','https://mm-software.com/ids/aas/001-001','image/jpeg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/perfume.jpg',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/perfume.jpg',
 'TM-ABC-1680','TMABC1680','TM11-ABC22-123456','9804760',2024,'2024-01-01',
 '2.0.0','1.0.0','1.0.2','CN','874512451','M&M China',
 'TM-ABC-1234','ABC-1234','perfume','Parfüm','Cosmetics','Kosmetika',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 '123456','EEA-EX-200-S/47-Q5',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/perfume.jpg',
 'https://mmsoftwaregmbh.sharepoint.com/_api/siteiconmanager/getsitelogo?type=%271%27&hash=638518734598723853',
 'Restricted use','Eingeschränkte Nutzung','2035-07-05',
 'PACT v2.0.0','C3 - recycling, waste treatment',2.2999999999999998,'piece',7.8,
 '2024-07-01T18:45:00-04:00','2034-07-01T18:45:00-04:01',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf');
 
INSERT INTO "SpecificAssetIds" ("AssetId","Name","Value") VALUES
(1,'Camera','M&M - 001'),
(1,'Camera','M&M - 002'),
(2,'Camera','M&M - 003');

INSERT INTO "Marking" ("Index","MarkingName","DesignationOfCertificateOrApproval","IssueDate","ExpiryDate","MarkingAdditionalText","MarkingFile") VALUES
(0,'0173-1#07-DAA603#004','KEMA99IECEX1105/128','2022-01-01','2030-01-01','additional information on the marking - 00',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/checkmark.png'),
(0,'0173-1#07-DAA603#005','KEMA99IECEX1105/129','2022-02-01','2030-02-01','additional information on the marking - 01',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/checkmark.png'),
(1,'0173-1#07-DAA603#006','KEMA99IECEX1105/130','2022-03-01','2030-03-01','additional information on the marking - 02',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/checkmark.png'),
(0,'0173-1#07-DAA603#007','KEMA99IECEX1105/131','2022-04-01','2030-04-01','additional information on the marking - 03',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/checkmark.png'),
(1,'0173-1#07-DAA603#008','KEMA99IECEX1105/132','2022-05-01','2030-05-01','additional information on the marking - 04',
 'https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/checkmark.png');

INSERT INTO "AssetMarking" ("AssetId","MarkingId") VALUES
(1,1),
(1,3),
(2,2),
(2,3),
(3,4),
(3,5);

INSERT INTO "ContactInformation" (
  "Index","RoleOfContactPerson","Language","TimeZone","AddressOfAdditionalLink",
  "NationalCode_en","NationalCode_de","CityTown_en","CityTown_de","Company_en","Company_de",
  "Department_en","Department_de","Street_en","Street_de","Zipcode_en","Zipcode_de","POBox_en","POBox_de",
  "ZipCodeOfPOBox_en","ZipCodeOfPOBox_de","StateCounty_en","StateCounty_de","NameOfContact_en","NameOfContact_de",
  "FirstName_en","FirstName_de","MiddleNames_en","MiddleNames_de","Title_en","Title_de","AcademicTitle_en","AcademicTitle_de",
  "FurtherDetailsOfContact_en","FurtherDetailsOfContact_de"
) VALUES
(0,'0173-1#07-AAS927#001','EN','Z+05:30','https://www.mm-software.com/','IN','IN','Mumbai','München',
 'M&M Software Development Center India','M&M Software Development Center India','Human Resources','Human Resources',
 '221B Baker Street','221B Baker Street','110001','110001','PO Box 123','PO Box 124','110002','110002',
 'Delhi','Delhi','Aarav Sharma','Aarav Sharma','Aarav','Aarav','Sharma','Sharma','Mr.','Herr','Dr.','Dr.',
 'Responsible for B2B sales in region-1','Responsible for B2B sales in region-1'),
(1,'0173-1#07-AAS928#001','DE','Z+01:00','https://www.mm-software.com/we/','DE','DE','Berlin','Berlin',
 'M&M Germany','M&M Germany','Finance','Finance','Hauptstraße 15','Hauptstraße 16','10115','10115','Postfach 789','Postfach 790',
 '10116','10116','Berlin','Berlin','Lukas Mueller','Lukas Müller','Lukas','Lukas','Müller','Mueller','Mr.','Herr','Dr.','Dr.',
 'Responsible for B2B sales in region-2','Responsible for B2B sales in region-2'),
(0,'0173-1#07-AAS931#001','EN','Z+05:30','https://www.mm-software.com/more-the-newsroom/detail/mm-software-auf-der-sps-2025/','IN','IN','Mumbai','München',
 'M&M Software Development Center India','M&M Software Development Center India','Operations','Operations',
 '221B Baker Street','221B Baker Street','110001','110001','PO Box 123','PO Box 124','110002','110002','Delhi','Delhi',
 'Priya Mehta','Priya Mehta','Priya','Priya','Mehta','Mehta','Ms','Frau','Prof.','Prof.','Responsible for B2B sales in region-3','Responsible for B2B sales in region-3'),
(1,'0173-1#07-AAS930#001','DE','Z+01:00','https://www.mm-software.com/more-the-newsroom/detail/entscheide-dich-teil-2/','DE','DE','Berlin','Berlin',
 'M&M Germany','M&M Germany','Human Resources','Human Resources','Hauptstraße 15','Hauptstraße 16','10115','10115','Postfach 789','Postfach 790',
 '10116','10116','Berlin','Berlin','Anna Schneider','Anna Schneider','Anna','Anna','Schneider','Schneider','Ms','Frau','Prof.','Prof.','Responsible for B2B sales in region-4','Responsible for B2B sales in region-4'),
(2,'0173-1#07-AAS929#001','FR','Z+08:00','https://www.mm-software.com/more-the-newsroom/detail/digitaler-produktpass-dpp-in-der-praxis-leitfaden-und-checkliste-fuer-unternehmen/','CN','CN','Beijing','Beijing',
 'M&M China','M&M China','Finance','Finance','88 Nanjing Road','89 Nanjing Road','200001','200001','P.O. Box 456','P.O. Box 457',
 '200002','200002','hanghai Municipality','hanghai Municipality','Anna Müller','Wei Li','Li','Li','Wei','Wei','Mr.','Herr','Dr.','Dr.',
 'Responsible for B2B sales in region-5','Responsible for B2B sales in region-5'),
(3,'0173-1#07-AAS928#001','DE','Z+01:00','https://www.mm-software.com/more-the-newsroom/','DE','DE','Berlin','Berlin',
 'M&M Germany','M&M Germany','Operations','Operations','Hauptstraße 15','Hauptstraße 16','10115','10115','Postfach 789','Postfach 790',
 '10116','10116','Berlin','Berlin','Johann Becker','Johann Becker','Johann','Johann','Becker','Becker','Ms','Frau','Prof.','Prof.',
 'Responsible for B2B sales in region-6','Responsible for B2B sales in region-6');

INSERT INTO "Phone" ("ContactInformationId","TelephoneNumber_en","TelephoneNumber_de","AvailableTime_en","AvailableTime_de","TypeOfTelephone") VALUES
(1,'+49 151 23456789','+49 151 23456790','Monday – Friday 08:00 to 16:00','Montag – Freitag 08:00 bis 16:00','0173-1#07-AAS754#001'),
(2,'+91 98765 43210','+91 98765 43211','Monday – Thursday 09:00 to 17:00','Montag – Donnerstag 09:00 bis 17:00','0173-1#07-AAS755#001'),
(3,'+49 160 98765432','+49 160 98765433','Tuesday – Saturday 07:30 to 15:30','Dienstag – Samstag 07:30 bis 15:30','0173-1#07-AAS756#001'),
(4,'+91 91234 56789','+91 91234 56790','Monday – Friday 10:00 to 18:00','Montag – Freitag 10:00 bis 18:00','0173-1#07-AAS757#001'),
(5,'+86 138 1234 5678','+86 138 1234 5679','Wednesday – Sunday 08:00 to 14:00','Mittwoch – Sonntag 08:00 bis 14:00','0173-1#07-AAS758#001'),
(6,'+49 170 12345678','+49 170 12345679','Monday – Friday 06:00 to 12:00','Montag – Freitag 06:00 bis 12:00','0173-1#07-AAS759#001');

INSERT INTO "Fax" ("ContactInformationId","FaxNumber_en","FaxNumber_de","TypeOfFaxNumber") VALUES
(1,'+49 151 23456789','+49 151 23456790','0173-1#07-AAS754#001'),
(2,'+91 98765 43210','+91 98765 43211','0173-1#07 AAS756#001'),
(3,'+49 160 98765432','+49 160 98765433','0173-1#07-AAS756#001'),
(4,'+91 91234 56789','+91 91234 56790','0173-1#07-AAS754#002'),
(5,'+86 138 1234 5678','+86 138 1234 5679','0173-1#07 AAS756#002'),
(6,'+49 170 12345678','+49 170 12345679','0173-1#07-AAS756#002');

INSERT INTO "Email" ("ContactInformationId","EmailAddress","TypeOfEmailAddress","PublicKey_en","PublicKey_de","TypeOfPublicKey_en","TypeOfPublicKey_de") VALUES
(1,'aarav.sharma@example.in','0173-1#07-AAS754#001','A1B2C3D4E5F67890ABCDEF1234567890ABCDEF12','A1B2C3D4E5F67890ABCDEF1234567890ABCDEF13','RSA Encryption','RSA-Verschlüsselung'),
(2,'lukas.mueller@example.de','0173-1#07-AAS756#001','B2C3D4E5F67890ABCDEF1234567890ABCDEF1234','B2C3D4E5F67890ABCDEF1234567890ABCDEF1235','ECC Encryption','ECC-Verschlüsselung'),
(3,'priya.mehta@example.in','0173-1#07-AAS757#001','C3D4E5F67890ABCDEF1234567890ABCDEF123456','C3D4E5F67890ABCDEF1234567890ABCDEF123457','DSA Signature','DSA-Signatur'),
(4,'anna.schneider@example.de','0173-1#07-AAS758#001','E5F67890ABCDEF1234567890ABCDEF1234567890','E5F67890ABCDEF1234567890ABCDEF1234567891','EdDSA Signature','EdDSA-Signatur'),
(5,'wei.li@example.cn','0173-1#07-AAS754#001','F67890ABCDEF1234567890ABCDEF1234567890AB','F67890ABCDEF1234567890ABCDEF1234567890AB','RSA Encryption','RSA-Verschlüsselung'),
(6,'johann.becker@example.de','0173-1#07-AAS756#001','D4E5F67890ABCDEF1234567890ABCDEF12345678','D4E5F67890ABCDEF1234567890ABCDEF12345679','ECC Encryption','ECC-Verschlüsselung');

INSERT INTO "IPCommunication" ("Index","AddressOfAdditionalLink","TypeOfCommunication","AvailableTime_en","AvailableTime_de") VALUES
(0,'https://www.mm-software.com/more-the-newsroom/','Chat','Monday – Friday 08:00 to 16:00','Montag – Freitag 08:00 bis 16:00'),
(1,'https://www.mm-software.com/more-the-newsroom/detail/digitaler-produktpass-dpp-in-der-praxis-leitfaden-und-checkliste-fuer-unternehmen/','Video call','Monday – Thursday 09:00 to 17:00','Montag – Donnerstag 09:00 bis 17:00'),
(0,'https://www.mm-software.com/more-the-newsroom/','Chat','Tuesday – Saturday 07:30 to 15:30','Dienstag – Samstag 07:30 bis 15:30'),
(0,'https://www.mm-software.com/we/','Video call','Monday – Friday 10:00 to 18:00','Montag – Freitag 10:00 bis 18:00'),
(0,'https://www.mm-software.com/we/code-of-conduct/','Chat','Wednesday – Sunday 08:00 to 14:00','Mittwoch – Sonntag 08:00 bis 14:00'),
(1,'https://www.mm-software.com/more-the-newsroom/anmeldung/','Video call','Monday – Friday 06:00 to 12:00','Montag – Freitag 06:00 bis 12:00'),
(2,'https://www.mm-software.com/','Chat','Monday – Friday 08:00 to 16:00','Montag – Freitag 08:00 bis 16:00');

INSERT INTO "ContactInformationIPCommunication" ("ContactInformationId","IPCommunicationId") VALUES
(1,1),
(1,2),
(2,3),
(3,4),
(4,5),
(5,4),
(6,5),
(6,6),
(6,7);


INSERT INTO "AssetContactInformation" (
    "AssetId",
    "ContactInformationId"
)
VALUES
    (1, 1),
    (1, 2),
    (2, 3),
    (3, 4),
    (3, 5),
    (3, 6);

INSERT INTO "ProductClassificationItem" ("Index","ProductClassificationSystem","ClassificationSystemVersion","ProductClassId") VALUES
(0,'ECLASS','14','19-01-01-01'),
(1,'IEC CDD','2024-09','IEC-CDD-AAA124'),
(2,'UNSPSC','23.0301','UNSPSC-43211503'),
(0,'ISO 13584','2023','ISO13584-XYZ789'),
(0,'ECLASS','5','27-02-03-05'),
(1,'IEC CDD','2024-09','IEC-CDD-AAA123');

INSERT INTO "AssetProductClassificationItem" ("AssetId","ProductClassificationItemId") VALUES
(1,1),
(1,2),
(1,3),
(2,4),
(3,5),
(3,6);

INSERT INTO "ProductOrSectorSpecificCarbonFootprint" (
  "AssetId","PcfCalculationMethod","PcfRuleOperator","PcfRuleName","PcfRuleVersion","PcfRuleOnlineReference","PcfApiEndpoint","PcfApiQuery"
) VALUES
(1,'IEC TS 63058','GHG Protocol','GHG Protocol Product Standard','1.1','https://ghgprotocol.org/standards/product-standard','https://api.carbonfootprint.org/v1/calculate','?productId=12345&unit=kgCO2e&scope=cradle-to-gate'),
(2,'EN 15804','ISO 14067','ISO 14067','2.1','https://www.iso.org/standard/43278.html','https://api.iso14067.org/v2/emissions','?sector=electronics&region=EU&year=2025'),
(3,'PACT v2.0.0','PAS 2050','PAS 2050','0.9','https://www.bsigroup.com/en-GB/PAS-2050-Carbon-Footprint/','https://api.pas2050.com/v1/footprint','?material=steel&quantity=1000kg&method=ISO14067');

INSERT INTO "Document" ("Index") VALUES
(0),(1),(2),(0),(1),(0),(1);

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

INSERT INTO "AssetDocument" ("AssetId","DocumentId") VALUES
(1,1),
(1,2),
(1,3),
(2,4),
(2,5),
(3,6),
(3,7);

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


INSERT INTO "DocumentVersion" (
  "Index","en","DigitalFile","Version","StatusSetDate","StatusValue","OrganizationShortName",
  "OrganizationOfficialName","Title_en","Title_de","Subtitle_en","Subtitle_de","Description_en","Description_de",
  "KeyWords_en","KeyWords_de","PreviewFile"
) VALUES
(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf','1','2023-01-01','Released','M&M','M&M Germany',
 'User Guide – DSLR Camera Model X100','Benutzerhandbuch – DSLR-Kamera Modell X100','Complete Instructions for Professional Photography','Vollständige Anleitung für professionelle Fotografie',
 'Detailed instructions for operating the X100 DSLR camera, including setup and troubleshooting.','Detaillierte Anweisungen zur Bedienung der DSLR-Kamera X100, einschließlich Einrichtung und Fehlerbehebung',
 'DSLR, Camera, Photography, User Guide, Setup','DSLR, Kamera, Fotografie, Benutzerhandbuch, Einrichtung','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(1,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf','1.1','2024-05-05','InReview','M&M','M&M Software Development Center India',
 'Technical Specification – Mirrorless Camera Z-Series','Technische Spezifikation – Systemkamera der Z-Serie','Detailed Specs for Advanced Imaging','Detaillierte Spezifikationen für fortschrittliche Bildgebung',
 'Comprehensive technical details of the Z-Series mirrorless camera, covering sensor and performance.','Umfassende technische Details der spiegellosen Kamera Z-Serie, einschließlich Sensor und Leistung.','Mirrorless, Camera, Specs, Imaging, Performance','Spiegellos, Kamera, Spezifikationen, Bildgebung, Leistung','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf','2.1','2026-01-01','Released','M&M','M&M China',
 'Maintenance Manual – Professional Camera Lens 50mm','Wartungshandbuch – Professionelles Kameraobjektiv 50 mm','Care and Cleaning Procedures','Pflege und Reinigungsverfahren',
 'Guidelines for cleaning and maintaining the 50mm professional lens for optimal performance.','Richtlinien zur Reinigung und Wartung des professionellen 50-mm-Objektivs für optimale Leistung.','Lens, Maintenance, Cleaning, Professional, Care','Objektiv, Wartung, Reinigung, Professionell, Pflege','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf','2.3','2025-10-10','InReview','M&M','M&M Germany',
 'Installation Guide – Wide-Angle Lens Kit','Installationsanleitung – Weitwinkel-Objektiv-Kit','Step-by-Step Setup Instructions','Schritt-für-Schritt-Installationsanleitung',
 'Step-by-step instructions for installing and configuring the wide-angle lens kit.','Schritt-für-Schritt-Anleitung zur Installation und Konfiguration des Weitwinkel-Objektivsets.','Wide-Angle, Lens, Installation, Setup, Kit','Weitwinkel, Objektiv, Installation, Einrichtung, Set','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf','0.9','2024-01-01','Released','M&M','M&M Software Development Center India',
 'Product Data Sheet – Telephoto Lens 200mm','Produktdatenblatt – Teleobjektiv 200 mm','Technical Data and Performance Metrics','Technische Daten und Leistungskennzahlen',
 'Technical data and compatibility details for the 200mm telephoto lens.','Technische Daten und Kompatibilitätsdetails für das 200-mm-Teleobjektiv.','Telephoto, Lens, Data Sheet, Specifications, Optics','Teleobjektiv, Objektiv, Datenblatt, Spezifikationen, Optik','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf','1.2','2024-03-03','InReview','M&M','M&M China',
 'Safety Instructions – Digital Camera Accessories','Sicherheitsanweisungen – Zubehör für Digitalkameras','Guidelines for Safe Usage','Richtlinien für sichere Verwendung',
 'Safety guidelines for handling batteries, chargers, and other camera accessories.','Sicherheitsrichtlinien für den Umgang mit Batterien, Ladegeräten und anderem Kamera-Zubehör.','Safety, Camera, Accessories, Guidelines, Handling','Sicherheit, Kamera, Zubehör, Richtlinien, Handhabung','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(1,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf','1.4','2023-01-01','Released','M&M','M&M Germany',
 'Perfume Catalog – Luxury Fragrance Collection 2025','Parfümkatalog – Luxusduftkollektion 2025','Explore Elegant Scents for Every Occasion','Entdecken Sie elegante Düfte für jeden Anlass',
 'A curated catalog showcasing premium perfumes with scent profiles and packaging details.','Ein kuratierter Katalog mit Premium-Parfums, Duftprofilen und Verpackungsdetails.','Perfume, Fragrance, Luxury, Catalog, Collection','Parfum, Duft, Luxus, Katalog, Kollektion','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(2,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf','2','2024-03-03','InReview','M&M','M&M Software Development Center India',
 'Quality Assurance Report – Eau de Parfum Series A','Qualitätssicherungsbericht – Eau de Parfum Serie A','Verified Standards and Testing Results','Geprüfte Standards und Testergebnisse',
 'Report detailing quality checks and compliance standards for Series A perfumes.','Bericht mit Qualitätsprüfungen und Konformitätsstandards für Parfums der Serie A.','Packaging, Perfume, Bottles, Caps, Compliance','Verpackung, Parfum, Flaschen, Verschlüsse, Konformität','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)'),

(0,'en','https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/data/dummy_document.pdf','1','2022-02-02','Released','M&M','M&M Germany',
 'Packaging Standards – Perfume Bottles and Caps','Verpackungsstandards – Parfümflaschen und Verschlüsse','Design and Material Compliance Guidelines','Richtlinien für Design und Materialkonformität',
 'Design and material compliance guidelines for perfume packaging.','Richtlinien für Design und Materialkonformität bei Parfumverpackungen.','Perfume, Fragrance, Luxury, Catalog, Collection','Parfum, Duft, Luxus, Katalog, Kollektion','preview-ribbon-isolated-transparent-background-395176472.jpg (800×291)');

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