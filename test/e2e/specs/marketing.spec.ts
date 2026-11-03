import path from 'node:path';
import { test, expect } from '../fixtures/probank';

const imagen = path.resolve(import.meta.dirname, '../fixtures/promo-1200x300.png');
const unico = () => Date.now().toString().slice(-6);
// Fecha local de hoy (dd/mm/aaaa): el destacado más reciente va primero en el Panel
const hoy = () => { const d = new Date(); return String(d.getDate()).padStart(2, '0') + '/' + String(d.getMonth() + 1).padStart(2, '0') + '/' + d.getFullYear(); };

// Pantallas reales de Marketing (la maqueta no guarda datos ni imágenes)
test.describe('Marketing', () => {
  test.beforeEach(({ app }) => test.skip(app.esPrototipo, 'Requiere el ambiente con base de datos'));

  test('la imagen se publica solo con imagen y vigencia, y aparece en el carrusel del Panel @MKT-01', async ({ app, page }) => {
    const titulo = 'Campaña e2e ' + unico();
    await app.ir('Marketing', 'Imágenes promocionales');
    await app.dx.boton('Nuevo').click();
    const ficha = page.locator('#content');
    await app.dx.escribir(ficha, /^Título\s*\*?$/, titulo);
    await app.dx.escribir(ficha, 'Vigente desde', '01/01/2026');
    await app.dx.escribir(ficha, 'Vigente hasta', '31/12/2030');
    await app.dx.boton('Guardar').click();
    await app.dx.aviso('guardado correctamente');

    // Sin imagen no se publica
    await app.dx.seleccionarFila(titulo);
    await app.dx.boton('Publicar').click();
    await app.dx.aviso(/Cargue la imagen/);

    // Con imagen (JPG o PNG) se publica y queda visible en el Panel
    await app.dx.seleccionarFila(titulo);
    await app.dx.boton('Editar').click();
    await page.locator('.card', { hasText: 'Imagen promocional' }).locator('input[type=file]').setInputFiles(imagen);
    await app.dx.boton('Guardar').click();
    await app.dx.aviso('con su imagen');
    await app.dx.seleccionarFila(titulo);
    await app.dx.boton('Publicar').click();
    await app.dx.aviso(/publicado: ya aparece en el Panel/);

    // Lo publicado no se elimina: se retira
    await app.dx.seleccionarFila(titulo);
    await app.dx.boton('Eliminar').click();
    await app.dx.aviso(/Solo se eliminan borradores/);

    await page.goto('/');
    await expect(page.locator('.panel-hero')).toContainText(titulo);

    // Retirada, deja de mostrarse
    await app.ir('Marketing', 'Imágenes promocionales');
    await app.dx.seleccionarFila(titulo);
    await app.dx.boton('Retirar').click();
    await page.getByRole('button', { name: 'Retirar' }).last().click();
    await app.dx.aviso(/retirada/);
    await page.goto('/');
    await expect(page.locator('.panel-hero')).not.toContainText(titulo);
  });

  test('el comunicado destacado va primero en el Panel y se lee con resumen, secciones y contacto @MKT-02', async ({ app, page }) => {
    const titulo = 'Comunicado e2e ' + unico();
    await app.ir('Marketing', 'Comunicados');
    await app.dx.boton('Nuevo').click();
    const ficha = page.locator('#content');
    await app.dx.escribir(ficha, /^Título\s*\*?$/, titulo);
    await app.dx.elegir(ficha, /^Tipo\s*\*?$/, 'Mantenimiento');
    await app.dx.escribir(ficha, 'Publicado desde', hoy());
    await app.dx.escribir(ficha, 'Publicado hasta', '31/12/2030');
    await app.dx.campo(ficha, 'Destacado').locator('.dx-switch').click();
    await app.dx.campo(ficha, 'Resumen').locator('textarea').fill('Ventana de mantenimiento del core.');
    await app.dx.campo(ficha, 'Contacto').locator('input').fill('Mesa de servicio · Ext. 1000');
    await page.locator('.dx-htmleditor-content').first().fill('Qué pasará: se actualiza el core.');
    await app.dx.boton('Guardar').click();
    await app.dx.aviso('guardado correctamente');
    await app.dx.seleccionarFila(titulo);
    await app.dx.boton('Publicar').click();
    await app.dx.aviso(/publicado: ya aparece en el Panel/);

    await page.goto('/');
    const destacado = page.locator('.com-hero');
    await expect(destacado).toContainText(titulo);
    await destacado.click();
    const lectura = app.dx.popup(titulo);
    await expect(lectura).toContainText('Ventana de mantenimiento del core.');
    await expect(lectura).toContainText('se actualiza el core');
    await expect(lectura).toContainText('Consultas: Mesa de servicio · Ext. 1000');
  });
});
