namespace ProBank.Contracts.Marketing;

// MKT-01: imágenes promocionales. Estado mostrado: Borrador, Programado, Publicado, Vencido o Retirado.
public sealed record PromocionDto(Guid Token, string Codigo, string Titulo, string Ubicacion, string? TextoAlternativo, string? Enlace,
    DateOnly? Desde, DateOnly? Hasta, string Audiencia, int Orden, string Estado, bool TieneImagen, string CreadoPor, DateTime FechaRegistro,
    string? PublicadoPor, DateTime? FechaPublicacion);

public sealed record GuardarPromocionRequest(string Titulo, string Ubicacion, string? TextoAlternativo, string? Enlace, DateOnly? Desde,
    DateOnly? Hasta, string Audiencia, int Orden);

/// <summary>Imagen de la pantalla de inicio de sesión (sin sesión: solo lo vigente para todos).</summary>
public sealed record PromocionInicioDto(Guid Token, string Titulo, string? TextoAlternativo, string? Enlace);

// MKT-02: comunicados estructurados (resumen, secciones en ContenidoHtml y contacto).
public sealed record ComunicadoDto(Guid Token, string Codigo, string Titulo, string Tipo, string Resumen, string? ContenidoHtml, string? Contacto,
    DateOnly? Desde, DateOnly? Hasta, string Audiencia, bool Destacado, string Estado, string CreadoPor, DateTime FechaRegistro,
    string? PublicadoPor, DateTime? FechaPublicacion);

public sealed record GuardarComunicadoRequest(string Titulo, string Tipo, string Resumen, string? ContenidoHtml, string? Contacto, DateOnly? Desde,
    DateOnly? Hasta, string Audiencia, bool Destacado);

/// <summary>Resultado de guardar: el token sirve para subir la imagen enseguida.</summary>
public sealed record MarketingGuardadoDto(Guid Token);
