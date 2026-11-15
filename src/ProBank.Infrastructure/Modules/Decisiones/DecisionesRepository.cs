using System.Data;
using System.Text.Json;
using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Decisiones;
using ProBank.Contracts.Decisiones;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Decisiones;

/// <summary>Decisiones: tipos con sus aprobadores, decisiones, firmas, notificados, versiones y trazabilidad. Solo procedimientos del esquema dec.</summary>
internal sealed class DecisionesRepository(ISqlConnectionFactory db) : IDecisionesRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static DateTime F(DateOnly d) => d.ToDateTime(TimeOnly.MinValue);

    private static DynamicParameters Con(object datos, Actor a)
    {
        var p = new DynamicParameters(datos);
        p.AddDynamicParams(new { ActorId = a.UsuarioId, a.RolActivo, a.Ip });
        return p;
    }

    private async Task<DynamicParameters> Ejecutar(string sp, DynamicParameters p, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition(sp, p, commandType: Sp.Tipo, cancellationToken: ct));
        return p;
    }

    // ---------- Tipos de decisión ----------
    public async Task<IReadOnlyList<TipoDecisionDto>> ListarTipos(bool soloActivos, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("dec.usp_Tipo_Listar", new { SoloActivos = soloActivos }, commandType: Sp.Tipo, cancellationToken: ct));
        var tipos = (await m.ReadAsync<TipoFila>()).ToList();
        var aprobadores = (await m.ReadAsync<AprobadorFila>()).ToLookup(a => a.Tipo);
        return tipos.Select(t => new TipoDecisionDto(t.Codigo, t.Nombre, t.Descripcion, t.Secuencial, t.Activo, t.Aprobadores, t.Decisiones,
            aprobadores[t.Codigo].Select(a => new AprobadorTipoDto(a.UsuarioId, a.Usuario, a.Nombre, a.Estado, a.Orden)).ToList())).ToList();
    }

    public async Task<string> GuardarTipo(string? codigo, GuardarTipoDecisionRequest r, Actor a, CancellationToken ct)
    {
        var p = Con(new { Codigo = codigo, r.Nombre, r.Descripcion, r.Secuencial, r.Activo, Aprobadores = JsonSerializer.Serialize(r.Aprobadores, Json) }, a);
        p.Add("CodigoGuardado", dbType: DbType.AnsiString, size: 10, direction: ParameterDirection.Output);
        return (await Ejecutar("dec.usp_Tipo_Guardar", p, ct)).Get<string>("CodigoGuardado");
    }

    // ---------- Decisiones ----------
    public async Task<IReadOnlyList<DecisionDto>> Listar(string alcance, Guid? token, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<DecisionFila>(new CommandDefinition("dec.usp_Decision_Listar",
            new { ActorId = a.UsuarioId, a.RolActivo, Alcance = alcance, Token = token }, commandType: Sp.Tipo, cancellationToken: ct))).Select(Dto).ToList();
    }

    public async Task<DecisionDetalleDto?> Detalle(Guid token, string evento, Actor a, CancellationToken ct)
    {
        if ((await Listar("todas", token, a, ct)).FirstOrDefault() is not { } d) return null;
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("dec.usp_Decision_Detalle", Con(new { Token = token, Evento = evento }, a),
            commandType: Sp.Tipo, cancellationToken: ct));
        var x = await m.ReadSingleAsync<DetalleFila>();
        var firmas = (await m.ReadAsync<FirmaDto>()).ToList();
        var notificados = (await m.ReadAsync<NotificadoDto>()).ToList();
        var eventos = (await m.ReadAsync<EventoDecisionDto>()).ToList();
        var versiones = (await m.ReadAsync<VersionFila>()).Select(v => new VersionDecisionDto(v.Version, v.Titulo, v.Area, DateOnly.FromDateTime(v.Fecha),
            v.Huella, v.Motivo, v.RegistradaPor, v.FechaRegistro, v.Descripcion)).ToList();
        var anteriores = (await m.ReadAsync<FirmaAnteriorDto>()).ToList();
        var adjuntos = (await m.ReadAsync<AdjuntoDecisionDto>()).ToList();
        return new DecisionDetalleDto(d, x.Descripcion, x.MotivoAnulacion, x.Huella, x.HuellaActual, x.Integra, x.TieneFirmas, firmas, notificados, eventos,
            versiones, anteriores, adjuntos);
    }

    public async Task<Guid> Guardar(Guid? token, GuardarDecisionRequest r, Actor a, CancellationToken ct)
    {
        var p = Con(new
        {
            Token = token, TipoCodigo = r.Tipo, r.Titulo, r.Descripcion, r.Area, Fecha = F(r.Fecha),
            Notificados = string.Join(',', r.Notificados ?? [])
        }, a);
        p.Add("TokenGuardado", dbType: DbType.Guid, direction: ParameterDirection.Output);
        return (await Ejecutar("dec.usp_Decision_Guardar", p, ct)).Get<Guid>("TokenGuardado");
    }

    public Task Enviar(Guid token, Actor a, CancellationToken ct) => Ejecutar("dec.usp_Decision_Enviar", Con(new { Token = token }, a), ct);

    public Task CambiarParticipante(Guid token, string rol, bool agregar, int usuarioId, int? orden, Actor a, CancellationToken ct) =>
        Ejecutar("dec.usp_Decision_CambiarParticipante", Con(new { Token = token, Rol = rol, Accion = agregar ? "Agregar" : "Quitar", UsuarioId = usuarioId, Orden = orden }, a), ct);

    public async Task<string> Firmar(Guid token, bool aprobar, string? comentario, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token, Aprobar = aprobar, Comentario = comentario }, a);
        p.Add("EstadoFinal", dbType: DbType.String, size: 20, direction: ParameterDirection.Output);
        return (await Ejecutar("dec.usp_Decision_Firmar", p, ct)).Get<string>("EstadoFinal");
    }

    public async Task<int> Modificar(Guid token, ModificarDecisionRequest r, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token, r.Titulo, r.Descripcion, r.Area, Fecha = F(r.Fecha), r.Motivo }, a);
        p.Add("VersionNueva", dbType: DbType.Int32, direction: ParameterDirection.Output);
        return (await Ejecutar("dec.usp_Decision_Modificar", p, ct)).Get<int>("VersionNueva");
    }

    public Task Anular(Guid token, string motivo, Actor a, CancellationToken ct) => Ejecutar("dec.usp_Decision_Anular", Con(new { Token = token, Motivo = motivo }, a), ct);

    // ---------- Archivos de respaldo ----------
    public Task RegistrarAdjunto(Guid token, Guid decisionToken, string nombre, string tipo, long tamano, byte[] hash, string ruta, int maximo, Actor a, CancellationToken ct) =>
        Ejecutar("dec.usp_Adjunto_Registrar", Con(new { Token = token, DecisionToken = decisionToken, Nombre = nombre, TipoContenido = tipo, Tamano = tamano, Hash = hash,
            Ruta = ruta, MaximoArchivos = maximo }, a), ct);

    public async Task<string> EliminarAdjunto(Guid token, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token }, a);
        p.Add("Ruta", dbType: DbType.AnsiString, size: 300, direction: ParameterDirection.Output);
        return (await Ejecutar("dec.usp_Adjunto_Eliminar", p, ct)).Get<string>("Ruta");
    }

    public async Task<(string Nombre, string Tipo, string Ruta)?> ObtenerAdjunto(Guid token, Actor a, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var f = await cn.QuerySingleOrDefaultAsync<AdjuntoFila>(new CommandDefinition("dec.usp_Adjunto_Obtener", Con(new { Token = token }, a), commandType: Sp.Tipo, cancellationToken: ct));
        return f is null ? null : (f.Nombre, f.TipoContenido, f.Ruta);
    }

    private static DecisionDto Dto(DecisionFila f) => new(f.Token, f.Codigo, f.TipoCodigo, f.Tipo, f.Titulo, f.Area, DateOnly.FromDateTime(f.Fecha),
        f.Estado, f.Secuencial, f.CreadoPor, f.FechaRegistro, f.FechaEnvio, f.FechaCierre, f.Aprobadas, f.TotalAprobadores, f.EsMiTurno, f.SoyCreador,
        f.SoyNotificado, f.SoyAprobador, f.Version, f.MiResultado, f.MiFechaFirma, f.MiVersionFirma);

    private sealed record AdjuntoFila(string Nombre, string TipoContenido, string Ruta, string Hash);
    private sealed record TipoFila(string Codigo, string Nombre, string? Descripcion, bool Secuencial, bool Activo, int Aprobadores, int Decisiones);
    private sealed record AprobadorFila(string Tipo, int UsuarioId, string Usuario, string Nombre, string Estado, int Orden);
    private sealed record DecisionFila(Guid Token, string Codigo, string TipoCodigo, string Tipo, string Titulo, string Area, DateTime Fecha,
        string Estado, bool Secuencial, string CreadoPor, DateTime FechaRegistro, DateTime? FechaEnvio, DateTime? FechaCierre, int Aprobadas,
        int TotalAprobadores, bool EsMiTurno, bool SoyCreador, bool SoyNotificado, bool SoyAprobador, int Version, string? MiResultado,
        DateTime? MiFechaFirma, int? MiVersionFirma);
    private sealed record DetalleFila(string Descripcion, string? MotivoAnulacion, string? Huella, string HuellaActual, bool Integra, bool TieneFirmas);
    private sealed record VersionFila(int Version, string Titulo, string Area, DateTime Fecha, string Huella, string? Motivo, string RegistradaPor,
        DateTime FechaRegistro, string Descripcion);
}
