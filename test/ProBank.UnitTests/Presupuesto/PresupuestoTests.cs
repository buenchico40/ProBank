using ProBank.Domain.Modules.Presupuesto;

namespace ProBank.UnitTests.Presupuesto;

public sealed class PresupuestoTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 2);
    private static readonly Guid A = Guid.NewGuid(), B = Guid.NewGuid();

    [Theory(DisplayName = "COM-03 · la renovación avisa dentro de sus días de aviso")]
    [InlineData("Activo", "2027-03-01", 60, "Vigente")]
    [InlineData("Activo", "2026-11-30", 60, "Por vencer")]
    [InlineData("Activo", "2026-11-02", 15, "Por vencer")]
    [InlineData("Activo", "2026-10-31", 15, "Vencido")]
    [InlineData("Requerimiento enviado", "2026-11-10", 30, "Requerimiento enviado")]
    [InlineData("Retirado", "2026-10-01", 30, "Retirado")]
    public void EstadoRenovacion(string estado, string vence, int aviso, string esperado) =>
        Assert.Equal(esperado, ReglasPresupuesto.EstadoRenovacion(estado, DateOnly.Parse(vence), aviso, Hoy));

    [Fact(DisplayName = "COM-04 · ejecución = (pagado + comprometido) ÷ (asignado + ajustes)")]
    public void Ejecucion()
    {
        Assert.Equal(32, ReglasPresupuesto.Ejecucion(120_000, 10_000, 24_000, 18_000));
        Assert.Equal(12, ReglasPresupuesto.Ejecucion(60_000, -10_000, 0, 6_000));
        Assert.Equal(0, ReglasPresupuesto.Ejecucion(0, 0, 0, 0));
    }

    [Fact(DisplayName = "Movimientos · reasignación entre partidas distintas; incremento solo destino; reducción solo origen")]
    public void Movimiento()
    {
        Assert.Null(ReglasPresupuesto.ValidarMovimiento("Reasignación", A, B, 10_000, "Saldo no utilizado"));
        Assert.Null(ReglasPresupuesto.ValidarMovimiento("Incremento", null, B, 15_000, "Comité"));
        Assert.Null(ReglasPresupuesto.ValidarMovimiento("Reducción", A, null, 5_000, "Recorte"));
        Assert.Equal("partidas", ReglasPresupuesto.ValidarMovimiento("Reasignación", A, A, 10, "X")!.Code);
        Assert.Equal("partidas", ReglasPresupuesto.ValidarMovimiento("Incremento", A, B, 10, "X")!.Code);
        Assert.Equal("partidas", ReglasPresupuesto.ValidarMovimiento("Reducción", null, B, 10, "X")!.Code);
        Assert.Equal("tipo", ReglasPresupuesto.ValidarMovimiento("Traspaso", A, B, 10, "X")!.Code);
        Assert.Equal("motivo", ReglasPresupuesto.ValidarMovimiento("Incremento", null, B, 10, " ")!.Code);
        Assert.Equal("monto", ReglasPresupuesto.ValidarMovimiento("Incremento", null, B, 0, "X")!.Code);
    }

    [Fact(DisplayName = "COM-02 · presupuestado lleva partida; no presupuestado no lleva")]
    public void Imputacion()
    {
        Assert.Null(ReglasPresupuesto.ValidarSolicitud("Licencias", "Aplicaciones", null, true, A, 48_000));
        Assert.Null(ReglasPresupuesto.ValidarSolicitud("Monitoreo", "Soporte de aplicaciones", "Proveedor", false, null, 18_500));
        Assert.Equal("partida", ReglasPresupuesto.ValidarSolicitud("X", "Redes", null, true, null, 1)!.Code);
        Assert.Equal("partida", ReglasPresupuesto.ValidarSolicitud("X", "Redes", null, false, A, 1)!.Code);
        Assert.Equal("area", ReglasPresupuesto.ValidarSolicitud("X", "Marketing", null, false, null, 1)!.Code);
        Assert.Equal("monto", ReglasPresupuesto.ValidarSolicitud("X", "Redes", null, false, null, 10.005m)!.Code);
        Assert.Null(ReglasPresupuesto.ValidarRenovacion("Antivirus", "Proveedor C", 9_800, true, A, 120));
        Assert.Equal("aviso", ReglasPresupuesto.ValidarRenovacion("Antivirus", "Proveedor C", 9_800, true, A, 0)!.Code);
    }

    [Fact(DisplayName = "COM-04 · la distribución del pago no repite partidas ni supera el monto; lo no presupuestado no se distribuye")]
    public void Distribucion()
    {
        Assert.Null(ReglasPresupuesto.ValidarPago("Proveedor A", 24_000, true, [(A, 18_000), (B, 6_000)]));
        Assert.Null(ReglasPresupuesto.ValidarPago("Proveedor A", 24_000, true, [(A, 10_000)])); // parcial: se completa antes de pagar
        Assert.Equal("distribucion", ReglasPresupuesto.ValidarPago("P", 24_000, true, [(A, 18_000), (A, 6_000)])!.Code);
        Assert.Equal("distribucion", ReglasPresupuesto.ValidarPago("P", 24_000, true, [(A, 30_000)])!.Code);
        Assert.Equal("distribucion", ReglasPresupuesto.ValidarPago("P", 24_000, false, [(A, 1_000)])!.Code);
        Assert.True(ReglasPresupuesto.DistribucionCuadra(24_000, [18_000, 6_000]));
        Assert.False(ReglasPresupuesto.DistribucionCuadra(24_000, [18_000]));
    }

    [Fact(DisplayName = "COM-01 · la partida exige año, área, rubro y asignado de 0 o más")]
    public void Partida()
    {
        Assert.Null(ReglasPresupuesto.ValidarPartida(2027, "TI", "Licencias de software", 126_000));
        Assert.Null(ReglasPresupuesto.ValidarPartida(2027, "MKT", "Nuevo rubro", 0));
        Assert.Equal("rubro", ReglasPresupuesto.ValidarPartida(2027, "TI", "", 1)!.Code);
        Assert.Equal("anio", ReglasPresupuesto.ValidarPartida(1999, "TI", "X", 1)!.Code);
        Assert.Equal("monto", ReglasPresupuesto.ValidarPartida(2027, "TI", "X", -1)!.Code);
        Assert.Equal("area", ReglasPresupuesto.ValidarPartida(2027, " ", "X", 1)!.Code);
        Assert.Equal("area", ReglasPresupuesto.ValidarPartida(2027, null, "X", 1)!.Code);
    }

    [Theory(DisplayName = "Áreas del presupuesto · código de 2 a 30 letras mayúsculas o números y nombre obligatorio")]
    [InlineData("TI", "Tecnología de la información", null)]
    [InlineData("MKT2", "Marketing", null)]
    [InlineData("T", "Muy corto", "codigo")]
    [InlineData("ti", "Minúsculas", "codigo")]
    [InlineData("OPE RA", "Con espacio", "codigo")]
    [InlineData("OPERACIÓN", "Con tilde", "codigo")]
    [InlineData("OPE", "", "nombre")]
    public void Area(string codigo, string nombre, string? error) =>
        Assert.Equal(error, ReglasPresupuesto.ValidarArea(codigo, nombre)?.Code);
}
