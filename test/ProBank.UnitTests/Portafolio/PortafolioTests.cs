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

    [Fact(DisplayName = "El informe de seguimiento codifica los textos (sin HTML inyectado)")]
    public void InformeCodificado()
    {
        var i = new InformeContenidoDto(1, "INF-0001", "Semanal", Hoy.AddDays(-4), Hoy, "Borrador", null);
        var datos = new InformeDatos(
            [new ResumenInformeDto("PRY-001", "<script>alert(1)</script>", "Proyecto", "En curso", 4, 1, 25, 2, 1, 0, 1)],
            [new BitacoraFila(Hoy.ToDateTime(new TimeOnly(9, 30)), "PRY-001", "TAR-0001", "Migrar", "Rechazo", "<b>falta</b> respaldo", "Ana")],
            [new AcuerdoInformeFila(Hoy.ToDateTime(TimeOnly.MinValue), "PRY-001", "ACU-0001", "Publicar en pruebas", "Luis", Hoy.AddDays(-1).ToDateTime(TimeOnly.MinValue), "Vencido")],
            []);
        var html = InformeHtml.Armar(i, datos, "Gerencia & TI", Hoy.ToDateTime(new TimeOnly(18, 0)));
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&lt;b&gt;falta&lt;/b&gt;", html);
        Assert.Contains("Gerencia &amp; TI", html);
        Assert.Contains("25 % (1 de 4)", html);
        Assert.Contains("Vencido", html);
        Assert.Contains("26/10/2026", html);
    }
}
