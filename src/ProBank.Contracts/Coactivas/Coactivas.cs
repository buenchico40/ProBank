namespace ProBank.Contracts.Coactivas;

// Catálogo de entidades
public sealed record EntidadDto(int EntidadId, string Ruc, string Nombre, string? CuentaDestino, string Estado);
public sealed record GuardarEntidadRequest(string Ruc, string Nombre, string? CuentaDestino, string Estado);

// Oficios (COA-02). Tipo de orden: "Bloqueo", "Levantamiento" o "Transferencia".
public sealed record OficioDto(Guid Token, string Numero, DateTime Recibido, string RemitidoPor, int Ordenes, int Bloqueos, int Levantamientos,
    int Transferencias, int Entidades, decimal MontoCoactivas, decimal Ejecutado, string Estado);

public sealed record OficioDetalleDto(Guid Token, string Numero, DateTime Recibido, string RemitidoPor, string? ContenidoHtml, int Ordenes,
    int Entidades, decimal MontoCoactivas, decimal Ejecutado, string Estado, string RegistradoPor, DateTime FechaRegistro,
    IReadOnlyList<OrdenDto> Coactivas);

/// <summary>Una coactiva del oficio con su orden. MontoOrdenado null = total.</summary>
public sealed record OrdenDto(int OrdenId, string Tipo, Guid CoactivaToken, string NumeroProceso, string Entidad, string Cliente, string Identificacion,
    decimal MontoCoactiva, decimal? MontoOrdenado, decimal Bloqueado, decimal Ejecutado, string Estado, string? Observacion);

public sealed record GuardarOficioRequest(string Numero, DateOnly Recibido, string RemitidoPor, string? ContenidoHtml);

/// <summary>
/// Bloqueo: coactiva nueva (proceso, entidad, cliente, monto y productos permitidos).
/// Levantamiento o transferencia: coactiva existente con fondos bloqueados y monto ordenado opcional (null = total).
/// </summary>
public sealed record AgregarOrdenRequest(string Tipo, string? NumeroProceso, int? EntidadId, string? TipoIdentificacion, string? Identificacion,
    string? Cliente, decimal? Monto, IReadOnlyList<string>? Productos, Guid? CoactivaToken, decimal? MontoOrdenado, string? Observacion);

// Coactivas (COA-01, COA-03, COA-05)
public sealed record CoactivaDto(Guid Token, string NumeroProceso, string Oficio, Guid OficioToken, DateTime OficioRecibido, string Entidad,
    string Cliente, string Identificacion, decimal Monto, decimal Bloqueado, decimal Levantado, decimal Transferido, string Cobertura,
    string Estado, string? ProductosPermitidos);

public sealed record CoactivaDetalleDto(Guid Token, string NumeroProceso, string Oficio, Guid OficioToken, int EntidadId, string Entidad,
    string? CuentaDestino, string TipoIdentificacion, string Identificacion, string Cliente, decimal Monto, decimal Bloqueado, decimal Levantado,
    decimal Transferido, string Cobertura, string Estado, string? ProductosPermitidos, string? Observacion,
    IReadOnlyList<ProductoClienteDto> Productos, IReadOnlyList<BloqueoDto> Bloqueos, IReadOnlyList<MovimientoDto> Movimientos,
    IReadOnlyList<OrdenCoactivaDto> Ordenes);

public sealed record ProductoClienteDto(int ProductoId, string Tipo, string Numero, decimal SaldoReferencial, decimal BloqueadoTotal,
    decimal BloqueadoEsta, string? Estados);
public sealed record BloqueoDto(int BloqueoId, string Codigo, string Producto, string Cuenta, decimal Monto, decimal Levantado, decimal Transferido,
    decimal Vigente, string Estado, DateTime Fecha);
public sealed record MovimientoDto(DateTime Fecha, string Tipo, string? Oficio, Guid? OficioToken, string Cuenta, decimal Monto, string Usuario, string? Detalle);
public sealed record OrdenCoactivaDto(int OrdenId, string Tipo, string Oficio, DateTime Recibido, string RemitidoPor, decimal? MontoOrdenado,
    decimal Ejecutado, string Estado);

public sealed record ActualizarCoactivaRequest(decimal Monto, IReadOnlyList<string>? Productos, string? Observacion);
public sealed record GuardarProductoRequest(int? ProductoId, string Tipo, string Numero, decimal Saldo);
public sealed record RegistrarBloqueoRequest(int ProductoId, decimal Monto, string? Observacion);

/// <summary>Levantamiento o transferencia: monto por bloqueo vigente de la coactiva.</summary>
public sealed record EjecutarOrdenRequest(int OrdenId, DateOnly Fecha, IReadOnlyList<MontoBloqueo> Montos, string? FormaPago, string? CuentaDestino,
    string? Comprobante, string? Observacion);
public sealed record MontoBloqueo(int BloqueoId, decimal Monto);

// Coactivas por cliente
public sealed record ClienteCoactivasDto(string Identificacion, string TipoIdentificacion, string Cliente, int Coactivas, int Oficios, int Entidades,
    decimal MontoTotal, decimal Bloqueado, string Cobertura);
public sealed record ProductoResumenDto(string Tipo, string Numero, decimal SaldoReferencial, decimal Bloqueado);

// Carga masiva (COA-04): la Web lee la hoja «Coactivas» y envía las celdas como texto.
public sealed record CargaMasivaRequest(IReadOnlyList<FilaCargaDto> Filas);
public sealed record FilaCargaDto(int Fila, string? Oficio, string? Recibido, string? RemitidoPor, string? Orden, string? Proceso, string? Ruc,
    string? Entidad, string? TipoIdentificacion, string? Identificacion, string? Cliente, string? Monto, string? Productos, string? MontoOrdenado,
    string? Observacion);
public sealed record CargaMasivaResultadoDto(int Oficios, int Ordenes, IReadOnlyList<ErrorCargaDto> Errores);
public sealed record ErrorCargaDto(int Fila, string Oficio, string Motivo);
