using ProBank.Application.Abstractions;
using ProBank.Contracts.Boveda;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Boveda;

namespace ProBank.Application.Modules.Boveda;

public interface IBovedaRepository
{
    Task<IReadOnlyList<GrupoAccesoDto>> ListarGrupos(Guid? token, Actor actor, CancellationToken ct);
    Task<GrupoDetalleDto> DetalleGrupo(Guid token, CancellationToken ct);
    Task<Guid> GuardarGrupo(Guid? token, GuardarGrupoRequest r, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<CertificadoDto>> ListarCertificados(DateOnly hoy, Guid? token, CancellationToken ct);
    Task<IReadOnlyList<RenovacionCertificadoDto>> RenovacionesCertificado(Guid token, CancellationToken ct);
    Task<Guid> GuardarCertificado(Guid? token, GuardarCertificadoRequest r, Actor actor, CancellationToken ct);
    Task RenovarCertificado(Guid token, DateOnly vence, string huella, DateOnly hoy, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<CuentaTecnicaDto>> ListarCuentas(DateOnly hoy, Guid? token, Actor actor, CancellationToken ct);
    Task<Guid> GuardarCuenta(Guid? token, GuardarCuentaRequest r, Actor actor, CancellationToken ct);
    Task<IReadOnlyList<SecretoDto>> ListarSecretos(DateOnly hoy, Guid? token, Actor actor, CancellationToken ct);
    Task<Guid> GuardarSecreto(Guid? token, GuardarSecretoRequest r, Actor actor, CancellationToken ct);

    /// <param name="clase">«cuenta» o «secreto».</param>
    Task<CredencialHistorialDto> HistorialCredencial(string clase, Guid token, CancellationToken ct);
    /// <summary>El SP valida el permiso y el grupo y registra el acceso antes de entregar el valor (descifrado por el driver).</summary>
    Task<CredencialValorDto> EntregarCredencial(string clase, Guid token, string accion, Actor actor, CancellationToken ct);
    Task<int> CambiarCredencial(string clase, Guid token, string valor, DateOnly vence, string? nota, DateOnly hoy, Actor actor, CancellationToken ct);
}

/// <summary>Cuenta técnica o secreto: clase que usa la base y opción del menú que da los permisos.</summary>
public enum TipoCredencial { Cuenta, Secreto }

internal static class Credencial
{
    public static string Clase(TipoCredencial t) => t == TipoCredencial.Cuenta ? "cuenta" : "secreto";
    public static string Opcion(TipoCredencial t) => t == TipoCredencial.Cuenta ? "cuentas" : "secretos";
    public static string? Nulo(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

// ---------- Consultas ----------
public sealed record ListarGruposQuery : IQuery<IReadOnlyList<GrupoAccesoDto>>;
public sealed record ObtenerGrupoQuery(Guid Token) : IQuery<GrupoAccesoDto>;
public sealed record DetalleGrupoQuery(Guid Token) : IQuery<GrupoDetalleDto>;
public sealed record ListarCertificadosQuery : IQuery<IReadOnlyList<CertificadoDto>>;
public sealed record ObtenerCertificadoQuery(Guid Token) : IQuery<CertificadoDto>;
public sealed record RenovacionesCertificadoQuery(Guid Token) : IQuery<IReadOnlyList<RenovacionCertificadoDto>>;
public sealed record ListarCuentasQuery : IQuery<IReadOnlyList<CuentaTecnicaDto>>;
public sealed record ObtenerCuentaQuery(Guid Token) : IQuery<CuentaTecnicaDto>;
public sealed record ListarSecretosQuery : IQuery<IReadOnlyList<SecretoDto>>;
public sealed record ObtenerSecretoQuery(Guid Token) : IQuery<SecretoDto>;
public sealed record HistorialCredencialQuery(TipoCredencial Tipo, Guid Token) : IQuery<CredencialHistorialDto>;

// ---------- Comandos ----------
public sealed record GuardarGrupoCommand(Guid? Token, GuardarGrupoRequest Request) : ICommand<BovedaGuardadoDto>;
public sealed record GuardarCertificadoCommand(Guid? Token, GuardarCertificadoRequest Request) : ICommand<BovedaGuardadoDto>;
public sealed record RenovarCertificadoCommand(Guid Token, RenovarCertificadoRequest Request) : ICommand<string>;
public sealed record GuardarCuentaCommand(Guid? Token, GuardarCuentaRequest Request) : ICommand<BovedaGuardadoDto>;
public sealed record GuardarSecretoCommand(Guid? Token, GuardarSecretoRequest Request) : ICommand<BovedaGuardadoDto>;
/// <summary>CRE-02: entregar el valor para mostrarlo o copiarlo es un comando (queda registrado), no una consulta.</summary>
public sealed record EntregarCredencialCommand(TipoCredencial Tipo, Guid Token, bool Copiar) : ICommand<CredencialValorDto>;
public sealed record CambiarCredencialCommand(TipoCredencial Tipo, Guid Token, CambiarCredencialRequest Request) : ICommand<CredencialCambiadaDto>;

internal sealed class BovedaConsultas(IBovedaRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) :
    IQueryHandler<ListarGruposQuery, IReadOnlyList<GrupoAccesoDto>>, IQueryHandler<ObtenerGrupoQuery, GrupoAccesoDto>,
    IQueryHandler<DetalleGrupoQuery, GrupoDetalleDto>,
    IQueryHandler<ListarCertificadosQuery, IReadOnlyList<CertificadoDto>>, IQueryHandler<ObtenerCertificadoQuery, CertificadoDto>,
    IQueryHandler<RenovacionesCertificadoQuery, IReadOnlyList<RenovacionCertificadoDto>>,
    IQueryHandler<ListarCuentasQuery, IReadOnlyList<CuentaTecnicaDto>>, IQueryHandler<ObtenerCuentaQuery, CuentaTecnicaDto>,
    IQueryHandler<ListarSecretosQuery, IReadOnlyList<SecretoDto>>, IQueryHandler<ObtenerSecretoQuery, SecretoDto>,
    IQueryHandler<HistorialCredencialQuery, CredencialHistorialDto>
{
    public async Task<Result<IReadOnlyList<GrupoAccesoDto>>> Handle(ListarGruposQuery q, CancellationToken ct) =>
        await permisos.Exigir("grupos", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<GrupoAccesoDto>>(e)
            : Result.Success(await repo.ListarGrupos(null, Actor.De(user), ct));

    public async Task<Result<GrupoAccesoDto>> Handle(ObtenerGrupoQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("grupos", "ver", ct) is { } e) return Result.Failure<GrupoAccesoDto>(e);
        return (await repo.ListarGrupos(q.Token, Actor.De(user), ct)).FirstOrDefault() is { } g ? Result.Success(g) : Result.Failure<GrupoAccesoDto>(Error.NotFound("El grupo de acceso"));
    }

    public async Task<Result<GrupoDetalleDto>> Handle(DetalleGrupoQuery q, CancellationToken ct) =>
        await permisos.Exigir("grupos", "ver", ct) is { } e ? Result.Failure<GrupoDetalleDto>(e) : Result.Success(await repo.DetalleGrupo(q.Token, ct));

    public async Task<Result<IReadOnlyList<CertificadoDto>>> Handle(ListarCertificadosQuery q, CancellationToken ct) =>
        await permisos.Exigir("certificados", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<CertificadoDto>>(e)
            : Result.Success(await repo.ListarCertificados(clock.Hoy, null, ct));

    public async Task<Result<CertificadoDto>> Handle(ObtenerCertificadoQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("certificados", "ver", ct) is { } e) return Result.Failure<CertificadoDto>(e);
        return (await repo.ListarCertificados(clock.Hoy, q.Token, ct)).FirstOrDefault() is { } c ? Result.Success(c) : Result.Failure<CertificadoDto>(Error.NotFound("El certificado"));
    }

    public async Task<Result<IReadOnlyList<RenovacionCertificadoDto>>> Handle(RenovacionesCertificadoQuery q, CancellationToken ct) =>
        await permisos.Exigir("certificados", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<RenovacionCertificadoDto>>(e)
            : Result.Success(await repo.RenovacionesCertificado(q.Token, ct));

    public async Task<Result<IReadOnlyList<CuentaTecnicaDto>>> Handle(ListarCuentasQuery q, CancellationToken ct) =>
        await permisos.Exigir("cuentas", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<CuentaTecnicaDto>>(e)
            : Result.Success(await repo.ListarCuentas(clock.Hoy, null, Actor.De(user), ct));

    public async Task<Result<CuentaTecnicaDto>> Handle(ObtenerCuentaQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("cuentas", "ver", ct) is { } e) return Result.Failure<CuentaTecnicaDto>(e);
        return (await repo.ListarCuentas(clock.Hoy, q.Token, Actor.De(user), ct)).FirstOrDefault() is { } c
            ? Result.Success(c) : Result.Failure<CuentaTecnicaDto>(Error.NotFound("La cuenta técnica"));
    }

    public async Task<Result<IReadOnlyList<SecretoDto>>> Handle(ListarSecretosQuery q, CancellationToken ct) =>
        await permisos.Exigir("secretos", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<SecretoDto>>(e)
            : Result.Success(await repo.ListarSecretos(clock.Hoy, null, Actor.De(user), ct));

    public async Task<Result<SecretoDto>> Handle(ObtenerSecretoQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("secretos", "ver", ct) is { } e) return Result.Failure<SecretoDto>(e);
        return (await repo.ListarSecretos(clock.Hoy, q.Token, Actor.De(user), ct)).FirstOrDefault() is { } s
            ? Result.Success(s) : Result.Failure<SecretoDto>(Error.NotFound("El secreto"));
    }

    public async Task<Result<CredencialHistorialDto>> Handle(HistorialCredencialQuery q, CancellationToken ct) =>
        await permisos.Exigir(Credencial.Opcion(q.Tipo), "ver", ct) is { } e ? Result.Failure<CredencialHistorialDto>(e)
            : Result.Success(await repo.HistorialCredencial(Credencial.Clase(q.Tipo), q.Token, ct));
}

internal sealed class BovedaHandlers(IBovedaRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) :
    ICommandHandler<GuardarGrupoCommand, BovedaGuardadoDto>, ICommandHandler<GuardarCertificadoCommand, BovedaGuardadoDto>,
    ICommandHandler<RenovarCertificadoCommand, string>, ICommandHandler<GuardarCuentaCommand, BovedaGuardadoDto>,
    ICommandHandler<GuardarSecretoCommand, BovedaGuardadoDto>
{
    public async Task<Result<BovedaGuardadoDto>> Handle(GuardarGrupoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("grupos", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<BovedaGuardadoDto>(e);
        var r = c.Request with { Nombre = c.Request.Nombre?.Trim() ?? "", Descripcion = Credencial.Nulo(c.Request.Descripcion), Miembros = (c.Request.Miembros ?? []).Distinct().ToList() };
        if (ReglasBoveda.ValidarGrupo(r.Nombre, r.Descripcion, r.Estado, r.Miembros) is { } v) return Result.Failure<BovedaGuardadoDto>(v);
        return Result.Success(new BovedaGuardadoDto(await repo.GuardarGrupo(c.Token, r, Actor.De(user), ct)));
    }

    public async Task<Result<BovedaGuardadoDto>> Handle(GuardarCertificadoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("certificados", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<BovedaGuardadoDto>(e);
        var x = c.Request;
        var r = x with
        {
            NombreComun = x.NombreComun?.Trim() ?? "", Emisor = x.Emisor?.Trim() ?? "", Almacen = x.Almacen?.Trim() ?? "", Responsable = x.Responsable?.Trim() ?? "",
            Huella = ReglasBoveda.NormalizarHuella(x.Huella), NombresAlternativos = Credencial.Nulo(x.NombresAlternativos), UsadoPor = Credencial.Nulo(x.UsadoPor)
        };
        if (ReglasBoveda.ValidarCertificado(r.NombreComun, r.Tipo, r.Emisor, r.Almacen, r.Responsable, r.DiasAviso, r.Huella, r.NombresAlternativos, r.UsadoPor) is { } v)
            return Result.Failure<BovedaGuardadoDto>(v);
        return Result.Success(new BovedaGuardadoDto(await repo.GuardarCertificado(c.Token, r, Actor.De(user), ct)));
    }

    public async Task<Result<string>> Handle(RenovarCertificadoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("certificados", "editar", ct) is { } e) return Result.Failure<string>(e);
        var huella = ReglasBoveda.NormalizarHuella(c.Request.Huella);
        if (ReglasBoveda.ValidarRenovacion(c.Request.Vence, huella, clock.Hoy) is { } v) return Result.Failure<string>(v);
        await repo.RenovarCertificado(c.Token, c.Request.Vence, huella!, clock.Hoy, Actor.De(user), ct);
        return Result.Success($"Renovación registrada · vence el {c.Request.Vence:dd/MM/yyyy}");
    }

    public async Task<Result<BovedaGuardadoDto>> Handle(GuardarCuentaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("cuentas", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<BovedaGuardadoDto>(e);
        var x = c.Request;
        var identidad = x.Tipo == ReglasBoveda.IdentidadAdministrada;
        var r = x with
        {
            Nombre = x.Nombre?.Trim() ?? "", Sistema = x.Sistema?.Trim() ?? "", Responsable = x.Responsable?.Trim() ?? "", Privilegios = Credencial.Nulo(x.Privilegios),
            Grupo = identidad ? null : x.Grupo, ProximaRotacion = identidad ? null : x.ProximaRotacion
        };
        if (ReglasBoveda.ValidarCuenta(r.Nombre, r.Tipo, r.Sistema, r.Responsable, r.Privilegios, r.Grupo) is { } v) return Result.Failure<BovedaGuardadoDto>(v);
        return Result.Success(new BovedaGuardadoDto(await repo.GuardarCuenta(c.Token, r, Actor.De(user), ct)));
    }

    public async Task<Result<BovedaGuardadoDto>> Handle(GuardarSecretoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("secretos", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<BovedaGuardadoDto>(e);
        var x = c.Request;
        var r = x with { Nombre = x.Nombre?.Trim() ?? "", Almacen = x.Almacen?.Trim() ?? "", Sistema = x.Sistema?.Trim() ?? "" };
        if (ReglasBoveda.ValidarSecreto(r.Nombre, r.Tipo, r.Almacen, r.Sistema, r.DiasAviso) is { } v) return Result.Failure<BovedaGuardadoDto>(v);
        return Result.Success(new BovedaGuardadoDto(await repo.GuardarSecreto(c.Token, r, Actor.De(user), ct)));
    }
}

/// <summary>
/// CRE-02 y CRE-03: valor de una cuenta técnica o de un secreto. Mostrar y copiar son permisos propios; cambiar el valor exige editar.
/// Además, la base exige ser miembro del grupo de acceso y registra cada entrega y cada cambio en la auditoría.
/// </summary>
internal sealed class CredencialHandlers(IBovedaRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) :
    ICommandHandler<EntregarCredencialCommand, CredencialValorDto>, ICommandHandler<CambiarCredencialCommand, CredencialCambiadaDto>
{
    public async Task<Result<CredencialValorDto>> Handle(EntregarCredencialCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir(Credencial.Opcion(c.Tipo), c.Copiar ? "copiar" : "mostrar", ct) is { } e) return Result.Failure<CredencialValorDto>(e);
        return Result.Success(await repo.EntregarCredencial(Credencial.Clase(c.Tipo), c.Token, c.Copiar ? "Copiar" : "Mostrar", Actor.De(user), ct));
    }

    public async Task<Result<CredencialCambiadaDto>> Handle(CambiarCredencialCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir(Credencial.Opcion(c.Tipo), "editar", ct) is { } e) return Result.Failure<CredencialCambiadaDto>(e);
        if (ReglasBoveda.ValidarValor(c.Request.Valor, c.Request.Dias) is { } v) return Result.Failure<CredencialCambiadaDto>(v);
        var nota = Credencial.Nulo(c.Request.Nota);
        if (nota?.Length > 200) return Result.Failure<CredencialCambiadaDto>(Error.Validation("nota", "La nota admite hasta 200 caracteres."));
        var vence = clock.Hoy.AddDays(c.Request.Dias);
        var version = await repo.CambiarCredencial(Credencial.Clase(c.Tipo), c.Token, c.Request.Valor, vence, nota, clock.Hoy, Actor.De(user), ct);
        return Result.Success(new CredencialCambiadaDto(version, vence));
    }
}
