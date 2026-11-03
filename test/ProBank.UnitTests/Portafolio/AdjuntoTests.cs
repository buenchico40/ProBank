using ProBank.Domain.Modules.Portafolio;

namespace ProBank.UnitTests.Portafolio;

public sealed class AdjuntoTests
{
    private static readonly byte[] Pdf = "%PDF-1.7"u8.ToArray();
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00];
    private static readonly byte[] Ole = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
    private static readonly byte[] Sql = "SELECT * FROM coa.Coactiva;\r\n"u8.ToArray();
    private static readonly string[] Tarea = [".zip", ".sql", ".pdf", ".doc", ".docx"];

    [Theory(DisplayName = "Adjuntos · se aceptan los tipos habilitados con su contenido real")]
    [InlineData("Acta.pdf", "application/pdf")]
    [InlineData("Alcance.DOCX", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("Antiguo.doc", "application/msword")]
    [InlineData("respaldo.zip", "application/zip")]
    [InlineData("script.sql", "text/plain")]
    public void Permitidos(string nombre, string tipo)
    {
        var ext = Path.GetExtension(nombre).ToLowerInvariant();
        var cabecera = ext switch { ".pdf" => Pdf, ".doc" => Ole, ".sql" => Sql, _ => Zip };
        var (limpio, t, error) = ReglasAdjunto.Validar(nombre, 1024, cabecera, Tarea, 5);
        Assert.Null(error);
        Assert.Equal((nombre, tipo), (limpio, t));
    }

    [Fact(DisplayName = "Adjuntos · se rechazan tipos no habilitados, archivos vacíos o mayores al máximo configurado")]
    public void Rechazados()
    {
        Assert.Equal("tipo", ReglasAdjunto.Validar("hoja.xlsx", 100, Zip, Tarea, 5).Error!.Code);   // del catálogo, pero no habilitado
        Assert.Equal("tipo", ReglasAdjunto.Validar("programa.exe", 100, Ole, Tarea, 5).Error!.Code); // fuera del catálogo
        Assert.Equal("tamano", ReglasAdjunto.Validar("a.pdf", 0, Pdf, Tarea, 5).Error!.Code);
        Assert.Equal("tamano", ReglasAdjunto.Validar("a.pdf", 5 * 1024 * 1024 + 1, Pdf, Tarea, 5).Error!.Code);
        Assert.Null(ReglasAdjunto.Validar("a.pdf", 5 * 1024 * 1024, Pdf, Tarea, 5).Error);
        Assert.Equal("nombre", ReglasAdjunto.Validar("  ", 100, Pdf, Tarea, 5).Error!.Code);
    }

    [Fact(DisplayName = "Adjuntos · un archivo renombrado no pasa y la ruta del cliente se descarta")]
    public void ContenidoYNombre()
    {
        Assert.Equal("contenido", ReglasAdjunto.Validar("virus.pdf", 100, "MZ\x90\0"u8, Tarea, 5).Error!.Code);
        Assert.Equal("contenido", ReglasAdjunto.Validar("virus.sql", 100, "MZ\x90\0"u8, Tarea, 5).Error!.Code);
        Assert.Equal("contenido", ReglasAdjunto.Validar("binario.sql", 100, [0x41, 0x00, 0x42], Tarea, 5).Error!.Code);
        Assert.Equal("contenido", ReglasAdjunto.Validar("datos.zip", 100, Pdf, Tarea, 5).Error!.Code);
        Assert.Equal("informe.pdf", ReglasAdjunto.Validar(@"C:\Users\x\..\informe.pdf", 100, Pdf, Tarea, 5).Nombre);
    }

    [Fact(DisplayName = "Adjuntos · cada documento lleva una descripción de hasta 300 caracteres")]
    public void Descripcion()
    {
        Assert.Equal(("Script de actualización de saldos", (Domain.Common.Error?)null), ReglasAdjunto.ValidarDescripcion("  Script de actualización de saldos "));
        Assert.Equal("descripcion", ReglasAdjunto.ValidarDescripcion("   ").Error!.Code);
        Assert.Equal("descripcion", ReglasAdjunto.ValidarDescripcion(null).Error!.Code);
        Assert.Equal("descripcion", ReglasAdjunto.ValidarDescripcion(new string('x', 301)).Error!.Code);
    }

    [Fact(DisplayName = "Adjuntos · la parametrización solo admite tipos del catálogo y de 1 a 20 MB")]
    public void Parametro()
    {
        Assert.Equal((".zip,.sql,.pdf", (Domain.Common.Error?)null), ReglasAdjunto.ValidarParametro(["ZIP", ".sql", " .pdf ", ".zip"], 5));
        Assert.Equal("extensiones", ReglasAdjunto.ValidarParametro([".exe"], 5).Error!.Code);
        Assert.Equal("extensiones", ReglasAdjunto.ValidarParametro([], 5).Error!.Code);
        Assert.Equal("maximo", ReglasAdjunto.ValidarParametro([".pdf"], 0).Error!.Code);
        Assert.Equal("maximo", ReglasAdjunto.ValidarParametro([".pdf"], 21).Error!.Code);
    }
}
