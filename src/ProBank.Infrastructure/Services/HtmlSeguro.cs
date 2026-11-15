using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;
using ProBank.Application.Abstractions;

namespace ProBank.Infrastructure.Services;

/// <summary>
/// Limpia el HTML del editor enriquecido (Decisiones) con HtmlSanitizer: solo las etiquetas del editor (párrafos, títulos, negrita,
/// cursiva, subrayado, listas, citas, tablas y enlaces http, https o mailto), sin estilos ni atributos de eventos.
/// </summary>
public sealed partial class HtmlSeguro : IHtmlSeguro
{
    private static readonly HtmlSanitizer Limpiador = Crear();

    private static HtmlSanitizer Crear()
    {
        var s = new HtmlSanitizer();
        s.AllowedTags.Clear();
        foreach (var t in new[] { "p", "br", "h2", "h3", "h4", "strong", "b", "em", "i", "u", "s", "ul", "ol", "li", "blockquote",
                                   "table", "thead", "tbody", "tr", "th", "td", "a", "span" })
            s.AllowedTags.Add(t);
        s.AllowedAttributes.Clear();
        s.AllowedAttributes.Add("href");
        s.AllowedAttributes.Add("colspan");
        s.AllowedAttributes.Add("rowspan");
        s.AllowedSchemes.Clear();
        s.AllowedSchemes.Add("http");
        s.AllowedSchemes.Add("https");
        s.AllowedSchemes.Add("mailto");
        s.AllowedCssProperties.Clear();
        s.AllowDataAttributes = false;
        return s;
    }

    public (string Html, string Texto) Limpiar(string? html)
    {
        var entrada = (html ?? "").Trim();
        // Texto escrito sin editor (o anterior a él): se conserva como texto con sus saltos de línea
        if (!Etiqueta().IsMatch(entrada))
            entrada = string.Join("", entrada.Replace("\r\n", "\n").Split('\n').Select(l => "<p>" + WebUtility.HtmlEncode(l) + "</p>"));
        var limpio = Limpiador.Sanitize(entrada).Trim();
        var texto = WebUtility.HtmlDecode(Etiquetas().Replace(Bloques().Replace(limpio, "\n"), "")).Trim();
        return (texto.Length == 0 ? "" : limpio, texto);
    }

    [GeneratedRegex(@"<[a-zA-Z][^>]*>")]
    private static partial Regex Etiqueta();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Etiquetas();

    [GeneratedRegex(@"</(p|h2|h3|h4|li|tr|blockquote)>|<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex Bloques();
}
