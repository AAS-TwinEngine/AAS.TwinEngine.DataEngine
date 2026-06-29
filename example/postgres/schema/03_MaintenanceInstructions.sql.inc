-- ============================================================
-- Maintenance Instructions Schema (Table Creation)
-- ============================================================
-- This file contains CREATE TABLE statements for maintenance-related entities:
-- - Maintenance Instructions For Specific Interval
-- - Alarm
-- - ContactForMaintenanceAuthorization - Email - Phone - Fax
-- - MaintenanceStep
-- - MaintenanceTool
-- - MaintenanceConsumable
-- - Asset-MaintenanceInstructions relationships
-- ============================================================

-- ============================================================
-- Maintenance Instructions
-- ============================================================

CREATE TABLE "MaintenanceInstructionsForSpecificInterval" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "MaintenanceID" VARCHAR(50),
    "NameOfMaintenance_en" TEXT,
    "NameOfMaintenance_de" TEXT,
    "SourceOfMaintenanceInstructions_en" TEXT,
    "SourceOfMaintenanceInstructions_de" TEXT,
    "RelatedStandardsLawsRegulations_en" TEXT,
    "RelatedStandardsLawsRegulations_de" TEXT,
    "SafetyRegulationsToBeObserved_en" TEXT,
    "SafetyRegulationsToBeObserved_de" TEXT,
    "MaintenanceIntervalValue" INT,
    "MaintenanceIntervalUnit" VARCHAR(50),
    "FlowChartOfMaintenanceSteps" TEXT,
    "NumberOfRequiredTechnicians" INT,
    "RequiredQualification_en" TEXT,
    "RequiredQualification_de" TEXT,
    "ValueTotalEstimatedWorkingTime" NUMERIC,
    "UnitValueTotalEstimatedWorkingTime" VARCHAR(50)
);

-- ============================================================
-- Asset ↔ MaintenanceInstruction Mapping
-- ============================================================

CREATE TABLE "AssetMaintenanceInstruction" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT NOT NULL REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "MaintenanceInstructionId" INT NOT NULL
        REFERENCES "MaintenanceInstructionsForSpecificInterval"("Id") ON DELETE CASCADE
);

-- ============================================================
-- Alarm Table
-- ============================================================

CREATE TABLE "Alarm" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "AlarmName_en" TEXT,
    "AlarmName_de" TEXT,
    "WarningLimitRelativeValue" NUMERIC,
    "WarningLimitSeverity" VARCHAR(50)
);

-- ============================================================
-- MaintenanceInstruction ↔ Alarm Mapping
-- ============================================================

CREATE TABLE "MaintenanceInstructionAlarm" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "MaintenanceInstructionId" INT NOT NULL
        REFERENCES "MaintenanceInstructionsForSpecificInterval"("Id") ON DELETE CASCADE,
    "AlarmId" INT NOT NULL REFERENCES "Alarm"("Id") ON DELETE CASCADE
);

-- ============================================================
-- Contact For Maintenance Authorization
-- ============================================================

CREATE TABLE "ContactForMaintenanceAuthorization" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "Company_en" TEXT,
    "Company_de" TEXT,
    "Department_en" TEXT,
    "Department_de" TEXT,
    "Title_en" VARCHAR(20),
    "Title_de" VARCHAR(20),
    "AcademicTitle_en" VARCHAR(20),
    "AcademicTitle_de" VARCHAR(20),
    "NameOfContact_en" TEXT,
    "NameOfContact_de" TEXT,
    "FirstName_en" TEXT,
    "FirstName_de" TEXT,
    "MiddleNames_en" TEXT,
    "MiddleNames_de" TEXT,
    "Street_en" TEXT,
    "Street_de" TEXT,
    "Zipcode_en" VARCHAR(20),
    "Zipcode_de" VARCHAR(20),
    "CityTown_en" TEXT,
    "CityTown_de" TEXT,
    "NationalCode_en" VARCHAR(10),
    "NationalCode_de" VARCHAR(10),
    "StateCounty_en" TEXT,
    "StateCounty_de" TEXT,
    "FurtherDetailsOfContact_en" TEXT,
    "FurtherDetailsOfContact_de" TEXT,
    "RoleOfContactPerson" TEXT
);

CREATE TABLE "Email" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ContactForMaintenanceAuthorizationId" INT UNIQUE
        REFERENCES "ContactForMaintenanceAuthorization"("Id") ON DELETE CASCADE,
    "EmailAddress" TEXT,
    "TypeOfEmailAddress" TEXT,
    "PublicKey_en" TEXT,
    "PublicKey_de" TEXT,
    "TypeOfPublicKey_en" TEXT,
    "TypeOfPublicKey_de" TEXT
);

CREATE TABLE "Phone" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ContactForMaintenanceAuthorizationId" INT UNIQUE
        REFERENCES "ContactForMaintenanceAuthorization"("Id") ON DELETE CASCADE,
    "TelephoneNumber_en" TEXT,
    "TelephoneNumber_de" TEXT,
    "AvailableTime_en" TEXT,
    "AvailableTime_de" TEXT,
    "TypeOfTelephone" TEXT
);

CREATE TABLE "Fax" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ContactForMaintenanceAuthorizationId" INT UNIQUE
        REFERENCES "ContactForMaintenanceAuthorization"("Id") ON DELETE CASCADE,
    "FaxNumber_en" TEXT,
    "FaxNumber_de" TEXT,
    "TypeOfFaxNumber" TEXT
);

CREATE TABLE "MaintenanceInstructionContactForMaintenanceAuthorization" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "MaintenanceInstructionId" INT REFERENCES "MaintenanceInstructionsForSpecificInterval"("Id") ON DELETE CASCADE,
    "ContactForMaintenanceAuthorizationId" INT REFERENCES "ContactForMaintenanceAuthorization"("Id") ON DELETE CASCADE
);

CREATE TABLE "MaintenanceStep" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "MaintenanceStepID" TEXT,
    "QuantityOfSparePartForMaintenanceStep" INT,
    "QuantityOfConsumablesForMaintenanceStep" INT,
    "UnitForQuantityOfConsumablesForMaintenanceStep" TEXT,
    "QuantityOfToolsForMaintenanceStep" INT,
    "DocumentationSignatureMandatory" BOOLEAN,
    "EndOfMaintenance" BOOLEAN,
    "MaintenanceStepName_en" TEXT,
    "MaintenanceStepName_de" TEXT,
    "LocalizationDescription_en" TEXT,
    "LocalizationDescription_de" TEXT,
    "InstructionMaintenanceStep_en" TEXT,
    "InstructionMaintenanceStep_de" TEXT,
    "ConditionForNextMaintenanceStep_en" TEXT,
    "ConditionForNextMaintenanceStep_de" TEXT,
    "ConditionForAlternativeNextStep_en" TEXT,
    "ConditionForAlternativeNextStep_de" TEXT,
    "RelatedDocumentOrFileMaintenanceStep" TEXT,
    "ValueEstimatedDurationTimeMaintenanceStep" INT,
    "UnitEstimatedDurationTimeMaintenanceStep" TEXT
);

CREATE TABLE "MaintenanceInstructionsForSpecificIntervalMaintenanceStep" (
    "MaintenanceInstructionsForSpecificIntervalId" INT
        REFERENCES "MaintenanceInstructionsForSpecificInterval"("Id") ON DELETE CASCADE,
    "MaintenanceStepId" INT REFERENCES "MaintenanceStep"("Id") ON DELETE CASCADE,
    PRIMARY KEY (
        "MaintenanceInstructionsForSpecificIntervalId",
        "MaintenanceStepId"
    )
);

CREATE TABLE "MaintenanceTool" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "ToolID" TEXT,
    "OrderCodeOfManufacturer" TEXT,
    "AddressOfAdditionalLink" TEXT,
    "ToolName_en" TEXT,
    "ToolName_de" TEXT,
    "CompanyNameToolSupplier_en" TEXT,
    "CompanyNameToolSupplier_de" TEXT,
    "ToolDescription_en" TEXT,
    "ToolDescription_de" TEXT,
    "MaxQuantityOfTool" INT
);

CREATE TABLE "AssetMaintenanceTool" (
    "AssetId" INT REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "MaintenanceToolId" INT REFERENCES "MaintenanceTool"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("AssetId", "MaintenanceToolId")
);

CREATE TABLE "MaintenanceConsumable" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "ConsumableID" TEXT,
    "UnitMaxQuantityOfConsumable" TEXT,
    "OrderCodeOfManufacturer" TEXT,
    "AddressOfAdditionalLink" TEXT,
    "ConsumableName_en" TEXT,
    "ConsumableName_de" TEXT,
    "CompanyNameSupplierConsumable_en" TEXT,
    "CompanyNameSupplierConsumable_de" TEXT,
    "ConsumableDescription_en" TEXT,
    "ConsumableDescription_de" TEXT,
    "DisposalInstructionsForConsumable_en" TEXT,
    "DisposalInstructionsForConsumable_de" TEXT,
    "QuantityOfConsumable" INT
);

CREATE TABLE "AssetMaintenanceConsumable" (
    "AssetId" INT REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "MaintenanceConsumableId" INT REFERENCES "MaintenanceConsumable"("Id") ON DELETE CASCADE,
    PRIMARY KEY (
        "AssetId",
        "MaintenanceConsumableId"
    )
);

CREATE TABLE "MaintenanceSparePart" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Index" INT,
    "SparePartID" TEXT,
    "OrderCodeOfManufacturer" TEXT,
    "AddressOfAdditionalLink" TEXT,
    "SparePartName_en" TEXT,
    "SparePartName_de" TEXT,
    "CompanyNameSupplierSparePart_en" TEXT,
    "CompanyNameSupplierSparePart_de" TEXT,
    "SparePartDescription_en" TEXT,
    "SparePartDescription_de" TEXT,
    "DisposalInstructionsForSparePart_en" TEXT,
    "DisposalInstructionsForSparePart_de" TEXT,
    "QuantityOfSparePart" INT
);

CREATE TABLE "AssetMaintenanceSparePart" (
    "AssetId" INT REFERENCES "Asset"("Id") ON DELETE CASCADE,
    "MaintenanceSparePartId" INT REFERENCES "MaintenanceSparePart"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("AssetId", "MaintenanceSparePartId")
);

