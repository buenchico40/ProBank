import type { Page } from '@playwright/test';
import { test, expect } from '../fixtures/probank';
import { ConsultasPage } from '../pages/consultas.page';

// ---------- Maqueta (datos de ejemplo del prototipo) ----------
test.describe('Consultas dinámicas · maqueta', () => {
  test.beforeEach(({ app }) => test.skip(!app.esPrototipo, 'Usa las consultas de ejemplo de la maqueta'));

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

// ---------- Aplicación real ----------
// La base de prueba usa un secreto de Key Vault (en desarrollo, Secretos:<nombre> en user-secrets) con la cadena de ProBank.
const SECRETO = process.env.PROBANK_E2E_SECRETO ?? 'cnx-probank';

// Llama al puente /ConsultasDinamicas/Datos (con el antiforgery de la página) para preparar datos; las pantallas se verifican aparte
async function datos(page: Page, accion: string, query = '', body?: unknown) {
  return page.evaluate(async ([a, q, b]) => {
    const r = await fetch('/ConsultasDinamicas/Datos/' + a + (q ? '?' + q : ''), b === undefined ? {} :
      { method: 'POST', headers: { 'Content-Type': 'application/json', RequestVerificationToken: (window as any).pb.token() }, body: JSON.stringify(b) });
    const t = await r.text();
    let json: any = t; try { json = t ? JSON.parse(t) : null; } catch { /* texto */ }
    return { status: r.status, json };
  }, [accion, query, body] as const);
}

/** Base «ProBank (e2e)» y la consulta indicada, publicadas; se reutilizan entre corridas. Omite la prueba si el secreto no está. */
async function consultaPublicada(page: Page, nombre: string, procedimiento: string, aprobadores: string[] = []) {
  let cx = ((await datos(page, 'Conexiones')).json as any[]).find(c => c.nombre === 'ProBank (e2e)')?.codigo;
  if (!cx) cx = (await datos(page, 'GuardarConexion', '', { nombre: 'ProBank (e2e)', servidor: 'probank', baseDatos: 'ProBank',
    autenticacion: 'Identidad administrada (Entra ID)', secretoKeyVault: SECRETO, estado: 'Activo' })).json;
  const prueba = (await datos(page, 'ProbarConexion', 'codigo=' + cx, {})).json;
  test.skip(!prueba?.exitosa, 'El secreto ' + SECRETO + ' no está disponible en este ambiente');
  let q = ((await datos(page, 'Catalogo')).json as any[]).find(c => c.nombre === nombre);
  const cuerpo = { nombre, procedimiento, tiempoMaximoSeg: 30, limiteFilas: 1000, conexiones: [cx], roles: ['Administrador'], aprobadores };
  const g = await datos(page, 'GuardarConsulta', q ? 'codigo=' + q.codigo : '', cuerpo);
  expect(g.status).toBe(200);
  const codigo = q?.codigo ?? /QRY-\d+/.exec(g.json)![0];
  expect((await datos(page, 'LeerDefinicion', `codigo=${codigo}&conexion=${cx}`, {})).status).toBe(200);
  return { cx, codigo };
}

test.describe('Consultas dinámicas', () => {
  test.beforeEach(({ app }) => test.skip(app.esPrototipo, 'Requiere el ambiente con base de datos'));

  test('solo procedimientos almacenados y la cadena de conexión nunca viaja al navegador @SQL-01 @SQL-02', async ({ app, page }) => {
    await app.ir('Consultas dinámicas', 'Bases de datos');
    const malo = await datos(page, 'GuardarConexion', '', { nombre: 'Mala', servidor: 's', baseDatos: 'b', autenticacion: 'Usuario SQL de solo lectura',
      secretoKeyVault: 'Server=x;Password=y', estado: 'Activo' });
    expect(malo.status).toBe(422);
    const lista = await datos(page, 'Conexiones');
    expect(JSON.stringify(lista.json)).not.toMatch(/Password=|Server=/i);
    await expect(page.locator('#content')).toContainText(/en Key Vault|No hay registros/);
    const sql = await datos(page, 'GuardarConsulta', '', { nombre: 'Texto libre', procedimiento: 'SELECT * FROM seg.Usuario', tiempoMaximoSeg: 30, limiteFilas: 10,
      conexiones: [], roles: [], aprobadores: [] });
    expect(sql.status).toBe(422);
  });

  test('las variables se resuelven al ejecutar, el resultado se exporta y todo queda en la auditoría @SQL-04 @SQL-05 @SQL-06', async ({ app, page }) => {
    await app.ir('Consultas dinámicas', 'Catálogo de consultas');
    const { codigo } = await consultaPublicada(page, 'e2e · Pagos a proveedores', 'rpt.usp_Pagos_PorProveedor');
    const d = (await datos(page, 'Consulta', 'codigo=' + codigo)).json;
    const parametros = d.parametros.map((p: any) => ({ ...p, valorDefecto: p.nombre === '@Desde' ? 'Inicio de año' : p.nombre === '@Hasta' ? 'Hoy' : null,
      obligatorio: p.nombre !== '@Proveedor' }));
    expect((await datos(page, 'GuardarParametros', 'codigo=' + codigo, { parametros })).status).toBe(200);
    if (d.estado !== 'Publicada') expect((await datos(page, 'EstadoConsulta', `codigo=${codigo}&accion=Publicar`, {})).status).toBe(200);

    await page.reload();
    const popup = await new ConsultasPage(app).ejecutar(codigo);
    await expect(app.dx.campo(popup, 'Desde (@Desde)').locator('input.dx-texteditor-input').first()).toHaveValue('Inicio de año');
    await popup.getByRole('button', { name: 'Ejecutar', exact: true }).click();
    const res = app.dx.popup('Resultados');
    await expect(res).toContainText(/Desde=Inicio de año → 01\/01\/\d{4}; Hasta=Hoy → \d{2}\/\d{2}\/\d{4}/);
    const ejecucion = /EJE-\d+/.exec(await res.locator('.dx-popup-title').textContent() ?? '')![0];

    for (const formato of ['csv', 'xlsx', 'pdf']) {
      const r = await page.request.get(`/ConsultasDinamicas/Datos/Exportar?codigo=${ejecucion}&formato=${formato}`);
      expect(r.status(), formato).toBe(200);
      expect(r.headers()['content-disposition']).toContain(`${codigo}_${ejecucion}.${formato}`);
    }
    const hoy = new Date(), dia = `${hoy.getFullYear()}-${String(hoy.getMonth() + 1).padStart(2, '0')}-${String(hoy.getDate()).padStart(2, '0')}`;
    const eventos = ((await datos(page, 'Auditoria', `desde=${dia}&hasta=${dia}`)).json as any[]).filter(e => e.ejecucion === ejecucion).map(e => e.evento);
    expect(eventos).toEqual(expect.arrayContaining(['Solicitud', 'Ejecución', 'Exportación CSV', 'Exportación XLSX', 'Exportación PDF']));
  });

  test('con aprobadores queda pendiente y quien la solicita no la aprueba ni la ejecuta antes @SQL-03', async ({ app, page }) => {
    await app.ir('Consultas dinámicas', 'Ejecuciones y aprobaciones');
    const opciones = (await datos(page, 'Opciones')).json;
    const yo = await page.evaluate(() => (window as any).pb.sesion.nombre as string);
    const otro = opciones.usuarios.find((u: any) => u.nombre !== yo);
    test.skip(!otro, 'Se necesita otro usuario activo como aprobador');
    const { cx, codigo } = await consultaPublicada(page, 'e2e · Coactivas por entidad', 'rpt.usp_Coactivas_PorEntidad', [otro.token]);
    const d = (await datos(page, 'Consulta', 'codigo=' + codigo)).json;
    // Los parámetros leídos nacen obligatorios: sin entidad ni estado, el reporte trae todo
    const parametros = d.parametros.map((p: any) => ({ ...p, obligatorio: false }));
    expect((await datos(page, 'GuardarParametros', 'codigo=' + codigo, { parametros })).status).toBe(200);
    if (d.estado !== 'Publicada') expect((await datos(page, 'EstadoConsulta', `codigo=${codigo}&accion=Publicar`, {})).status).toBe(200);
    const r = await datos(page, 'Ejecutar', '', { consultaCodigo: codigo, conexion: cx, parametros: [] });
    expect(r.json.estado).toBe('Pendiente de aprobación');
    expect((await datos(page, 'Resolver', 'codigo=' + r.json.ejecucion, { aprobar: true })).status).toBe(422);
    expect((await datos(page, 'EjecutarAprobada', 'codigo=' + r.json.ejecucion, {})).json.detail).toContain('Faltan aprobaciones');
    expect((await datos(page, 'GuardarProgramacion', 'consulta=' + codigo, { conexion: cx, frecuencia: 'Diaria', hora: '07:00', dias: [], formato: 'csv',
      parametros: {}, destinatarios: ['a@banco.ec'], activa: true })).status).toBe(422);
    await page.reload();
    await expect(page.locator('#content')).toContainText(r.json.ejecucion);
  });
});
