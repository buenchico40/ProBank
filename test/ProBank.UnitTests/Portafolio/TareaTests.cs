using ProBank.Domain.Modules.Portafolio;

namespace ProBank.UnitTests.Portafolio;

public sealed class TareaTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 30);
    private static readonly DateTime Ahora = new(2026, 10, 30, 10, 0, 0);
    private const int Creador = 1, Tecnico = 4, A = 2, B = 3;

    private static Tarea Nueva(params int[] aprobadores) =>
        Tarea.Crear(10, null, "Migrar procedimientos", "Base de datos", Tecnico, 1, Hoy, Hoy.AddDays(20), aprobadores, Creador, Hoy).Value;

    [Fact(DisplayName = "PRY-04 · sin aprobadores pasa directo a En curso")]
    public void SinAprobadores()
    {
        var t = Nueva();
        Assert.True(t.EnviarAAprobar().IsSuccess);
        Assert.Equal(EstadoTarea.EnCurso, t.Estado);
    }

    [Fact(DisplayName = "PRY-04 · con dos aprobadores necesita ambos, en cualquier orden")]
    public void DosAprobadores()
    {
        var t = Nueva(A, B);
        t.EnviarAAprobar();
        Assert.True(t.Aprobar(B, Ahora).IsSuccess);
        Assert.Equal(EstadoTarea.EnRevision, t.Estado);
        Assert.True(t.Aprobar(A, Ahora).IsSuccess);
        Assert.Equal(EstadoTarea.EnCurso, t.Estado);
    }

    [Fact(DisplayName = "PRY-04 · quien no es aprobador no puede aprobar")]
    public void NoAprobador() { var t = Nueva(A); t.EnviarAAprobar(); Assert.Equal("forbidden", t.Aprobar(B, Ahora).Error.Code); }

    [Fact(DisplayName = "PRY-04 · un aprobador no aprueba dos veces")]
    public void DobleAprobacion()
    {
        var t = Nueva(A, B); t.EnviarAAprobar(); t.Aprobar(A, Ahora);
        Assert.Equal("ya_decidio", t.Aprobar(A, Ahora).Error.Code);
    }

    [Fact(DisplayName = "PRY-05 · el rechazo exige comentario y devuelve la tarea al técnico")]
    public void Rechazo()
    {
        var t = Nueva(A); t.EnviarAAprobar();
        Assert.Equal("comentario_obligatorio", t.Rechazar(A, "  ", Ahora).Error.Code);
        Assert.True(t.Rechazar(A, "Falta respaldo previo", Ahora).IsSuccess);
        Assert.Equal(EstadoTarea.Pendiente, t.Estado);
    }

    [Fact(DisplayName = "PRY-03 · no se crea sin título, con fecha pasada, con fin antes del inicio ni con un área desconocida")]
    public void Validaciones()
    {
        Assert.True(Tarea.Crear(1, null, "", "Core", Tecnico, 1, Hoy, Hoy, [], Creador, Hoy).IsFailure);
        Assert.True(Tarea.Crear(1, null, "X", "Core", Tecnico, 1, Hoy.AddDays(-5), Hoy.AddDays(-1), [], Creador, Hoy).IsFailure);
        Assert.True(Tarea.Crear(1, null, "X", "Core", Tecnico, 1, Hoy.AddDays(5), Hoy.AddDays(2), [], Creador, Hoy).IsFailure);
        Assert.True(Tarea.Crear(1, null, "X", "Contabilidad", Tecnico, 1, Hoy, Hoy, [], Creador, Hoy).IsFailure);
    }

    [Fact(DisplayName = "PRY-06 · avance = completadas ÷ total")]
    public void Avance()
    {
        var t1 = Nueva(); t1.EnviarAAprobar(); t1.Completar();
        var t2 = Nueva();
        Assert.Equal(50m, AvanceItem.Calcular([t1, t2]));
        Assert.Equal(0m, AvanceItem.Calcular([]));
    }
}
