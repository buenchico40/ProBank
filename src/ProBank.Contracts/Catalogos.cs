namespace ProBank.Contracts;

public sealed record ItemDto(Guid Token, string Codigo, string Titulo, string Tipo, string Estado, IReadOnlyList<string> Responsables);
public sealed record UsuarioDto(int UsuarioId, string Usuario, string Nombre);
