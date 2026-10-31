namespace ProBank.Contracts.Portafolio;

// Tareas (PRY-03, PRY-04, PRY-05)
public sealed record TareaDto(Guid Token, string Codigo, string Titulo, Guid ItemToken, string Item, int? HitoId, string? Hito, string Area,
    int TecnicoId, string Tecnico, int Orden, DateOnly FechaInicio, DateOnly FechaTerminacion, int Avance, string Estado,
    IReadOnlyList<string> Aprobadores, IReadOnlyList<int> AprobadorIds, string AvanceAprobacion);

public sealed record TareaDetalleDto(string? DescripcionHtml, IReadOnlyList<AprobadorTareaDto> Aprobadores, IReadOnlyList<EventoTareaDto> Historial);
public sealed record AprobadorTareaDto(int UsuarioId, string Usuario, string Nombre, string? Decision, DateTime? Fecha, string? Comentario);
public sealed record EventoTareaDto(DateTime Fecha, string Evento, string? Detalle, string Usuario);

public sealed record CrearTareaRequest(Guid ItemToken, int? HitoId, string Titulo, string Area, int TecnicoId, int Orden,
    DateOnly FechaInicio, DateOnly FechaTerminacion, IReadOnlyList<int> AprobadorIds, string? DescripcionHtml);

public sealed record ActualizarTareaRequest(int? HitoId, string Titulo, string Area, int TecnicoId, int Orden, DateOnly FechaInicio,
    DateOnly FechaTerminacion, int Avance, IReadOnlyList<int>? AprobadorIds, string? DescripcionHtml);

public sealed record RechazarRequest(string Comentario);

// Aprobaciones (PRY-04)
public sealed record AprobacionDto(Guid Token, string Codigo, string Titulo, string Item, string SolicitadaPor, DateTime Enviada,
    string Estado, string Aprobaciones, string? MiDecision, string EstadoTarea);
