import { test as base, expect } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

/**
 * Base64 URL encodes a string
 */
export function base64EncodeUrl(str: string): string {
  return Buffer.from(str, 'utf-8').toString('base64');
}

// Pre-encoded identifiers
export const aasIdentifier = base64EncodeUrl('https://mm-software.com/ids/aas/000-001');
export const submodelIdentifierContact = base64EncodeUrl('https://mm-software.com/submodel/000-001/ContactInformation');
export const submodelIdentifierNameplate = base64EncodeUrl('https://mm-software.com/submodel/000-001/Nameplate');
export const submodelIdentifierReliability = base64EncodeUrl('https://mm-software.com/submodel/000-001/Reliability');

/**
 * Asserts that an API response is successful (2xx)
 */
export function assertSuccessResponse(response: { ok(): boolean; status(): number; statusText(): string }): void {
  expect(response.ok(), `Expected successful response but got ${response.status()}: ${response.statusText()}`).toBeTruthy();
}

/**
 * Loads expected JSON from a test data file and compares with actual JSON.
 * Comparison is done via normalized (compact) JSON strings.
 */
export async function compareJson(actual: unknown, testDataPath: string): Promise<void> {
  const expectedJson = fs.readFileSync(testDataPath, 'utf-8');
  const expectedObj = JSON.parse(expectedJson);

  const expectedNormalized = JSON.stringify(expectedObj);
  const actualNormalized = JSON.stringify(actual);

  expect(actualNormalized).toBe(expectedNormalized);
}

/**
 * Resolves a test data file path relative to the test file's directory.
 */
export function testDataPath(dir: string, ...segments: string[]): string {
  return path.join(dir, 'test-data', ...segments);
}

// Re-export for convenience
export { expect };
export const test = base;
