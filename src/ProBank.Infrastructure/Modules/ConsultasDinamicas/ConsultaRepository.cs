using System.Data;
using System.Globalization;
using System.Text.Json;
using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Modules.ConsultasDinamicas;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.ConsultasDinamicas;

/// <summary>Consultas dinámicas: bases, catálogo, ejecuciones, auditoría y envíos programados. Solo procedimientos del esquema rpt.</summary>
internal sealed class ConsultaRepository(ISqlConnectionFactory db, ICurrentUser user) : IConsultaRepository
{
    private static DynamicParameters Con(object datos, Actor a)
    {
        var p = new DynamicParameters(datos);
        p.AddDynamicParams(new { ActorId = a.UsuarioId, a.RolActivo, a.Ip });
        return p;
    }

    private async Task<List<T>> Lista<T>(string sp, object? p, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<T>(new CommandDefinition(sp, p, commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    private async Task<DynamicParameters> Ejecutar(string sp, DynamicParameters p, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition(sp, p, commandType: Sp.Tipo, cancellationToken: ct));
        return p;
    }

    private static string Json<T>(T valor) => JsonSerializer.Serialize(valor);

    // ---------- Bases de datos ----------
    public async Task<IReadOnlyList<ConexionDto>> ListarConexiones(CancellationToken ct) => await Lista<ConexionDto>("rpt.usp_Conexion_Listar", null, ct);

    public async Task<ConexionDetalleDto?> DetalleConexion(string codigo, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("rpt.usp_Conexion_Listar", new { Codigo = codigo }, commandType: Sp.Tipo, cancellationToken: ct));
        var c = await m.ReadSingleOrDefaultAsync<ConexionDto>();
        return c is null ? null : new ConexionDetalleDto(c, (await m.ReadAsync<ConsultaResumenDto>()).ToList());
    }

    public async Task<string> GuardarConexion(string? codigo, GuardarConexionRequest r, Actor a, CancellationToken ct)
    {
        var p = Con(new { Codigo = codigo, r.Nombre, r.Servidor, r.BaseDatos, r.Autenticacion, r.SecretoKeyVault, r.Estado }, a);
        p.Add("CodigoGuardado", dbType: DbType.AnsiString, size: 20, direction: ParameterDirection.Output);
        return (await Ejecutar("rpt.usp_Conexion_Guardar", p, ct)).Get<string>("CodigoGuardado");
    }

    public Task EliminarConexion(string codigo, Actor a, CancellationToken ct) => Ejecutar("rpt.usp_Conexion_Eliminar", Con(new { Codigo = codigo }, a), ct);

    // ---------- Catálogo ----------
    public async Task<IReadOnlyList<ConsultaDto>> ListarConsultas(CancellationToken ct) => await Lista<ConsultaDto>("rpt.usp_Consulta_Listar", null, ct);

    public async Task<ConsultaDetalleDto?> DetalleConsulta(string codigo, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("rpt.usp_Consulta_Detalle", new { Codigo = codigo }, commandType: Sp.Tipo, cancellationToken: ct));
        var q = await m.ReadSingleOrDefaultAsync<ConsultaFila>();
        if (q is null) return null;
        var conexiones = (await m.ReadAsync<OpcionDto>()).ToList();
        var roles = (await m.ReadAsync<string>()).ToList();
        var aprobadores = (await m.ReadAsync<(Guid Token, string Nombre)>()).Select(x => new UsuarioOpcionDto(x.Token, x.Nombre, null)).ToList();
        var parametros = (await m.ReadAsync<ParametroDto>()).ToList();
        var ultimas = (await m.ReadAsync<EjecucionResumenDto>()).ToList();
        var programaciones = (await m.ReadAsync<ProgramacionFila>()).Select(Programacion).ToList();
        var columnas = string.IsNullOrEmpty(q.ColumnasJson) ? [] : JsonSerializer.Deserialize<List<ColumnaDto>>(q.ColumnasJson, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
        return new ConsultaDetalleDto(q.Codigo, q.Nombre, q.Descripcion, q.Procedimiento, q.TiempoMaximoSeg, q.LimiteFilas, q.Estado, q.DefinicionLeida,
            q.CreadoPor, q.FechaRegistro, conexiones, roles, aprobadores, parametros, columnas, ultimas, programaciones);
    }

    private static ProgramacionDto Programacion(ProgramacionFila f) => new(f.Codigo, f.ConexionCodigo, f.Base, f.Frecuencia,
        f.Hora.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
        string.IsNullOrEmpty(f.Dias) ? [] : f.Dias.Split(',').Select(int.Parse).ToList(), f.DiaDelMes, f.Formato,
        JsonSerializer.Deserialize<Dictionary<string, string?>>(f.ParametrosJson) ?? [],
        f.Destinatarios.Split(", ", StringSplitOptions.RemoveEmptyEntries), f.Responsable, f.ProximaEjecucion, f.UltimaEjecucion, f.UltimoResultado, f.Estado);

    public async Task<(string Codigo, bool VolvioBorrador)> GuardarConsulta(string? codigo, GuardarConsultaRequest r, Actor a, CancellationToken ct)
    {
        var p = Con(new
        {
            Codigo = codigo, r.Nombre, r.Descripcion, r.Procedimiento, r.TiempoMaximoSeg, r.LimiteFilas,
            Conexiones = Json(r.Conexiones), Roles = Json(r.Roles), Aprobadores = Json(r.Aprobadores),
        }, a);
        p.Add("CodigoGuardado", dbType: DbType.AnsiString, size: 20, direction: ParameterDirection.Output);
        p.Add("VolvioBorrador", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        await Ejecutar("rpt.usp_Consulta_Guardar", p, ct);
        return (p.Get<string>("CodigoGuardado"), p.Get<bool>("VolvioBorrador"));
    }

    public async Task<bool> GuardarDefinicion(string codigo, string conexion, IReadOnlyList<(string Nombre, string TipoSql, string Tipo)> parametros,
        IReadOnlyList<ColumnaDto> columnas, Actor a, CancellationToken ct)
    {
        var p = Con(new
        {
            Codigo = codigo, ConexionCodigo = conexion,
            ParametrosJson = Json(parametros.Select(x => new { nombre = x.Nombre, tipoSql = x.TipoSql, tipo = x.Tipo })),
            ColumnasJson = Json(columnas.Select(c => new { nombre = c.Nombre, tipo = c.Tipo })),
        }, a);
        p.Add("VolvioBorrador", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        await Ejecutar("rpt.usp_Consulta_GuardarDefinicion", p, ct);
        return p.Get<bool>("VolvioBorrador");
    }

    public Task GuardarParametros(string codigo, IReadOnlyList<ParametroDto> parametros, Actor a, CancellationToken ct) =>
        Ejecutar("rpt.usp_Consulta_GuardarParametros", Con(new
        {
            Codigo = codigo,
            ParametrosJson = Json(parametros.Select(x => new { nombre = x.Nombre, etiqueta = x.Etiqueta, tipo = x.Tipo, obligatorio = x.Obligatorio,
                valorDefecto = x.ValorDefecto, opciones = x.Opciones })),
        }, a), ct);

    public Task CambiarEstadoConsulta(string codigo, string accion, Actor a, CancellationToken ct) =>
        Ejecutar("rpt.usp_Consulta_CambiarEstado", Con(new { Codigo = codigo, Accion = accion }, a), ct);

    public Task EliminarConsulta(string codigo, Actor a, CancellationToken ct) => Ejecutar("rpt.usp_Consulta_Eliminar", Con(new { Codigo = codigo }, a), ct);

    public async Task<IReadOnlyList<ConsultaDisponibleDto>> Disponibles(string rolActivo, CancellationToken ct) =>
        await Lista<ConsultaDisponibleDto>("rpt.usp_Consulta_Disponibles", new { RolActivo = rolActivo }, ct);

    public async Task<OpcionesBase> Opciones(CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("rpt.usp_Consulta_Opciones", commandType: Sp.Tipo, cancellationToken: ct));
        return new OpcionesBase((await m.ReadAsync<string>()).ToList(), (await m.ReadAsync<UsuarioOpcionDto>()).ToList(), (await m.ReadAsync<OpcionDto>()).ToList());
    }

    public async Task<ConsultaEjecutable?> Obtener(string codigo, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("rpt.usp_Consulta_Obtener", new { Codigo = codigo }, commandType: Sp.Tipo, cancellationToken: ct));
        var c = await m.ReadSingleOrDefaultAsync<ObtenerFila>();
        if (c is null) return null;
        var conexiones = (await m.ReadAsync<(string Codigo, string Nombre)>()).Select(x => new OpcionDto(x.Codigo, x.Nombre, "Activo")).ToList();
        var roles = (await m.ReadAsync<string>()).ToList();
        var aprobadores = (await m.ReadAsync<(int UsuarioId, string Nombre)>()).ToList();
        var parametros = (await m.ReadAsync<ParametroFila>())
            .Select(p => new ParametroConsulta(p.Nombre, p.Etiqueta, Enum.Parse<TipoParametro>(p.Tipo), p.Obligatorio, p.ValorDefecto, p.Opciones)).ToList();
        var consulta = new Consulta
        {
            ConsultaId = c.ConsultaId, Codigo = c.Codigo, Nombre = c.Nombre, Procedimiento = c.Procedimiento,
            TiempoMaximoSeg = c.TiempoMaximoSeg, LimiteFilas = c.LimiteFilas, Publicada = c.Estado == "Publicada",
            ConexionesPermitidas = conexiones.Select(x => x.Codigo).ToList(), RolesPermitidos = roles,
            Aprobadores = aprobadores.Select(x => x.UsuarioId).ToList(), Parametros = parametros
        };
        return new ConsultaEjecutable(consulta, conexiones, aprobadores.Select(x => x.Nombre).ToList());
    }

    // ---------- Ejecuciones ----------
    public async Task<(long Id, string Codigo)> CrearEjecucion(int consultaId, string conexion, int solicitanteId, IReadOnlyList<int> aprobadores,
        string parametrosResueltos, string parametrosJson, bool programada, bool pendienteAprobacion, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return await cn.QuerySingleAsync<(long, string)>(new CommandDefinition("rpt.usp_Ejecucion_Crear", new
        {
            ConsultaId = consultaId, ConexionCodigo = conexion, SolicitanteId = solicitanteId, Aprobadores = string.Join(',', aprobadores),
            ParametrosResueltos = parametrosResueltos.Length > 2000 ? parametrosResueltos[..2000] : parametrosResueltos, ParametrosJson = parametrosJson,
            Programada = programada, Estado = pendienteAprobacion ? "PendienteAprobacion" : "Aprobada"
        }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task CerrarEjecucion(long ejecucionId, int? filas, int? duracionMs, bool truncado, string? rutaResultado, string? mensaje, string estado, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("rpt.usp_Ejecucion_Cerrar", new
        {
            EjecucionId = ejecucionId, Filas = filas, DuracionMs = duracionMs, Truncado = truncado, RutaResultado = rutaResultado, Mensaje = mensaje, Estado = estado
        }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    /// <summary>SQL-06: bitácora de cada evento de la ejecución, con IP y usuario (sin usuario: el programador).</summary>
    public async Task RegistrarEvento(long ejecucionId, string evento, string detalle, string resultado, int? filas, int? duracionMs, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("rpt.usp_EjecucionEvento_Registrar", new
        {
            EjecucionId = ejecucionId, UsuarioId = user.UsuarioId, Evento = evento, Detalle = detalle.Length > 2000 ? detalle[..2000] : detalle,
            Resultado = resultado, Filas = filas, DuracionMs = duracionMs, Ip = user.UsuarioId == 0 || string.IsNullOrEmpty(user.Ip) ? null : user.Ip
        }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<EjecucionDto>> ListarEjecuciones(int usuarioId, bool todas, CancellationToken ct) =>
        await Lista<EjecucionDto>("rpt.usp_Ejecucion_Listar", new { UsuarioId = usuarioId, Todas = todas }, ct);

    public async Task<EjecucionInterna?> ObtenerEjecucion(string codigo, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("rpt.usp_Ejecucion_Obtener", new { Codigo = codigo }, commandType: Sp.Tipo, cancellationToken: ct));
        var e = await m.ReadSingleOrDefaultAsync<EjecucionFila>();
        if (e is null) return null;
        var aprobaciones = (await m.ReadAsync<AprobacionFila>()).Select(a => (a.UsuarioId, new AprobacionDto(a.Nombre, a.Decision, a.Fecha, a.Comentario))).ToList();
        var eventos = (await m.ReadAsync<EventoFila>())
            .Select(v => new EventoEjecucionDto(v.Fecha, v.Usuario, v.Evento, e.Codigo, null, null, v.Detalle, v.Filas, v.DuracionMs, v.Resultado, v.Ip)).ToList();
        return new EjecucionInterna(e.EjecucionId, e.Codigo, e.ConsultaCodigo, e.ConsultaNombre, e.Procedimiento, e.ConexionCodigo, e.Base, e.SolicitanteId,
            e.SolicitadaPor, e.Fecha, e.ParametrosResueltos, e.ParametrosJson, e.Programada, e.Filas, e.DuracionMs, e.Truncado, e.RutaResultado, e.Mensaje,
            e.Estado, aprobaciones, eventos);
    }

    public async Task<string> ResolverEjecucion(string codigo, int usuarioId, bool aprobar, string? comentario, string ip, CancellationToken ct)
    {
        var p = new DynamicParameters(new { Codigo = codigo, UsuarioId = usuarioId, Aprobar = aprobar, Comentario = comentario, Ip = ip });
        p.Add("Estado", dbType: DbType.AnsiString, size: 20, direction: ParameterDirection.Output);
        return (await Ejecutar("rpt.usp_Ejecucion_Resolver", p, ct)).Get<string>("Estado");
    }

    public async Task<IReadOnlyList<EventoEjecucionDto>> ListarEventos(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        await Lista<EventoEjecucionDto>("rpt.usp_EjecucionEvento_Listar",
            new { Desde = desde.ToDateTime(TimeOnly.MinValue), Hasta = hasta.ToDateTime(TimeOnly.MinValue) }, ct);

    public async Task<IReadOnlyList<(long EjecucionId, string Codigo, string Ruta)>> ResultadosVencidos(int dias, CancellationToken ct) =>
        await Lista<(long, string, string)>("rpt.usp_Ejecucion_ResultadosVencidos", new { Dias = dias }, ct);

    public async Task QuitarResultado(long ejecucionId, int dias, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("rpt.usp_Ejecucion_QuitarResultado", new { EjecucionId = ejecucionId, Dias = dias },
            commandType: Sp.Tipo, cancellationToken: ct));
    }

    // ---------- Envíos programados ----------
    public async Task<string> GuardarProgramacion(string? codigo, string consulta, GuardarProgramacionRequest r, DateTime proxima, Actor a, CancellationToken ct)
    {
        var p = Con(new
        {
            Codigo = codigo, ConsultaCodigo = consulta, ConexionCodigo = r.Conexion, r.Frecuencia,
            Hora = TimeSpan.ParseExact(r.Hora, @"hh\:mm", CultureInfo.InvariantCulture),
            Dias = r.Frecuencia == "Semanal" ? string.Join(',', r.Dias.Order()) : null,
            DiaDelMes = r.Frecuencia == "Mensual" ? (byte?)(r.DiaDelMes ?? 0) : null,
            r.Formato, ParametrosJson = Json(r.Parametros), Destinatarios = Json(r.Destinatarios.Select(d => new { correo = d })),
            Estado = r.Activa ? "Activa" : "Pausada", ProximaEjecucion = proxima,
        }, a);
        p.Add("CodigoGuardado", dbType: DbType.AnsiString, size: 10, direction: ParameterDirection.Output);
        return (await Ejecutar("rpt.usp_Programacion_Guardar", p, ct)).Get<string>("CodigoGuardado");
    }

    public Task EliminarProgramacion(string codigo, Actor a, CancellationToken ct) => Ejecutar("rpt.usp_Programacion_Eliminar", Con(new { Codigo = codigo }, a), ct);

    private sealed record ConsultaFila(string Codigo, string Nombre, string? Descripcion, string Procedimiento, int TiempoMaximoSeg, int LimiteFilas, string Estado,
        string? ColumnasJson, DateTime? DefinicionLeida, string CreadoPor, DateTime FechaRegistro);
    private sealed record ObtenerFila(int ConsultaId, string Codigo, string Nombre, string Procedimiento, int TiempoMaximoSeg, int LimiteFilas, string Estado);
    private sealed record ParametroFila(string Nombre, string Etiqueta, string Tipo, bool Obligatorio, string? ValorDefecto, string? Opciones);
    private sealed record ProgramacionFila(string Codigo, string ConexionCodigo, string Base, string Frecuencia, TimeSpan Hora, string? Dias, int? DiaDelMes,
        string Formato, string ParametrosJson, string Destinatarios, string Responsable, DateTime ProximaEjecucion, DateTime? UltimaEjecucion,
        string? UltimoResultado, string Estado);
    private sealed record EjecucionFila(long EjecucionId, string Codigo, string ConsultaCodigo, string ConsultaNombre, string Procedimiento, string ConexionCodigo,
        string Base, int SolicitanteId, string SolicitadaPor, DateTime Fecha, string ParametrosResueltos, string ParametrosJson, bool Programada, int? Filas,
        int? DuracionMs, bool Truncado, string? RutaResultado, string? Mensaje, string Estado);
    private sealed record AprobacionFila(int UsuarioId, string Nombre, string? Decision, DateTime? Fecha, string? Comentario);
    private sealed record EventoFila(DateTime Fecha, string Usuario, string Evento, string? Detalle, int? Filas, int? DuracionMs, string Resultado, string? Ip);
}
