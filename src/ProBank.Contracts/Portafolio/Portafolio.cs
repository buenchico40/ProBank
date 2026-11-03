namespace ProBank.Contracts.Portafolio;

// Proyectos, mejoras y bugs (PRY-02, PRY-06)
public sealed record ItemPortafolioDto(Guid Token, string Codigo, string Titulo, string Tipo, string Estado, string? Responsables,
    IReadOnlyList<int> ResponsableIds, int Tareas, int Completadas, int Avance);

public sealed record ItemDetalleDto(Guid Token, string Codigo, string Titulo, string Tipo, string Estado, string? DescripcionHtml,
    DateTime FechaRegistro, string? CreadoPor, IReadOnlyList<ResponsableDto> Responsables, IReadOnlyList<HitoDto> Hitos,
    IReadOnlyList<AcuerdoDto> Acuerdos);
public sealed record ResponsableDto(int UsuarioId, string Usuario, string Nombre, string? Roles);
public sealed record HitoDto(int HitoId, string Nombre, int Orden, DateOnly? FechaInicio, DateOnly? FechaFin, int Tareas);

public sealed record GuardarItemRequest(string Tipo, string Titulo, string? DescripcionHtml, IReadOnlyList<int> ResponsableIds, string Estado);
public sealed record ItemGuardadoDto(Guid Token, string Codigo);

/// <summary>Documento adjunto de un ítem o de una tarea; se descarga por su token.</summary>
public sealed record AdjuntoDto(Guid Token, string Nombre, string Descripcion, string TipoContenido, long Tamano, string SubidoPor, DateTime FechaRegistro);
public sealed record ActualizarAdjuntoRequest(string Descripcion);

/// <summary>Tipos y tamaño máximo de los adjuntos de un ámbito: "Item" (proyectos, mejoras y bugs) o "Tarea".</summary>
public sealed record ParametroAdjuntoDto(string Ambito, IReadOnlyList<string> Extensiones, int MaximoMB);
/// <summary>Parametrización vigente y el catálogo de tipos que se pueden habilitar.</summary>
public sealed record ParametrosAdjuntoDto(IReadOnlyList<ParametroAdjuntoDto> Ambitos, IReadOnlyList<string> Catalogo, int MaximoMBPermitido);
public sealed record GuardarParametroAdjuntoRequest(IReadOnlyList<string> Extensiones, int MaximoMB);
public sealed record GuardarHitosRequest(IReadOnlyList<HitoRequest> Hitos);
public sealed record HitoRequest(int? HitoId, string Nombre, int Orden, DateOnly? Inicio, DateOnly? Fin);

// Cronograma (PRY-07)
public sealed record CronogramaDto(IReadOnlyList<CronoItemDto> Items, IReadOnlyList<CronoHitoDto> Hitos, IReadOnlyList<CronoTareaDto> Tareas);
public sealed record CronoItemDto(int ItemId, Guid Token, string Codigo, string Titulo, string Tipo, string Estado);
public sealed record CronoHitoDto(int HitoId, int ItemId, string Nombre, int Orden, DateOnly? FechaInicio, DateOnly? FechaFin);
public sealed record CronoTareaDto(Guid Token, string Codigo, string Titulo, int ItemId, string Item, int? HitoId, string Area, string Tecnico,
    DateOnly FechaInicio, DateOnly FechaTerminacion, int Avance, string Estado);

// Acuerdos semanales (PRY-08)
public sealed record AcuerdoDto(int AcuerdoId, string Codigo, Guid ItemToken, string Item, string ItemTitulo, int RevisionId, DateOnly FechaRevision,
    string? Temas, string Descripcion, int ResponsableId, string Responsable, DateOnly FechaCumplimiento, DateOnly? FechaCumplido, string Estado);
public sealed record RegistrarAcuerdosRequest(Guid ItemToken, DateOnly Fecha, string? Temas, IReadOnlyList<NuevoAcuerdo> Acuerdos);
public sealed record NuevoAcuerdo(string Descripcion, int ResponsableId, DateOnly Fecha);
public sealed record ActualizarAcuerdoRequest(string Descripcion, int ResponsableId, DateOnly FechaCumplimiento);

// Informes de seguimiento
public sealed record InformeDto(int InformeId, string Codigo, string Tipo, DateOnly Desde, DateOnly Hasta, string Incluye, string Estado,
    DateTime? FechaGenerado, string? GeneradoPor);
/// <summary>TienePdf: el informe ya se generó y su PDF se descarga de api/portafolio/informes/{id}/pdf.</summary>
public sealed record InformeContenidoDto(int InformeId, string Codigo, string Tipo, DateOnly Desde, DateOnly Hasta, string Estado, bool TienePdf);
public sealed record GuardarInformeRequest(string Tipo, DateOnly Desde, DateOnly Hasta);
public sealed record ResumenInformeDto(string Item, string Titulo, string Tipo, string Estado, int Tareas, int Completadas, int Avance,
    int TareasConMovimiento, int Acuerdos, int Cumplidos, int Vencidos);

// Parametrización (PRY-09)
public sealed record AccionRolDto(string Clave, string Nombre, string Estado, IReadOnlyList<string> Roles);
public sealed record RolResumenDto(string Nombre, string? Descripcion, string Estado, int Usuarios);
public sealed record ParametrizacionDto(IReadOnlyList<AccionRolDto> Acciones, IReadOnlyList<RolResumenDto> Roles);
public sealed record GuardarAccionRolRequest(IReadOnlyList<string> Roles);
