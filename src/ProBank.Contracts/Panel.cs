namespace ProBank.Contracts;

/// <summary>Inicio / Panel: indicadores, gráficos y contenidos vigentes de Marketing para el rol activo.</summary>
public sealed record PanelDto(int CoactivasActivas, int AprobacionesPendientes, int TareasAsignadas, int RenovacionesPorVencer,
    IReadOnlyList<SerieDto> Cobertura, IReadOnlyList<SerieDto> Items, IReadOnlyList<PromocionPanelDto> Promociones,
    IReadOnlyList<ComunicadoPanelDto> Comunicados);

public sealed record SerieDto(string Etiqueta, int Valor);
public sealed record PromocionPanelDto(int PromocionId, string Titulo, DateTime? Hasta);
public sealed record ComunicadoPanelDto(string Codigo, string Titulo, string Tipo, string ContenidoHtml, DateTime Desde, DateTime? Hasta,
    string Audiencia, bool Destacado);
