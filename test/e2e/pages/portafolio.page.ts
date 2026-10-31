import type { App } from '../fixtures/probank';

export class PortafolioPage {
  constructor(private readonly app: App) {}

  async abrirItems() { await this.app.ir('Portafolio', 'Proyectos, mejoras y bugs'); }
  async abrirTareas() { await this.app.ir('Portafolio', 'Mis tareas'); }
  async abrirAprobaciones() { await this.app.ir('Portafolio', 'Aprobaciones'); }

  /** PRY-03 y PRY-04: nueva tarea desde el detalle del ítem, con técnico y aprobadores. */
  async nuevaTareaEnItem(item: string, datos: { titulo: string; area: string; tecnico: string; fecha: string; aprobadores: string[] }) {
    const { dx, page } = this.app;
    await this.abrirItems();
    await dx.desplegarFila(item);
    await dx.boton('Nueva tarea').click();
    const p = dx.popup('Crear tarea');
    await dx.escribir(p, 'Título', datos.titulo);
    await dx.elegir(p, 'Área', datos.area);
    await dx.elegir(p, 'Técnico', datos.tecnico);
    await dx.escribir(p, 'Fecha de terminación', datos.fecha);
    if (datos.aprobadores.length) await dx.elegirVarios(p, 'Aprobadores', datos.aprobadores);
    await page.getByRole('button', { name: 'Crear tarea' }).click();
  }
}
