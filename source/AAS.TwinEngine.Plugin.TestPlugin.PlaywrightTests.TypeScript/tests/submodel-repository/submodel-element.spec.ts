import { test, assertSuccessResponse, compareJson, submodelIdentifierContact, submodelIdentifierNameplate, submodelIdentifierReliability, testDataPath, expect } from '../api-test-base';
import * as path from 'path';

const TEST_DIR = path.dirname(__filename);

test.describe('Submodel Repository - Submodel Elements', () => {
  test('GetSubmodelElement ContactInfo ContactInformation - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/submodels/${submodelIdentifierContact}/submodel-elements/ContactInformation1`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodelElement_ContactInfo_ContactInformation_Expected.json'));
  });

  test('GetSubmodelElement Nameplate Markings - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/submodels/${submodelIdentifierNameplate}/submodel-elements/Markings`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodelElement_Nameplate_Markings_Expected.json'));
  });

  test('GetSubmodelElement Nameplate ManufacturerName - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/submodels/${submodelIdentifierNameplate}/submodel-elements/ManufacturerName`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodelElement_Nameplate_ManufacturerName_Expected.json'));
  });

  test('GetSubmodelElement Reliability ReliabilityCharacteristics MTTF - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/submodels/${submodelIdentifierReliability}/submodel-elements/ReliabilityCharacteristics.MTTF`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodelElement_Reliability_ReliabilityCharacteristics_MTTF.json'));
  });
});
