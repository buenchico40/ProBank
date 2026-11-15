namespace ProBank.Contracts.Decisiones;

// Tipos de decisión (DEC-02): flujo en secuencia o todos a la vez, con sus aprobadores.
public sealed record TipoDecisionDto(string Codigo, string Nombre, string? Descripcion, bool Secuencial, bool Activo, int Aprobadores, int Decisiones,
    IReadOnlyList<AprobadorTipoDto> ListaAprobadores);
public sealed record AprobadorTipoDto(int UsuarioId, string Usuario, string Nombre, string Estado, int Orden);
public sealed record GuardarTipoDecisionRequest(string Nombre, string? Descripcion, bool Secuencial, bool Activo, IReadOnlyList<AprobadorTipoRequest> Aprobadores);
public sealed record AprobadorTipoRequest(int UsuarioId, int Orden);

// Decisiones (DEC-01): Borrador → En aprobación → Aprobada, Rechazada o Anulada. EsMiTurno: espera la firma del usuario conectado;
// MiResultado, MiFechaFirma y MiVersionFirma: su última firma (Aprobada o Rechazada), si firmó alguna versión.
public sealed record DecisionDto(Guid Token, string Codigo, string TipoCodigo, string Tipo, string Titulo, string Area, DateOnly Fecha,
    string Estado, bool Secuencial, string CreadoPor, DateTime FechaRegistro, DateTime? FechaEnvio, DateTime? FechaCierre, int Aprobadas,
    int TotalAprobadores, bool EsMiTurno, bool SoyCreador, bool SoyNotificado, bool SoyAprobador, int Version, string? MiResultado = null,
    DateTime? MiFechaFirma = null, int? MiVersionFirma = null);
/// <summary>
/// Integra: el contenido no cambió desde que se envió o se modificó (huella guardada = huella actual).
/// TieneFirmas: alguien firmó (en cualquier versión), así que los aprobadores ya no se quitan.
/// </summary>
public sealed record DecisionDetalleDto(DecisionDto Decision, string Descripcion, string? MotivoAnulacion, string? Huella, string HuellaActual, bool Integra,
    bool TieneFirmas, IReadOnlyList<FirmaDto> Aprobadores, IReadOnlyList<NotificadoDto> Notificados, IReadOnlyList<EventoDecisionDto> Eventos,
    IReadOnlyList<VersionDecisionDto> Versiones, IReadOnlyList<FirmaAnteriorDto> FirmasAnteriores, IReadOnlyList<AdjuntoDecisionDto> Adjuntos);
/// <summary>DEC-03: firma de un aprobador. SelloValido: el sello coincide con la huella, el usuario, el resultado y la fecha. Origen: Tipo o Agregado.</summary>
public sealed record FirmaDto(int Orden, int UsuarioId, string Usuario, string Nombre, string Origen, string Resultado, string? Comentario, DateTime? Fecha,
    string? Ip, string? Sello, bool? SelloValido);
/// <summary>DEC-05: notificados (solo ven la decisión). UltimaConsulta: la última vez que la abrieron.</summary>
public sealed record NotificadoDto(int UsuarioId, string Usuario, string Nombre, DateTime Desde, string AgregadoPor, DateTime? UltimaConsulta);
/// <summary>DEC-06: trazabilidad de la decisión.</summary>
public sealed record EventoDecisionDto(DateTime Fecha, string Usuario, string Nombre, string Evento, string? Detalle, string? Ip);
/// <summary>DEC-07: versión enviada del contenido (la 1 al enviarla y una por cada modificación de un aprobador).</summary>
public sealed record VersionDecisionDto(int Version, string Titulo, string Area, DateOnly Fecha, string Huella, string? Motivo, string RegistradaPor,
    DateTime FechaRegistro, string Descripcion);
/// <summary>DEC-07: firma de una versión anterior; su sello se verifica contra la huella de esa versión.</summary>
public sealed record FirmaAnteriorDto(int Version, string Usuario, string Nombre, string Resultado, string? Comentario, DateTime Fecha, string? Ip, string Sello,
    bool SelloValido);

/// <summary>Notificados: ids de usuario; en borrador reemplazan a los anteriores.</summary>
public sealed record GuardarDecisionRequest(string Tipo, string Titulo, string Descripcion, string Area, DateOnly Fecha, IReadOnlyList<int>? Notificados);
/// <summary>DEC-03 y DEC-05: agregar o quitar un aprobador (Orden: turno; vacío, al final) o un notificado.</summary>
public sealed record ParticipanteRequest(int UsuarioId, int? Orden);
/// <summary>La clave del usuario confirma su identidad al firmar; no se guarda.</summary>
public sealed record FirmarDecisionRequest(bool Aprobar, string Clave, string? Comentario);
/// <summary>DEC-07: el aprobador corrige el contenido (con motivo y su clave) y todos vuelven a firmar.</summary>
public sealed record ModificarDecisionRequest(string Titulo, string Descripcion, string Area, DateOnly Fecha, string Motivo, string Clave);
public sealed record AnularDecisionRequest(string Motivo);
public sealed record DecisionGuardadaDto(Guid Token);
/// <summary>DEC-08: archivo de respaldo con su huella SHA-256 (no forma parte de la huella de la decisión).</summary>
public sealed record AdjuntoDecisionDto(Guid Token, string Nombre, string TipoContenido, long Tamano, string Hash, DateTime Fecha);
