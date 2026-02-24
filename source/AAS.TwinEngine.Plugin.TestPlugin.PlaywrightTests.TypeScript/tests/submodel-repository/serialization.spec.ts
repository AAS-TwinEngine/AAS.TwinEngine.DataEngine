import { test, assertSuccessResponse, aasIdentifier, submodelIdentifierContact, submodelIdentifierNameplate, submodelIdentifierReliability, expect } from '../api-test-base';

test.describe('Submodel Repository - Serialization', () => {
  test('GetAppropriateSerialization with multiple submodels - should return success', async ({ request }) => {
    // Arrange
    const url = `/serialization`
      + `?aasIds=${aasIdentifier}`
      + `&submodelIds=${submodelIdentifierContact}`
      + `&submodelIds=${submodelIdentifierNameplate}`
      + `&submodelIds=${submodelIdentifierReliability}`
      + `&includeConceptDescriptions=false`;

    // Act
    const response = await request.get(url);
    const content = await response.text();

    // Assert
    expect(content).toBeTruthy();

    expect(content).toContain('https://mm-software.com/submodel/000-001/Nameplate');
    expect(content).toContain('https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation');
    expect(content).toContain('http://schemas.openxmlformats.org/package/2006/relationships');
  });
});
