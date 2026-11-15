import type { Browser, Page } from '@playwright/test';
import { test, expect } from '../fixtures/probank';

// Llama al puente /Decisiones/Datos (con el antiforgery de la página) para preparar datos; las pantallas se verifican aparte
async function datos(page: Page, accion: string, query = '', body?: unknown) {
  return page.evaluate(async ([a, q, b]) => {
    const r = await fetch('/Decisiones/Datos/' + a + (q ? '?' + q : ''), b === undefined ? {} :
      { method: 'POST', headers: { 'Content-Type': 'application/json', RequestVerificationToken: (window as any).pb.token() }, body: JSON.stringify(b) });
    const t = await r.text();
    let json: any = t; try { json = t ? JSON.parse(t) : null; } catch { /* texto */ }
    return { status: r.status, json };
  }, [accion, query, body] as const);
}

const hoy = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; };
// Aprobador para las pruebas de firma: otro usuario con «Mis aprobaciones» (en local, aprobador.local)
const APROBADOR = process.env.PROBANK_E2E_APROBADOR;
const CLAVE = process.env.PROBANK_E2E_APROBADOR_CLAVE;

/** Tipo «e2e · Decisión» con el aprobador indicado; se reutiliza entre corridas. */
async function tipo(page: Page, aprobadores: number[]) {
  const t = ((await datos(page, 'Tipos', 'soloActivos=false')).json as any[]).find(x => x.nombre === 'e2e · Decisión');
  const r = await datos(page, 'GuardarTipo', t ? 'codigo=' + t.codigo : '', { nombre: 'e2e · Decisión', descripcion: 'Prueba de punta a punta', secuencial: true, activo: true,
    aprobadores: aprobadores.map((usuarioId, i) => ({ usuarioId, orden: i + 1 })) });
  expect(r.status).toBe(200);
  return ((await datos(page, 'Tipos', 'soloActivos=true')).json as any[]).find(x => x.nombre === 'e2e · Decisión').codigo as string;
}

async function usuarios(page: Page) {
  const us = (await datos(page, 'Usuarios')).json as any[];
  const yo = await page.evaluate(() => (window as any).pb.sesion.usuario as string);
  return { yo: us.find(u => u.usuario === yo), otros: us.filter(u => u.usuario !== yo), todos: us };
}

async function nueva(page: Page, codigoTipo: string, notificados: number[]) {
  const r = await datos(page, 'Guardar', '', { tipo: codigoTipo, titulo: 'e2e · ' + Date.now(), descripcion: 'Se decide algo por $ 1.500.\nCon alcance definido.',
    area: 'Pruebas', fecha: hoy(), notificados });
  expect(r.status).toBe(200);
  return r.json.token as string;
}

async function entrarComo(browser: Browser, baseURL: string, usuario: string, clave: string) {
  const ctx = await browser.newContext({ baseURL, ignoreHTTPSErrors: true });
  const p = await ctx.newPage();
  await p.goto('Cuenta/Ingresar');
  await p.fill('#Usuario', usuario); await p.fill('#Clave', clave);
  await Promise.all([p.waitForURL(u => !u.pathname.includes('Ingresar')), p.click('button[type=submit]')]);
  await p.goto('Decisiones/Pendientes');
  return p;
}

test.describe('Decisiones', () => {
  test.beforeEach(({ app }) => test.skip(app.esPrototipo, 'Requiere el ambiente con base de datos'));

  test('se registra con los aprobadores de su tipo y sus notificados, y todo queda en la trazabilidad @DEC-01 @DEC-02 @DEC-05 @DEC-06', async ({ app, page }) => {
    await app.ir('Decisiones', 'Registro de decisiones');
    const { yo, otros } = await usuarios(page);
    test.skip(otros.length < 2, 'Se necesitan otros dos usuarios activos');
    const codigo = await tipo(page, [otros[0].usuarioId, yo.usuarioId]);
    const token = await nueva(page, codigo, [otros[1].usuarioId]);
    let x = (await datos(page, 'Decision', 'token=' + token)).json;
    expect(x.aprobadores.map((a: any) => a.usuario)).toEqual([otros[0].usuario]); // quien la registra no la aprueba
    expect(x.notificados.map((n: any) => n.usuario)).toEqual([otros[1].usuario]);
    expect((await datos(page, 'AgregarParticipante', `token=${token}&rol=aprobadores`, { usuarioId: yo.usuarioId, orden: null })).status).toBe(422);
    expect((await datos(page, 'Enviar', 'token=' + token, {})).status).toBe(200);
    x = (await datos(page, 'Decision', 'token=' + token)).json;
    expect(x.decision.estado).toBe('En aprobación');
    expect(x.huella).toMatch(/^[0-9A-F]{64}$/);
    expect(x.integra).toBe(true);
    // Enviada: los notificados se agregan pero ya no se quitan
    expect((await datos(page, 'QuitarParticipante', `token=${token}&rol=notificados&usuarioId=${otros[1].usuarioId}`, {})).status).toBe(409);
    const eventos = x.eventos.map((e: any) => e.evento);
    expect(eventos).toEqual(expect.arrayContaining(['Registro', 'Aprobadores del tipo', 'Notificado agregado', 'Envío a aprobación', 'Consulta']));
    await page.reload();
    await expect(page.locator('#content')).toContainText(x.decision.codigo);
  });

  test('el PDF lleva el detalle y está firmado digitalmente @DEC-04', async ({ app, page }) => {
    await app.ir('Decisiones', 'Registro de decisiones');
    const { yo, otros } = await usuarios(page);
    test.skip(otros.length < 1, 'Se necesita otro usuario activo');
    const token = await nueva(page, await tipo(page, [otros[0].usuarioId, yo.usuarioId]), []);
    const r = await page.request.get(`/Decisiones/Datos/Pdf?token=${token}&descargar=true`);
    expect(r.status()).toBe(200);
    expect(r.headers()['content-type']).toContain('application/pdf');
    const pdf = (await r.body()).toString('latin1');
    expect(pdf).toContain('/ByteRange'); // firma digital del PDF
    const eventos = (await datos(page, 'Decision', 'token=' + token)).json.eventos.map((e: any) => e.evento);
    expect(eventos).toContain('Exportación PDF');
  });

  test('los archivos se agregan en borrador, quedan fijos al enviarla y su descarga queda en la trazabilidad @DEC-08', async ({ app, page }) => {
    await app.ir('Decisiones', 'Registro de decisiones');
    const { yo, otros } = await usuarios(page);
    test.skip(otros.length < 1, 'Se necesita otro usuario activo');
    const token = await nueva(page, await tipo(page, [otros[0].usuarioId, yo.usuarioId]), []);
    const subir = (nombre: string, contenido: string) => page.evaluate(async ([t, n, c]) => {
      const fd = new FormData(); fd.append('archivo', new Blob([c], { type: 'text/plain' }), n);
      const r = await fetch('/Decisiones/Datos/SubirArchivo?token=' + t, { method: 'POST', headers: { RequestVerificationToken: (window as any).pb.token() }, body: fd });
      return r.status;
    }, [token, nombre, contenido] as const);
    expect(await subir('acta-comite.txt', 'Acta del comité: se aprueba la propuesta.')).toBe(200);
    expect(await subir('acta-comite.txt', 'otra vez')).toBe(409); // mismo nombre
    let x = (await datos(page, 'Decision', 'token=' + token)).json;
    expect(x.adjuntos.map((a: any) => a.nombre)).toEqual(['acta-comite.txt']);
    expect(x.adjuntos[0].hash).toMatch(/^[0-9A-F]{64}$/);
    expect((await page.request.get('/Decisiones/Datos/Archivo?token=' + x.adjuntos[0].token)).status()).toBe(200);
    expect((await datos(page, 'Enviar', 'token=' + token, {})).status).toBe(200);
    expect(await subir('otro.txt', 'tarde')).toBe(409); // enviada: los archivos quedan fijos
    expect((await datos(page, 'QuitarArchivo', 'token=' + x.adjuntos[0].token, {})).status).toBe(409);
    x = (await datos(page, 'Decision', 'token=' + token)).json;
    expect(x.integra).toBe(true);
    expect(x.eventos.map((e: any) => e.evento)).toEqual(expect.arrayContaining(['Archivo agregado', 'Descarga de archivo']));
  });

  test('el aprobador firma con su clave; si modifica, nace una versión nueva y todos vuelven a firmar @DEC-03 @DEC-07', async ({ app, page, browser, baseURL }) => {
    test.skip(!APROBADOR || !CLAVE, 'Defina PROBANK_E2E_APROBADOR y PROBANK_E2E_APROBADOR_CLAVE');
    await app.ir('Decisiones', 'Registro de decisiones');
    const { todos } = await usuarios(page);
    const apr = todos.find(u => u.usuario === APROBADOR);
    test.skip(!apr, 'El aprobador no es un usuario activo');
    const token = await nueva(page, await tipo(page, [apr.usuarioId]), []);
    expect((await datos(page, 'Enviar', 'token=' + token, {})).status).toBe(200);

    const a = await entrarComo(browser, baseURL!, APROBADOR!, CLAVE!);
    expect((await datos(a, 'Firmar', 'token=' + token, { aprobar: true, clave: 'clave-incorrecta' })).status).toBe(422);
    const x = (await datos(a, 'Decision', 'token=' + token)).json;
    const m = await datos(a, 'Modificar', 'token=' + token, { titulo: x.decision.titulo, descripcion: x.descripcion + '\nAclaración: $ 1.200.', area: 'Pruebas',
      fecha: hoy(), motivo: 'e2e: se corrige el monto', clave: CLAVE });
    expect(m.status).toBe(200);
    let d = (await datos(a, 'Decision', 'token=' + token)).json;
    expect(d.decision.version).toBe(2);
    expect(d.aprobadores.every((f: any) => f.resultado === 'Pendiente')).toBe(true);
    expect((await datos(a, 'Firmar', 'token=' + token, { aprobar: true, clave: CLAVE, comentario: 'Conforme' })).status).toBe(200);
    d = (await datos(a, 'Decision', 'token=' + token)).json;
    expect(d.decision.estado).toBe('Aprobada');
    expect(d.aprobadores[0].selloValido).toBe(true);
    expect(d.versiones).toHaveLength(2);
    // Ya no espera su firma: la encuentra en «Firmadas por mí» con su resultado
    expect(((await datos(a, 'Decisiones', 'alcance=pendientes')).json as any[]).some(x => x.token === token)).toBe(false);
    const firmada = ((await datos(a, 'Decisiones', 'alcance=firmadas')).json as any[]).find(x => x.token === token);
    expect(firmada).toMatchObject({ miResultado: 'Aprobada', miVersionFirma: 2 });
    // Ya hubo firmas: los aprobadores no se quitan
    expect((await datos(page, 'QuitarParticipante', `token=${token}&rol=aprobadores&usuarioId=${apr.usuarioId}`, {})).status).toBe(409);
    await a.context().close();
  });
});
