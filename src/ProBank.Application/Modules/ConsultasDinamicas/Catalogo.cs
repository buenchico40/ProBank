using ProBank.Application.Abstractions;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.ConsultasDinamicas;

namespace ProBank.Application.Modules.ConsultasDinamicas;

// ---------- Bases de datos (SQL-02) ----------
public sealed record ListarConexionesQuery : IQuery<IReadOnlyList<ConexionDto>>;
public sealed record DetalleConexionQuery(string Codigo) : IQuery<ConexionDetalleDto>;
public sealed record GuardarConexionCommand(string? Codigo, GuardarConexionRequest Request) : ICommand<string>;
public sealed record EliminarConexionCommand(string Codigo) : ICommand<string>;
public sealed record ProbarConexionCommand(string Codigo) : ICommand<PruebaConexionDto>;

// ---------- Catálogo de consultas (SQL-01, SQL-03, SQL-04) ----------
public sealed record ListarConsultasQuery : IQuery<IReadOnlyList<ConsultaDto>>;
public sealed record DetalleConsultaQuery(string Codigo) : IQuery<ConsultaDetalleDto>;
public sealed record OpcionesConsultaQuery : IQuery<OpcionesConsultaDto>;
public sealed record GuardarConsultaCommand(string? Codigo, GuardarConsultaRequest Request) : ICommand<string>;
/// <summary>Lee los parámetros y columnas del procedimiento en una de las bases permitidas.</summary>
public sealed record LeerDefinicionCommand(string Codigo, string Conexion) : ICommand<string>;
public sealed record GuardarParametrosCommand(string Codigo, GuardarParametrosRequest Request) : ICommand<string>;
public sealed record CambiarEstadoConsultaCommand(string Codigo, string Accion) : ICommand<string>;
public sealed record EliminarConsultaCommand(string Codigo) : ICommand<string>;

internal sealed class CatalogoConsultas(IConsultaRepository repo, IPermisos permisos, IClock clock) :
    IQueryHandler<ListarConexionesQuery, IReadOnlyList<ConexionDto>>,
    IQueryHandler<DetalleConexionQuery, ConexionDetalleDto>,
    IQueryHandler<ListarConsultasQuery, IReadOnlyList<ConsultaDto>>,
    IQueryHandler<DetalleConsultaQuery, ConsultaDetalleDto>,
    IQueryHandler<OpcionesConsultaQuery, OpcionesConsultaDto>
{
    public async Task<Result<IReadOnlyList<ConexionDto>>> Handle(ListarConexionesQuery q, CancellationToken ct) =>
        await permisos.Exigir("conexiones", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<ConexionDto>>(e) : Result.Success(await repo.ListarConexiones(ct));

    public async Task<Result<ConexionDetalleDto>> Handle(DetalleConexionQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("conexiones", "ver", ct) is { } e) return Result.Failure<ConexionDetalleDto>(e);
        return await repo.DetalleConexion(q.Codigo, ct) is { } d ? Result.Success(d) : Result.Failure<ConexionDetalleDto>(Error.NotFound("La base de datos"));
    }

    public async Task<Result<IReadOnlyList<ConsultaDto>>> Handle(ListarConsultasQuery q, CancellationToken ct) =>
        await permisos.Exigir("consultas", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<ConsultaDto>>(e) : Result.Success(await repo.ListarConsultas(ct));

    public async Task<Result<ConsultaDetalleDto>> Handle(DetalleConsultaQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("consultas", "ver", ct) is { } e) return Result.Failure<ConsultaDetalleDto>(e);
        return await repo.DetalleConsulta(q.Codigo, ct) is { } d ? Result.Success(d) : Result.Failure<ConsultaDetalleDto>(Error.NotFound("La consulta"));
    }

    public async Task<Result<OpcionesConsultaDto>> Handle(OpcionesConsultaQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("consultas", "ver", ct) is { } e) return Result.Failure<OpcionesConsultaDto>(e);
        var o = await repo.Opciones(ct);
        return Result.Success(new OpcionesConsultaDto(o.Roles, o.Usuarios, o.Conexiones, ResolverParametros.Variables(clock.Hoy),
            ReglasConsultas.Formatos, ReglasConsultas.Autenticaciones));
    }
}

internal sealed class CatalogoComandos(IConsultaRepository repo, IConsultaExecutor executor, IPermisos permisos, ICurrentUser user, IAuditLog audit) :
    ICommandHandler<GuardarConexionCommand, string>, ICommandHandler<EliminarConexionCommand, string>, ICommandHandler<ProbarConexionCommand, PruebaConexionDto>,
    ICommandHandler<GuardarConsultaCommand, string>, ICommandHandler<LeerDefinicionCommand, string>, ICommandHandler<GuardarParametrosCommand, string>,
    ICommandHandler<CambiarEstadoConsultaCommand, string>, ICommandHandler<EliminarConsultaCommand, string>
{
    public async Task<Result<string>> Handle(GuardarConexionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("conexiones", c.Codigo is null ? "crear" : "editar", ct) is { } e) return Result.Failure<string>(e);
        var r = c.Request with { Nombre = c.Request.Nombre?.Trim() ?? "", Servidor = c.Request.Servidor?.Trim() ?? "", BaseDatos = c.Request.BaseDatos?.Trim() ?? "",
            SecretoKeyVault = c.Request.SecretoKeyVault?.Trim() ?? "" };
        if (ReglasConsultas.ValidarConexion(r.Nombre, r.Servidor, r.BaseDatos, r.Autenticacion, r.SecretoKeyVault, r.Estado) is { } v) return Result.Failure<string>(v);
        return Result.Success(await repo.GuardarConexion(c.Codigo, r, Actor.De(user), ct));
    }

    public async Task<Result<string>> Handle(EliminarConexionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("conexiones", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        await repo.EliminarConexion(c.Codigo, Actor.De(user), ct);
        return Result.Success($"Base de datos {c.Codigo} eliminada");
    }

    /// <summary>Abre la conexión con la cadena de Key Vault y mide la respuesta; el resultado queda en la auditoría.</summary>
    public async Task<Result<PruebaConexionDto>> Handle(ProbarConexionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("conexiones", "editar", ct) is { } e) return Result.Failure<PruebaConexionDto>(e);
        var r = await executor.Probar(c.Codigo, ct);
        await audit.Registrar("Consultas dinámicas", "Prueba de la base de datos " + c.Codigo, (r.Exitosa ? "Exitosa · " : "Fallida · ") + r.Mensaje, ct);
        return Result.Success(r);
    }

    public async Task<Result<string>> Handle(GuardarConsultaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("consultas", c.Codigo is null ? "crear" : "editar", ct) is { } e) return Result.Failure<string>(e);
        var r = c.Request with
        {
            Nombre = c.Request.Nombre?.Trim() ?? "", Procedimiento = c.Request.Procedimiento?.Trim() ?? "",
            Descripcion = string.IsNullOrWhiteSpace(c.Request.Descripcion) ? null : c.Request.Descripcion.Trim(),
            Conexiones = c.Request.Conexiones ?? [], Roles = c.Request.Roles ?? [], Aprobadores = c.Request.Aprobadores ?? [],
        };
        if (ReglasConsultas.ValidarConsulta(r.Nombre, r.Procedimiento, r.TiempoMaximoSeg, r.LimiteFilas, r.Descripcion) is { } v) return Result.Failure<string>(v);
        var (codigo, volvio) = await repo.GuardarConsulta(c.Codigo, r, Actor.De(user), ct);
        return Result.Success(c.Codigo is null ? $"Consulta {codigo} creada en borrador: lea la definición del procedimiento y publíquela"
            : volvio ? $"Consulta {codigo} guardada: cambió su procedimiento o sus bases y volvió a Borrador; revísela y publíquela de nuevo"
            : $"Consulta {codigo} guardada");
    }

    /// <summary>SQL-01: los parámetros y columnas se leen del catálogo de la base (no se escriben a mano).</summary>
    public async Task<Result<string>> Handle(LeerDefinicionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("consultas", "editar", ct) is { } e) return Result.Failure<string>(e);
        if (await repo.DetalleConsulta(c.Codigo, ct) is not { } q) return Result.Failure<string>(Error.NotFound("La consulta"));
        if (!q.Conexiones.Any(x => x.Codigo == c.Conexion && x.Estado == "Activo"))
            return Result.Failure<string>(Error.Validation("conexion", "Elija una base de datos activa permitida para la consulta."));
        DefinicionProcedimiento? d;
        try { d = await executor.LeerDefinicion(c.Conexion, q.Procedimiento, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result.Failure<string>(new Error("definicion", $"No se pudo leer la definición en {c.Conexion}: {ex.Message}"));
        }
        if (d is null) return Result.Failure<string>(Error.Validation("procedimiento", $"El procedimiento {q.Procedimiento} no existe en la base {c.Conexion}."));
        var parametros = d.Parametros.Select(p => (p.Nombre, p.TipoSql, ReglasConsultas.TipoDesdeSql(p.TipoSql).ToString())).ToList();
        var volvio = await repo.GuardarDefinicion(c.Codigo, c.Conexion, parametros, d.Columnas, Actor.De(user), ct);
        return Result.Success($"Definición leída: {parametros.Count} parámetro(s) y {d.Columnas.Count} columna(s)"
            + (volvio ? ". Cambiaron los parámetros: la consulta volvió a Borrador" : ""));
    }

    public async Task<Result<string>> Handle(GuardarParametrosCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("consultas", "editar", ct) is { } e) return Result.Failure<string>(e);
        var ps = c.Request.Parametros ?? [];
        foreach (var p in ps)
        {
            if (!Enum.TryParse<TipoParametro>(p.Tipo, out var tipo)) return Result.Failure<string>(Error.Validation("tipo", $"{p.Nombre}: el control es Texto, Numero, Fecha o Lista."));
            if (ReglasConsultas.ValidarParametro(new ParametroConsulta(p.Nombre, p.Etiqueta?.Trim() ?? "", tipo, p.Obligatorio, p.ValorDefecto, p.Opciones)) is { } v)
                return Result.Failure<string>(v);
        }
        await repo.GuardarParametros(c.Codigo, ps.Select(p => p with { Etiqueta = p.Etiqueta.Trim(), ValorDefecto = p.ValorDefecto?.Trim(),
            Opciones = p.Opciones is null ? null : string.Join(",", p.Opciones.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) }).ToList(),
            Actor.De(user), ct);
        return Result.Success($"Parámetros de {c.Codigo} actualizados");
    }

    public async Task<Result<string>> Handle(CambiarEstadoConsultaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("consultas", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        if (c.Accion is not ("Publicar" or "Retirar")) return Result.Failure<string>(Error.Validation("accion", "La acción es Publicar o Retirar."));
        await repo.CambiarEstadoConsulta(c.Codigo, c.Accion, Actor.De(user), ct);
        return Result.Success(c.Accion == "Publicar" ? $"Consulta {c.Codigo} publicada: ya la pueden ejecutar sus roles"
            : $"Consulta {c.Codigo} retirada: vuelve a Borrador y no se ejecuta ni se envía");
    }

    public async Task<Result<string>> Handle(EliminarConsultaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("consultas", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        await repo.EliminarConsulta(c.Codigo, Actor.De(user), ct);
        return Result.Success($"Consulta {c.Codigo} eliminada");
    }
}
