using ProBank.Application.Modules.Presupuesto;
using ProBank.Contracts.Presupuesto;

namespace ProBank.UnitTests.Presupuesto;

public sealed class ReportePresupuestoTests
{
    private static readonly DateTime Ahora = new(2026, 11, 2, 10, 30, 0);

    private static PartidaDto Partida(string codigo, decimal asignado, decimal ajustes, decimal comprometido, decimal pagado, string estado = "Activo") =>
        new(Guid.NewGuid(), codigo, 2026, "TI", "Rubro " + codigo, asignado, ajustes, comprometido, pagado, asignado + ajustes - comprometido - pagado,
            0, estado, "Gerencia", Ahora, null, null);

    private static PagoReporte Pago(string codigo, int mes, int dia, decimal monto, string estado, bool presupuestado = true) =>
        new(codigo, new DateOnly(2026, mes, dia), "Proveedor", "SC-0001", "Compra", null, presupuestado, presupuestado ? "PRE-2026-01" : null, monto, monto, estado);

    private static ReportePresupuestoModelo Armar() => ReportePresupuestoModelo.Armar(2026, new ReportePresupuestoDatos("TI", "Tecnología de la información",
        [Partida("PRE-2026-01", 120_000, 10_000, 6_000, 18_000), Partida("PRE-2026-02", 60_000, 0, 0, 0)],
        [
            new ServicioReporte("REN-0001", "Antivirus", "Proveedor C", new DateOnly(2026, 11, 20), 9_800, true, "PRE-2026-02", "Infraestructura", "Activo", 60, 0, null),
            new ServicioReporte("REN-0002", "Correo", "Proveedor D", new DateOnly(2026, 6, 1), 5_000, false, null, null, "Activo", 30, 4_000, "SC-0002"),
        ],
        [Pago("PAG-0001", 3, 15, 18_000, "Pagado"), Pago("PAG-0002", 6, 1, 4_000, "Pagado", presupuestado: false), Pago("PAG-0003", 12, 10, 2_000, "Pendiente")]),
        "Gerencia de TI", Ahora);

    [Fact(DisplayName = "Reporte de presupuesto · indicadores: presupuestado = asignado + ajustes, ejecución y pagos sin partida")]
    public void Indicadores()
    {
        var m = Armar();
        Assert.Equal("PRE-TI-2026", m.Codigo);
        Assert.Equal("Presupuesto Tecnología de la información 2026", m.Titulo);
        var k = m.Indicadores.ToDictionary(x => x.Titulo);
        Assert.Equal("$ 190.000,00", k["Presupuestado"].Valor);
        Assert.Equal("$ 18.000,00", k["Pagado"].Valor);
        Assert.Equal("Ejecución 13 %", k["Pagado"].Nota);   // (18.000 + 6.000) ÷ 190.000
        Assert.Equal("$ 166.000,00", k["Disponible"].Valor);
        Assert.Equal("$ 4.000,00", k["No presupuestado"].Valor);
        Assert.Equal("1 pago(s) sin partida", k["No presupuestado"].Nota);
    }

    [Fact(DisplayName = "Reporte de presupuesto · pagos por mes (pagado y por pagar) y estado de servicios y pagos")]
    public void SeriesYEstados()
    {
        var m = Armar();
        Assert.Equal(12, m.PorMes.Count);
        Assert.Equal(18_000, m.PorMes[2].Pagado);
        Assert.Equal(2_000, m.PorMes[11].Programado);
        Assert.Equal(24_000, m.PorMes.Sum(x => x.Pagado + x.Programado));
        // Servicios: el antivirus vence en 18 días con 60 de aviso; el correo ya venció
        Assert.Equal(["Por vencer", "Vencido"], m.Servicios.Select(s => s.Estado));
        Assert.Equal(1_000, m.Servicios[1].Diferencia);
        // Un pago pendiente con fecha futura figura como programado
        Assert.Equal("Programado", m.Pagos.Single(p => p.Pago.Codigo == "PAG-0003").Estado);
        Assert.Equal(60_000, m.PorPartida.Single(p => p.Partida.StartsWith("PRE-2026-02", StringComparison.Ordinal)).Disponible);
    }
}
