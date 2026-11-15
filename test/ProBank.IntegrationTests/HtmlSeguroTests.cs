using ProBank.Infrastructure.Services;

namespace ProBank.IntegrationTests;

/// <summary>DEC-01: la descripción de una decisión (editor enriquecido) se guarda limpia.</summary>
public sealed class HtmlSeguroTests
{
    private static readonly HtmlSeguro Limpiador = new();

    [Fact(DisplayName = "DEC-01 · Conserva el formato del editor y quita scripts, eventos, estilos y enlaces javascript")]
    public void QuitaLoPeligroso()
    {
        var (html, texto) = Limpiador.Limpiar(
            "<h3>Alcance</h3><p onclick=\"x()\" style=\"color:red\"><strong>Monto:</strong> $ 18.000</p><script>alert(1)</script>" +
            "<ul><li>Uno</li></ul><a href=\"javascript:alert(1)\">mal</a><a href=\"https://banco.ec\">bien</a><img src=x onerror=y>");
        Assert.Contains("<h3>Alcance</h3>", html);
        Assert.Contains("<strong>Monto:</strong>", html);
        Assert.Contains("<li>Uno</li>", html);
        Assert.Contains("href=\"https://banco.ec\"", html);
        Assert.DoesNotContain("script", html);
        Assert.DoesNotContain("onclick", html);
        Assert.DoesNotContain("style", html);
        Assert.DoesNotContain("javascript", html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("Monto: $ 18.000", texto);
    }

    [Fact(DisplayName = "DEC-01 · El texto sin etiquetas se guarda como párrafos y una descripción vacía queda vacía")]
    public void TextoYVacio()
    {
        Assert.Equal("<p>Línea 1</p><p>a &lt; b</p>", Limpiador.Limpiar("Línea 1\na < b").Html);
        Assert.Equal(("", ""), Limpiador.Limpiar("<p><br></p>"));
        Assert.Equal(("", ""), Limpiador.Limpiar("<script>alert(1)</script>"));
    }
}
