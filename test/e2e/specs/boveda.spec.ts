import type { Page } from '@playwright/test';
import { test, expect } from '../fixtures/probank';

// Llama al puente /Boveda/Datos (con el antiforgery de la página) para preparar datos; las pantallas se verifican aparte
async function datos(page: Page, accion: string, query = '', body?: unknown) {
  return page.evaluate(async ([a, q, b]) => {
    const r = await fetch('/Boveda/Datos/' + a + (q ? '?' + q : ''), b === undefined ? {} :
      { method: 'POST', headers: { 'Content-Type': 'application/json', RequestVerificationToken: (window as any).pb.token() }, body: JSON.stringify(b) });
    const t = await r.text();
    let json: any = t; try { json = t ? JSON.parse(t) : null; } catch { /* texto */ }
    return { status: r.status, json };
  }, [accion, query, body] as const);
}

const iso = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
const enDias = (n: number) => { const d = new Date(); d.setDate(d.getDate() + n); return iso(d); };

/** Grupo de prueba con los miembros indicados (ids de usuario); se reutiliza entre corridas. */
async function grupo(page: Page, nombre: string, miembros: number[]) {
  const g = ((await datos(page, 'Grupos')).json as any[]).find(x => x.nombre === nombre);
  const r = await datos(page, 'GuardarGrupo', g ? 'token=' + g.token : '', { nombre, descripcion: 'Prueba de punta a punta', estado: 'Activo', miembros });
  expect(r.status).toBe(200);
  return r.json.token as string;
}

/** Secreto de prueba en el grupo indicado; se reutiliza entre corridas. */
async function secreto(page: Page, nombre: string, grupoToken: string, expira: string | null) {
  const s = ((await datos(page, 'Secretos')).json as any[]).find(x => x.nombre === nombre && x.almacen === 'kv-e2e');
  const r = await datos(page, 'GuardarSecreto', s ? 'token=' + s.token : '', { nombre, tipo: 'Clave de API', almacen: 'kv-e2e', sistema: 'Pruebas e2e',
    grupo: grupoToken, diasAviso: 30, expira, activo: true });
  expect(r.status).toBe(200);
  return r.json.token as string;
}

async function yoYOtro(page: Page) {
  const usuarios = (await datos(page, 'Usuarios')).json as any[];
  const yo = await page.evaluate(() => (window as any).pb.sesion.usuario as string);
  return { yo: usuarios.find(u => u.usuario === yo), otro: usuarios.find(u => u.usuario !== yo) };
}

test.describe('Bóveda', () => {
  test.beforeEach(({ app }) => test.skip(app.esPrototipo, 'Requiere el ambiente con base de datos y las llaves de Always Encrypted'));

  test('vencimiento con días de aviso, rotación del valor con versiones y renovación de certificados @CRE-01', async ({ app, page }) => {
    await app.ir('Bóveda', 'Secretos');
    const { yo } = await yoYOtro(page);
    const g = await grupo(page, 'e2e · Bóveda', [yo.usuarioId]);
    const s = await secreto(page, 'e2e--Vence', g, enDias(10));
    expect(((await datos(page, 'Secretos')).json as any[]).find(x => x.token === s).estado).toBe('Por vencer');

    const antes = ((await datos(page, 'Secretos')).json as any[]).find(x => x.token === s).version;
    const c = await datos(page, 'Cambiar', 'clase=secretos&token=' + s, { valor: 'e2e-valor-' + Date.now() + '-abcdef', dias: 90, nota: 'e2e' });
    expect(c.status).toBe(200);
    expect(c.json).toEqual({ version: antes + 1, vence: enDias(90) });
    const despues = ((await datos(page, 'Secretos')).json as any[]).find(x => x.token === s);
    expect(despues.estado).toBe('Vigente');
    const h = (await datos(page, 'Historial', 'clase=secretos&token=' + s)).json;
    expect(h.versiones.filter((v: any) => v.vigente)).toHaveLength(1);
    expect(h.versiones[0].version).toBe(antes + 1);

    expect((await datos(page, 'Cambiar', 'clase=secretos&token=' + s, { valor: 'corta', dias: 90 })).status).toBe(422);

    const certs = (await datos(page, 'Certificados')).json as any[];
    let cert = certs.find(x => x.nombreComun === 'e2e.probank.local' && x.activo);
    if (!cert) {
      const n = await datos(page, 'GuardarCertificado', '', { nombreComun: 'e2e.probank.local', tipo: 'SSL/TLS', emisor: 'CA de pruebas', vence: enDias(20),
        almacen: 'Almacén e2e', responsable: 'Pruebas', diasAviso: 30, huella: null, nombresAlternativos: null, usadoPor: null, activo: true });
      expect(n.status).toBe(200);
      cert = ((await datos(page, 'Certificados')).json as any[]).find(x => x.token === n.json.token);
    }
    const huella = Array.from({ length: 40 }, () => '0123456789ABCDEF'[Math.floor(Math.random() * 16)]).join('');
    expect((await datos(page, 'RenovarCertificado', 'token=' + cert.token, { vence: enDias(400), huella })).status).toBe(200);
    expect(((await datos(page, 'Renovaciones', 'token=' + cert.token)).json as any[])[0].huellaNueva).toBe(huella);
    expect(((await datos(page, 'Certificados')).json as any[]).find(x => x.token === cert.token).estado).toBe('Vigente');
  });

  test('mostrar y copiar solo para los miembros del grupo, con cada acceso registrado @CRE-02', async ({ app, page }) => {
    await app.ir('Bóveda', 'Secretos');
    const { yo, otro } = await yoYOtro(page);
    test.skip(!otro, 'Se necesita otro usuario activo para el grupo sin acceso');
    const g = await grupo(page, 'e2e · Bóveda', [yo.usuarioId]);
    const s = await secreto(page, 'e2e--Mostrar', g, null);
    const valor = 'e2e-mostrar-' + Date.now() + '-0123456789';
    expect((await datos(page, 'Cambiar', 'clase=secretos&token=' + s, { valor, dias: 180 })).status).toBe(200);

    await page.reload();
    await page.locator('tr', { hasText: 'e2e--Mostrar' }).locator('.dx-command-expand').click();
    await page.locator('.md').getByRole('button', { name: 'Mostrar' }).click();
    await expect(page.locator('.cred-show')).toHaveText(valor);
    await expect(app.dx.popup('Mostrar credencial')).toContainText('Consulta registrada en Auditoría');
    await page.locator('.dx-popup-bottom').getByRole('button', { name: 'Cerrar' }).click();
    const accesos = (await datos(page, 'Historial', 'clase=secretos&token=' + s)).json.accesos as any[];
    expect(accesos[0]).toMatchObject({ accion: 'Mostrar', usuario: yo.usuario });

    // Grupo del que no es miembro: ni mostrar, ni copiar, ni cambiar
    const ajeno = await grupo(page, 'e2e · Bóveda sin acceso', [otro.usuarioId]);
    const s2 = await secreto(page, 'e2e--Ajeno', ajeno, null);
    expect(((await datos(page, 'Secretos')).json as any[]).find(x => x.token === s2).esMiembro).toBe(false);
    expect((await datos(page, 'Mostrar', 'clase=secretos&token=' + s2, {})).status).toBe(403);
    expect((await datos(page, 'Copiar', 'clase=secretos&token=' + s2, {})).status).toBe(403);
    expect((await datos(page, 'Cambiar', 'clase=secretos&token=' + s2, { valor: 'e2e-ajeno-0123456789', dias: 90 })).status).toBe(403);
  });

  test('el valor nunca viaja en los listados ni en el historial y se indica el cifrado @CRE-03', async ({ app, page }) => {
    await app.ir('Bóveda', 'Secretos');
    const { yo } = await yoYOtro(page);
    const g = await grupo(page, 'e2e · Bóveda', [yo.usuarioId]);
    const s = await secreto(page, 'e2e--Cifrado', g, null);
    const valor = 'e2e-cifrado-' + Date.now() + '-0123456789';
    expect((await datos(page, 'Cambiar', 'clase=secretos&token=' + s, { valor, dias: 90 })).status).toBe(200);
    for (const [a, q] of [['Secretos', ''], ['Secreto', 'token=' + s], ['Historial', 'clase=secretos&token=' + s], ['DetalleGrupo', 'token=' + g]])
      expect(JSON.stringify((await datos(page, a, q)).json), a).not.toContain(valor);
    await page.reload();
    await page.locator('tr', { hasText: 'e2e--Cifrado' }).locator('.dx-command-expand').click();
    await expect(page.locator('.md .cred')).toContainText('Always Encrypted');
    await expect(page.locator('.md .cred')).not.toContainText(valor);
  });
});
