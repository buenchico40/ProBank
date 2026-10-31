import { test, expect } from '../fixtures/probank';
import { PortafolioPage } from '../pages/portafolio.page';

test.describe('Portafolio', () => {
  test('crear tarea en un proyecto con técnico y dos aprobadores @PRY-03 @PRY-04', async ({ app }) => {
    const pf = new PortafolioPage(app);
    await pf.nuevaTareaEnItem('PRY-001', {
      titulo: 'Migrar procedimientos almacenados', area: 'Base de datos', tecnico: 'Usuario de ejemplo 4',
      fecha: '20/11/2027', aprobadores: ['Usuario de ejemplo 3', 'Usuario de ejemplo 1'],
    });
    await app.dx.aviso(/requiere la aprobación de sus 2 aprobadores/);
    await pf.abrirTareas();
    await expect(app.dx.fila('Migrar procedimientos almacenados')).toContainText('Usuario de ejemplo 4');
  });

  test('ver cronograma desde el ítem muestra solo ese ítem @PRY-07', async ({ app, page }) => {
    const pf = new PortafolioPage(app);
    await pf.abrirItems();
    await app.dx.desplegarFila('PRY-001');
    await app.dx.boton('Ver en cronograma').click();
    await expect(page.getByRole('button', { name: /Solo PRY-001/ })).toBeVisible();
    await expect(page.locator('.dx-gantt')).not.toContainText('MEJ-002');
  });

  test('rechazar exige comentario @PRY-05', async ({ app, page }) => {
    const pf = new PortafolioPage(app);
    await pf.abrirAprobaciones();
    await app.dx.seleccionarFila('TAR-0006');
    await app.dx.boton('Rechazar').click();
    await page.getByRole('button', { name: 'Rechazar' }).last().click();
    await expect(app.dx.popup('Rechazar tarea')).toBeVisible(); // no se cierra sin motivo
  });
});
