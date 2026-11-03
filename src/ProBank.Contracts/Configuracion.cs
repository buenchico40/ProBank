namespace ProBank.Contracts.Configuracion;

/// <summary>Parámetro global: Tipo Texto, Numero, Lista, Correo o Secreto (solo el nombre en Key Vault, con su Prefijo).</summary>
public sealed record ParametroGlobalDto(string Clave, string Categoria, string Nombre, string? Descripcion, string Tipo, string? Opciones,
    int? Minimo, int? Maximo, string? Prefijo, string? Valor, string? ModificadoPor, DateTime? FechaModificacion);

/// <summary>Valores por clave; solo se guardan los que cambian.</summary>
public sealed record GuardarParametrosGlobalesRequest(IReadOnlyDictionary<string, string?> Valores);

public sealed record ProbarCorreoRequest(string Destinatario);
