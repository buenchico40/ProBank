import { test, expect } from '../fixtures/probank';

test.describe('Seguridad', () => {
  test('un usuario puede tener varios roles @SEG-01', async ({ app }) => {
    await app.ir('Seguridad', 'Usuarios');
    await expect(app.dx.fila('admin.sistema')).toContainText('Gerencia de TI');
  });
});
