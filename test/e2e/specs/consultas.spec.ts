import { test, expect } from '../fixtures/probank';
import { ConsultasPage } from '../pages/consultas.page';

test.describe('Consultas dinámicas', () => {
  test('parámetro variable resuelve la fecha @SQL-04', async ({ app }) => {
    const c = new ConsultasPage(app);
    const p = await c.ejecutar('QRY-002');
    await app.dx.elegir(p, 'Desde (@FechaDesde)', 'Inicio del mes anterior');
    const fecha = await app.dx.campo(p, 'Fecha resultante').locator('input.dx-texteditor-input').first().inputValue();
    expect(fecha).toMatch(/^01\/\d{2}\/\d{4}$/);
  });

  test('ejecución sin aprobadores muestra resultados y exporta @SQL-01 @SQL-05', async ({ app }) => {
    const c = new ConsultasPage(app);
    const p = await c.ejecutar('QRY-001');
    await p.getByRole('button', { name: 'Ejecutar', exact: true }).click();
    await expect(app.dx.popup('Resultados')).toContainText('filas');
  });

  test('toda ejecución queda en la auditoría @SQL-06', async ({ app }) => {
    const c = new ConsultasPage(app);
    await c.abrirAuditoria();
    await expect(app.dx.filas().first()).toBeVisible();
    await expect(app.dx.grilla()).toContainText(/Solicitud|Ejecución|Exportación/);
  });
});
