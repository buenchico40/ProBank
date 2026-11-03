using ProBank.Application.Modules.Portafolio;
using ProBank.Contracts.Portafolio;
using ProBank.Domain.Modules.Portafolio;

namespace ProBank.UnitTests.Portafolio;

public sealed class PortafolioTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 30);

    [Fact(DisplayName = "PRY-02 · el ítem necesita al menos un responsable")]
    public void Responsables()
    {
        Assert.Null(ReglasItem.Validar("Proyecto", "Portal de clientes", [4, 7], "Borrador"));
        Assert.Equal("responsables", ReglasItem.Validar("Mejora", "Reporte", [], "Pendiente")!.Code);
        Assert.Equal("tipo", ReglasItem.Validar("Épica", "X", [1], "Pendiente")!.Code);
        Assert.Equal("estado", ReglasItem.Validar("Bug", "X", [1], "Cerrado a medias")!.Code);
    }

    [Fact(DisplayName = "Completar deja el avance en 100 % y solo se completa una tarea en curso")]
    public void Completar()
    {
        var t = Tarea.Crear(1, null, "Validar despliegue", "Servidores", 4, 1, Hoy, Hoy.AddDays(3), [9], 1, Hoy).Value;
        Assert.True(t.Completar().IsFailure);
        t.EnviarAAprobar();
        t.Aprobar(9, Hoy.ToDateTime(TimeOnly.MinValue));
        Assert.True(t.Completar().IsSuccess);
        Assert.Equal((EstadoTarea.Completada, 100), (t.Estado, t.Avance));
    }

    [Fact(DisplayName = "PRY-04 · quien crea la tarea no la aprueba")]
    public void CreadorNoAprueba()
    {
        var t = Tarea.Crear(1, null, "Ajuste en core", "Core", 4, 1, Hoy, Hoy.AddDays(3), [1, 9], 1, Hoy).Value;
        t.EnviarAAprobar();
        Assert.Equal("forbidden", t.Aprobar(1, Hoy.ToDateTime(TimeOnly.MinValue)).Error.Code);
    }

    private static InformeDatos DatosInforme() => new(
        [new ResumenInformeDto("PRY-001", "Portal", "Proyecto", "En curso", 4, 1, 25, 2, 2, 1, 1),
         new ResumenInformeDto("BUG-002", "Error de saldo", "Bug", "En curso", 2, 2, 100, 1, 1, 0, 0)],
        [new BitacoraFila(Hoy.AddDays(-4).ToDateTime(new TimeOnly(9, 30)), "PRY-001", "TAR-0001", "Migrar", "Rechazo", "falta respaldo", "Ana"),
         new BitacoraFila(Hoy.AddDays(-4).ToDateTime(new TimeOnly(11, 0)), "PRY-001", "TAR-0001", "Migrar", "Envío a aprobación", null, "Ana"),
         new BitacoraFila(Hoy.ToDateTime(new TimeOnly(8, 0)), "BUG-002", "TAR-0002", "Corregir", "Envío a aprobación", null, "Luis")],
        [new AcuerdoInformeFila(Hoy.ToDateTime(TimeOnly.MinValue), "PRY-001", "ACU-0001", "Publicar en pruebas", "Luis", Hoy.AddDays(-1).ToDateTime(TimeOnly.MinValue), "Vencido"),
         new AcuerdoInformeFila(Hoy.ToDateTime(TimeOnly.MinValue), "PRY-001", "ACU-0002", "Revisar respaldo", "Ana", Hoy.ToDateTime(TimeOnly.MinValue), "Cumplido")],
        []);

    [Fact(DisplayName = "Informe de seguimiento · indicadores del periodo")]
    public void InformeIndicadores()
    {
        var i = new InformeContenidoDto(1, "INF-0001", "Semanal", Hoy.AddDays(-4), Hoy, "Borrador", false);
        var m = InformeModelo.Armar(i, DatosInforme(), "Gerencia & TI", Hoy.ToDateTime(new TimeOnly(18, 0)));
        Assert.Equal("Informe semanal de seguimiento", m.Titulo);
        Assert.Equal("Del 26/10/2026 al 30/10/2026", m.Periodo);
        Assert.Equal("Generado el 30/10/2026 18:00 por Gerencia & TI", m.Generado);
        Assert.Equal(["2", "50 %", "3", "2"], m.Indicadores.Select(k => k.Valor));
        Assert.Equal("3 de 6 tareas completadas", m.Indicadores[1].Nota);
        Assert.Equal("1 cumplidos · 1 vencidos", m.Indicadores[3].Nota);
    }

    [Fact(DisplayName = "Informe de seguimiento · series de los gráficos")]
    public void InformeGraficos()
    {
        var i = new InformeContenidoDto(1, "INF-0001", "Semanal", Hoy.AddDays(-4), Hoy, "Borrador", false);
        var m = InformeModelo.Armar(i, DatosInforme(), "Ana", Hoy.ToDateTime(TimeOnly.MinValue));
        Assert.Equal(["BUG-002", "PRY-001"], m.AvancePorItem.Select(x => x.Item));      // de mayor a menor avance
        Assert.Equal(3, m.AvancePorItem[1].Pendientes);
        Assert.Equal(["Cumplido", "Vencido"], m.AcuerdosPorEstado.Select(x => x.Nombre));
        Assert.Equal([2, 0, 0, 0, 1], m.MovimientosPorDia.Select(x => x.Cantidad));       // un punto por día, con ceros
        Assert.Equal(("Envío a aprobación", 2), (m.EventosPorTipo[0].Nombre, m.EventosPorTipo[0].Cantidad));
    }
}
