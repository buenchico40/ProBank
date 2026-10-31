using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.ConsultasDinamicas;

namespace ProBank.UnitTests.ConsultasDinamicas;

public sealed class VariablesParametroTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 30); // viernes

    [Theory(DisplayName = "SQL-04 · variables de fecha")]
    [InlineData("Hoy", "2026-10-30")]
    [InlineData("Ayer", "2026-10-29")]
    [InlineData("Inicio de semana", "2026-10-26")]
    [InlineData("Fin de semana", "2026-11-01")]
    [InlineData("Inicio de mes", "2026-10-01")]
    [InlineData("Fin de mes", "2026-10-31")]
    [InlineData("Inicio del mes anterior", "2026-09-01")]
    [InlineData("Fin del mes anterior", "2026-09-30")]
    [InlineData("Inicio de trimestre", "2026-10-01")]
    [InlineData("Fin de trimestre", "2026-12-31")]
    [InlineData("Inicio de año", "2026-01-01")]
    [InlineData("Fin de año", "2026-12-31")]
    public void Fechas(string variable, string esperado) =>
        Assert.Equal(DateOnly.Parse(esperado), VariablesParametro.Resolver(variable, Hoy));

    [Fact(DisplayName = "SQL-04 · fin del mes anterior en marzo de año bisiesto")]
    public void Bisiesto() => Assert.Equal(new DateOnly(2028, 2, 29), VariablesParametro.Resolver("Fin del mes anterior", new DateOnly(2028, 3, 15)));

    [Fact(DisplayName = "SQL-04 · variables numéricas")]
    public void Numeros()
    {
        Assert.Equal(2026, VariablesParametro.Resolver("Año actual", Hoy));
        Assert.Equal(2025, VariablesParametro.Resolver("Año anterior", Hoy));
        Assert.Equal(10, VariablesParametro.Resolver("Mes actual", Hoy));
    }

    [Fact(DisplayName = "SQL-04 · el texto auditado muestra la variable y su valor")]
    public void TextoAuditado()
    {
        var def = new[] { new ParametroConsulta("@FechaCorte", "Fecha de corte", TipoParametro.Fecha, true, "Fin del mes anterior") };
        var r = ResolverParametros.Resolver(def, [], Hoy);
        Assert.True(r.IsSuccess);
        Assert.Equal("Fecha de corte=Fin del mes anterior → 30/09/2026", r.Value.Texto);
    }

    [Fact(DisplayName = "SQL-01 · solo nombres de procedimiento de dos partes")]
    public void Procedimientos()
    {
        Assert.True(Consulta.EsProcedimientoValido("rpt.usp_SaldosPorProducto"));
        Assert.False(Consulta.EsProcedimientoValido("SELECT * FROM x"));
        Assert.False(Consulta.EsProcedimientoValido("rpt.usp_X; DROP TABLE y"));
        Assert.False(Consulta.EsProcedimientoValido("usp_SinEsquema"));
    }

    [Fact(DisplayName = "SQL-03 · quien solicita no aprueba su propia ejecución")]
    public void Segregacion()
    {
        var c = new CircuitoAprobacion([7, 8], solicitanteId: 7);
        Assert.Equal("forbidden", c.Aprobar(7, DateTime.Now).Error.Code);
    }
}
