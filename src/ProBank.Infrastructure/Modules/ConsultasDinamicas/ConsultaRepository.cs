using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Domain.Modules.ConsultasDinamicas;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.ConsultasDinamicas;

internal sealed class ConsultaRepository(ISqlConnectionFactory db, ICurrentUser user) : IConsultaRepository
{
    public async Task<Consulta?> Obtener(string codigo, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("rpt.usp_Consulta_Obtener", new { Codigo = codigo }, commandType: Sp.Tipo, cancellationToken: ct));
        var c = await m.ReadSingleOrDefaultAsync<ConsultaFila>();
        if (c is null) return null;
        var conexiones = (await m.ReadAsync<int>()).ToList();
        var roles = (await m.ReadAsync<string>()).ToList();
        var aprobadores = (await m.ReadAsync<int>()).ToList();
        var parametros = (await m.ReadAsync<ParametroFila>())
            .Select(p => new ParametroConsulta(p.Nombre, p.Etiqueta, Enum.Parse<TipoParametro>(p.Tipo), p.Obligatorio, p.ValorDefecto)).ToList();
        return new Consulta
        {
            ConsultaId = c.ConsultaId, Codigo = c.Codigo, Nombre = c.Nombre, Procedimiento = c.Procedimiento,
            TiempoMaximoSeg = c.TiempoMaximoSeg, LimiteFilas = c.LimiteFilas, Publicada = c.Estado == "Publicada",
            ConexionesPermitidas = conexiones, RolesPermitidos = roles, Aprobadores = aprobadores, Parametros = parametros
        };
    }

    public async Task<(long Id, string Codigo)> CrearEjecucion(int consultaId, int conexionId, int solicitanteId, IReadOnlyList<int> aprobadores,
        string parametrosResueltos, bool pendienteAprobacion, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return await cn.QuerySingleAsync<(long, string)>(new CommandDefinition("rpt.usp_Ejecucion_Crear", new
        {
            ConsultaId = consultaId, ConexionId = conexionId, SolicitanteId = solicitanteId, Aprobadores = string.Join(',', aprobadores),
            ParametrosResueltos = parametrosResueltos, Estado = pendienteAprobacion ? "PendienteAprobacion" : "Aprobada"
        }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task CerrarEjecucion(long ejecucionId, int filas, int duracionMs, string estado, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("rpt.usp_Ejecucion_Cerrar",
            new { EjecucionId = ejecucionId, Filas = filas, DuracionMs = duracionMs, Estado = estado }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    /// <summary>SQL-06: bitácora de cada evento de la ejecución, con IP y usuario.</summary>
    public async Task RegistrarEvento(long ejecucionId, string evento, string detalle, string resultado, int? filas, int? duracionMs, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("rpt.usp_EjecucionEvento_Registrar", new
        {
            EjecucionId = ejecucionId, UsuarioId = user.UsuarioId, Evento = evento, Detalle = detalle, Resultado = resultado,
            Filas = filas, DuracionMs = duracionMs, Ip = user.Ip
        }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    private sealed record ConsultaFila(int ConsultaId, string Codigo, string Nombre, string Procedimiento, int TiempoMaximoSeg, int LimiteFilas, string Estado);
    private sealed record ParametroFila(string Nombre, string Etiqueta, string Tipo, bool Obligatorio, string? ValorDefecto);
}
