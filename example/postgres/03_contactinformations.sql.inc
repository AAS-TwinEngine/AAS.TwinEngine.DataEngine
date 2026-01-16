-- ============================================================
-- Contact Information Data Insertion
-- ============================================================
-- This file contains CREATE And INSERT statements for contact-related entities:
-- - ContactInformation
-- - Phone numbers
-- - Fax numbers
-- - Email addresses
-- - IP Communication channels
-- - Asset-ContactInformation relationships
-- ============================================================
-- ============================================================
-- Contact Information Tables
-- ============================================================

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


-- ============================================================
-- Contact Information
-- ============================================================

INSERT INTO "ContactInformation" (
  "Index","RoleOfContactPerson","Language","TimeZone","AddressOfAdditionalLink",
  "NationalCode_en","NationalCode_de","CityTown_en","CityTown_de","Company_en","Company_de",
  "Department_en","Department_de","Street_en","Street_de","Zipcode_en","Zipcode_de","POBox_en","POBox_de",
  "ZipCodeOfPOBox_en","ZipCodeOfPOBox_de","StateCounty_en","StateCounty_de","NameOfContact_en","NameOfContact_de",
  "FirstName_en","FirstName_de","MiddleNames_en","MiddleNames_de","Title_en","Title_de","AcademicTitle_en","AcademicTitle_de",
  "FurtherDetailsOfContact_en","FurtherDetailsOfContact_de"
) VALUES
(0,'0173-1#07-AAS927#001','EN','Z+05:30','https://www.mm-software.com/','IN','IN','Mumbai','München',
 'M&M Software mainment Center India','M&M Software mainment Center India','Human Resources','Human Resources',
 '221B Baker Street','221B Baker Street','110001','110001','PO Box 123','PO Box 124','110002','110002',
 'Delhi','Delhi','Aarav Sharma','Aarav Sharma','Aarav','Aarav','Sharma','Sharma','Mr.','Herr','Dr.','Dr.',
 'Responsible for B2B sales in region-1','Responsible for B2B sales in region-1'),
(1,'0173-1#07-AAS928#001','DE','Z+01:00','https://www.mm-software.com/we/','DE','DE','Berlin','Berlin',
 'M&M Germany','M&M Germany','Finance','Finance','Hauptstraße 15','Hauptstraße 16','10115','10115','Postfach 789','Postfach 790',
 '10116','10116','Berlin','Berlin','Lukas Mueller','Lukas Müller','Lukas','Lukas','Müller','Mueller','Mr.','Herr','Dr.','Dr.',
 'Responsible for B2B sales in region-2','Responsible for B2B sales in region-2'),
(0,'0173-1#07-AAS931#001','EN','Z+05:30','https://www.mm-software.com/more-the-newsroom/detail/mm-software-auf-der-sps-2025/','IN','IN','Mumbai','München',
 'M&M Software mainment Center India','M&M Software mainment Center India','Operations','Operations',
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

-- ============================================================
-- Phone Numbers
-- ============================================================

INSERT INTO "Phone" ("ContactInformationId","TelephoneNumber_en","TelephoneNumber_de","AvailableTime_en","AvailableTime_de","TypeOfTelephone") VALUES
(1,'+49 151 23456789','+49 151 23456790','Monday – Friday 08:00 to 16:00','Montag – Freitag 08:00 bis 16:00','0173-1#07-AAS754#001'),
(2,'+91 98765 43210','+91 98765 43211','Monday – Thursday 09:00 to 17:00','Montag – Donnerstag 09:00 bis 17:00','0173-1#07-AAS755#001'),
(3,'+49 160 98765432','+49 160 98765433','Tuesday – Saturday 07:30 to 15:30','Dienstag – Samstag 07:30 bis 15:30','0173-1#07-AAS756#001'),
(4,'+91 91234 56789','+91 91234 56790','Monday – Friday 10:00 to 18:00','Montag – Freitag 10:00 bis 18:00','0173-1#07-AAS757#001'),
(5,'+86 138 1234 5678','+86 138 1234 5679','Wednesday – Sunday 08:00 to 14:00','Mittwoch – Sonntag 08:00 bis 14:00','0173-1#07-AAS758#001'),
(6,'+49 170 12345678','+49 170 12345679','Monday – Friday 06:00 to 12:00','Montag – Freitag 06:00 bis 12:00','0173-1#07-AAS759#001');

-- ============================================================
-- Fax Numbers
-- ============================================================

INSERT INTO "Fax" ("ContactInformationId","FaxNumber_en","FaxNumber_de","TypeOfFaxNumber") VALUES
(1,'+49 151 23456789','+49 151 23456790','0173-1#07-AAS754#001'),
(2,'+91 98765 43210','+91 98765 43211','0173-1#07 AAS756#001'),
(3,'+49 160 98765432','+49 160 98765433','0173-1#07-AAS756#001'),
(4,'+91 91234 56789','+91 91234 56790','0173-1#07-AAS754#002'),
(5,'+86 138 1234 5678','+86 138 1234 5679','0173-1#07 AAS756#002'),
(6,'+49 170 12345678','+49 170 12345679','0173-1#07-AAS756#002');

-- ============================================================
-- Email Addresses
-- ============================================================

INSERT INTO "Email" ("ContactInformationId","EmailAddress","TypeOfEmailAddress","PublicKey_en","PublicKey_de","TypeOfPublicKey_en","TypeOfPublicKey_de") VALUES
(1,'aarav.sharma@example.in','0173-1#07-AAS754#001','A1B2C3D4E5F67890ABCDEF1234567890ABCDEF12','A1B2C3D4E5F67890ABCDEF1234567890ABCDEF13','RSA Encryption','RSA-Verschlüsselung'),
(2,'lukas.mueller@example.de','0173-1#07-AAS756#001','B2C3D4E5F67890ABCDEF1234567890ABCDEF1234','B2C3D4E5F67890ABCDEF1234567890ABCDEF1235','ECC Encryption','ECC-Verschlüsselung'),
(3,'priya.mehta@example.in','0173-1#07-AAS757#001','C3D4E5F67890ABCDEF1234567890ABCDEF123456','C3D4E5F67890ABCDEF1234567890ABCDEF123457','DSA Signature','DSA-Signatur'),
(4,'anna.schneider@example.de','0173-1#07-AAS758#001','E5F67890ABCDEF1234567890ABCDEF1234567890','E5F67890ABCDEF1234567890ABCDEF1234567891','EdDSA Signature','EdDSA-Signatur'),
(5,'wei.li@example.cn','0173-1#07-AAS754#001','F67890ABCDEF1234567890ABCDEF1234567890AB','F67890ABCDEF1234567890ABCDEF1234567890AB','RSA Encryption','RSA-Verschlüsselung'),
(6,'johann.becker@example.de','0173-1#07-AAS756#001','D4E5F67890ABCDEF1234567890ABCDEF12345678','D4E5F67890ABCDEF1234567890ABCDEF12345679','ECC Encryption','ECC-Verschlüsselung');

-- ============================================================
-- IP Communication Channels
-- ============================================================

INSERT INTO "IPCommunication" ("Index","AddressOfAdditionalLink","TypeOfCommunication","AvailableTime_en","AvailableTime_de") VALUES
(0,'https://www.mm-software.com/more-the-newsroom/','Chat','Monday – Friday 08:00 to 16:00','Montag – Freitag 08:00 bis 16:00'),
(1,'https://www.mm-software.com/more-the-newsroom/detail/digitaler-produktpass-dpp-in-der-praxis-leitfaden-und-checkliste-fuer-unternehmen/','Video call','Monday – Thursday 09:00 to 17:00','Montag – Donnerstag 09:00 bis 17:00'),
(0,'https://www.mm-software.com/more-the-newsroom/','Chat','Tuesday – Saturday 07:30 to 15:30','Dienstag – Samstag 07:30 bis 15:30'),
(0,'https://www.mm-software.com/we/','Video call','Monday – Friday 10:00 to 18:00','Montag – Freitag 10:00 bis 18:00'),
(0,'https://www.mm-software.com/we/code-of-conduct/','Chat','Wednesday – Sunday 08:00 to 14:00','Mittwoch – Sonntag 08:00 bis 14:00'),
(1,'https://www.mm-software.com/more-the-newsroom/anmeldung/','Video call','Monday – Friday 06:00 to 12:00','Montag – Freitag 06:00 bis 12:00'),
(2,'https://www.mm-software.com/','Chat','Monday – Friday 08:00 to 16:00','Montag – Freitag 08:00 bis 16:00');

-- ============================================================
-- Contact-IPCommunication Relationships
-- ============================================================

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

-- ============================================================
-- Asset-ContactInformation Relationships
-- ============================================================

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
