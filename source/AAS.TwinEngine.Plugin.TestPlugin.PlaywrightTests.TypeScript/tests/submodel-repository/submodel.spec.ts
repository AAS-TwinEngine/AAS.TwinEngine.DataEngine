import { test, assertSuccessResponse, compareJson, submodelIdentifierContact, submodelIdentifierNameplate, submodelIdentifierReliability, testDataPath, expect } from '../api-test-base';
import * as path from 'path';

const TEST_DIR = path.dirname(__filename);

test.describe('Submodel Repository - Submodels', () => {
  test('GetSubmodel Nameplate - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/submodels/${submodelIdentifierNameplate}/`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodel_Nameplate_Expected.json'));
  });

  test('GetSubmodel ContactInfo - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/submodels/${submodelIdentifierContact}/`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodel_ContactInfo_Expected.json'));
  });

  test('GetSubmodel Reliability - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/submodels/${submodelIdentifierReliability}/`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodel_Reliability_Expected.json'));
  });
});
