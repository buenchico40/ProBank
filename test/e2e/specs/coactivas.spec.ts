import { test, expect } from '../fixtures/probank';

test.describe('Coactivas', () => {
  test('el monto se muestra en formato de moneda @COA-05', async ({ app }) => {
    await app.ir('Coactivas', 'Coactivas');
    await expect(app.dx.grilla()).toContainText(/\$ [\d.]+,\d{2}/);
  });
});
