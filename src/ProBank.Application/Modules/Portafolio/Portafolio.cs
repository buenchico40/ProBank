using ProBank.Application.Abstractions;
using ProBank.Contracts.Portafolio;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Portafolio;

namespace ProBank.Application.Modules.Portafolio;

/// <summary>Datos del periodo para armar el informe de seguimiento.</summary>
public sealed record InformeDatos(IReadOnlyList<ResumenInformeDto> Resumen, IReadOnlyList<BitacoraFila> Bitacora,
    IReadOnlyList<AcuerdoInformeFila> Acuerdos, IReadOnlyList<TemaRevisionFila> Temas);
public sealed record BitacoraFila(DateTime Fecha, string Item, string Tarea, string Titulo, string Evento, string? Detalle, string Usuario);
public sealed record AcuerdoInformeFila(DateTime FechaRevision, string Item, string Codigo, string Descripcion, string Responsable, DateTime FechaCumplimiento, string Estado);
public sealed record TemaRevisionFila(DateTime Fecha, string Item, string Temas);

public interface IPortafolioRepository
{
    Task<IReadOnlyList<ItemPortafolioDto>> ListarItems(bool soloAbiertos, CancellationToken ct);
    Task<ItemDetalleDto?> ObtenerItem(Guid token, DateOnly hoy, CancellationToken ct);
    Task<ItemGuardadoDto> GuardarItem(Guid? token, GuardarItemRequest r, Actor actor, CancellationToken ct);
    /// <summary>Devuelve las rutas de los documentos adjuntos que se quitaron con el ítem.</summary>
    Task<IReadOnlyList<string>> EliminarItem(Guid token, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<AdjuntoDto>> ListarAdjuntos(AmbitoAdjunto ambito, Guid duenoToken, CancellationToken ct);
    Task<(AmbitoAdjunto Ambito, string Nombre, string TipoContenido, string Ruta)?> ObtenerAdjunto(Guid token, CancellationToken ct);
    Task RegistrarAdjunto(AmbitoAdjunto ambito, Guid duenoToken, Guid token, string nombre, string descripcion, string tipoContenido, long tamano, string ruta, Actor actor, CancellationToken ct);
    Task ActualizarAdjunto(Guid token, string descripcion, Actor actor, CancellationToken ct);
    /// <summary>Devuelve la ruta del archivo para borrarlo del almacén.</summary>
    Task<string> EliminarAdjunto(Guid token, Actor actor, CancellationToken ct);
    Task<IReadOnlyList<ParametroAdjuntoDto>> ParametrosAdjunto(CancellationToken ct);
    Task GuardarParametroAdjunto(AmbitoAdjunto ambito, string extensiones, int maximoMB, Actor actor, CancellationToken ct);
    Task GuardarHitos(Guid itemToken, IReadOnlyList<HitoRequest> hitos, Actor actor, CancellationToken ct);
    Task<CronogramaDto> Cronograma(Guid? itemToken, CancellationToken ct);

    Task<IReadOnlyList<AcuerdoDto>> ListarAcuerdos(DateOnly hoy, CancellationToken ct);
    Task<int> RegistrarAcuerdos(RegistrarAcuerdosRequest r, Actor actor, CancellationToken ct);
    Task ActualizarAcuerdo(int acuerdoId, ActualizarAcuerdoRequest r, Actor actor, CancellationToken ct);
    Task EliminarAcuerdo(int acuerdoId, Actor actor, CancellationToken ct);
    Task CumplirAcuerdo(int acuerdoId, DateOnly hoy, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<InformeDto>> ListarInformes(CancellationToken ct);
    Task<int> GuardarInforme(int? informeId, GuardarInformeRequest r, Actor actor, CancellationToken ct);
    Task<InformeContenidoDto?> ObtenerInforme(int informeId, CancellationToken ct);
    Task<InformeDatos> DatosInforme(DateOnly desde, DateOnly hasta, CancellationToken ct);
    Task GenerarInforme(int informeId, string rutaPdf, Actor actor, CancellationToken ct);

    Task<ParametrizacionDto> Parametrizacion(CancellationToken ct);
    Task GuardarAccionRol(string accion, IReadOnlyList<string> roles, Actor actor, CancellationToken ct);
}

// ---------- Consultas ----------
public sealed record ListarItemsQuery(bool SoloAbiertos) : IQuery<IReadOnlyList<ItemPortafolioDto>>;
public sealed record ObtenerItemQuery(Guid Token) : IQuery<ItemDetalleDto>;
public sealed record CronogramaQuery(Guid? ItemToken) : IQuery<CronogramaDto>;
public sealed record ListarAcuerdosQuery : IQuery<IReadOnlyList<AcuerdoDto>>;
public sealed record ListarInformesQuery : IQuery<IReadOnlyList<InformeDto>>;
public sealed record ObtenerInformeQuery(int InformeId) : IQuery<InformeContenidoDto>;
public sealed record ResumenInformeQuery(int InformeId) : IQuery<IReadOnlyList<ResumenInformeDto>>;
public sealed record ParametrizacionQuery : IQuery<ParametrizacionDto>;

// ---------- Comandos ----------
public sealed record GuardarItemCommand(Guid? Token, GuardarItemRequest Request) : ICommand<ItemGuardadoDto>;
public sealed record EliminarItemCommand(Guid Token) : ICommand<string>;
public sealed record GuardarHitosCommand(Guid ItemToken, IReadOnlyList<HitoRequest> Hitos) : ICommand<string>;
public sealed record RegistrarAcuerdosCommand(RegistrarAcuerdosRequest Request) : ICommand<int>;
public sealed record ActualizarAcuerdoCommand(int AcuerdoId, ActualizarAcuerdoRequest Request) : ICommand<string>;
public sealed record EliminarAcuerdoCommand(int AcuerdoId) : ICommand<string>;
public sealed record CumplirAcuerdoCommand(int AcuerdoId) : ICommand<string>;
public sealed record GuardarInformeCommand(int? InformeId, GuardarInformeRequest Request) : ICommand<int>;
public sealed record GenerarInformeCommand(int InformeId) : ICommand<string>;
public sealed record InformePdfQuery(int InformeId) : IQuery<ArchivoAdjunto>;
public sealed record GuardarAccionRolCommand(string Accion, IReadOnlyList<string> Roles) : ICommand<string>;

internal sealed class PortafolioConsultas(IPortafolioRepository repo, IPermisos permisos, IClock clock) :
    IQueryHandler<ListarItemsQuery, IReadOnlyList<ItemPortafolioDto>>,
    IQueryHandler<ObtenerItemQuery, ItemDetalleDto>,
    IQueryHandler<CronogramaQuery, CronogramaDto>,
    IQueryHandler<ListarAcuerdosQuery, IReadOnlyList<AcuerdoDto>>,
    IQueryHandler<ListarInformesQuery, IReadOnlyList<InformeDto>>,
    IQueryHandler<ObtenerInformeQuery, InformeContenidoDto>,
    IQueryHandler<ResumenInformeQuery, IReadOnlyList<ResumenInformeDto>>,
    IQueryHandler<ParametrizacionQuery, ParametrizacionDto>
{
    // Los formularios de tareas y acuerdos también listan los ítems abiertos.
    public async Task<Result<IReadOnlyList<ItemPortafolioDto>>> Handle(ListarItemsQuery q, CancellationToken ct) =>
        await permisos.Tiene("proyectos", "ver", ct) || await permisos.Tiene("tareas", "ver", ct) || await permisos.Tiene("acuerdos", "ver", ct)
            ? Result.Success(await repo.ListarItems(q.SoloAbiertos, ct)) : Result.Failure<IReadOnlyList<ItemPortafolioDto>>(Acceso.SinPermiso);

    public async Task<Result<ItemDetalleDto>> Handle(ObtenerItemQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("proyectos", "ver", ct) is { } e) return Result.Failure<ItemDetalleDto>(e);
        return await repo.ObtenerItem(q.Token, clock.Hoy, ct) is { } i ? Result.Success(i) : Result.Failure<ItemDetalleDto>(Error.NotFound("El proyecto, mejora o bug"));
    }

    public async Task<Result<CronogramaDto>> Handle(CronogramaQuery q, CancellationToken ct) =>
        await permisos.Exigir("gantt", "ver", ct) is { } e ? Result.Failure<CronogramaDto>(e) : Result.Success(await repo.Cronograma(q.ItemToken, ct));

    public async Task<Result<IReadOnlyList<AcuerdoDto>>> Handle(ListarAcuerdosQuery q, CancellationToken ct) =>
        await permisos.Exigir("acuerdos", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<AcuerdoDto>>(e) : Result.Success(await repo.ListarAcuerdos(clock.Hoy, ct));

    public async Task<Result<IReadOnlyList<InformeDto>>> Handle(ListarInformesQuery q, CancellationToken ct) =>
        await permisos.Exigir("informes", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<InformeDto>>(e) : Result.Success(await repo.ListarInformes(ct));

    public async Task<Result<InformeContenidoDto>> Handle(ObtenerInformeQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("informes", "ver", ct) is { } e) return Result.Failure<InformeContenidoDto>(e);
        return await repo.ObtenerInforme(q.InformeId, ct) is { } i ? Result.Success(i) : Result.Failure<InformeContenidoDto>(Error.NotFound("El informe"));
    }

    public async Task<Result<IReadOnlyList<ResumenInformeDto>>> Handle(ResumenInformeQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("informes", "ver", ct) is { } e) return Result.Failure<IReadOnlyList<ResumenInformeDto>>(e);
        if (await repo.ObtenerInforme(q.InformeId, ct) is not { } i) return Result.Failure<IReadOnlyList<ResumenInformeDto>>(Error.NotFound("El informe"));
        return Result.Success((await repo.DatosInforme(i.Desde, i.Hasta, ct)).Resumen);
    }

    public async Task<Result<ParametrizacionDto>> Handle(ParametrizacionQuery q, CancellationToken ct) =>
        await permisos.Exigir("paramproy", "ver", ct) is { } e ? Result.Failure<ParametrizacionDto>(e) : Result.Success(await repo.Parametrizacion(ct));
}

internal sealed class GuardarItemHandler(IPortafolioRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<GuardarItemCommand, ItemGuardadoDto>
{
    public async Task<Result<ItemGuardadoDto>> Handle(GuardarItemCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("proyectos", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<ItemGuardadoDto>(e);
        var r = c.Request with { Titulo = c.Request.Titulo.Trim(), ResponsableIds = c.Request.ResponsableIds.Distinct().ToList() };
        if (ReglasItem.Validar(r.Tipo, r.Titulo, r.ResponsableIds, r.Estado) is { } v) return Result.Failure<ItemGuardadoDto>(v);
        return Result.Success(await repo.GuardarItem(c.Token, r, Actor.De(user), ct));
    }
}

internal sealed class EliminarItemHandler(IPortafolioRepository repo, IPermisos permisos, ICurrentUser user, IAlmacenArchivos almacen) : ICommandHandler<EliminarItemCommand, string>
{
    public async Task<Result<string>> Handle(EliminarItemCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("proyectos", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        foreach (var ruta in await repo.EliminarItem(c.Token, Actor.De(user), ct))
            await almacen.Eliminar(Adjuntos.Contenedor, ruta, CancellationToken.None);
        return Result.Success("Borrador eliminado");
    }
}

internal sealed class GuardarHitosHandler(IPortafolioRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<GuardarHitosCommand, string>
{
    public async Task<Result<string>> Handle(GuardarHitosCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("proyectos", "editar", ct) is { } e) return Result.Failure<string>(e);
        if (c.Hitos.Any(h => string.IsNullOrWhiteSpace(h.Nombre) || h.Nombre.Trim().Length > 150))
            return Result.Failure<string>(Error.Validation("hito", "Cada hito necesita un nombre (máximo 150 caracteres)."));
        if (c.Hitos.Any(h => h.Inicio is not null && h.Fin is not null && h.Fin < h.Inicio))
            return Result.Failure<string>(Error.Validation("hito", "La fecha de fin del hito no puede ser anterior al inicio."));
        await repo.GuardarHitos(c.ItemToken, c.Hitos.Select(h => h with { Nombre = h.Nombre.Trim() }).ToList(), Actor.De(user), ct);
        return Result.Success("Hitos guardados");
    }
}

/// <summary>PRY-08: una revisión semanal con uno o varios acuerdos (responsable y fecha de cumplimiento).</summary>
internal sealed class RegistrarAcuerdosHandler(IPortafolioRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<RegistrarAcuerdosCommand, int>
{
    public async Task<Result<int>> Handle(RegistrarAcuerdosCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("acuerdos", "crear", ct) is { } e) return Result.Failure<int>(e);
        var acuerdos = c.Request.Acuerdos.Where(a => !string.IsNullOrWhiteSpace(a.Descripcion)).Select(a => a with { Descripcion = a.Descripcion.Trim() }).ToList();
        if (acuerdos.Count == 0) return Result.Failure<int>(Error.Validation("acuerdos", "Agregue al menos un acuerdo."));
        if (acuerdos.Any(a => a.Descripcion.Length > 500)) return Result.Failure<int>(Error.Validation("acuerdos", "Cada acuerdo admite hasta 500 caracteres."));
        if (acuerdos.Any(a => a.Fecha < c.Request.Fecha)) return Result.Failure<int>(Error.Validation("acuerdos", "La fecha de cumplimiento no puede ser anterior a la revisión."));
        return Result.Success(await repo.RegistrarAcuerdos(c.Request with { Acuerdos = acuerdos, Temas = c.Request.Temas?.Trim() }, Actor.De(user), ct));
    }
}

internal sealed class AcuerdoHandlers(IPortafolioRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) :
    ICommandHandler<ActualizarAcuerdoCommand, string>, ICommandHandler<EliminarAcuerdoCommand, string>, ICommandHandler<CumplirAcuerdoCommand, string>
{
    public async Task<Result<string>> Handle(ActualizarAcuerdoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("acuerdos", "editar", ct) is { } e) return Result.Failure<string>(e);
        if (string.IsNullOrWhiteSpace(c.Request.Descripcion) || c.Request.Descripcion.Trim().Length > 500)
            return Result.Failure<string>(Error.Validation("descripcion", "La descripción es obligatoria (máximo 500 caracteres)."));
        await repo.ActualizarAcuerdo(c.AcuerdoId, c.Request with { Descripcion = c.Request.Descripcion.Trim() }, Actor.De(user), ct);
        return Result.Success("Acuerdo actualizado");
    }

    public async Task<Result<string>> Handle(EliminarAcuerdoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("acuerdos", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        await repo.EliminarAcuerdo(c.AcuerdoId, Actor.De(user), ct);
        return Result.Success("Acuerdo eliminado");
    }

    // El responsable del acuerdo lo marca cumplido aunque su rol no vea la opción; los demás necesitan la función
    // «cumplir» y la acción parametrizada (PRY-09). Ambas reglas las valida el SP.
    public async Task<Result<string>> Handle(CumplirAcuerdoCommand c, CancellationToken ct)
    {
        await repo.CumplirAcuerdo(c.AcuerdoId, clock.Hoy, Actor.De(user), ct);
        return Result.Success("Acuerdo cumplido");
    }
}

internal sealed class InformeHandlers(IPortafolioRepository repo, IPermisos permisos, ICurrentUser user, IClock clock,
    IGeneradorInforme generador, IAlmacenArchivos almacen) :
    ICommandHandler<GuardarInformeCommand, int>, ICommandHandler<GenerarInformeCommand, string>, IQueryHandler<InformePdfQuery, ArchivoAdjunto>
{
    public async Task<Result<int>> Handle(GuardarInformeCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("informes", c.InformeId is null ? "crear" : "editar", ct) is { } e) return Result.Failure<int>(e);
        var r = c.Request;
        if (r.Tipo is not ("Semanal" or "Mensual")) return Result.Failure<int>(Error.Validation("tipo", "El informe es Semanal o Mensual."));
        if (r.Hasta < r.Desde || r.Hasta.DayNumber - r.Desde.DayNumber > 92) return Result.Failure<int>(Error.Validation("periodo", "El periodo no es válido (máximo 3 meses)."));
        return Result.Success(await repo.GuardarInforme(c.InformeId, r, Actor.De(user), ct));
    }

    /// <summary>
    /// Arma el informe (gráficos y detalle del periodo) con DevExpress Reporting y guarda el PDF como copia en Blob Storage
    /// (se puede regenerar: reemplaza la copia anterior).
    /// </summary>
    public async Task<Result<string>> Handle(GenerarInformeCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("informes", "generar", ct) is { } e) return Result.Failure<string>(e);
        if (await repo.ObtenerInforme(c.InformeId, ct) is not { } i) return Result.Failure<string>(Error.NotFound("El informe"));
        var datos = await repo.DatosInforme(i.Desde, i.Hasta, ct);
        var pdf = generador.Pdf(InformeModelo.Armar(i, datos, user.Nombre, clock.Ahora));
        var ruta = RutaPdf(i.Codigo);
        using (var s = new MemoryStream(pdf, writable: false)) await almacen.Guardar(Adjuntos.Contenedor, ruta, s, "application/pdf", ct);
        await repo.GenerarInforme(i.InformeId, ruta, Actor.De(user), ct);
        return Result.Success($"Informe {i.Codigo} generado con {datos.Resumen.Count} ítem(s)");
    }

    public async Task<Result<ArchivoAdjunto>> Handle(InformePdfQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("informes", "ver", ct) is { } e) return Result.Failure<ArchivoAdjunto>(e);
        if (await repo.ObtenerInforme(q.InformeId, ct) is not { TienePdf: true } i) return Result.Failure<ArchivoAdjunto>(Error.NotFound("El PDF del informe"));
        return await almacen.Abrir(Adjuntos.Contenedor, RutaPdf(i.Codigo), ct) is { } s
            ? Result.Success(new ArchivoAdjunto(i.Codigo + ".pdf", "application/pdf", s))
            : Result.Failure<ArchivoAdjunto>(Error.NotFound("El PDF del informe"));
    }

    private static string RutaPdf(string codigo) => $"pry/informes/{codigo}.pdf";
}

internal sealed class GuardarAccionRolHandler(IPortafolioRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<GuardarAccionRolCommand, string>
{
    public async Task<Result<string>> Handle(GuardarAccionRolCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("paramproy", "editar", ct) is { } e) return Result.Failure<string>(e);
        if (c.Roles.Count == 0) return Result.Failure<string>(Error.Validation("roles", "Seleccione al menos un rol."));
        await repo.GuardarAccionRol(c.Accion, c.Roles, Actor.De(user), ct);
        return Result.Success(string.Join(", ", c.Roles));
    }
}
