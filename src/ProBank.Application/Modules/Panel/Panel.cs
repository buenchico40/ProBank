using ProBank.Application.Abstractions;
using ProBank.Contracts;
using ProBank.Domain.Common;

namespace ProBank.Application.Modules.Panel;

public sealed record ObtenerPanelQuery : IQuery<PanelDto>;

public interface IPanelRepository
{
    Task<PanelDto> Obtener(int usuarioId, string rolActivo, DateOnly hoy, CancellationToken ct);
}

internal sealed class ObtenerPanelHandler(IPanelRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) : IQueryHandler<ObtenerPanelQuery, PanelDto>
{
    public async Task<Result<PanelDto>> Handle(ObtenerPanelQuery q, CancellationToken ct) =>
        await permisos.Tiene("inicio", "ver", ct)
            ? Result.Success(await repo.Obtener(user.UsuarioId, user.RolActivo, clock.Hoy, ct))
            : Result.Failure<PanelDto>(Error.Forbidden("Tu rol activo no tiene acceso al Panel."));
}
