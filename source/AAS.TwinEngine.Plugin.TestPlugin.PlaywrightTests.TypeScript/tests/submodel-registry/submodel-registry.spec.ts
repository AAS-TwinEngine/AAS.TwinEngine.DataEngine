import { test, assertSuccessResponse, compareJson, submodelIdentifierContact, submodelIdentifierNameplate, submodelIdentifierReliability, testDataPath, expect } from '../api-test-base';
import * as path from 'path';

const TEST_DIR = path.dirname(__filename);

test.describe('Submodel Registry', () => {
  test('GetSubmodelDescriptorById Contact - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/submodel-descriptors/${submodelIdentifierContact}`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodelDescriptorById_Contact_Expected.json'));
  });

  test('GetSubmodelDescriptorById Nameplate - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/submodel-descriptors/${submodelIdentifierNameplate}`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodelDescriptorById_Nameplate_Expected.json'));
  });

  test('GetSubmodelDescriptorById Reliability - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/submodel-descriptors/${submodelIdentifierReliability}`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodelDescriptorById_Reliability_Expected.json'));
  });
});
