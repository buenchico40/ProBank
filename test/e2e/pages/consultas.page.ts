import { expect, type App } from '../fixtures/probank';

export class ConsultasPage {
  constructor(private readonly app: App) {}
  async abrirCatalogo() { await this.app.ir('Consultas dinámicas', 'Catálogo de consultas'); }
  async abrirAuditoria() { await this.app.ir('Consultas dinámicas', 'Auditoría de ejecuciones'); }

  async ejecutar(consulta: string) {
    const { dx } = this.app;
    await this.abrirCatalogo();
    await dx.seleccionarFila(consulta);
    await dx.boton('Ejecutar consulta').click();
    const popup = dx.popup('Ejecutar consulta');
    await expect(popup).toContainText(/Parámetros de/i); // el formulario terminó de armarse
    return popup;
  }
}
