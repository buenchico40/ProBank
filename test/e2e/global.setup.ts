import { test as setup } from '@playwright/test';
import fs from 'node:fs';

// Inicia sesión con Entra ID una vez por rol y guarda la sesión en .auth/<rol>.json
const usuarios: Record<string, { usuario: string; clave: string }> = JSON.parse(process.env.PROBANK_USUARIOS || '{}');

for (const [rol, cred] of Object.entries(usuarios)) {
  setup(`sesión ${rol}`, async ({ page }) => {
    await page.goto('/');
    await page.getByRole('textbox', { name: /correo|email|usuario/i }).fill(cred.usuario);
    await page.getByRole('button', { name: /siguiente|next/i }).click();
    await page.getByRole('textbox', { name: /contraseña|password/i }).fill(cred.clave);
    await page.getByRole('button', { name: /iniciar sesión|sign in/i }).click();
    await page.getByRole('button', { name: /no|sí|yes/i }).first().click().catch(() => undefined); // "¿Mantener la sesión?"
    await page.waitForURL(/\/(?!.*login)/);
    fs.mkdirSync('.auth', { recursive: true });
    await page.context().storageState({ path: `.auth/${rol}.json` });
  });
}
