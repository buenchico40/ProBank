using ProBank.Application.Abstractions;
using ProBank.Contracts.Portafolio;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Portafolio;

namespace ProBank.Application.Modules.Portafolio;

/// <summary>Contenedor de Blob Storage de los documentos adjuntos (infra/modules/storage).</summary>
internal static class Adjuntos
{
    public const string Contenedor = "adjuntos";

    /// <summary>Ver: proyectos para los de un ítem, tareas para los de una tarea.</summary>
    public static Task<Error?> ExigirVer(IPermisos permisos, AmbitoAdjunto ambito, CancellationToken ct) =>
        permisos.Exigir(ambito == AmbitoAdjunto.Item ? "proyectos" : "tareas", "ver", ct);

    /// <summary>Modificar: quien crea o edita ítems; en una tarea, su técnico asignado o quien edita tareas (la base lo vuelve a validar).</summary>
    public static async Task<Error?> ExigirModificar(IPermisos permisos, ITareaRepository tareas, ICurrentUser user, AmbitoAdjunto ambito, Guid? tareaToken, CancellationToken ct)
    {
        if (ambito == AmbitoAdjunto.Item)
            return await permisos.Tiene("proyectos", "editar", ct) || await permisos.Tiene("proyectos", "crear", ct) ? null : Acceso.SinPermiso;
        if (await permisos.Tiene("tareas", "editar", ct)) return null;
        var t = tareaToken is null ? null : await tareas.Obtener(tareaToken.Value, ct);
        return t?.TecnicoId == user.UsuarioId ? null : Error.Forbidden("Solo el técnico asignado o quien edita tareas puede modificar sus documentos.");
    }
}

/// <summary>Archivo listo para descargar.</summary>
public sealed record ArchivoAdjunto(string Nombre, string TipoContenido, Stream Contenido);

public sealed record ListarAdjuntosQuery(AmbitoAdjunto Ambito, Guid DuenoToken) : IQuery<IReadOnlyList<AdjuntoDto>>;
public sealed record DescargarAdjuntoQuery(Guid Token) : IQuery<ArchivoAdjunto>;
public sealed record ParametrosAdjuntoQuery : IQuery<ParametrosAdjuntoDto>;
public sealed record SubirAdjuntoCommand(AmbitoAdjunto Ambito, Guid DuenoToken, string Nombre, string? Descripcion, byte[] Contenido) : ICommand<AdjuntoDto>;
public sealed record ActualizarAdjuntoCommand(Guid Token, string? Descripcion) : ICommand<string>;
public sealed record EliminarAdjuntoCommand(Guid Token) : ICommand<string>;
public sealed record GuardarParametroAdjuntoCommand(string Ambito, GuardarParametroAdjuntoRequest Request) : ICommand<string>;

internal sealed class AdjuntosConsultas(IPortafolioRepository repo, IPermisos permisos, IAlmacenArchivos almacen) :
    IQueryHandler<ListarAdjuntosQuery, IReadOnlyList<AdjuntoDto>>,
    IQueryHandler<DescargarAdjuntoQuery, ArchivoAdjunto>,
    IQueryHandler<ParametrosAdjuntoQuery, ParametrosAdjuntoDto>
{
    public async Task<Result<IReadOnlyList<AdjuntoDto>>> Handle(ListarAdjuntosQuery q, CancellationToken ct) =>
        await Adjuntos.ExigirVer(permisos, q.Ambito, ct) is { } e ? Result.Failure<IReadOnlyList<AdjuntoDto>>(e) : Result.Success(await repo.ListarAdjuntos(q.Ambito, q.DuenoToken, ct));

    public async Task<Result<ArchivoAdjunto>> Handle(DescargarAdjuntoQuery q, CancellationToken ct)
    {
        if (await repo.ObtenerAdjunto(q.Token, ct) is not { } a) return Result.Failure<ArchivoAdjunto>(Error.NotFound("El documento"));
        if (await Adjuntos.ExigirVer(permisos, a.Ambito, ct) is { } e) return Result.Failure<ArchivoAdjunto>(e);
        return await almacen.Abrir(Adjuntos.Contenedor, a.Ruta, ct) is { } s
            ? Result.Success(new ArchivoAdjunto(a.Nombre, a.TipoContenido, s))
            : Result.Failure<ArchivoAdjunto>(Error.NotFound("El archivo del documento"));
    }

    // Quien ve proyectos o tareas necesita saber qué puede adjuntar
    public async Task<Result<ParametrosAdjuntoDto>> Handle(ParametrosAdjuntoQuery q, CancellationToken ct) =>
        await permisos.Tiene("proyectos", "ver", ct) || await permisos.Tiene("tareas", "ver", ct) || await permisos.Tiene("paramproy", "ver", ct)
            ? Result.Success(new ParametrosAdjuntoDto(await repo.ParametrosAdjunto(ct), ReglasAdjunto.Extensiones, ReglasAdjunto.MaximoMBPermitido))
            : Result.Failure<ParametrosAdjuntoDto>(Acceso.SinPermiso);
}

/// <summary>Valida con la parametrización del ámbito, guarda el archivo y lo registra; si el registro falla, el archivo se borra.</summary>
internal sealed class SubirAdjuntoHandler(IPortafolioRepository repo, ITareaRepository tareas, IPermisos permisos, ICurrentUser user, IAlmacenArchivos almacen)
    : ICommandHandler<SubirAdjuntoCommand, AdjuntoDto>
{
    public async Task<Result<AdjuntoDto>> Handle(SubirAdjuntoCommand c, CancellationToken ct)
    {
        var tareaToken = c.Ambito == AmbitoAdjunto.Tarea ? c.DuenoToken : (Guid?)null;
        if (await Adjuntos.ExigirModificar(permisos, tareas, user, c.Ambito, tareaToken, ct) is { } e) return Result.Failure<AdjuntoDto>(e);
        var p = (await repo.ParametrosAdjunto(ct)).FirstOrDefault(x => x.Ambito == c.Ambito.ToString());
        if (p is null) return Result.Failure<AdjuntoDto>(Error.Validation("parametro", "Los adjuntos no están parametrizados."));
        var cabecera = c.Contenido.AsSpan(0, Math.Min(ReglasAdjunto.BytesCabecera, c.Contenido.Length));
        var (nombre, tipo, error) = ReglasAdjunto.Validar(c.Nombre, c.Contenido.LongLength, cabecera, p.Extensiones, p.MaximoMB);
        if (error is not null) return Result.Failure<AdjuntoDto>(error);
        var (descripcion, ed) = ReglasAdjunto.ValidarDescripcion(c.Descripcion);
        if (ed is not null) return Result.Failure<AdjuntoDto>(ed);

        var token = Guid.NewGuid();
        var ruta = $"pry/{c.Ambito.ToString().ToLowerInvariant()}/{c.DuenoToken:N}/{token:N}{Path.GetExtension(nombre).ToLowerInvariant()}";
        using (var s = new MemoryStream(c.Contenido, writable: false)) await almacen.Guardar(Adjuntos.Contenedor, ruta, s, tipo, ct);
        try
        {
            await repo.RegistrarAdjunto(c.Ambito, c.DuenoToken, token, nombre, descripcion, tipo, c.Contenido.LongLength, ruta, Actor.De(user), ct);
        }
        catch
        {
            await almacen.Eliminar(Adjuntos.Contenedor, ruta, CancellationToken.None);
            throw;
        }
        return Result.Success(new AdjuntoDto(token, nombre, descripcion, tipo, c.Contenido.LongLength, user.Nombre, DateTime.Now));
    }
}

internal sealed class ActualizarAdjuntoHandler(IPortafolioRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<ActualizarAdjuntoCommand, string>
{
    public async Task<Result<string>> Handle(ActualizarAdjuntoCommand c, CancellationToken ct)
    {
        if (await repo.ObtenerAdjunto(c.Token, ct) is not { } a) return Result.Failure<string>(Error.NotFound("El documento"));
        // El técnico de la tarea lo valida la base (aquí no se conoce la tarea del documento)
        if (a.Ambito == AmbitoAdjunto.Item && await permisos.Exigir("proyectos", "editar", ct) is { } e) return Result.Failure<string>(e);
        var (descripcion, ed) = ReglasAdjunto.ValidarDescripcion(c.Descripcion);
        if (ed is not null) return Result.Failure<string>(ed);
        await repo.ActualizarAdjunto(c.Token, descripcion, Actor.De(user), ct);
        return Result.Success("Descripción actualizada");
    }
}

internal sealed class EliminarAdjuntoHandler(IPortafolioRepository repo, ITareaRepository tareas, IPermisos permisos, ICurrentUser user, IAlmacenArchivos almacen)
    : ICommandHandler<EliminarAdjuntoCommand, string>
{
    public async Task<Result<string>> Handle(EliminarAdjuntoCommand c, CancellationToken ct)
    {
        if (await repo.ObtenerAdjunto(c.Token, ct) is not { } a) return Result.Failure<string>(Error.NotFound("El documento"));
        // El técnico de la tarea lo valida la base (aquí no se conoce la tarea del documento)
        if (a.Ambito == AmbitoAdjunto.Item && await permisos.Exigir("proyectos", "editar", ct) is { } e) return Result.Failure<string>(e);
        var ruta = await repo.EliminarAdjunto(c.Token, Actor.De(user), ct);
        await almacen.Eliminar(Adjuntos.Contenedor, ruta, ct);
        return Result.Success("Documento quitado");
    }
}

internal sealed class GuardarParametroAdjuntoHandler(IPortafolioRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<GuardarParametroAdjuntoCommand, string>
{
    public async Task<Result<string>> Handle(GuardarParametroAdjuntoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("paramproy", "editar", ct) is { } e) return Result.Failure<string>(e);
        if (!Enum.TryParse<AmbitoAdjunto>(c.Ambito, out var ambito)) return Result.Failure<string>(Error.Validation("ambito", "El ámbito debe ser Item o Tarea."));
        var (ext, error) = ReglasAdjunto.ValidarParametro(c.Request.Extensiones, c.Request.MaximoMB);
        if (error is not null) return Result.Failure<string>(error);
        await repo.GuardarParametroAdjunto(ambito, ext!, c.Request.MaximoMB, Actor.De(user), ct);
        return Result.Success($"{ext} · máximo {c.Request.MaximoMB} MB");
    }
}
