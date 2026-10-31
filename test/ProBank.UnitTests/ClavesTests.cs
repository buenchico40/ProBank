using ProBank.Domain.Common;

namespace ProBank.UnitTests;

public sealed class ClavesTests
{
    [Fact(DisplayName = "SEG-06 · la clave generada se verifica y otra no")]
    public void Verifica()
    {
        var (hash, sal) = Claves.Generar("Clave-Segura-2026!");
        Assert.True(Claves.Verificar("Clave-Segura-2026!", hash, sal));
        Assert.False(Claves.Verificar("clave-segura-2026!", hash, sal));
    }

    [Fact(DisplayName = "SEG-06 · la misma clave produce hashes distintos (sal aleatoria)")]
    public void SalAleatoria() => Assert.NotEqual(Claves.Generar("Clave-Segura-2026!").Hash, Claves.Generar("Clave-Segura-2026!").Hash);

    [Theory(DisplayName = "SEG-06 · longitud mínima de 14 caracteres")]
    [InlineData("corta", false)]
    [InlineData("1234567890123", false)]
    [InlineData("12345678901234", true)]
    public void Longitud(string clave, bool valida) => Assert.Equal(valida, Claves.Validar(clave) is null);

    [Theory(DisplayName = "SEG-06 · la clave nueva no puede contener el nombre de usuario")]
    [InlineData("Ana.Perez-2026-Segura", false)]
    [InlineData("Otra-Clave-Muy-Segura", true)]
    public void SinUsuario(string clave, bool valida) => Assert.Equal(valida, Claves.ValidarNueva(clave, "ana.perez") is null);

    [Fact(DisplayName = "SEG-07 · la clave temporal cumple la política y no se repite")]
    public void Temporal()
    {
        var a = Claves.GenerarTemporal();
        Assert.Null(Claves.Validar(a));
        Assert.Equal(24, a.Length);
        Assert.NotEqual(a, Claves.GenerarTemporal());
    }
}
