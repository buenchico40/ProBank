using ProBank.Domain.Modules.Coactivas;

namespace ProBank.UnitTests.Coactivas;

public sealed class CoactivasTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 31);

    private static FilaCarga Fila(int n, string oficio = "OF-2026-0145", string orden = "Bloqueo de fondos", string proceso = "CO-2026-0310",
        string monto = "1500", string productos = "Cuenta de ahorros, cuenta corriente", string ordenado = "", string recibido = "28/10/2026",
        string tipoId = "Cédula", string id = "0912345678") =>
        new(n, oficio, recibido, "Juzgado de ejemplo", orden, proceso, "1760000000001", tipoId, id, "Cliente de ejemplo 1", monto, productos, ordenado, "");

    [Theory(DisplayName = "COA-05 · cobertura con lo bloqueado y lo transferido")]
    [InlineData(856, 0, 0, Cobertura.SinBloqueo)]
    [InlineData(856, 400, 0, Cobertura.Parcial)]
    [InlineData(856, 456, 400, Cobertura.Total)]
    [InlineData(856, 0, 856, Cobertura.Total)]
    public void Cobertura_(int monto, int bloqueado, int transferido, Cobertura esperada) =>
        Assert.Equal(esperada, CoberturaCoactiva.Calcular(monto, bloqueado, transferido));

    [Theory(DisplayName = "COA-04 · identificación según su tipo")]
    [InlineData("Cédula", "0912345678", true)]
    [InlineData("Cédula", "912345678", false)]
    [InlineData("RUC", "1760000000001", true)]
    [InlineData("RUC", "176000000000", false)]
    [InlineData("Pasaporte", "AB123456", true)]
    [InlineData("Pasaporte", "AB-123", false)]
    [InlineData("DNI", "123", false)]
    public void Identificacion(string tipo, string id, bool valida) => Assert.Equal(valida, ReglasCoactiva.ValidarIdentificacion(tipo, id) is null);

    [Fact(DisplayName = "COA-03 · productos a bloquear: tipos del catálogo, vacío = todos")]
    public void Productos()
    {
        Assert.Equal(("Cuenta de ahorros,Cuenta corriente", (string?)null), ReglasCoactiva.NormalizarProductos("cuenta de ahorros, Cuenta corriente"));
        Assert.Equal(((string?)null, (string?)null), ReglasCoactiva.NormalizarProductos(" "));
        Assert.NotNull(ReglasCoactiva.NormalizarProductos("Tarjeta de crédito").Error);
    }

    [Fact(DisplayName = "COA-04 · fila válida del formato (montos con coma o punto decimal)")]
    public void FilaValida()
    {
        var (validas, errores) = CargaMasiva.Validar([Fila(6, monto: "1.500,50"), Fila(7, proceso: "CO-2", monto: "2,000.25")], Hoy);
        Assert.Empty(errores);
        Assert.Equal([1500.50m, 2000.25m], validas.Select(v => v.Monto!.Value));
        Assert.Equal("Cuenta de ahorros,Cuenta corriente", validas[0].Productos);
        Assert.Equal(TipoOrden.Bloqueo, validas[0].Tipo);
    }

    [Fact(DisplayName = "COA-04 · si una fila tiene errores, su oficio completo no se carga")]
    public void OficioConError()
    {
        var (validas, errores) = CargaMasiva.Validar([
            Fila(6), Fila(7, proceso: "CO-2", monto: "0"),
            Fila(8, oficio: "OF-OTRO", proceso: "CO-3")], Hoy);
        Assert.Equal(["OF-OTRO"], validas.Select(v => v.Oficio).Distinct());
        Assert.Contains(errores, e => e.Fila == 7 && e.Motivo.Contains("mayor que 0"));
    }

    [Fact(DisplayName = "COA-04 · fecha futura, orden desconocida, duplicados y montos con más de 2 decimales")]
    public void Errores()
    {
        var (_, errores) = CargaMasiva.Validar([
            Fila(6, recibido: "05/11/2026"), Fila(7, oficio: "OF-2", orden: "Embargo"),
            Fila(8, oficio: "OF-3"), Fila(9, oficio: "OF-3"),
            Fila(10, oficio: "OF-4", monto: "10.123")], Hoy);
        Assert.Contains(errores, e => e.Fila == 6 && e.Motivo.Contains("posterior"));
        Assert.Contains(errores, e => e.Fila == 7 && e.Motivo.Contains("Orden no válida"));
        Assert.Contains(errores, e => e.Fila == 9 && e.Motivo.Contains("duplicada"));
        Assert.Contains(errores, e => e.Fila == 10 && e.Motivo.Contains("2 decimales"));
    }

    [Fact(DisplayName = "COA-01 · levantamiento: monto ordenado opcional y sin productos")]
    public void Levantamiento()
    {
        var (validas, errores) = CargaMasiva.Validar([
            Fila(6, orden: "Levantamiento de bloqueo", monto: "", productos: "", ordenado: ""),
            Fila(7, oficio: "OF-2", orden: "Transferencia de fondos", monto: "", productos: "", ordenado: "250"),
            Fila(8, oficio: "OF-3", orden: "Levantamiento de bloqueo", monto: "", productos: "Inversión")], Hoy);
        Assert.Null(validas.Single(v => v.Fila == 6).MontoOrdenado);
        Assert.Equal(250m, validas.Single(v => v.Fila == 7).MontoOrdenado);
        Assert.Contains(errores, e => e.Fila == 8 && e.Motivo.Contains("solo aplica"));
    }
}
