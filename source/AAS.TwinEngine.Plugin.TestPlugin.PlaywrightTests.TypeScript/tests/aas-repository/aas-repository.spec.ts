import { test, assertSuccessResponse, compareJson, aasIdentifier, testDataPath, expect } from '../api-test-base';
import * as path from 'path';

const TEST_DIR = path.dirname(__filename);

test.describe('AAS Repository', () => {
  test('GetShellById - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/shells/${aasIdentifier}`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetShellById_Expected.json'));
  });

  test('GetAssetInformationById - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/shells/${aasIdentifier}/asset-information`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetAssetInformationById_Expected.json'));
  });

  test('GetSubmodelRefById - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/shells/${aasIdentifier}/submodel-refs`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetSubmodelRefById_Expected.json'));
  });
});
