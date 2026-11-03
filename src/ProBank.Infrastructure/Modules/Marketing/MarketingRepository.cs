using System.Data;
using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Marketing;
using ProBank.Contracts.Marketing;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Marketing;

/// <summary>Imágenes promocionales y comunicados: solo procedimientos del esquema mkt.</summary>
internal sealed class MarketingRepository(ISqlConnectionFactory db) : IMarketingRepository
{
    private static DateTime F(DateOnly d) => d.ToDateTime(TimeOnly.MinValue);
    private static DateTime? F(DateOnly? d) => d?.ToDateTime(TimeOnly.MinValue);
    private static DateOnly? D(DateTime? d) => d is null ? null : DateOnly.FromDateTime(d.Value);

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

    public async Task<IReadOnlyList<string>> Audiencias(CancellationToken ct) => await Lista<string>("mkt.usp_Audiencia_Listar", null, ct);

    // ---------- Imágenes promocionales ----------
    public async Task<IReadOnlyList<PromocionDto>> ListarPromociones(DateOnly hoy, CancellationToken ct) =>
        (await Lista<PromocionFila>("mkt.usp_Promocion_Listar", new { Hoy = F(hoy) }, ct)).Select(Promocion).ToList();

    public async Task<PromocionDto?> ObtenerPromocion(Guid token, DateOnly hoy, CancellationToken ct) =>
        (await Lista<PromocionFila>("mkt.usp_Promocion_Obtener", new { Token = token, Hoy = F(hoy) }, ct)).Select(Promocion).FirstOrDefault();

    public async Task<Guid> GuardarPromocion(Guid? token, GuardarPromocionRequest r, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token, r.Titulo, r.Ubicacion, r.TextoAlternativo, r.Enlace, Desde = F(r.Desde), Hasta = F(r.Hasta), r.Audiencia, r.Orden }, a);
        p.Add("TokenGuardado", dbType: DbType.Guid, direction: ParameterDirection.Output);
        return (await Ejecutar("mkt.usp_Promocion_Guardar", p, ct)).Get<Guid>("TokenGuardado");
    }

    public async Task<string?> GuardarImagen(Guid token, string ruta, string tipo, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token, Ruta = ruta, Tipo = tipo }, a);
        p.Add("RutaAnterior", dbType: DbType.AnsiString, size: 300, direction: ParameterDirection.Output);
        return (await Ejecutar("mkt.usp_Promocion_GuardarImagen", p, ct)).Get<string?>("RutaAnterior");
    }

    public async Task<string?> EliminarPromocion(Guid token, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token }, a);
        p.Add("Ruta", dbType: DbType.AnsiString, size: 300, direction: ParameterDirection.Output);
        return (await Ejecutar("mkt.usp_Promocion_Eliminar", p, ct)).Get<string?>("Ruta");
    }

    public async Task<string> PublicarPromocion(Guid token, DateOnly hoy, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token, Hoy = F(hoy) }, a);
        p.Add("EstadoVigente", dbType: DbType.AnsiString, size: 20, direction: ParameterDirection.Output);
        return (await Ejecutar("mkt.usp_Promocion_Publicar", p, ct)).Get<string>("EstadoVigente");
    }

    public Task RetirarPromocion(Guid token, Actor a, CancellationToken ct) => Ejecutar("mkt.usp_Promocion_Retirar", Con(new { Token = token }, a), ct);

    public async Task<ImagenPromocion?> ImagenPromocion(Guid token, DateOnly hoy, CancellationToken ct) =>
        (await Lista<ImagenFila>("mkt.usp_Promocion_Imagen", new { Token = token, Hoy = F(hoy) }, ct))
            .Select(f => new ImagenPromocion(f.ImagenRuta, f.ImagenTipo, f.Ubicacion, f.Audiencia, f.Estado)).FirstOrDefault();

    public async Task<IReadOnlyList<PromocionInicioDto>> PromocionesInicioSesion(DateOnly hoy, CancellationToken ct) =>
        await Lista<PromocionInicioDto>("mkt.usp_Promocion_ListarInicioSesion", new { Hoy = F(hoy) }, ct);

    // ---------- Comunicados ----------
    public async Task<IReadOnlyList<ComunicadoDto>> ListarComunicados(DateOnly hoy, Guid? token, CancellationToken ct) =>
        (await Lista<ComunicadoFila>("mkt.usp_Comunicado_Listar", new { Hoy = F(hoy), Token = token }, ct)).Select(Comunicado).ToList();

    public async Task<Guid> GuardarComunicado(Guid? token, GuardarComunicadoRequest r, Actor a, CancellationToken ct)
    {
        var p = Con(new
        {
            Token = token, r.Titulo, r.Tipo, r.Resumen, r.ContenidoHtml, r.Contacto, Desde = F(r.Desde), Hasta = F(r.Hasta), r.Audiencia, r.Destacado
        }, a);
        p.Add("TokenGuardado", dbType: DbType.Guid, direction: ParameterDirection.Output);
        return (await Ejecutar("mkt.usp_Comunicado_Guardar", p, ct)).Get<Guid>("TokenGuardado");
    }

    public Task EliminarComunicado(Guid token, Actor a, CancellationToken ct) => Ejecutar("mkt.usp_Comunicado_Eliminar", Con(new { Token = token }, a), ct);

    public async Task<string> PublicarComunicado(Guid token, DateOnly hoy, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token, Hoy = F(hoy) }, a);
        p.Add("EstadoVigente", dbType: DbType.AnsiString, size: 20, direction: ParameterDirection.Output);
        return (await Ejecutar("mkt.usp_Comunicado_Publicar", p, ct)).Get<string>("EstadoVigente");
    }

    public Task RetirarComunicado(Guid token, Actor a, CancellationToken ct) => Ejecutar("mkt.usp_Comunicado_Retirar", Con(new { Token = token }, a), ct);

    private static PromocionDto Promocion(PromocionFila f) => new(f.Token, f.Codigo, f.Titulo, f.Ubicacion, f.TextoAlternativo, f.Enlace, D(f.Desde), D(f.Hasta),
        f.Audiencia, f.Orden, f.Estado, f.TieneImagen, f.CreadoPor, f.FechaRegistro, f.PublicadoPor, f.FechaPublicacion);

    private static ComunicadoDto Comunicado(ComunicadoFila f) => new(f.Token, f.Codigo, f.Titulo, f.Tipo, f.Resumen, f.ContenidoHtml, f.Contacto, D(f.Desde), D(f.Hasta),
        f.Audiencia, f.Destacado, f.Estado, f.CreadoPor, f.FechaRegistro, f.PublicadoPor, f.FechaPublicacion);

    private sealed record PromocionFila(Guid Token, string Codigo, string Titulo, string Ubicacion, string? TextoAlternativo, string? Enlace, DateTime? Desde,
        DateTime? Hasta, string Audiencia, int Orden, string Estado, bool TieneImagen, string CreadoPor, DateTime FechaRegistro, string? PublicadoPor, DateTime? FechaPublicacion);
    private sealed record ComunicadoFila(Guid Token, string Codigo, string Titulo, string Tipo, string Resumen, string? ContenidoHtml, string? Contacto, DateTime? Desde,
        DateTime? Hasta, string Audiencia, bool Destacado, string Estado, string CreadoPor, DateTime FechaRegistro, string? PublicadoPor, DateTime? FechaPublicacion);
    private sealed record ImagenFila(string ImagenRuta, string ImagenTipo, string Ubicacion, string Audiencia, string Estado);
}
