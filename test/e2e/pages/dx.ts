import { expect, type Locator, type Page } from '@playwright/test';

/** Ayudas para controles DevExtreme (grilla, selectbox, tagbox, popup, avisos). */
export class Dx {
  constructor(private readonly page: Page) {}

  boton(nombre: string | RegExp): Locator { return this.page.getByRole('button', { name: nombre }).first(); }

  grilla(): Locator { return this.page.locator('.dx-datagrid').first(); }
  filas(): Locator { return this.grilla().locator('.dx-datagrid-rowsview .dx-data-row'); }
  fila(texto: string | RegExp): Locator { return this.filas().filter({ hasText: texto }).first(); }

  /** Selecciona la fila sin abrir su detalle (la celda del código es un enlace). */
  async seleccionarFila(texto: string | RegExp) { await this.fila(texto).locator('td').nth(1).click(); }
  async desplegarFila(texto: string | RegExp) { await this.fila(texto).locator('.dx-command-expand').click(); }

  popup(titulo: string | RegExp): Locator { return this.page.locator('.dx-overlay-content').filter({ hasText: titulo }).last(); }
  campo(contenedor: Locator, etiqueta: string | RegExp): Locator { return contenedor.locator('.dx-field-item').filter({ hasText: etiqueta }).first(); }

  async escribir(contenedor: Locator, etiqueta: string | RegExp, valor: string) {
    const input = this.campo(contenedor, etiqueta).locator('input.dx-texteditor-input').first();
    await input.fill(valor);
    await input.press('Tab'); // confirma el valor (DevExtreme lo toma al salir del campo)
  }

  /** Abre el desplegable y devuelve SU lista (DevExtreme deja en el DOM las listas de otros editores ya cerrados). */
  private async abrirLista(contenedor: Locator, etiqueta: string | RegExp): Promise<Locator> {
    const editor = this.campo(contenedor, etiqueta).locator('.dx-selectbox, .dx-tagbox').first();
    const lista = this.page.locator('.dx-dropdowneditor-overlay .dx-list:visible');
    await editor.click();
    if (!(await lista.first().isVisible().catch(() => false))) await editor.click();
    await expect(lista.first()).toBeVisible();
    const id = await editor.locator('input').first().getAttribute('aria-controls');
    return id ? this.page.locator(`[id="${id}"]`).locator('xpath=ancestor-or-self::*[contains(@class,"dx-list")][1]') : lista.last();
  }

  async elegir(contenedor: Locator, etiqueta: string | RegExp, opcion: string) {
    const lista = await this.abrirLista(contenedor, etiqueta);
    await lista.locator('.dx-list-item').filter({ hasText: opcion }).first().click();
  }

  async elegirVarios(contenedor: Locator, etiqueta: string | RegExp, opciones: string[]) {
    const lista = await this.abrirLista(contenedor, etiqueta);
    for (const o of opciones) await lista.locator('.dx-list-item').filter({ hasText: o }).first().click();
    await this.page.keyboard.press('Escape');
  }

  async aviso(texto: string | RegExp) { await expect(this.page.locator('.dx-toast-message').last()).toContainText(texto); }
}
