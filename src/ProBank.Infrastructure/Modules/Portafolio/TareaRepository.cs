using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Portafolio;
using ProBank.Contracts.Portafolio;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Portafolio;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Portafolio;

/// <summary>Repositorio de tareas: solo procedimientos almacenados del esquema pry.</summary>
internal sealed class TareaRepository(ISqlConnectionFactory db) : ITareaRepository
{
    private static DateTime F(DateOnly d) => d.ToDateTime(TimeOnly.MinValue);
    private static IReadOnlyList<int> Ids(string? csv) => string.IsNullOrEmpty(csv) ? [] : csv.Split(',').Select(int.Parse).ToList();

    public async Task<int?> ObtenerItemId(Guid itemToken, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return await cn.ExecuteScalarAsync<int?>(new CommandDefinition("pry.usp_Item_ObtenerId", new { Token = itemToken }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task<Tarea?> Obtener(Guid token, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var multi = await cn.QueryMultipleAsync(new CommandDefinition("pry.usp_Tarea_Obtener", new { Token = token }, commandType: Sp.Tipo, cancellationToken: ct));
        var fila = await multi.ReadSingleOrDefaultAsync<TareaFila>();
        if (fila is null) return null;
        var aprobadores = (await multi.ReadAsync<int>()).ToList();
        var decisiones = await multi.ReadAsync<DecisionFila>();
        var circuito = new CircuitoAprobacion(aprobadores, fila.CreadoPorId);
        foreach (var d in decisiones)
            circuito.Cargar(new DecisionAprobador(d.AprobadorId, Enum.Parse<Decision>(d.Decision), d.Fecha, d.Comentario));
        return Tarea.Rehidratar(fila.TareaId, fila.Token, fila.ItemId, fila.HitoId, fila.Titulo, fila.Area, fila.TecnicoId, fila.Orden,
            DateOnly.FromDateTime(fila.FechaInicio), DateOnly.FromDateTime(fila.FechaTerminacion), fila.Avance, Enum.Parse<EstadoTarea>(fila.Estado), circuito);
    }

    public async Task<string> Crear(Tarea t, string? descripcionHtml, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var p = new DynamicParameters(new
        {
            t.Token, t.ItemId, t.HitoId, t.Titulo, t.Area, t.TecnicoId, t.Orden, FechaInicio = F(t.FechaInicio), FechaTerminacion = F(t.FechaTerminacion),
            DescripcionHtml = descripcionHtml, UsuarioId = a.UsuarioId, a.RolActivo, a.Ip, Aprobadores = string.Join(',', t.Aprobacion.Aprobadores)
        });
        p.Add("Codigo", dbType: System.Data.DbType.String, size: 20, direction: System.Data.ParameterDirection.Output);
        await cn.ExecuteAsync(new CommandDefinition("pry.usp_Tarea_Crear", p, commandType: Sp.Tipo, cancellationToken: ct));
        return p.Get<string>("Codigo");
    }

    public async Task Actualizar(Guid token, ActualizarTareaRequest r, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("pry.usp_Tarea_Actualizar", new
        {
            Token = token, r.HitoId, r.Titulo, r.Area, r.TecnicoId, r.Orden, FechaInicio = F(r.FechaInicio), FechaTerminacion = F(r.FechaTerminacion), r.Avance,
            r.DescripcionHtml, Aprobadores = r.AprobadorIds is null ? null : string.Join(',', r.AprobadorIds), ActorId = a.UsuarioId, a.RolActivo, a.Ip
        }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task GuardarEstado(Tarea t, Actor a, string evento, string? detalle, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var ultima = t.Aprobacion.Decisiones.Count > 0 ? t.Aprobacion.Decisiones[^1] : null;
        await cn.ExecuteAsync(new CommandDefinition("pry.usp_Tarea_GuardarEstado", new
        {
            t.Token, Estado = t.Estado.ToString(), t.Avance, UsuarioId = a.UsuarioId, a.RolActivo, a.Ip, Evento = evento, Detalle = detalle,
            DecisionAprobadorId = ultima?.AprobadorId, Decision = ultima?.Decision.ToString(), Comentario = ultima?.Comentario
        }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<TareaDto>> Listar(Guid? itemToken, int? tecnicoId, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var filas = await cn.QueryAsync<TareaListaFila>(new CommandDefinition("pry.usp_Tarea_Listar",
            new { ItemToken = itemToken, TecnicoId = tecnicoId }, commandType: Sp.Tipo, cancellationToken: ct));
        return filas.Select(f => new TareaDto(f.Token, f.Codigo, f.Titulo, f.ItemToken, f.Item, f.HitoId, f.Hito, f.Area, f.TecnicoId, f.Tecnico, f.Orden,
            DateOnly.FromDateTime(f.FechaInicio), DateOnly.FromDateTime(f.FechaTerminacion), f.Avance, f.Estado,
            string.IsNullOrEmpty(f.Aprobadores) ? [] : f.Aprobadores.Split(", "), Ids(f.AprobadorIds), f.AvanceAprobacion)).ToList();
    }

    public async Task<TareaDetalleDto> Detalle(Guid token, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("pry.usp_Tarea_Detalle", new { Token = token }, commandType: Sp.Tipo, cancellationToken: ct));
        var desc = await m.ReadSingleOrDefaultAsync<string?>();
        var aprobadores = (await m.ReadAsync<AprobadorTareaDto>()).ToList();
        var historial = (await m.ReadAsync<EventoTareaDto>()).ToList();
        return new TareaDetalleDto(desc, aprobadores, historial);
    }

    public async Task<IReadOnlyList<AprobacionDto>> ListarAprobaciones(int usuarioId, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<AprobacionDto>(new CommandDefinition("pry.usp_Aprobacion_Listar", new { UsuarioId = usuarioId }, commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    private sealed record TareaFila(int TareaId, Guid Token, int ItemId, int? HitoId, string Titulo, string Area, int TecnicoId, int Orden,
        DateTime FechaInicio, DateTime FechaTerminacion, int Avance, string Estado, int CreadoPorId, string Codigo);
    private sealed record DecisionFila(int AprobadorId, string Decision, DateTime Fecha, string? Comentario);
    private sealed record TareaListaFila(Guid Token, string Codigo, string Titulo, Guid ItemToken, string Item, int? HitoId, string? Hito, string Area,
        int TecnicoId, string Tecnico, int Orden, DateTime FechaInicio, DateTime FechaTerminacion, int Avance, string Estado, string? Aprobadores,
        string? AprobadorIds, string AvanceAprobacion);
}
