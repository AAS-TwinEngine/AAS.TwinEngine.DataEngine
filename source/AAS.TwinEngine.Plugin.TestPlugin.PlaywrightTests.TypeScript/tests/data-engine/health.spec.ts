import { test, assertSuccessResponse, expect } from '../api-test-base';

test.describe('Data Engine', () => {
  test('GetHealth - should return success and Healthy', async ({ request }) => {
    // Arrange
    const url = '/healthz';

    // Act
    const response = await request.get(url);

    // Assert
    assertSuccessResponse(response);
    const content = await response.text();
    expect(content).toBe('Healthy');
  });
});
