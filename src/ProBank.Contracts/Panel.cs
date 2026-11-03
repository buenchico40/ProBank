namespace ProBank.Contracts;

/// <summary>Inicio / Panel: indicadores, gráficos y contenidos vigentes de Marketing para el rol activo.</summary>
public sealed record PanelDto(int CoactivasActivas, int AprobacionesPendientes, int TareasAsignadas, int RenovacionesPorVencer,
    IReadOnlyList<SerieDto> Cobertura, IReadOnlyList<SerieDto> Items, IReadOnlyList<PromocionPanelDto> Promociones,
    IReadOnlyList<ComunicadoPanelDto> Comunicados);

public sealed record SerieDto(string Etiqueta, int Valor);
/// <summary>Imagen del carrusel: se descarga de /Marketing/Datos/Imagen?token= (TieneImagen).</summary>
public sealed record PromocionPanelDto(Guid Token, string Titulo, string? TextoAlternativo, string? Enlace, DateTime? Hasta, bool TieneImagen);
public sealed record ComunicadoPanelDto(string Codigo, string Titulo, string Tipo, string Resumen, string? ContenidoHtml, string? Contacto,
    DateTime Desde, DateTime? Hasta, string Audiencia, bool Destacado);
