using ProBank.Application.Abstractions;
using ProBank.Contracts.Seguridad;
using ProBank.Domain.Common;

namespace ProBank.Api.Seguridad;

/// <summary>
/// SEG-03: inicio de sesión con usuario y contraseña. El mensaje de error es el mismo si el usuario no existe o la
/// clave no coincide, para no revelar qué cuentas existen. Cada intento queda en auditoría.
/// </summary>
public sealed class ServicioIngreso(ICredencialesRepository credenciales, EmisorTokens emisor)
{
    public static readonly Error Incorrecto = Error.Validation("credenciales", "Usuario o contraseña incorrectos.");
    public static readonly Error Bloqueado = Error.Validation("bloqueado", "La cuenta está bloqueada por intentos fallidos. Intenta de nuevo en 15 minutos.");

    // Hash de relleno: si el usuario no existe se calcula igual un PBKDF2 y la respuesta tarda lo mismo.
    private static readonly (byte[] Hash, byte[] Sal) Relleno = Claves.Generar(Guid.NewGuid().ToString());

    public async Task<Result<IngresoResponse>> Ingresar(IngresarRequest r, CancellationToken ct)
    {
        var usuario = r.Usuario.Trim();
        var u = await credenciales.Obtener(usuario, ct);

        if (u is null || u.ClaveHash is null || u.ClaveSal is null)
        {
            Claves.Verificar(r.Clave, Relleno.Hash, Relleno.Sal);
            return await Fallar(usuario, "Usuario inexistente o sin contraseña", r.Ip, Incorrecto, ct);
        }
        if (u.Bloqueado) return await Fallar(usuario, "Cuenta bloqueada", r.Ip, Bloqueado, ct);
        if (!Claves.Verificar(r.Clave, u.ClaveHash, u.ClaveSal)) return await Fallar(usuario, "Contraseña incorrecta", r.Ip, Incorrecto, ct);
        if (u.Estado != "Activo") return await Fallar(usuario, $"Usuario {u.Estado}", r.Ip, Incorrecto, ct);
        if (u.IpPermitida is not null && u.IpPermitida != r.Ip)
            return await Fallar(usuario, $"IP no permitida ({r.Ip})", r.Ip, Error.Forbidden("Esta cuenta solo puede ingresar desde su IP autorizada."), ct);

        await credenciales.RegistrarIngreso(usuario, true, null, r.Ip, ct);
        var (token, expira) = emisor.Emitir(u, r.Ip);
        var predeterminado = u.RolPredeterminado is { } rp && u.Roles.Contains(rp) ? rp : u.Roles.FirstOrDefault();
        return Result.Success(new IngresoResponse(token, expira, u.Nombre, u.Roles, predeterminado, u.DebeCambiarClave));
    }

    private async Task<Result<IngresoResponse>> Fallar(string usuario, string motivo, string ip, Error error, CancellationToken ct)
    {
        await credenciales.RegistrarIngreso(usuario, false, motivo, ip, ct);
        return Result.Failure<IngresoResponse>(error);
    }
}
