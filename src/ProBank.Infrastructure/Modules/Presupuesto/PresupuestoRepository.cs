using System.Data;
using System.Text.Json;
using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Presupuesto;
using ProBank.Contracts.Presupuesto;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Presupuesto;

/// <summary>Presupuesto: partidas, movimientos, renovaciones, solicitudes, pagos y documentos. Solo procedimientos del esquema com.</summary>
internal sealed class PresupuestoRepository(ISqlConnectionFactory db) : IPresupuestoRepository
{
    private static DateTime F(DateOnly d) => d.ToDateTime(TimeOnly.MinValue);
    private static DateOnly D(DateTime d) => DateOnly.FromDateTime(d);

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

    private async Task<DynamicParameters> Ejecutar(string sp, DynamicParameters p, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition(sp, p, commandType: Sp.Tipo, cancellationToken: ct));
        return p;
    }

    // Guardar: los SP devuelven el token en @TokenGuardado
    private async Task<Guid> Guardar(string sp, object datos, Actor a, CancellationToken ct)
    {
        var p = Con(datos, a);
        p.Add("TokenGuardado", dbType: DbType.Guid, direction: ParameterDirection.Output);
        return (await Ejecutar(sp, p, ct)).Get<Guid>("TokenGuardado");
    }

    // ---------- Áreas ----------
    public async Task<IReadOnlyList<AreaDto>> ListarAreas(bool soloActivas, CancellationToken ct) =>
        await Lista<AreaDto>("com.usp_Area_Listar", new { SoloActivas = soloActivas }, ct);

    public Task GuardarArea(bool nueva, GuardarAreaRequest r, Actor a, CancellationToken ct) =>
        Ejecutar("com.usp_Area_Guardar", Con(new { Nueva = nueva, r.Codigo, r.Nombre, r.Activa }, a), ct);

    // ---------- Partidas ----------
    public async Task<IReadOnlyList<PartidaDto>> ListarPartidas(int? anio, string? area, CancellationToken ct) =>
        await Lista<PartidaDto>("com.usp_Partida_Listar", new { Anio = anio, Area = area }, ct);

    public async Task<PartidaDetalleDto> DetallePartida(Guid token, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("com.usp_Partida_Detalle", new { Token = token }, commandType: Sp.Tipo, cancellationToken: ct));
        var movs = (await m.ReadAsync<MovFila>()).Select(f => new MovimientoPartidaDto(f.Codigo, D(f.Fecha), f.Tipo, f.Efecto, f.Estado)).ToList();
        var pagos = (await m.ReadAsync<PagoAplicadoFila>()).Select(f => new PagoAplicadoDto(f.Pago, f.Solicitud, D(f.Fecha), f.Monto, f.Estado)).ToList();
        var comp = (await m.ReadAsync<CompromisoDto>()).ToList();
        return new PartidaDetalleDto(movs, pagos, comp);
    }

    public Task<Guid> GuardarPartida(Guid? token, GuardarPartidaRequest r, Actor a, CancellationToken ct) =>
        Guardar("com.usp_Partida_Guardar", new { Token = token, Anio = (short)r.Anio, r.Area, r.Rubro, r.Asignado }, a, ct);

    public Task CambiarEstadoPartida(Guid token, string accion, Actor a, CancellationToken ct) =>
        Ejecutar("com.usp_Partida_CambiarEstado", Con(new { Token = token, Accion = accion }, a), ct);

    public Task EliminarPartida(Guid token, Actor a, CancellationToken ct) => Ejecutar("com.usp_Partida_Eliminar", Con(new { Token = token }, a), ct);

    public async Task<BorradorPreparadoDto> PrepararBorrador(string area, Actor a, CancellationToken ct)
    {
        var p = Con(new { Area = area }, a);
        p.Add("Anio", dbType: DbType.Int16, direction: ParameterDirection.Output);
        p.Add("Copiadas", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await Ejecutar("com.usp_Presupuesto_Preparar", p, ct);
        return new BorradorPreparadoDto(p.Get<short>("Anio"), p.Get<int>("Copiadas"));
    }

    public async Task<ReportePresupuestoDatos> DatosReporte(string area, int anio, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("com.usp_Presupuesto_Reporte", Con(new { Area = area, Anio = (short)anio }, a),
            commandType: Sp.Tipo, cancellationToken: ct));
        var ar = await m.ReadSingleAsync<(string Codigo, string Nombre)>();
        var partidas = (await m.ReadAsync<PartidaDto>()).ToList();
        var servicios = (await m.ReadAsync<ServicioReporteFila>()).Select(f => new ServicioReporte(f.Codigo, f.Servicio, f.Proveedor, D(f.Vence), f.MontoEstimado,
            f.Presupuestado, f.Partida, f.PartidaRubro, f.Estado, f.DiasAviso, f.Pagado, f.Solicitud)).ToList();
        var pagos = (await m.ReadAsync<PagoReporteFila>()).Select(f => new PagoReporte(f.Codigo, D(f.Fecha), f.Proveedor, f.Solicitud, f.Descripcion, f.Servicio,
            f.Presupuestado, f.Partidas, f.MontoPago, f.Monto, f.Estado)).ToList();
        return new ReportePresupuestoDatos(ar.Codigo, ar.Nombre, partidas, servicios, pagos);
    }

    // ---------- Movimientos ----------
    public async Task<IReadOnlyList<MovimientoDto>> ListarMovimientos(CancellationToken ct) =>
        (await Lista<MovimientoFila>("com.usp_Movimiento_Listar", null, ct)).Select(f => new MovimientoDto(f.Token, f.Codigo, D(f.Fecha), f.Tipo, f.Monto, f.Motivo,
            f.Estado, f.Comentario, f.OrigenToken, f.Origen, f.OrigenRubro, f.OrigenDisponible, f.DestinoToken, f.Destino, f.DestinoRubro, f.DestinoDisponible,
            f.RegistradoPor, f.FechaRegistro, f.ResueltoPor, f.FechaResolucion)).ToList();

    public Task<Guid> GuardarMovimiento(Guid? token, GuardarMovimientoRequest r, Actor a, CancellationToken ct) =>
        Guardar("com.usp_Movimiento_Guardar", new { Token = token, Fecha = F(r.Fecha), r.Tipo, OrigenToken = r.Origen, DestinoToken = r.Destino, r.Monto, r.Motivo }, a, ct);

    public Task ResolverMovimiento(Guid token, bool aprobar, string? comentario, Actor a, CancellationToken ct) =>
        Ejecutar("com.usp_Movimiento_Resolver", Con(new { Token = token, Aprobar = aprobar, Comentario = comentario }, a), ct);

    public Task AnularMovimiento(Guid token, Actor a, CancellationToken ct) => Ejecutar("com.usp_Movimiento_Anular", Con(new { Token = token }, a), ct);

    // ---------- Renovaciones ----------
    public async Task<IReadOnlyList<RenovacionDto>> ListarRenovaciones(DateOnly hoy, CancellationToken ct) =>
        (await Lista<RenovacionFila>("com.usp_Renovacion_Listar", new { Hoy = F(hoy) }, ct)).Select(f => new RenovacionDto(f.Token, f.Codigo, f.Servicio, f.Proveedor,
            D(f.Vence), f.DiasParaVencer, f.MontoEstimado, f.Presupuestado, f.PartidaToken, f.Partida, f.PartidaRubro, f.DiasAviso, f.Estado, f.Solicitud, f.SolicitudEstado)).ToList();

    public Task<Guid> GuardarRenovacion(Guid? token, GuardarRenovacionRequest r, Actor a, CancellationToken ct) =>
        Guardar("com.usp_Renovacion_Guardar", new
        {
            Token = token, r.Servicio, r.Proveedor, Vence = F(r.Vence), r.MontoEstimado, r.Presupuestado, PartidaToken = r.Partida, r.DiasAviso
        }, a, ct);

    public async Task<string> EnviarRequerimiento(Guid token, string area, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token, Area = area }, a);
        p.Add("Solicitud", dbType: DbType.AnsiString, size: 20, direction: ParameterDirection.Output);
        return (await Ejecutar("com.usp_Renovacion_EnviarRequerimiento", p, ct)).Get<string>("Solicitud");
    }

    public Task RenovarServicio(Guid token, DateOnly nuevoVence, Actor a, CancellationToken ct) =>
        Ejecutar("com.usp_Renovacion_Renovar", Con(new { Token = token, NuevoVence = F(nuevoVence) }, a), ct);

    public Task RetirarRenovacion(Guid token, Actor a, CancellationToken ct) => Ejecutar("com.usp_Renovacion_Retirar", Con(new { Token = token }, a), ct);

    // ---------- Solicitudes ----------
    public async Task<IReadOnlyList<SolicitudDto>> ListarSolicitudes(CancellationToken ct) => await Lista<SolicitudDto>("com.usp_Solicitud_Listar", new { Token = (Guid?)null }, ct);

    public async Task<SolicitudDetalleDto?> DetalleSolicitud(Guid token, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("com.usp_Solicitud_Listar", new { Token = token }, commandType: Sp.Tipo, cancellationToken: ct));
        var s = await m.ReadSingleOrDefaultAsync<SolicitudDto>();
        if (s is null) return null;
        var pagos = (await m.ReadAsync<PagoResumenFila>()).Select(f => new PagoResumenDto(f.Token, f.Codigo, D(f.Fecha), f.Monto, f.Estado)).ToList();
        var docs = (await m.ReadAsync<DocumentoCompraDto>()).ToList();
        return new SolicitudDetalleDto(s, pagos, docs);
    }

    public Task<Guid> GuardarSolicitud(Guid? token, GuardarSolicitudRequest r, Actor a, CancellationToken ct) =>
        Guardar("com.usp_Solicitud_Guardar", new { Token = token, r.Descripcion, r.Area, r.Proveedor, r.Presupuestado, PartidaToken = r.Partida, r.Monto }, a, ct);

    public Task ResolverSolicitud(Guid token, bool aprobar, string? comentario, Actor a, CancellationToken ct) =>
        Ejecutar("com.usp_Solicitud_Resolver", Con(new { Token = token, Aprobar = aprobar, Comentario = comentario }, a), ct);

    public Task AnularSolicitud(Guid token, Actor a, CancellationToken ct) => Ejecutar("com.usp_Solicitud_Anular", Con(new { Token = token }, a), ct);

    // ---------- Pagos ----------
    public async Task<IReadOnlyList<PagoDto>> ListarPagos(CancellationToken ct) =>
        (await Lista<PagoFila>("com.usp_Pago_Listar", new { Token = (Guid?)null }, ct)).Select(Pago).ToList();

    public async Task<PagoDetalleDto?> DetallePago(Guid token, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("com.usp_Pago_Listar", new { Token = token }, commandType: Sp.Tipo, cancellationToken: ct));
        var f = await m.ReadSingleOrDefaultAsync<PagoFila>();
        if (f is null) return null;
        var dist = (await m.ReadAsync<DistribucionDto>()).ToList();
        var docs = (await m.ReadAsync<DocumentoCompraDto>()).ToList();
        return new PagoDetalleDto(Pago(f), dist, docs);
    }

    public Task<Guid> GuardarPago(Guid? token, GuardarPagoRequest r, Actor a, CancellationToken ct) =>
        Guardar("com.usp_Pago_Guardar", new
        {
            Token = token, SolicitudToken = r.Solicitud, r.Proveedor, Fecha = F(r.Fecha), r.Monto,
            Distribucion = JsonSerializer.Serialize((r.Distribucion ?? []).Select(d => new { partida = d.Partida, monto = d.Monto }))
        }, a, ct);

    public Task Pagar(Guid token, DateOnly hoy, Actor a, CancellationToken ct) => Ejecutar("com.usp_Pago_Pagar", Con(new { Token = token, Hoy = F(hoy) }, a), ct);

    public Task AnularPago(Guid token, Actor a, CancellationToken ct) => Ejecutar("com.usp_Pago_Anular", Con(new { Token = token }, a), ct);

    // ---------- Documentos ----------
    public Task RegistrarDocumento(Guid duenoToken, Guid token, string tipo, string nombre, string tipoContenido, long tamano, string ruta, Actor a, CancellationToken ct) =>
        Ejecutar("com.usp_Documento_Registrar", Con(new
        {
            DuenoToken = duenoToken, Token = token, Tipo = tipo, Nombre = nombre, TipoContenido = tipoContenido, Tamano = tamano, Ruta = ruta
        }, a), ct);

    public async Task<(string Dueno, string Nombre, string TipoContenido, string Ruta)?> ObtenerDocumento(Guid token, CancellationToken ct)
    {
        var f = (await Lista<DocumentoFila>("com.usp_Documento_Obtener", new { Token = token }, ct)).FirstOrDefault();
        return f is null ? null : (f.Dueno, f.Nombre, f.TipoContenido, f.Ruta);
    }

    public async Task<string> EliminarDocumento(Guid token, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token }, a);
        p.Add("Ruta", dbType: DbType.AnsiString, size: 300, direction: ParameterDirection.Output);
        return (await Ejecutar("com.usp_Documento_Eliminar", p, ct)).Get<string>("Ruta");
    }

    private static PagoDto Pago(PagoFila f) => new(f.Token, f.Codigo, f.SolicitudToken, f.Solicitud, f.SolicitudDescripcion, f.Presupuestado, f.Proveedor, D(f.Fecha),
        f.Monto, f.Estado, f.Partidas, f.Distribuido, f.RegistradoPor, f.FechaRegistro, f.PagadoPor, f.FechaPagado, f.Documentos);

    private sealed record ServicioReporteFila(string Codigo, string Servicio, string Proveedor, DateTime Vence, decimal MontoEstimado, bool Presupuestado,
        string? Partida, string? PartidaRubro, string Estado, int DiasAviso, decimal Pagado, string? Solicitud);
    private sealed record PagoReporteFila(string Codigo, DateTime Fecha, string Proveedor, string Solicitud, string Descripcion, string? Servicio,
        bool Presupuestado, string? Partidas, decimal MontoPago, decimal Monto, string Estado);
    private sealed record MovFila(string Codigo, DateTime Fecha, string Tipo, decimal Efecto, string Estado);
    private sealed record PagoAplicadoFila(string Pago, string Solicitud, DateTime Fecha, decimal Monto, string Estado);
    private sealed record MovimientoFila(Guid Token, string Codigo, DateTime Fecha, string Tipo, decimal Monto, string Motivo, string Estado, string? Comentario,
        Guid? OrigenToken, string? Origen, string? OrigenRubro, decimal? OrigenDisponible, Guid? DestinoToken, string? Destino, string? DestinoRubro,
        decimal? DestinoDisponible, string RegistradoPor, DateTime FechaRegistro, string? ResueltoPor, DateTime? FechaResolucion);
    private sealed record RenovacionFila(Guid Token, string Codigo, string Servicio, string Proveedor, DateTime Vence, int DiasParaVencer, decimal MontoEstimado,
        bool Presupuestado, Guid? PartidaToken, string? Partida, string? PartidaRubro, int DiasAviso, string Estado, string? Solicitud, string? SolicitudEstado);
    private sealed record PagoResumenFila(Guid Token, string Codigo, DateTime Fecha, decimal Monto, string Estado);
    private sealed record PagoFila(Guid Token, string Codigo, Guid SolicitudToken, string Solicitud, string SolicitudDescripcion, bool Presupuestado, string Proveedor,
        DateTime Fecha, decimal Monto, string Estado, string Partidas, decimal Distribuido, string RegistradoPor, DateTime FechaRegistro, string? PagadoPor,
        DateTime? FechaPagado, int Documentos);
    private sealed record DocumentoFila(string Dueno, string Nombre, string TipoContenido, string Ruta);
}
