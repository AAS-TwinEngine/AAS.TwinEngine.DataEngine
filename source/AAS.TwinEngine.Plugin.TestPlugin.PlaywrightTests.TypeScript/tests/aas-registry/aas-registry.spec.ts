import { test, assertSuccessResponse, compareJson, aasIdentifier, testDataPath, expect } from '../api-test-base';
import * as path from 'path';

const TEST_DIR = path.dirname(__filename);

test.describe('AAS Registry', () => {
  test('GetAllShellDescriptors - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = '/shell-descriptors';

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetAllShellDescriptors_Expected.json'));
  });

  test('GetAllShellDescriptors - with pagination', async ({ request }) => {
    // Arrange
    const urlLimit2 = '/shell-descriptors?limit=2';
    const urlLimit3 = '/shell-descriptors?limit=3';

    // Act
    const responseLimit2 = await request.get(urlLimit2);
    const responseLimit3 = await request.get(urlLimit3);

    // Assert
    assertSuccessResponse(responseLimit2);
    assertSuccessResponse(responseLimit3);

    const contentLimit2 = await responseLimit2.text();
    const contentLimit3 = await responseLimit3.text();

    expect(contentLimit2).toBeTruthy();
    expect(contentLimit3).toBeTruthy();

    const jsonLimit2 = JSON.parse(contentLimit2);
    const jsonLimit3 = JSON.parse(contentLimit3);

    expect(jsonLimit2).toBeDefined();
    expect(jsonLimit3).toBeDefined();

    // Verify that limit 3 contains one more element than limit 2
    const countLimit2 = jsonLimit2.result.length;
    const countLimit3 = jsonLimit3.result.length;

    expect(countLimit3).toBe(countLimit2 + 1);
  });

  test('GetShellDescriptorById - should return success with expected content', async ({ request }) => {
    // Arrange
    const url = `/shell-descriptors/${aasIdentifier}`;

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBeTruthy();

    const json = JSON.parse(content);
    expect(json).toBeDefined();

    await compareJson(json, testDataPath(TEST_DIR, 'GetShellDescriptorById_Expected.json'));
  });
});
