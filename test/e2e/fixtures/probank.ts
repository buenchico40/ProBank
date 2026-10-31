import { test as base, expect, type Page } from '@playwright/test';
import { Dx } from '../pages/dx';

type Fixtures = { app: App };

/** Fachada de la aplicación: entra (prototipo o ambiente real) y navega por el menú. */
export class App {
  readonly dx: Dx;
  constructor(readonly page: Page, readonly esPrototipo: boolean) { this.dx = new Dx(page); }

  async entrar() {
    await this.page.goto('');
    if (this.esPrototipo) {
      await this.page.getByRole('button', { name: 'Ingresar' }).click();
      await expect(this.page.locator('#accordion')).toBeVisible();
    }
  }

  async ir(modulo: string, opcion: string) {
    if (this.esPrototipo) {
      const menu = this.page.locator('#accordion');
      const op = menu.locator('[data-k]').getByText(opcion, { exact: true }).first();
      if (!(await op.isVisible())) await menu.getByText(modulo, { exact: true }).first().click();
      await op.click();
    } else {
      await this.page.getByRole('navigation').getByRole('link', { name: new RegExp(modulo) }).click();
    }
    await expect(this.page.locator('h1').first()).toBeVisible();
  }
}

export const test = base.extend<Fixtures>({
  app: async ({ page }, use, info) => {
    const app = new App(page, info.project.name === 'prototipo');
    await app.entrar();
    await use(app);
  },
});
export { expect };
