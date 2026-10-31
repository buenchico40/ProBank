using System.Security.Cryptography;

namespace ProBank.Domain.Common;

/// <summary>
/// Contraseñas de usuarios (SEG-06): PBKDF2-SHA512, 600 000 iteraciones, sal de 32 bytes y hash de 64 bytes.
/// Lo usan la API (verificar, cambiar, restablecer) y ProBank.Setup (crear); el archivo se enlaza en ambos.
/// </summary>
public static class Claves
{
    public const int LongitudMinima = 14;
    private const int Iteraciones = 600_000;
    // Sin caracteres que se confunden al dictar o copiar (0/O, 1/l/I).
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    public static (byte[] Hash, byte[] Sal) Generar(string clave)
    {
        var sal = RandomNumberGenerator.GetBytes(32);
        return (Derivar(clave, sal), sal);
    }

    public static bool Verificar(string clave, byte[] hash, byte[] sal) =>
        CryptographicOperations.FixedTimeEquals(Derivar(clave, sal), hash);

    public static string? Validar(string clave) => clave.Length < LongitudMinima
        ? $"La contraseña debe tener al menos {LongitudMinima} caracteres."
        : null;

    /// <summary>Reglas de una clave nueva elegida por el usuario: longitud y que no contenga el nombre de usuario.</summary>
    public static string? ValidarNueva(string clave, string usuario)
    {
        if (Validar(clave) is { } error) return error;
        if (usuario.Length >= 3 && clave.Contains(usuario, StringComparison.OrdinalIgnoreCase))
            return "La contraseña no puede contener el nombre de usuario.";
        return null;
    }

    /// <summary>Clave temporal aleatoria (20 caracteres + sufijo), que el usuario debe cambiar al ingresar.</summary>
    public static string GenerarTemporal() =>
        string.Concat(Enumerable.Range(0, 20).Select(_ => Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)])) + "-Pb1";

    private static byte[] Derivar(string clave, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA512, 64);
}
