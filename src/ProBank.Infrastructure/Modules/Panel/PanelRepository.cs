using Dapper;
using ProBank.Application.Modules.Panel;
using ProBank.Contracts;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Panel;

internal sealed class PanelRepository(ISqlConnectionFactory db) : IPanelRepository
{
    public async Task<PanelDto> Obtener(int usuarioId, string rolActivo, DateOnly hoy, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("rpt.usp_Panel_Resumen",
            new { UsuarioId = usuarioId, RolActivo = rolActivo, Hoy = hoy.ToDateTime(TimeOnly.MinValue) }, commandType: Sp.Tipo, cancellationToken: ct));
        var k = await m.ReadSingleAsync<Kpis>();
        var cobertura = (await m.ReadAsync<SerieDto>()).ToList();
        var items = (await m.ReadAsync<SerieDto>()).ToList();
        var promos = (await m.ReadAsync<PromocionPanelDto>()).ToList();
        var comunicados = (await m.ReadAsync<ComunicadoPanelDto>()).ToList();
        return new PanelDto(k.CoactivasActivas, k.AprobacionesPendientes, k.TareasAsignadas, k.RenovacionesPorVencer, cobertura, items, promos, comunicados);
    }

    private sealed record Kpis(int CoactivasActivas, int AprobacionesPendientes, int TareasAsignadas, int RenovacionesPorVencer);
}
