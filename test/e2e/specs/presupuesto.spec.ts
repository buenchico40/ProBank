import type { Page } from '@playwright/test';
import { test, expect } from '../fixtures/probank';

const unico = () => Date.now().toString().slice(-6);
const hoy = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; };
const enDias = (n: number) => { const d = new Date(); d.setDate(d.getDate() + n); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; };

// Llama al puente /Presupuesto/Datos (con el antiforgery de la página) para preparar datos; las pantallas se verifican aparte
async function datos(page: Page, accion: string, query = '', body?: unknown) {
  return page.evaluate(async ([a, q, b]) => {
    const r = await fetch('/Presupuesto/Datos/' + a + (q ? '?' + q : ''), b === undefined ? {} :
      { method: 'POST', headers: { 'Content-Type': 'application/json', RequestVerificationToken: (window as any).pb.token() }, body: JSON.stringify(b) });
    const t = await r.text();
    return { status: r.status, json: t ? JSON.parse(t) : null };
  }, [accion, query, body] as const);
}

async function partidaActiva(page: Page, rubro: string, asignado: number) {
  const anio = new Date().getFullYear();
  const g = await datos(page, 'GuardarPartida', '', { anio, area: 'TI', rubro, asignado });
  expect(g.status).toBe(200);
  expect((await datos(page, 'EstadoPartida', `token=${g.json}&accion=Aprobar`, {})).status).toBe(200);
  return g.json as string;
}

// Pantallas reales de Presupuesto (la maqueta no guarda datos)
test.describe('Presupuesto', () => {
  test.beforeEach(({ app }) => test.skip(app.esPrototipo, 'Requiere el ambiente con base de datos'));

  test('el borrador del año siguiente se copia de las partidas activas y se aprueba después @COM-01', async ({ app, page }) => {
    const rubro = 'Rubro e2e ' + unico();
    await app.ir('Presupuesto', 'Presupuesto por área');
    await partidaActiva(page, rubro, 50000);
    await app.dx.boton('Preparar año siguiente').click();
    await app.dx.aviso(/borrador TI \d{4}/);
    const siguiente = (await datos(page, 'Partidas', `anio=${new Date().getFullYear() + 1}`)).json as any[];
    const copia = siguiente.find(p => p.rubro === rubro);
    expect(copia).toMatchObject({ estado: 'Borrador', area: 'TI', asignado: 50000 });
    expect((await datos(page, 'EstadoPartida', `token=${copia.token}&accion=Aprobar`, {})).status).toBe(200);
  });

  test('la solicitud presupuestada exige partida y la no presupuestada no la lleva @COM-02', async ({ app, page }) => {
    await app.ir('Presupuesto', 'Solicitudes de compra');
    const sin = await datos(page, 'GuardarSolicitud', '', { descripcion: 'Sin partida', area: 'Redes', presupuestado: true, partida: null, monto: 100 });
    expect(sin.status).toBe(422);
    const fuera = await datos(page, 'GuardarSolicitud', '', { descripcion: 'Fuera de presupuesto e2e ' + unico(), area: 'Redes', presupuestado: false, partida: null, monto: 100 });
    expect(fuera.status).toBe(200);
    await page.reload();
    await expect(page.locator('#content')).toContainText('No presupuestado');
  });

  test('la renovación avisa dentro de sus días y el requerimiento crea la solicitud @COM-03', async ({ app, page }) => {
    const servicio = 'Servicio e2e ' + unico();
    await app.ir('Presupuesto', 'Renovaciones de servicios');
    const partida = await partidaActiva(page, 'Renovaciones e2e ' + unico(), 20000);
    const r = await datos(page, 'GuardarRenovacion', '', { servicio, proveedor: 'Proveedor e2e', vence: enDias(20), montoEstimado: 5000, presupuestado: true, partida, diasAviso: 30 });
    expect(r.status).toBe(200);
    await page.reload();
    await expect(page.locator('#content')).toContainText(servicio);
    await expect(page.locator('#content')).toContainText('servicio(s) por vencer');
    await app.dx.seleccionarFila(servicio);
    await app.dx.boton('Enviar requerimiento').click();
    await page.getByRole('button', { name: 'Enviar' }).last().click();
    await app.dx.aviso(/Requerimiento SC-\d+ enviado/);
  });

  test('el pago pagado descuenta de la partida solo si su distribución cuadra @COM-04', async ({ app, page }) => {
    await app.ir('Presupuesto', 'Pagos');
    const partida = await partidaActiva(page, 'Pagos e2e ' + unico(), 30000);
    const sc = (await datos(page, 'GuardarSolicitud', '', { descripcion: 'Compra e2e', area: 'Servidores', presupuestado: true, partida, monto: 12000 })).json;
    expect((await datos(page, 'ResolverSolicitud', `token=${sc}`, { aprobar: true })).status).toBe(200);
    const pago = { solicitud: sc, proveedor: 'Proveedor e2e', fecha: hoy(), monto: 12000 };
    const pg = (await datos(page, 'GuardarPago', '', { ...pago, distribucion: [{ partida, monto: 5000 }] })).json;
    expect((await datos(page, 'Pagar', `token=${pg}`, {})).status).toBe(422);
    expect((await datos(page, 'GuardarPago', `token=${pg}`, { ...pago, distribucion: [{ partida, monto: 12000 }] })).status).toBe(200);
    expect((await datos(page, 'Pagar', `token=${pg}`, {})).status).toBe(200);
    const p = ((await datos(page, 'Partidas', `anio=${new Date().getFullYear()}`)).json as any[]).find(x => x.token === partida);
    expect(p).toMatchObject({ pagado: 12000, comprometido: 0, disponible: 18000 });
  });
});
