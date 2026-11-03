using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Portafolio;
using ProBank.Contracts.Presupuesto;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Portafolio;
using ProBank.Domain.Modules.Presupuesto;

namespace ProBank.Application.Modules.Presupuesto;

public interface IPresupuestoRepository
{
    Task<IReadOnlyList<AreaDto>> ListarAreas(bool soloActivas, CancellationToken ct);
    Task GuardarArea(bool nueva, GuardarAreaRequest r, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<PartidaDto>> ListarPartidas(int? anio, string? area, CancellationToken ct);
    Task<PartidaDetalleDto> DetallePartida(Guid token, CancellationToken ct);
    Task<Guid> GuardarPartida(Guid? token, GuardarPartidaRequest r, Actor actor, CancellationToken ct);
    Task CambiarEstadoPartida(Guid token, string accion, Actor actor, CancellationToken ct);
    Task EliminarPartida(Guid token, Actor actor, CancellationToken ct);
    Task<BorradorPreparadoDto> PrepararBorrador(string area, Actor actor, CancellationToken ct);
    /// <summary>Datos del reporte de un área y un año (deja la generación en la auditoría).</summary>
    Task<ReportePresupuestoDatos> DatosReporte(string area, int anio, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<MovimientoDto>> ListarMovimientos(CancellationToken ct);
    Task<Guid> GuardarMovimiento(Guid? token, GuardarMovimientoRequest r, Actor actor, CancellationToken ct);
    Task ResolverMovimiento(Guid token, bool aprobar, string? comentario, Actor actor, CancellationToken ct);
    Task AnularMovimiento(Guid token, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<RenovacionDto>> ListarRenovaciones(DateOnly hoy, CancellationToken ct);
    Task<Guid> GuardarRenovacion(Guid? token, GuardarRenovacionRequest r, Actor actor, CancellationToken ct);
    /// <summary>Devuelve el código de la solicitud de compra creada.</summary>
    Task<string> EnviarRequerimiento(Guid token, string area, Actor actor, CancellationToken ct);
    Task RenovarServicio(Guid token, DateOnly nuevoVence, Actor actor, CancellationToken ct);
    Task RetirarRenovacion(Guid token, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<SolicitudDto>> ListarSolicitudes(CancellationToken ct);
    Task<SolicitudDetalleDto?> DetalleSolicitud(Guid token, CancellationToken ct);
    Task<Guid> GuardarSolicitud(Guid? token, GuardarSolicitudRequest r, Actor actor, CancellationToken ct);
    Task ResolverSolicitud(Guid token, bool aprobar, string? comentario, Actor actor, CancellationToken ct);
    Task AnularSolicitud(Guid token, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<PagoDto>> ListarPagos(CancellationToken ct);
    Task<PagoDetalleDto?> DetallePago(Guid token, CancellationToken ct);
    Task<Guid> GuardarPago(Guid? token, GuardarPagoRequest r, Actor actor, CancellationToken ct);
    Task Pagar(Guid token, DateOnly hoy, Actor actor, CancellationToken ct);
    Task AnularPago(Guid token, Actor actor, CancellationToken ct);

    Task RegistrarDocumento(Guid duenoToken, Guid token, string tipo, string nombre, string tipoContenido, long tamano, string ruta, Actor actor, CancellationToken ct);
    /// <summary>Dueno: «Solicitud» o «Pago».</summary>
    Task<(string Dueno, string Nombre, string TipoContenido, string Ruta)?> ObtenerDocumento(Guid token, CancellationToken ct);
    Task<string> EliminarDocumento(Guid token, Actor actor, CancellationToken ct);
}

internal static class Pres
{
    public static readonly string[] Opciones = ["presupuesto", "movimientos", "renovaciones", "solicitudes", "pagos", "areaspres"];

    /// <summary>Las partidas y las áreas se eligen en todas las opciones del módulo: basta ver cualquiera de ellas.</summary>
    public static async Task<bool> VeAlguna(IPermisos permisos, CancellationToken ct)
    {
        foreach (var o in Opciones) if (await permisos.Tiene(o, "ver", ct)) return true;
        return false;
    }

    public static Error? Rechazo(ResolverRequest r) =>
        !r.Aprobar && string.IsNullOrWhiteSpace(r.Comentario) ? Error.Validation("comentario", "Indique el motivo del rechazo.")
        : r.Comentario?.Trim().Length > 500 ? Error.Validation("comentario", "El comentario admite hasta 500 caracteres.")
        : null;

    public static string? Nulo(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

// ---------- Consultas ----------
public sealed record ListarAreasQuery(bool SoloActivas) : IQuery<IReadOnlyList<AreaDto>>;
public sealed record ListarPartidasQuery(int? Anio, string? Area = null) : IQuery<IReadOnlyList<PartidaDto>>;
public sealed record DetallePartidaQuery(Guid Token) : IQuery<PartidaDetalleDto>;
public sealed record ListarMovimientosQuery : IQuery<IReadOnlyList<MovimientoDto>>;
public sealed record ListarRenovacionesQuery : IQuery<IReadOnlyList<RenovacionDto>>;
public sealed record ListarSolicitudesQuery : IQuery<IReadOnlyList<SolicitudDto>>;
public sealed record DetalleSolicitudQuery(Guid Token) : IQuery<SolicitudDetalleDto>;
public sealed record ListarPagosQuery : IQuery<IReadOnlyList<PagoDto>>;
public sealed record DetallePagoQuery(Guid Token) : IQuery<PagoDetalleDto>;
public sealed record DescargarDocumentoQuery(Guid Token) : IQuery<ArchivoAdjunto>;

// ---------- Comandos ----------
public sealed record GuardarPartidaCommand(Guid? Token, GuardarPartidaRequest Request) : ICommand<Guid>;
public sealed record CambiarEstadoPartidaCommand(Guid Token, string Accion) : ICommand<string>;
public sealed record EliminarPartidaCommand(Guid Token) : ICommand<string>;
public sealed record PrepararBorradorCommand(string Area) : ICommand<BorradorPreparadoDto>;
public sealed record GuardarAreaCommand(bool Nueva, GuardarAreaRequest Request) : ICommand<string>;
public sealed record GuardarMovimientoCommand(Guid? Token, GuardarMovimientoRequest Request) : ICommand<Guid>;
public sealed record ResolverMovimientoCommand(Guid Token, ResolverRequest Request) : ICommand<string>;
public sealed record AnularMovimientoCommand(Guid Token) : ICommand<string>;
public sealed record GuardarRenovacionCommand(Guid? Token, GuardarRenovacionRequest Request) : ICommand<Guid>;
public sealed record EnviarRequerimientoCommand(Guid Token) : ICommand<string>;
public sealed record RenovarServicioCommand(Guid Token, DateOnly NuevoVence) : ICommand<string>;
public sealed record RetirarRenovacionCommand(Guid Token) : ICommand<string>;
public sealed record GuardarSolicitudCommand(Guid? Token, GuardarSolicitudRequest Request) : ICommand<Guid>;
public sealed record ResolverSolicitudCommand(Guid Token, ResolverRequest Request) : ICommand<string>;
public sealed record AnularSolicitudCommand(Guid Token) : ICommand<string>;
public sealed record GuardarPagoCommand(Guid? Token, GuardarPagoRequest Request) : ICommand<Guid>;
public sealed record PagarCommand(Guid Token) : ICommand<string>;
public sealed record AnularPagoCommand(Guid Token) : ICommand<string>;
public sealed record SubirDocumentoCommand(Guid DuenoToken, string Tipo, string Nombre, byte[] Contenido) : ICommand<DocumentoCompraDto>;
public sealed record EliminarDocumentoCommand(Guid Token) : ICommand<string>;

internal sealed class PresupuestoConsultas(IPresupuestoRepository repo, IPermisos permisos, IClock clock, IAlmacenArchivos almacen) :
    IQueryHandler<ListarAreasQuery, IReadOnlyList<AreaDto>>,
    IQueryHandler<ListarPartidasQuery, IReadOnlyList<PartidaDto>>,
    IQueryHandler<DetallePartidaQuery, PartidaDetalleDto>,
    IQueryHandler<ListarMovimientosQuery, IReadOnlyList<MovimientoDto>>,
    IQueryHandler<ListarRenovacionesQuery, IReadOnlyList<RenovacionDto>>,
    IQueryHandler<ListarSolicitudesQuery, IReadOnlyList<SolicitudDto>>,
    IQueryHandler<DetalleSolicitudQuery, SolicitudDetalleDto>,
    IQueryHandler<ListarPagosQuery, IReadOnlyList<PagoDto>>,
    IQueryHandler<DetallePagoQuery, PagoDetalleDto>,
    IQueryHandler<DescargarDocumentoQuery, ArchivoAdjunto>
{
    public async Task<Result<IReadOnlyList<AreaDto>>> Handle(ListarAreasQuery q, CancellationToken ct) =>
        await Pres.VeAlguna(permisos, ct) ? Result.Success(await repo.ListarAreas(q.SoloActivas, ct)) : Result.Failure<IReadOnlyList<AreaDto>>(Acceso.SinPermiso);

    public async Task<Result<IReadOnlyList<PartidaDto>>> Handle(ListarPartidasQuery q, CancellationToken ct) =>
        await Pres.VeAlguna(permisos, ct) ? Result.Success(await repo.ListarPartidas(q.Anio, q.Area, ct)) : Result.Failure<IReadOnlyList<PartidaDto>>(Acceso.SinPermiso);

    public async Task<Result<PartidaDetalleDto>> Handle(DetallePartidaQuery q, CancellationToken ct) =>
        await permisos.Exigir("presupuesto", "ver", ct) is { } e ? Result.Failure<PartidaDetalleDto>(e) : Result.Success(await repo.DetallePartida(q.Token, ct));

    public async Task<Result<IReadOnlyList<MovimientoDto>>> Handle(ListarMovimientosQuery q, CancellationToken ct) =>
        await permisos.Exigir("movimientos", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<MovimientoDto>>(e) : Result.Success(await repo.ListarMovimientos(ct));

    public async Task<Result<IReadOnlyList<RenovacionDto>>> Handle(ListarRenovacionesQuery q, CancellationToken ct) =>
        await permisos.Exigir("renovaciones", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<RenovacionDto>>(e) : Result.Success(await repo.ListarRenovaciones(clock.Hoy, ct));

    // Los pagos eligen su solicitud: también la ve quien ve pagos
    public async Task<Result<IReadOnlyList<SolicitudDto>>> Handle(ListarSolicitudesQuery q, CancellationToken ct) =>
        await permisos.Tiene("solicitudes", "ver", ct) || await permisos.Tiene("pagos", "ver", ct)
            ? Result.Success(await repo.ListarSolicitudes(ct)) : Result.Failure<IReadOnlyList<SolicitudDto>>(Acceso.SinPermiso);

    public async Task<Result<SolicitudDetalleDto>> Handle(DetalleSolicitudQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("solicitudes", "ver", ct) is { } e) return Result.Failure<SolicitudDetalleDto>(e);
        return await repo.DetalleSolicitud(q.Token, ct) is { } d ? Result.Success(d) : Result.Failure<SolicitudDetalleDto>(Error.NotFound("La solicitud"));
    }

    public async Task<Result<IReadOnlyList<PagoDto>>> Handle(ListarPagosQuery q, CancellationToken ct) =>
        await permisos.Exigir("pagos", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<PagoDto>>(e) : Result.Success(await repo.ListarPagos(ct));

    public async Task<Result<PagoDetalleDto>> Handle(DetallePagoQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("pagos", "ver", ct) is { } e) return Result.Failure<PagoDetalleDto>(e);
        return await repo.DetallePago(q.Token, ct) is { } d ? Result.Success(d) : Result.Failure<PagoDetalleDto>(Error.NotFound("El pago"));
    }

    public async Task<Result<ArchivoAdjunto>> Handle(DescargarDocumentoQuery q, CancellationToken ct)
    {
        if (await repo.ObtenerDocumento(q.Token, ct) is not { } d) return Result.Failure<ArchivoAdjunto>(Error.NotFound("El documento"));
        if (await permisos.Exigir(d.Dueno == "Solicitud" ? "solicitudes" : "pagos", "ver", ct) is { } e) return Result.Failure<ArchivoAdjunto>(e);
        return await almacen.Abrir(Adjuntos.Contenedor, d.Ruta, ct) is { } s
            ? Result.Success(new ArchivoAdjunto(d.Nombre, d.TipoContenido, s)) : Result.Failure<ArchivoAdjunto>(Error.NotFound("El archivo del documento"));
    }
}

internal sealed class PartidaHandlers(IPresupuestoRepository repo, IPermisos permisos, ICurrentUser user) :
    ICommandHandler<GuardarPartidaCommand, Guid>, ICommandHandler<CambiarEstadoPartidaCommand, string>,
    ICommandHandler<EliminarPartidaCommand, string>, ICommandHandler<PrepararBorradorCommand, BorradorPreparadoDto>,
    ICommandHandler<GuardarAreaCommand, string>
{
    public async Task<Result<string>> Handle(GuardarAreaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("areaspres", c.Nueva ? "crear" : "editar", ct) is { } e) return Result.Failure<string>(e);
        var r = c.Request with { Codigo = c.Request.Codigo?.Trim().ToUpperInvariant() ?? "", Nombre = c.Request.Nombre?.Trim() ?? "" };
        if (ReglasPresupuesto.ValidarArea(r.Codigo, r.Nombre) is { } v) return Result.Failure<string>(v);
        await repo.GuardarArea(c.Nueva, r, Actor.De(user), ct);
        return Result.Success(r.Activa ? "Área " + r.Codigo + " guardada" : "Área " + r.Codigo + " inactiva: conserva sus partidas, pero no recibe nuevas ni prepara borrador");
    }

    public async Task<Result<Guid>> Handle(GuardarPartidaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("presupuesto", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<Guid>(e);
        var r = c.Request with { Rubro = c.Request.Rubro?.Trim() ?? "", Area = c.Request.Area?.Trim().ToUpperInvariant() ?? "" };
        if (ReglasPresupuesto.ValidarPartida(r.Anio, r.Area, r.Rubro, r.Asignado) is { } v) return Result.Failure<Guid>(v);
        return Result.Success(await repo.GuardarPartida(c.Token, r, Actor.De(user), ct));
    }

    public async Task<Result<string>> Handle(CambiarEstadoPartidaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("presupuesto", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        if (c.Accion is not ("Aprobar" or "Cerrar")) return Result.Failure<string>(Error.Validation("accion", "La acción es Aprobar o Cerrar."));
        await repo.CambiarEstadoPartida(c.Token, c.Accion, Actor.De(user), ct);
        return Result.Success(c.Accion == "Aprobar" ? "Partida aprobada: ya se puede usar en solicitudes, renovaciones y pagos" : "Partida cerrada");
    }

    public async Task<Result<string>> Handle(EliminarPartidaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("presupuesto", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        await repo.EliminarPartida(c.Token, Actor.De(user), ct);
        return Result.Success("Partida eliminada");
    }

    public async Task<Result<BorradorPreparadoDto>> Handle(PrepararBorradorCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("presupuesto", "crear", ct) is { } e) return Result.Failure<BorradorPreparadoDto>(e);
        if (string.IsNullOrWhiteSpace(c.Area)) return Result.Failure<BorradorPreparadoDto>(Error.Validation("area", "Elija el área del presupuesto."));
        return Result.Success(await repo.PrepararBorrador(c.Area.Trim().ToUpperInvariant(), Actor.De(user), ct));
    }
}

internal sealed class MovimientoHandlers(IPresupuestoRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) :
    ICommandHandler<GuardarMovimientoCommand, Guid>, ICommandHandler<ResolverMovimientoCommand, string>, ICommandHandler<AnularMovimientoCommand, string>
{
    public async Task<Result<Guid>> Handle(GuardarMovimientoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("movimientos", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<Guid>(e);
        var r = c.Request with { Motivo = c.Request.Motivo?.Trim() ?? "" };
        if (ReglasPresupuesto.ValidarMovimiento(r.Tipo, r.Origen, r.Destino, r.Monto, r.Motivo) is { } v) return Result.Failure<Guid>(v);
        if (r.Fecha > clock.Hoy) return Result.Failure<Guid>(Error.Validation("fecha", "La fecha no puede ser posterior a hoy."));
        return Result.Success(await repo.GuardarMovimiento(c.Token, r, Actor.De(user), ct));
    }

    public async Task<Result<string>> Handle(ResolverMovimientoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("movimientos", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        if (Pres.Rechazo(c.Request) is { } v) return Result.Failure<string>(v);
        await repo.ResolverMovimiento(c.Token, c.Request.Aprobar, Pres.Nulo(c.Request.Comentario), Actor.De(user), ct);
        return Result.Success(c.Request.Aprobar ? "Movimiento aprobado: el presupuesto de las partidas se actualizó" : "Movimiento rechazado");
    }

    public async Task<Result<string>> Handle(AnularMovimientoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("movimientos", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        await repo.AnularMovimiento(c.Token, Actor.De(user), ct);
        return Result.Success("Movimiento anulado");
    }
}

internal sealed class RenovacionHandlers(IPresupuestoRepository repo, IPermisos permisos, ICurrentUser user) :
    ICommandHandler<GuardarRenovacionCommand, Guid>, ICommandHandler<EnviarRequerimientoCommand, string>,
    ICommandHandler<RenovarServicioCommand, string>, ICommandHandler<RetirarRenovacionCommand, string>
{
    public async Task<Result<Guid>> Handle(GuardarRenovacionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("renovaciones", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<Guid>(e);
        var r = c.Request with { Servicio = c.Request.Servicio?.Trim() ?? "", Proveedor = c.Request.Proveedor?.Trim() ?? "", Partida = c.Request.Presupuestado ? c.Request.Partida : null };
        if (ReglasPresupuesto.ValidarRenovacion(r.Servicio, r.Proveedor, r.MontoEstimado, r.Presupuestado, r.Partida, r.DiasAviso) is { } v) return Result.Failure<Guid>(v);
        return Result.Success(await repo.GuardarRenovacion(c.Token, r, Actor.De(user), ct));
    }

    // El requerimiento lo pide Gerencia de TI (como en el prototipo); Compras lo gestiona como solicitud
    public async Task<Result<string>> Handle(EnviarRequerimientoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("renovaciones", "crear", ct) is { } e) return Result.Failure<string>(e);
        var sc = await repo.EnviarRequerimiento(c.Token, "Gerencia de TI", Actor.De(user), ct);
        return Result.Success($"Requerimiento {sc} enviado al área de Compras para su gestión");
    }

    public async Task<Result<string>> Handle(RenovarServicioCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("renovaciones", "editar", ct) is { } e) return Result.Failure<string>(e);
        await repo.RenovarServicio(c.Token, c.NuevoVence, Actor.De(user), ct);
        return Result.Success($"Renovación registrada: ahora vence el {c.NuevoVence:dd/MM/yyyy}");
    }

    public async Task<Result<string>> Handle(RetirarRenovacionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("renovaciones", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        await repo.RetirarRenovacion(c.Token, Actor.De(user), ct);
        return Result.Success("Renovación retirada: deja de avisar");
    }
}

internal sealed class SolicitudHandlers(IPresupuestoRepository repo, IPermisos permisos, ICurrentUser user) :
    ICommandHandler<GuardarSolicitudCommand, Guid>, ICommandHandler<ResolverSolicitudCommand, string>, ICommandHandler<AnularSolicitudCommand, string>
{
    public async Task<Result<Guid>> Handle(GuardarSolicitudCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("solicitudes", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<Guid>(e);
        var r = c.Request with { Descripcion = c.Request.Descripcion?.Trim() ?? "", Proveedor = Pres.Nulo(c.Request.Proveedor), Partida = c.Request.Presupuestado ? c.Request.Partida : null };
        if (ReglasPresupuesto.ValidarSolicitud(r.Descripcion, r.Area, r.Proveedor, r.Presupuestado, r.Partida, r.Monto) is { } v) return Result.Failure<Guid>(v);
        return Result.Success(await repo.GuardarSolicitud(c.Token, r, Actor.De(user), ct));
    }

    public async Task<Result<string>> Handle(ResolverSolicitudCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("solicitudes", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        if (Pres.Rechazo(c.Request) is { } v) return Result.Failure<string>(v);
        await repo.ResolverSolicitud(c.Token, c.Request.Aprobar, Pres.Nulo(c.Request.Comentario), Actor.De(user), ct);
        return Result.Success(c.Request.Aprobar ? "Solicitud aprobada: queda como comprometido de su partida" : "Solicitud rechazada");
    }

    public async Task<Result<string>> Handle(AnularSolicitudCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("solicitudes", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        await repo.AnularSolicitud(c.Token, Actor.De(user), ct);
        return Result.Success("Solicitud anulada: libera su comprometido");
    }
}

internal sealed class PagoHandlers(IPresupuestoRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) :
    ICommandHandler<GuardarPagoCommand, Guid>, ICommandHandler<PagarCommand, string>, ICommandHandler<AnularPagoCommand, string>
{
    public async Task<Result<Guid>> Handle(GuardarPagoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("pagos", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<Guid>(e);
        var r = c.Request with { Proveedor = c.Request.Proveedor?.Trim() ?? "", Distribucion = c.Request.Distribucion ?? [] };
        if ((await repo.DetalleSolicitud(r.Solicitud, ct))?.Solicitud is not { } s) return Result.Failure<Guid>(Error.NotFound("La solicitud"));
        if (ReglasPresupuesto.ValidarPago(r.Proveedor, r.Monto, s.Presupuestado, r.Distribucion!.Select(d => (d.Partida, d.Monto)).ToList()) is { } v) return Result.Failure<Guid>(v);
        if (r.Fecha > clock.Hoy.AddYears(1)) return Result.Failure<Guid>(Error.Validation("fecha", "La fecha del pago no es válida."));
        return Result.Success(await repo.GuardarPago(c.Token, r, Actor.De(user), ct));
    }

    public async Task<Result<string>> Handle(PagarCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("pagos", "editar", ct) is { } e) return Result.Failure<string>(e);
        await repo.Pagar(c.Token, clock.Hoy, Actor.De(user), ct);
        return Result.Success("Pago registrado como Pagado: descuenta de las partidas de su distribución");
    }

    public async Task<Result<string>> Handle(AnularPagoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("pagos", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        await repo.AnularPago(c.Token, Actor.De(user), ct);
        return Result.Success("Pago anulado");
    }
}

/// <summary>Contrato de la solicitud; facturas (PDF/XML) y distribución del pago. Se verifica el contenido de cada archivo.</summary>
internal sealed class DocumentoHandlers(IPresupuestoRepository repo, IPermisos permisos, ICurrentUser user, IAlmacenArchivos almacen) :
    ICommandHandler<SubirDocumentoCommand, DocumentoCompraDto>, ICommandHandler<EliminarDocumentoCommand, string>
{
    public async Task<Result<DocumentoCompraDto>> Handle(SubirDocumentoCommand c, CancellationToken ct)
    {
        if (!ReglasPresupuesto.Documentos.TryGetValue(c.Tipo, out var exts)) return Result.Failure<DocumentoCompraDto>(Error.Validation("tipo", "El documento es Contrato, Factura o Distribución."));
        var opcion = c.Tipo == "Contrato" ? "solicitudes" : "pagos";
        if (!(await permisos.Tiene(opcion, "editar", ct) || await permisos.Tiene(opcion, "crear", ct))) return Result.Failure<DocumentoCompraDto>(Acceso.SinPermiso);
        var cabecera = c.Contenido.AsSpan(0, Math.Min(ReglasAdjunto.BytesCabecera, c.Contenido.Length));
        var (nombre, tipo, error) = ReglasAdjunto.Validar(c.Nombre, c.Contenido.LongLength, cabecera, exts, ReglasPresupuesto.MaximoMBDocumento);
        if (error is not null) return Result.Failure<DocumentoCompraDto>(error);
        var token = Guid.NewGuid();
        var ruta = $"com/{(c.Tipo == "Contrato" ? "solicitudes" : "pagos")}/{c.DuenoToken:N}/{token:N}{Path.GetExtension(nombre).ToLowerInvariant()}";
        using (var s = new MemoryStream(c.Contenido, writable: false)) await almacen.Guardar(Adjuntos.Contenedor, ruta, s, tipo, ct);
        try { await repo.RegistrarDocumento(c.DuenoToken, token, c.Tipo, nombre, tipo, c.Contenido.LongLength, ruta, Actor.De(user), ct); }
        catch { await almacen.Eliminar(Adjuntos.Contenedor, ruta, CancellationToken.None); throw; }
        return Result.Success(new DocumentoCompraDto(token, c.Tipo, nombre, tipo, c.Contenido.LongLength, user.Nombre, DateTime.Now));
    }

    public async Task<Result<string>> Handle(EliminarDocumentoCommand c, CancellationToken ct)
    {
        var ruta = await repo.EliminarDocumento(c.Token, Actor.De(user), ct);
        await almacen.Eliminar(Adjuntos.Contenedor, ruta, CancellationToken.None);
        return Result.Success("Documento quitado");
    }
}
