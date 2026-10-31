using ProBank.Application.Abstractions;
using ProBank.Contracts;
using ProBank.Domain.Common;

namespace ProBank.Application.Modules.Catalogos;

public interface ICatalogoRepository
{
    Task<IReadOnlyList<ItemDto>> Items(bool soloAbiertos, CancellationToken ct);
    Task<IReadOnlyList<UsuarioDto>> UsuariosActivos(CancellationToken ct);
}

public sealed record ListarItemsQuery(bool SoloAbiertos) : IQuery<IReadOnlyList<ItemDto>>;
public sealed record ListarUsuariosQuery : IQuery<IReadOnlyList<UsuarioDto>>;

internal sealed class ListarItemsHandler(ICatalogoRepository repo) : IQueryHandler<ListarItemsQuery, IReadOnlyList<ItemDto>>
{
    public async Task<Result<IReadOnlyList<ItemDto>>> Handle(ListarItemsQuery q, CancellationToken ct) => Result.Success(await repo.Items(q.SoloAbiertos, ct));
}

internal sealed class ListarUsuariosHandler(ICatalogoRepository repo) : IQueryHandler<ListarUsuariosQuery, IReadOnlyList<UsuarioDto>>
{
    public async Task<Result<IReadOnlyList<UsuarioDto>>> Handle(ListarUsuariosQuery q, CancellationToken ct) => Result.Success(await repo.UsuariosActivos(ct));
}
