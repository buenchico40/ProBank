using System.Data;
using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Boveda;
using ProBank.Contracts.Boveda;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Boveda;

/// <summary>
/// Bóveda: grupos de acceso, certificados, cuentas técnicas y secretos. Solo procedimientos del esquema cred.
/// Los valores (cred.CredencialVersion.Valor) van cifrados con Always Encrypted: se leen y escriben solo por la conexión cifrada.
/// </summary>
internal sealed class BovedaRepository(ISqlConnectionFactory db) : IBovedaRepository
{
    private static DateTime F(DateOnly d) => d.ToDateTime(TimeOnly.MinValue);
    private static DateTime? F(DateOnly? d) => d?.ToDateTime(TimeOnly.MinValue);
    private static DateOnly D(DateTime d) => DateOnly.FromDateTime(d);
    private static DateOnly? D(DateTime? d) => d is null ? null : DateOnly.FromDateTime(d.Value);

    private static DynamicParameters Con(object datos, Actor a)
    {
        var p = new DynamicParameters(datos);
        p.AddDynamicParams(new { ActorId = a.UsuarioId, a.RolActivo, a.Ip });
        return p;
    }

    private async Task<List<T>> Lista<T>(string sp, object? p, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<T>(new CommandDefinition(sp, p, commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    private async Task<Guid> Guardar(string sp, object datos, Actor a, CancellationToken ct)
    {
        var p = Con(datos, a);
        p.Add("TokenGuardado", dbType: DbType.Guid, direction: ParameterDirection.Output);
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition(sp, p, commandType: Sp.Tipo, cancellationToken: ct));
        return p.Get<Guid>("TokenGuardado");
    }

    // ---------- Grupos de acceso ----------
    public async Task<IReadOnlyList<GrupoAccesoDto>> ListarGrupos(Guid? token, Actor a, CancellationToken ct) =>
        await Lista<GrupoAccesoDto>("cred.usp_Grupo_Listar", new { ActorId = a.UsuarioId, Token = token }, ct);

    public async Task<GrupoDetalleDto> DetalleGrupo(Guid token, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("cred.usp_Grupo_Detalle", new { Token = token }, commandType: Sp.Tipo, cancellationToken: ct));
        var miembros = (await m.ReadAsync<MiembroGrupoDto>()).ToList();
        var credenciales = (await m.ReadAsync<CredencialGrupoDto>()).ToList();
        return new GrupoDetalleDto(miembros, credenciales);
    }

    public Task<Guid> GuardarGrupo(Guid? token, GuardarGrupoRequest r, Actor a, CancellationToken ct) =>
        Guardar("cred.usp_Grupo_Guardar", new { Token = token, r.Nombre, r.Descripcion, r.Estado, Miembros = string.Join(',', r.Miembros) }, a, ct);

    // ---------- Certificados ----------
    public async Task<IReadOnlyList<CertificadoDto>> ListarCertificados(DateOnly hoy, Guid? token, CancellationToken ct) =>
        (await Lista<CertificadoFila>("cred.usp_Certificado_Listar", new { Hoy = F(hoy), Token = token }, ct)).Select(f => new CertificadoDto(f.Token, f.Codigo,
            f.NombreComun, f.Tipo, f.Emisor, D(f.Vence), f.DiasParaVencer, f.Almacen, f.Responsable, f.DiasAviso, f.Huella, f.NombresAlternativos, f.UsadoPor,
            f.Activo, f.Estado, f.UltimaRenovacion)).ToList();

    public async Task<IReadOnlyList<RenovacionCertificadoDto>> RenovacionesCertificado(Guid token, CancellationToken ct) =>
        (await Lista<RenovacionFila>("cred.usp_Certificado_Renovaciones", new { Token = token }, ct)).Select(f => new RenovacionCertificadoDto(f.Fecha,
            D(f.VenceAnterior), D(f.VenceNuevo), f.HuellaAnterior, f.HuellaNueva, f.RegistradoPor)).ToList();

    public Task<Guid> GuardarCertificado(Guid? token, GuardarCertificadoRequest r, Actor a, CancellationToken ct) =>
        Guardar("cred.usp_Certificado_Guardar", new
        {
            Token = token, r.NombreComun, r.Tipo, r.Emisor, Vence = F(r.Vence), r.Almacen, r.Responsable, r.DiasAviso, r.Huella, r.NombresAlternativos,
            r.UsadoPor, r.Activo
        }, a, ct);

    public async Task RenovarCertificado(Guid token, DateOnly vence, string huella, DateOnly hoy, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("cred.usp_Certificado_Renovar", Con(new { Token = token, Vence = F(vence), Huella = huella, Hoy = F(hoy) }, a),
            commandType: Sp.Tipo, cancellationToken: ct));
    }

    // ---------- Cuentas técnicas y secretos (sin valores) ----------
    public async Task<IReadOnlyList<CuentaTecnicaDto>> ListarCuentas(DateOnly hoy, Guid? token, Actor a, CancellationToken ct) =>
        (await Lista<CuentaFila>("cred.usp_Cuenta_Listar", new { Hoy = F(hoy), ActorId = a.UsuarioId, Token = token }, ct)).Select(f => new CuentaTecnicaDto(f.Token,
            f.Nombre, f.Tipo, f.Sistema, f.Responsable, f.Privilegios, D(f.UltimaRotacion), D(f.ProximaRotacion), f.DiasParaRotar, f.Estado, f.GrupoToken,
            f.GrupoCodigo, f.GrupoNombre, f.Version, f.FechaVersion, f.TieneValor, f.EsMiembro, f.Activo)).ToList();

    public Task<Guid> GuardarCuenta(Guid? token, GuardarCuentaRequest r, Actor a, CancellationToken ct) =>
        Guardar("cred.usp_Cuenta_Guardar", new
        {
            Token = token, r.Nombre, r.Tipo, r.Sistema, r.Responsable, r.Privilegios, GrupoToken = r.Grupo, ProximaRotacion = F(r.ProximaRotacion), r.Activo
        }, a, ct);

    public async Task<IReadOnlyList<SecretoDto>> ListarSecretos(DateOnly hoy, Guid? token, Actor a, CancellationToken ct) =>
        (await Lista<SecretoFila>("cred.usp_Secreto_Listar", new { Hoy = F(hoy), ActorId = a.UsuarioId, Token = token }, ct)).Select(f => new SecretoDto(f.Token,
            f.Nombre, f.Tipo, f.Almacen, f.Sistema, f.Version, D(f.UltimaRotacion), D(f.Expira), f.DiasParaVencer, f.DiasAviso, f.Estado, f.GrupoToken,
            f.GrupoCodigo, f.GrupoNombre, f.FechaVersion, f.TieneValor, f.EsMiembro, f.Activo)).ToList();

    public Task<Guid> GuardarSecreto(Guid? token, GuardarSecretoRequest r, Actor a, CancellationToken ct) =>
        Guardar("cred.usp_Secreto_Guardar", new
        {
            Token = token, r.Nombre, r.Tipo, r.Almacen, r.Sistema, GrupoToken = r.Grupo, r.DiasAviso, Expira = F(r.Expira), r.Activo
        }, a, ct);

    public async Task<CredencialHistorialDto> HistorialCredencial(string clase, Guid token, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("cred.usp_Credencial_Historial", new { Clase = clase, Token = token },
            commandType: Sp.Tipo, cancellationToken: ct));
        var versiones = (await m.ReadAsync<VersionFila>()).Select(f => new VersionCredencialDto(f.Version, f.Fecha, D(f.Vence), f.RegistradoPor, f.Nota, f.Vigente)).ToList();
        var accesos = (await m.ReadAsync<AccesoCredencialDto>()).ToList();
        return new CredencialHistorialDto(versiones, accesos);
    }

    // ---------- Valores cifrados (CRE-03) ----------
    public async Task<CredencialValorDto> EntregarCredencial(string clase, Guid token, string accion, Actor a, CancellationToken ct)
    {
        await using var cn = await db.AbrirCifrada(ct);
        return await cn.QuerySingleAsync<CredencialValorDto>(new CommandDefinition("cred.usp_Credencial_Mostrar",
            Con(new { Clase = clase, Token = token, Accion = accion }, a), commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task<int> CambiarCredencial(string clase, Guid token, string valor, DateOnly vence, string? nota, DateOnly hoy, Actor a, CancellationToken ct)
    {
        // Always Encrypted exige que el parámetro cifrado tenga exactamente el tipo de la columna: nvarchar(4000)
        var p = Con(new
        {
            Clase = clase, Token = token, Valor = new DbString { Value = valor, IsAnsi = false, IsFixedLength = false, Length = 4000 },
            Vence = F(vence), Nota = nota, Hoy = F(hoy)
        }, a);
        p.Add("VersionNueva", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await using var cn = await db.AbrirCifrada(ct);
        await cn.ExecuteAsync(new CommandDefinition("cred.usp_Credencial_Cambiar", p, commandType: Sp.Tipo, cancellationToken: ct));
        return p.Get<int>("VersionNueva");
    }

    private sealed record CertificadoFila(Guid Token, string Codigo, string NombreComun, string Tipo, string Emisor, DateTime Vence, int DiasParaVencer,
        string Almacen, string Responsable, int DiasAviso, string? Huella, string? NombresAlternativos, string? UsadoPor, bool Activo, string Estado,
        DateTime? UltimaRenovacion);
    private sealed record RenovacionFila(DateTime Fecha, DateTime VenceAnterior, DateTime VenceNuevo, string? HuellaAnterior, string HuellaNueva, string RegistradoPor);
    private sealed record CuentaFila(Guid Token, string Nombre, string Tipo, string Sistema, string Responsable, string? Privilegios, DateTime? UltimaRotacion,
        DateTime? ProximaRotacion, int? DiasParaRotar, string Estado, Guid? GrupoToken, string? GrupoCodigo, string? GrupoNombre, int Version,
        DateTime? FechaVersion, bool TieneValor, bool EsMiembro, bool Activo);
    private sealed record SecretoFila(Guid Token, string Nombre, string Tipo, string Almacen, string Sistema, int Version, DateTime? UltimaRotacion,
        DateTime? Expira, int? DiasParaVencer, int DiasAviso, string Estado, Guid GrupoToken, string GrupoCodigo, string GrupoNombre, DateTime? FechaVersion,
        bool TieneValor, bool EsMiembro, bool Activo);
    private sealed record VersionFila(int Version, DateTime Fecha, DateTime? Vence, string RegistradoPor, string? Nota, bool Vigente);
}
