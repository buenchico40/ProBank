using ProBank.Domain.Modules.Marketing;

namespace ProBank.UnitTests.Marketing;

public sealed class MarketingTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 2);

    [Theory(DisplayName = "MKT-01 · lo publicado se muestra como programado, vigente o vencido según su vigencia")]
    [InlineData("Publicado", "2026-11-10", "2026-11-30", "Programado")]
    [InlineData("Publicado", "2026-11-02", "2026-11-02", "Publicado")]
    [InlineData("Publicado", "2026-10-01", "2026-11-01", "Vencido")]
    [InlineData("Borrador", "2026-10-01", "2026-11-01", "Borrador")]
    [InlineData("Retirado", "2026-11-01", "2026-11-30", "Retirado")]
    public void EstadoVigente(string estado, string desde, string hasta, string esperado) =>
        Assert.Equal(esperado, ReglasMarketing.EstadoVigente(estado, DateOnly.Parse(desde), DateOnly.Parse(hasta), Hoy));

    [Fact(DisplayName = "MKT-01 · la imagen promocional valida título, ubicación, orden, vigencia y enlace https")]
    public void Promocion()
    {
        Assert.Null(ReglasMarketing.ValidarPromocion("Ahorro navideño", "Panel principal", Hoy, Hoy.AddDays(30), 1, "Todos", "Familia ahorrando", "https://banco.ec/ahorro"));
        Assert.Null(ReglasMarketing.ValidarPromocion("Sin fechas aún", "Inicio de sesión", null, null, 3, "Marketing", null, null));
        Assert.Equal("titulo", ReglasMarketing.ValidarPromocion(" ", "Panel principal", null, null, 1, "Todos", null, null)!.Code);
        Assert.Equal("ubicacion", ReglasMarketing.ValidarPromocion("X", "Pie de página", null, null, 1, "Todos", null, null)!.Code);
        Assert.Equal("orden", ReglasMarketing.ValidarPromocion("X", "Panel principal", null, null, 0, "Todos", null, null)!.Code);
        Assert.Equal("vigencia", ReglasMarketing.ValidarPromocion("X", "Panel principal", Hoy, Hoy.AddDays(-1), 1, "Todos", null, null)!.Code);
        Assert.Equal("enlace", ReglasMarketing.ValidarPromocion("X", "Panel principal", null, null, 1, "Todos", null, "javascript:alert(1)")!.Code);
        Assert.Equal("enlace", ReglasMarketing.ValidarPromocion("X", "Panel principal", null, null, 1, "Todos", null, "http://sin-tls.ec")!.Code);
        Assert.Equal("audiencia", ReglasMarketing.ValidarPromocion("X", "Panel principal", null, null, 1, "", null, null)!.Code);
    }

    [Fact(DisplayName = "MKT-02 · el comunicado exige tipo y resumen; el contacto es opcional")]
    public void Comunicado()
    {
        Assert.Null(ReglasMarketing.ValidarComunicado("Mantenimiento del core", "Mantenimiento", "El core no estará disponible el sábado.", "Mesa de servicio · Ext. 1000", Hoy, Hoy.AddDays(5), "Todos"));
        Assert.Null(ReglasMarketing.ValidarComunicado("Capacitación", "Noticia", "Sesiones para TI.", null, null, null, "Gerencia de TI"));
        Assert.Equal("tipo", ReglasMarketing.ValidarComunicado("X", "Rumor", "Resumen", null, null, null, "Todos")!.Code);
        Assert.Equal("resumen", ReglasMarketing.ValidarComunicado("X", "Aviso", "", null, null, null, "Todos")!.Code);
        Assert.Equal("resumen", ReglasMarketing.ValidarComunicado("X", "Aviso", new string('x', 301), null, null, null, "Todos")!.Code);
        Assert.Equal("contacto", ReglasMarketing.ValidarComunicado("X", "Aviso", "R", new string('x', 151), null, null, "Todos")!.Code);
    }
}
