using System.Data;
using System.Text.Json;
using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Seguridad;
using ProBank.Contracts.Seguridad;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Seguridad;

/// <summary>Repositorio de Seguridad: solo procedimientos almacenados de los esquemas seg y aud.</summary>
internal sealed class SeguridadRepository(ISqlConnectionFactory db) : ISeguridadRepository
{
    private static readonly char[] Coma = [','];

    public async Task<IReadOnlyList<UsuarioDto>> ListarUsuarios(CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var filas = await cn.QueryAsync<UsuarioFila>(new CommandDefinition("seg.usp_Usuario_Listar", commandType: Sp.Tipo, cancellationToken: ct));
        return filas.Select(f => new UsuarioDto(f.Token, f.Usuario, f.Nombre, f.Correo, Lista(f.Roles), f.RolPredeterminado, f.Estado,
            f.Bloqueado, f.DebeCambiarClave, f.UltimaConexion, f.UltimaIp, f.IpPermitida)).ToList();
    }

    public async Task<IReadOnlyList<IngresoUsuarioDto>> ListarIngresos(Guid token, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<IngresoUsuarioDto>(new CommandDefinition("seg.usp_Usuario_ListarIngresos", new { Token = token },
            commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    public async Task<Guid> CrearUsuario(CrearUsuarioRequest r, byte[] hash, byte[] sal, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var p = new DynamicParameters(new
        {
            r.Usuario, r.Nombre, r.Correo, Roles = string.Join(',', r.Roles), r.RolPredeterminado, ClaveHash = hash, ClaveSal = sal,
            ActorId = a.UsuarioId, a.RolActivo, a.Ip
        });
        p.Add("Token", dbType: DbType.Guid, direction: ParameterDirection.Output);
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Usuario_Crear", p, commandType: Sp.Tipo, cancellationToken: ct));
        return p.Get<Guid>("Token");
    }

    public async Task<int> ActualizarUsuario(Guid token, ActualizarUsuarioRequest r, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var p = new DynamicParameters(new
        {
            Token = token, r.Nombre, r.Correo, Roles = string.Join(',', r.Roles), r.RolPredeterminado, r.Estado,
            ActorId = a.UsuarioId, a.RolActivo, a.Ip
        });
        p.Add("UsuarioId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Usuario_Actualizar", p, commandType: Sp.Tipo, cancellationToken: ct));
        return p.Get<int>("UsuarioId");
    }

    public async Task<int> RestablecerClave(Guid token, byte[] hash, byte[] sal, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var p = new DynamicParameters(new { Token = token, ClaveHash = hash, ClaveSal = sal, ActorId = a.UsuarioId, a.RolActivo, a.Ip });
        p.Add("UsuarioId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Usuario_RestablecerClave", p, commandType: Sp.Tipo, cancellationToken: ct));
        return p.Get<int>("UsuarioId");
    }

    public async Task Desbloquear(Guid token, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Usuario_Desbloquear", new { Token = token, ActorId = a.UsuarioId, a.RolActivo, a.Ip },
            commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task<int> RevocarSesiones(Guid token, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var p = new DynamicParameters(new { Token = token, ActorId = a.UsuarioId, a.RolActivo, a.Ip });
        p.Add("UsuarioId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Usuario_RevocarSesiones", p, commandType: Sp.Tipo, cancellationToken: ct));
        return p.Get<int>("UsuarioId");
    }

    public async Task<PerfilDto?> ObtenerPerfil(int usuarioId, string rolActivo, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("seg.usp_Usuario_ObtenerPerfil",
            new { UsuarioId = usuarioId, RolActivo = rolActivo }, commandType: Sp.Tipo, cancellationToken: ct));
        var f = await m.ReadSingleOrDefaultAsync<PerfilFila>();
        var roles = (await m.ReadAsync<string>()).ToList();
        var permisos = (await m.ReadAsync<PermisoFila>()).Select(x => $"{x.Opcion}:{x.Accion}").ToList();
        return f is null ? null
            : new PerfilDto(f.Usuario, f.Nombre, f.Correo, roles, rolActivo, f.RolPredeterminado, f.DebeCambiarClave, f.ClaveCambiada,
                f.UltimaConexion, f.UltimaIp, f.ConexionAnterior, f.IpAnterior, permisos, usuarioId);
    }

    public async Task CambiarClave(int usuarioId, byte[] hash, byte[] sal, string rolActivo, string ip, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Usuario_CambiarClave",
            new { UsuarioId = usuarioId, ClaveHash = hash, ClaveSal = sal, RolActivo = rolActivo, Ip = ip }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task CambiarRol(int usuarioId, string rol, bool predeterminado, string ip, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Usuario_CambiarRol",
            new { UsuarioId = usuarioId, Rol = rol, Predeterminado = predeterminado, Ip = ip }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task<SesionVigente?> ObtenerSello(int usuarioId, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var f = await cn.QuerySingleOrDefaultAsync<SelloFila>(new CommandDefinition("seg.usp_Usuario_ObtenerSello",
            new { UsuarioId = usuarioId }, commandType: Sp.Tipo, cancellationToken: ct));
        return f is null ? null : new SesionVigente(f.SelloSesion, f.Estado);
    }

    public async Task<IReadOnlyList<RolDto>> ListarRoles(CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<RolDto>(new CommandDefinition("seg.usp_Rol_Listar", commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    public async Task<int> GuardarRol(int? rolId, GuardarRolRequest r, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var p = new DynamicParameters(new { RolId = rolId, r.Nombre, r.Descripcion, r.Estado, ActorId = a.UsuarioId, a.RolActivo, a.Ip });
        p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Rol_Guardar", p, commandType: Sp.Tipo, cancellationToken: ct));
        return p.Get<int>("Id");
    }

    public async Task<IReadOnlyList<PermisoDto>> ListarPermisos(int rolId, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<PermisoDto>(new CommandDefinition("seg.usp_Permiso_ListarPorRol", new { RolId = rolId },
            commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    public async Task GuardarPermisos(int rolId, IReadOnlyList<string> permisos, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Permiso_Guardar",
            new { RolId = rolId, Permisos = string.Join(',', permisos), ActorId = a.UsuarioId, a.RolActivo, a.Ip }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<AuditoriaDto>> ListarAuditoria(DateOnly desde, DateOnly hasta, string? modulo, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<AuditoriaDto>(new CommandDefinition("aud.usp_Auditoria_Listar",
            new { Desde = desde.ToDateTime(TimeOnly.MinValue), Hasta = hasta.ToDateTime(TimeOnly.MinValue), Modulo = modulo },
            commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    public async Task<IReadOnlyList<ParametroDto>> ListarParametros(CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<ParametroDto>(new CommandDefinition("seg.usp_Parametro_Listar", commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    public async Task GuardarParametros(IReadOnlyList<ValorParametro> valores, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var json = JsonSerializer.Serialize(valores.Select(v => new { clave = v.Clave, valor = v.Valor }));
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Parametro_Guardar",
            new { Valores = json, ActorId = a.UsuarioId, a.RolActivo, a.Ip }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    private static IReadOnlyList<string> Lista(string? csv) =>
        string.IsNullOrEmpty(csv) ? [] : csv.Split(Coma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private sealed record UsuarioFila(Guid Token, string Usuario, string Nombre, string? Correo, string? RolPredeterminado, string Estado,
        bool Bloqueado, bool DebeCambiarClave, DateTime? UltimaConexion, string? UltimaIp, string? IpPermitida, string? Roles);
    private sealed record PerfilFila(string Usuario, string Nombre, string? Correo, string? RolPredeterminado, string Estado, bool DebeCambiarClave,
        DateTime? ClaveCambiada, DateTime? UltimaConexion, string? UltimaIp, DateTime? ConexionAnterior, string? IpAnterior);
    private sealed record PermisoFila(string Opcion, string Accion);
    private sealed record SelloFila(int SelloSesion, string Estado);
}

/// <summary>SEG-03: permiso del rol activo del usuario actual, resuelto por seg.fn_TienePermiso.</summary>
internal sealed class SqlPermisos(ISqlConnectionFactory db, ICurrentUser user) : IPermisos
{
    public async Task<bool> Tiene(string opcion, string accion, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return await cn.ExecuteScalarAsync<bool>(new CommandDefinition("seg.usp_Permiso_Verificar",
            new { user.UsuarioId, user.RolActivo, Opcion = opcion, Accion = accion }, commandType: Sp.Tipo, cancellationToken: ct));
    }
}
