using Dapper;
using ProBank.Application.Modules.Catalogos;
using ProBank.Contracts;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Catalogos;

internal sealed class CatalogoRepository(ISqlConnectionFactory db) : ICatalogoRepository
{
    public async Task<IReadOnlyList<ItemDto>> Items(bool soloAbiertos, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var filas = await cn.QueryAsync<ItemFila>(new CommandDefinition("pry.usp_Item_Listar", new { SoloAbiertos = soloAbiertos }, commandType: Sp.Tipo, cancellationToken: ct));
        return filas.Select(f => new ItemDto(f.Token, f.Codigo, f.Titulo, f.Tipo, f.Estado,
            string.IsNullOrEmpty(f.Responsables) ? [] : f.Responsables.Split(", "))).ToList();
    }

    public async Task<IReadOnlyList<UsuarioDto>> UsuariosActivos(CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<UsuarioDto>(new CommandDefinition("seg.usp_Usuario_ListarActivos", commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    private sealed record ItemFila(Guid Token, string Codigo, string Titulo, string Tipo, string Estado, string? Responsables, string? ResponsableIds,
        int Tareas, int Completadas, int Avance);
}
