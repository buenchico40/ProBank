namespace ProBank.Contracts.Presupuesto;

// Áreas que llevan presupuesto (com.Area); el código no cambia
public sealed record AreaDto(string Codigo, string Nombre, bool Activa, int Partidas);
public sealed record GuardarAreaRequest(string Codigo, string Nombre, bool Activa);

// Partidas (COM-01): ejecución calculada en com.vw_Partida
public sealed record PartidaDto(Guid Token, string Codigo, int Anio, string Area, string Rubro, decimal Asignado, decimal Ajustes, decimal Comprometido,
    decimal Pagado, decimal Disponible, int Ejecucion, string Estado, string CreadoPor, DateTime FechaRegistro, string? AprobadoPor, DateTime? FechaAprobacion);
public sealed record PartidaDetalleDto(IReadOnlyList<MovimientoPartidaDto> Movimientos, IReadOnlyList<PagoAplicadoDto> Pagos, IReadOnlyList<CompromisoDto> Compromisos);
/// <summary>Efecto: positivo si entra a la partida, negativo si sale.</summary>
public sealed record MovimientoPartidaDto(string Codigo, DateOnly Fecha, string Tipo, decimal Efecto, string Estado);
public sealed record PagoAplicadoDto(string Pago, string Solicitud, DateOnly Fecha, decimal Monto, string Estado);
public sealed record CompromisoDto(string Codigo, string Descripcion, decimal Monto, decimal Pagado, decimal PorPagar, string Estado);
public sealed record GuardarPartidaRequest(int Anio, string Area, string Rubro, decimal Asignado);
public sealed record BorradorPreparadoDto(int Anio, int Copiadas);

// Ajustes y movimientos
public sealed record MovimientoDto(Guid Token, string Codigo, DateOnly Fecha, string Tipo, decimal Monto, string Motivo, string Estado, string? Comentario,
    Guid? OrigenToken, string? Origen, string? OrigenRubro, decimal? OrigenDisponible, Guid? DestinoToken, string? Destino, string? DestinoRubro,
    decimal? DestinoDisponible, string RegistradoPor, DateTime FechaRegistro, string? ResueltoPor, DateTime? FechaResolucion);
public sealed record GuardarMovimientoRequest(DateOnly Fecha, string Tipo, Guid? Origen, Guid? Destino, decimal Monto, string Motivo);
/// <summary>Aprobar o rechazar (movimientos y solicitudes); el comentario es obligatorio al rechazar.</summary>
public sealed record ResolverRequest(bool Aprobar, string? Comentario);

// Renovaciones (COM-03)
public sealed record RenovacionDto(Guid Token, string Codigo, string Servicio, string Proveedor, DateOnly Vence, int DiasParaVencer, decimal MontoEstimado,
    bool Presupuestado, Guid? PartidaToken, string? Partida, string? PartidaRubro, int DiasAviso, string Estado, string? Solicitud, string? SolicitudEstado);
public sealed record GuardarRenovacionRequest(string Servicio, string Proveedor, DateOnly Vence, decimal MontoEstimado, bool Presupuestado, Guid? Partida, int DiasAviso);
public sealed record RenovarRequest(DateOnly NuevoVence);

// Solicitudes de compra (COM-02)
public sealed record SolicitudDto(Guid Token, string Codigo, string Descripcion, string Area, string? Proveedor, bool Presupuestado, Guid? PartidaToken,
    string? Partida, string? PartidaRubro, decimal Monto, decimal Pagado, decimal PorPagar, string Origen, string Aprobacion, string Estado, string? Comentario,
    string Solicitante, DateTime FechaRegistro, string? AprobadoPor, DateTime? FechaAprobacion, int Documentos);
public sealed record SolicitudDetalleDto(SolicitudDto Solicitud, IReadOnlyList<PagoResumenDto> Pagos, IReadOnlyList<DocumentoCompraDto> Documentos);
public sealed record PagoResumenDto(Guid Token, string Codigo, DateOnly Fecha, decimal Monto, string Estado);
public sealed record GuardarSolicitudRequest(string Descripcion, string Area, string? Proveedor, bool Presupuestado, Guid? Partida, decimal Monto);

// Pagos (COM-04)
public sealed record PagoDto(Guid Token, string Codigo, Guid SolicitudToken, string Solicitud, string SolicitudDescripcion, bool Presupuestado, string Proveedor,
    DateOnly Fecha, decimal Monto, string Estado, string Partidas, decimal Distribuido, string RegistradoPor, DateTime FechaRegistro, string? PagadoPor,
    DateTime? FechaPagado, int Documentos);
public sealed record PagoDetalleDto(PagoDto Pago, IReadOnlyList<DistribucionDto> Distribucion, IReadOnlyList<DocumentoCompraDto> Documentos);
public sealed record DistribucionDto(Guid PartidaToken, string Partida, string Rubro, decimal Monto);
public sealed record GuardarPagoRequest(Guid Solicitud, string Proveedor, DateOnly Fecha, decimal Monto, IReadOnlyList<DistribucionRequest>? Distribucion);
public sealed record DistribucionRequest(Guid Partida, decimal Monto);

/// <summary>Contrato de una solicitud o factura / distribución de un pago (Blob Storage).</summary>
public sealed record DocumentoCompraDto(Guid Token, string Tipo, string Nombre, string TipoContenido, long Tamano, string SubidoPor, DateTime FechaRegistro);
