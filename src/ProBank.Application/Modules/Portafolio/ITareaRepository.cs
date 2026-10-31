using ProBank.Application.Abstractions;
using ProBank.Contracts.Portafolio;
using ProBank.Domain.Modules.Portafolio;

namespace ProBank.Application.Modules.Portafolio;

public interface ITareaRepository
{
    Task<int?> ObtenerItemId(Guid itemToken, CancellationToken ct);
    Task<Tarea?> Obtener(Guid token, CancellationToken ct);
    Task<string> Crear(Tarea tarea, string? descripcionHtml, Actor actor, CancellationToken ct);
    Task Actualizar(Guid token, ActualizarTareaRequest r, Actor actor, CancellationToken ct);
    /// <summary>Guarda el estado decidido por el dominio con su evento de historial y la decisión del aprobador, si la hay.</summary>
    Task GuardarEstado(Tarea tarea, Actor actor, string evento, string? detalle, CancellationToken ct);
    Task<IReadOnlyList<TareaDto>> Listar(Guid? itemToken, int? tecnicoId, CancellationToken ct);
    Task<TareaDetalleDto> Detalle(Guid token, CancellationToken ct);
    Task<IReadOnlyList<AprobacionDto>> ListarAprobaciones(int usuarioId, CancellationToken ct);
}
