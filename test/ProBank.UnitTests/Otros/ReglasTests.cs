using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Domain.Modules.Versiones;

namespace ProBank.UnitTests.Otros;

public sealed class ReglasTests
{
    [Fact(DisplayName = "VER-02 · versión semántica")]
    public void SemVer()
    {
        Assert.True(NumeroVersion.TryParse("v2.10.3", out var v));
        Assert.True(v > new NumeroVersion(2, 9, 99));
        Assert.False(NumeroVersion.TryParse("2.10", out _));
    }

    [Fact(DisplayName = "VER-04 · dependencia con versión mínima")]
    public void Dependencia() => Assert.False(ReglasVersion.CumpleDependencia(new(1, 4, 0), new(1, 5, 0)));

    [Fact(DisplayName = "VER-03 · no se despliega una versión que no está aprobada")]
    public void Transiciones() => Assert.False(ReglasVersion.PuedePasarA(EstadoVersion.EnPruebas, EstadoVersion.Desplegada));

    [Fact(DisplayName = "SQL-07 · mensual sin día = último día del mes")]
    public void ProgramacionMensual()
    {
        var p = new Programacion(1, "QRY-001", 1, Frecuencia.Mensual, new TimeOnly(7, 0), [], null, "xlsx", ["a@banco.ec"], new Dictionary<string, string?>(), 1);
        Assert.Equal(new DateTime(2026, 10, 31, 7, 0, 0), CalendarioProgramacion.Proxima(p, new DateTime(2026, 10, 30, 9, 0, 0)));
        Assert.Equal(new DateTime(2026, 11, 30, 7, 0, 0), CalendarioProgramacion.Proxima(p, new DateTime(2026, 10, 31, 8, 0, 0)));
    }
}
