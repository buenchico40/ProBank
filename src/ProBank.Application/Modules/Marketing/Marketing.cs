using ProBank.Application.Abstractions;
using ProBank.Contracts.Marketing;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Marketing;
using ProBank.Domain.Modules.Portafolio;

namespace ProBank.Application.Modules.Marketing;

public interface IMarketingRepository
{
    Task<IReadOnlyList<string>> Audiencias(CancellationToken ct);

    Task<IReadOnlyList<PromocionDto>> ListarPromociones(DateOnly hoy, CancellationToken ct);
    Task<PromocionDto?> ObtenerPromocion(Guid token, DateOnly hoy, CancellationToken ct);
    Task<Guid> GuardarPromocion(Guid? token, GuardarPromocionRequest r, Actor actor, CancellationToken ct);
    /// <summary>Devuelve la ruta de la imagen anterior (para borrarla), si tenía.</summary>
    Task<string?> GuardarImagen(Guid token, string ruta, string tipo, Actor actor, CancellationToken ct);
    /// <summary>Devuelve la ruta de su imagen (para borrarla), si tenía.</summary>
    Task<string?> EliminarPromocion(Guid token, Actor actor, CancellationToken ct);
    Task<string> PublicarPromocion(Guid token, DateOnly hoy, Actor actor, CancellationToken ct);
    Task RetirarPromocion(Guid token, Actor actor, CancellationToken ct);
    Task<ImagenPromocion?> ImagenPromocion(Guid token, DateOnly hoy, CancellationToken ct);
    Task<IReadOnlyList<PromocionInicioDto>> PromocionesInicioSesion(DateOnly hoy, CancellationToken ct);

    Task<IReadOnlyList<ComunicadoDto>> ListarComunicados(DateOnly hoy, Guid? token, CancellationToken ct);
    Task<Guid> GuardarComunicado(Guid? token, GuardarComunicadoRequest r, Actor actor, CancellationToken ct);
    Task EliminarComunicado(Guid token, Actor actor, CancellationToken ct);
    Task<string> PublicarComunicado(Guid token, DateOnly hoy, Actor actor, CancellationToken ct);
    Task RetirarComunicado(Guid token, Actor actor, CancellationToken ct);
}

/// <summary>Imagen guardada de una promoción y lo necesario para decidir quién la ve.</summary>
public sealed record ImagenPromocion(string Ruta, string Tipo, string Ubicacion, string Audiencia, string Estado);

/// <summary>Archivo de imagen listo para enviar.</summary>
public sealed record ArchivoImagen(string Tipo, Stream Contenido);

internal static class Mkt
{
    /// <summary>Contenedor de Blob Storage de Marketing (infra/modules/storage).</summary>
    public const string Contenedor = "marketing";

    /// <summary>Mensaje al publicar: programado (aparece desde su fecha) o ya visible.</summary>
    public static string Publicado(string codigo, string estado, string donde) =>
        estado == "Programado" ? $"{codigo} programado: aparecerá en {donde} desde el inicio de su vigencia." : $"{codigo} publicado: ya aparece en {donde}.";
}

// ---------- Consultas ----------
public sealed record AudienciasQuery : IQuery<IReadOnlyList<string>>;
public sealed record ListarPromocionesQuery : IQuery<IReadOnlyList<PromocionDto>>;
public sealed record ObtenerPromocionQuery(Guid Token) : IQuery<PromocionDto>;
/// <summary>Imagen para el usuario conectado: quien administra promociones ve todas; los demás, solo las vigentes para su rol.</summary>
public sealed record ImagenPromocionQuery(Guid Token) : IQuery<ArchivoImagen>;
/// <summary>Sin sesión: imágenes vigentes de la pantalla de inicio de sesión dirigidas a todos.</summary>
public sealed record PromocionesInicioQuery : IQuery<IReadOnlyList<PromocionInicioDto>>;
public sealed record ImagenInicioQuery(Guid Token) : IQuery<ArchivoImagen>;
public sealed record ListarComunicadosQuery : IQuery<IReadOnlyList<ComunicadoDto>>;
public sealed record ObtenerComunicadoQuery(Guid Token) : IQuery<ComunicadoDto>;

// ---------- Comandos ----------
public sealed record GuardarPromocionCommand(Guid? Token, GuardarPromocionRequest Request) : ICommand<MarketingGuardadoDto>;
public sealed record SubirImagenCommand(Guid Token, string Nombre, byte[] Contenido) : ICommand<string>;
public sealed record EliminarPromocionCommand(Guid Token) : ICommand<string>;
public sealed record PublicarPromocionCommand(Guid Token) : ICommand<string>;
public sealed record RetirarPromocionCommand(Guid Token) : ICommand<string>;
public sealed record GuardarComunicadoCommand(Guid? Token, GuardarComunicadoRequest Request) : ICommand<MarketingGuardadoDto>;
public sealed record EliminarComunicadoCommand(Guid Token) : ICommand<string>;
public sealed record PublicarComunicadoCommand(Guid Token) : ICommand<string>;
public sealed record RetirarComunicadoCommand(Guid Token) : ICommand<string>;

internal sealed class MarketingConsultas(IMarketingRepository repo, IPermisos permisos, ICurrentUser user, IClock clock, IAlmacenArchivos almacen) :
    IQueryHandler<AudienciasQuery, IReadOnlyList<string>>,
    IQueryHandler<ListarPromocionesQuery, IReadOnlyList<PromocionDto>>,
    IQueryHandler<ObtenerPromocionQuery, PromocionDto>,
    IQueryHandler<ImagenPromocionQuery, ArchivoImagen>,
    IQueryHandler<ListarComunicadosQuery, IReadOnlyList<ComunicadoDto>>,
    IQueryHandler<ObtenerComunicadoQuery, ComunicadoDto>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(AudienciasQuery q, CancellationToken ct) =>
        await permisos.Tiene("promos", "ver", ct) || await permisos.Tiene("comunicados", "ver", ct)
            ? Result.Success(await repo.Audiencias(ct)) : Result.Failure<IReadOnlyList<string>>(Acceso.SinPermiso);

    public async Task<Result<IReadOnlyList<PromocionDto>>> Handle(ListarPromocionesQuery q, CancellationToken ct) =>
        await permisos.Exigir("promos", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<PromocionDto>>(e) : Result.Success(await repo.ListarPromociones(clock.Hoy, ct));

    public async Task<Result<PromocionDto>> Handle(ObtenerPromocionQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("promos", "ver", ct) is { } e) return Result.Failure<PromocionDto>(e);
        return await repo.ObtenerPromocion(q.Token, clock.Hoy, ct) is { } p ? Result.Success(p) : Result.Failure<PromocionDto>(Error.NotFound("La imagen promocional"));
    }

    public async Task<Result<ArchivoImagen>> Handle(ImagenPromocionQuery q, CancellationToken ct)
    {
        if (await repo.ImagenPromocion(q.Token, clock.Hoy, ct) is not { } i) return Result.Failure<ArchivoImagen>(Error.NotFound("La imagen"));
        // Igual que el Panel (mkt.fn_VisiblePara): lo vigente para «Todos» o para el rol activo; Administrador y Marketing ven todo
        var visible = i.Estado == "Publicado" && (i.Audiencia == "Todos" || i.Audiencia == user.RolActivo || user.RolActivo is "Administrador" or "Marketing");
        if (!visible && !await permisos.Tiene("promos", "ver", ct)) return Result.Failure<ArchivoImagen>(Acceso.SinPermiso);
        return await almacen.Abrir(Mkt.Contenedor, i.Ruta, ct) is { } s
            ? Result.Success(new ArchivoImagen(i.Tipo, s)) : Result.Failure<ArchivoImagen>(Error.NotFound("La imagen"));
    }

    public async Task<Result<IReadOnlyList<ComunicadoDto>>> Handle(ListarComunicadosQuery q, CancellationToken ct) =>
        await permisos.Exigir("comunicados", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<ComunicadoDto>>(e) : Result.Success(await repo.ListarComunicados(clock.Hoy, null, ct));

    public async Task<Result<ComunicadoDto>> Handle(ObtenerComunicadoQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("comunicados", "ver", ct) is { } e) return Result.Failure<ComunicadoDto>(e);
        return (await repo.ListarComunicados(clock.Hoy, q.Token, ct)).FirstOrDefault() is { } c ? Result.Success(c) : Result.Failure<ComunicadoDto>(Error.NotFound("El comunicado"));
    }
}

/// <summary>Pantalla de inicio de sesión (sin usuario): solo lo publicado, vigente, con ubicación «Inicio de sesión» y para todos.</summary>
internal sealed class InicioSesionConsultas(IMarketingRepository repo, IClock clock, IAlmacenArchivos almacen) :
    IQueryHandler<PromocionesInicioQuery, IReadOnlyList<PromocionInicioDto>>,
    IQueryHandler<ImagenInicioQuery, ArchivoImagen>
{
    public async Task<Result<IReadOnlyList<PromocionInicioDto>>> Handle(PromocionesInicioQuery q, CancellationToken ct) =>
        Result.Success(await repo.PromocionesInicioSesion(clock.Hoy, ct));

    public async Task<Result<ArchivoImagen>> Handle(ImagenInicioQuery q, CancellationToken ct)
    {
        if (await repo.ImagenPromocion(q.Token, clock.Hoy, ct) is not { Estado: "Publicado", Ubicacion: "Inicio de sesión", Audiencia: "Todos" } i)
            return Result.Failure<ArchivoImagen>(Error.NotFound("La imagen"));
        return await almacen.Abrir(Mkt.Contenedor, i.Ruta, ct) is { } s
            ? Result.Success(new ArchivoImagen(i.Tipo, s)) : Result.Failure<ArchivoImagen>(Error.NotFound("La imagen"));
    }
}

internal sealed class PromocionHandlers(IMarketingRepository repo, IPermisos permisos, ICurrentUser user, IClock clock, IAlmacenArchivos almacen) :
    ICommandHandler<GuardarPromocionCommand, MarketingGuardadoDto>, ICommandHandler<SubirImagenCommand, string>,
    ICommandHandler<EliminarPromocionCommand, string>, ICommandHandler<PublicarPromocionCommand, string>, ICommandHandler<RetirarPromocionCommand, string>
{
    public async Task<Result<MarketingGuardadoDto>> Handle(GuardarPromocionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("promos", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<MarketingGuardadoDto>(e);
        var r = c.Request with { Titulo = c.Request.Titulo?.Trim() ?? "", TextoAlternativo = Nulo(c.Request.TextoAlternativo), Enlace = Nulo(c.Request.Enlace) };
        if (ReglasMarketing.ValidarPromocion(r.Titulo, r.Ubicacion, r.Desde, r.Hasta, r.Orden, r.Audiencia, r.TextoAlternativo, r.Enlace) is { } v)
            return Result.Failure<MarketingGuardadoDto>(v);
        return Result.Success(new MarketingGuardadoDto(await repo.GuardarPromocion(c.Token, r, Actor.De(user), ct)));
    }

    /// <summary>JPG o PNG verificado por su contenido; reemplaza la imagen anterior (que se borra del almacén).</summary>
    public async Task<Result<string>> Handle(SubirImagenCommand c, CancellationToken ct)
    {
        if (!(await permisos.Tiene("promos", "editar", ct) || await permisos.Tiene("promos", "crear", ct))) return Result.Failure<string>(Acceso.SinPermiso);
        var cabecera = c.Contenido.AsSpan(0, Math.Min(ReglasAdjunto.BytesCabecera, c.Contenido.Length));
        var (nombre, tipo, error) = ReglasAdjunto.Validar(c.Nombre, c.Contenido.LongLength, cabecera, ReglasMarketing.ExtensionesImagen, ReglasMarketing.MaximoMBImagen);
        if (error is not null) return Result.Failure<string>(error);
        var ruta = $"promociones/{c.Token:N}/{Guid.NewGuid():N}{Path.GetExtension(nombre).ToLowerInvariant()}";
        using (var s = new MemoryStream(c.Contenido, writable: false)) await almacen.Guardar(Mkt.Contenedor, ruta, s, tipo, ct);
        string? anterior;
        try { anterior = await repo.GuardarImagen(c.Token, ruta, tipo, Actor.De(user), ct); }
        catch { await almacen.Eliminar(Mkt.Contenedor, ruta, CancellationToken.None); throw; }
        if (anterior is not null) await almacen.Eliminar(Mkt.Contenedor, anterior, CancellationToken.None);
        return Result.Success($"Imagen {nombre} cargada");
    }

    public async Task<Result<string>> Handle(EliminarPromocionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("promos", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        if (await repo.EliminarPromocion(c.Token, Actor.De(user), ct) is { } ruta) await almacen.Eliminar(Mkt.Contenedor, ruta, CancellationToken.None);
        return Result.Success("Imagen promocional eliminada");
    }

    public async Task<Result<string>> Handle(PublicarPromocionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("promos", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        if (await repo.ObtenerPromocion(c.Token, clock.Hoy, ct) is not { } p) return Result.Failure<string>(Error.NotFound("La imagen promocional"));
        var estado = await repo.PublicarPromocion(c.Token, clock.Hoy, Actor.De(user), ct);
        return Result.Success(Mkt.Publicado(p.Codigo, estado, p.Ubicacion == "Inicio de sesión" ? "el inicio de sesión" : "el Panel"));
    }

    public async Task<Result<string>> Handle(RetirarPromocionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("promos", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        await repo.RetirarPromocion(c.Token, Actor.De(user), ct);
        return Result.Success("Retirada: deja de mostrarse");
    }

    private static string? Nulo(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

internal sealed class ComunicadoHandlers(IMarketingRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) :
    ICommandHandler<GuardarComunicadoCommand, MarketingGuardadoDto>, ICommandHandler<EliminarComunicadoCommand, string>,
    ICommandHandler<PublicarComunicadoCommand, string>, ICommandHandler<RetirarComunicadoCommand, string>
{
    public async Task<Result<MarketingGuardadoDto>> Handle(GuardarComunicadoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("comunicados", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<MarketingGuardadoDto>(e);
        var r = c.Request with
        {
            Titulo = c.Request.Titulo?.Trim() ?? "", Resumen = c.Request.Resumen?.Trim() ?? "",
            Contacto = string.IsNullOrWhiteSpace(c.Request.Contacto) ? null : c.Request.Contacto.Trim(),
            ContenidoHtml = string.IsNullOrWhiteSpace(c.Request.ContenidoHtml) ? null : c.Request.ContenidoHtml,
        };
        if (ReglasMarketing.ValidarComunicado(r.Titulo, r.Tipo, r.Resumen, r.Contacto, r.Desde, r.Hasta, r.Audiencia) is { } v)
            return Result.Failure<MarketingGuardadoDto>(v);
        return Result.Success(new MarketingGuardadoDto(await repo.GuardarComunicado(c.Token, r, Actor.De(user), ct)));
    }

    public async Task<Result<string>> Handle(EliminarComunicadoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("comunicados", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        await repo.EliminarComunicado(c.Token, Actor.De(user), ct);
        return Result.Success("Comunicado eliminado");
    }

    public async Task<Result<string>> Handle(PublicarComunicadoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("comunicados", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        if ((await repo.ListarComunicados(clock.Hoy, c.Token, ct)).FirstOrDefault() is not { } m) return Result.Failure<string>(Error.NotFound("El comunicado"));
        var estado = await repo.PublicarComunicado(c.Token, clock.Hoy, Actor.De(user), ct);
        return Result.Success(Mkt.Publicado(m.Codigo, estado, "el Panel"));
    }

    public async Task<Result<string>> Handle(RetirarComunicadoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("comunicados", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        await repo.RetirarComunicado(c.Token, Actor.De(user), ct);
        return Result.Success("Retirado: deja de mostrarse en el Panel");
    }
}
