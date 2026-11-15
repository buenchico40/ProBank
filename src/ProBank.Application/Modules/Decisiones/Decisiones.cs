using System.Security.Cryptography;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Portafolio;
using ProBank.Contracts.Decisiones;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Decisiones;
using ProBank.Domain.Modules.Portafolio;

namespace ProBank.Application.Modules.Decisiones;

public interface IDecisionesRepository
{
    Task<IReadOnlyList<TipoDecisionDto>> ListarTipos(bool soloActivos, CancellationToken ct);
    Task<string> GuardarTipo(string? codigo, GuardarTipoDecisionRequest r, Actor actor, CancellationToken ct);

    /// <param name="alcance">«todas», «pendientes» (esperan la firma del usuario), «notificadas» (le notificaron) o «firmadas» (las firmó).</param>
    Task<IReadOnlyList<DecisionDto>> Listar(string alcance, Guid? token, Actor actor, CancellationToken ct);
    /// <summary>La base valida que el usuario pueda verla y deja la consulta (o la exportación, <paramref name="evento"/>) en la trazabilidad.</summary>
    Task<DecisionDetalleDto?> Detalle(Guid token, string evento, Actor actor, CancellationToken ct);
    Task<Guid> Guardar(Guid? token, GuardarDecisionRequest r, Actor actor, CancellationToken ct);
    Task Enviar(Guid token, Actor actor, CancellationToken ct);
    /// <param name="rol">«Aprobador» o «Notificado».</param>
    Task CambiarParticipante(Guid token, string rol, bool agregar, int usuarioId, int? orden, Actor actor, CancellationToken ct);
    /// <summary>Devuelve el estado de la decisión después de la firma.</summary>
    Task<string> Firmar(Guid token, bool aprobar, string? comentario, Actor actor, CancellationToken ct);
    /// <summary>Devuelve la versión nueva.</summary>
    Task<int> Modificar(Guid token, ModificarDecisionRequest r, Actor actor, CancellationToken ct);
    Task Anular(Guid token, string motivo, Actor actor, CancellationToken ct);

    // DEC-08: archivos de respaldo (en borrador, solo quien la registró; la base lo valida)
    Task RegistrarAdjunto(Guid token, Guid decisionToken, string nombre, string tipo, long tamano, byte[] hash, string ruta, int maximo, Actor actor, CancellationToken ct);
    /// <summary>Devuelve la ruta del archivo para borrarlo del almacén.</summary>
    Task<string> EliminarAdjunto(Guid token, Actor actor, CancellationToken ct);
    /// <summary>Solo para quien puede ver la decisión; la descarga queda en la trazabilidad.</summary>
    Task<(string Nombre, string Tipo, string Ruta)?> ObtenerAdjunto(Guid token, Actor actor, CancellationToken ct);
}

/// <summary>DEC-04: PDF de la decisión con su detalle, las firmas y la trazabilidad, firmado con el certificado del banco.</summary>
public interface IGeneradorDecisionPdf
{
    Task<byte[]> Pdf(DecisionDetalleDto decision, string generadoPor, DateTime ahora, CancellationToken ct);
}

// ---------- Consultas ----------
public sealed record ListarTiposDecisionQuery(bool SoloActivos) : IQuery<IReadOnlyList<TipoDecisionDto>>;
/// <param name="Alcance">«todas», «pendientes», «notificadas» o «firmadas» (las que el usuario aprobó o rechazó).</param>
public sealed record ListarDecisionesQuery(string Alcance) : IQuery<IReadOnlyList<DecisionDto>>;
public sealed record DetalleDecisionQuery(Guid Token) : IQuery<DecisionDetalleDto>;
public sealed record PdfDecisionQuery(Guid Token) : IQuery<ArchivoAdjunto>;

// ---------- Comandos ----------
public sealed record GuardarTipoDecisionCommand(string? Codigo, GuardarTipoDecisionRequest Request) : ICommand<string>;
public sealed record GuardarDecisionCommand(Guid? Token, GuardarDecisionRequest Request) : ICommand<DecisionGuardadaDto>;
public sealed record EnviarDecisionCommand(Guid Token) : ICommand<string>;
/// <param name="Rol">«Aprobador» o «Notificado».</param>
public sealed record CambiarParticipanteCommand(Guid Token, string Rol, bool Agregar, ParticipanteRequest Request) : ICommand<string>;
public sealed record FirmarDecisionCommand(Guid Token, FirmarDecisionRequest Request) : ICommand<string>;
public sealed record ModificarDecisionCommand(Guid Token, ModificarDecisionRequest Request) : ICommand<string>;
public sealed record AnularDecisionCommand(Guid Token, AnularDecisionRequest Request) : ICommand<string>;

internal sealed class DecisionesConsultas(IDecisionesRepository repo, IPermisos permisos, ICurrentUser user, IClock clock, IGeneradorDecisionPdf generador,
    IAuditLog audit) :
    IQueryHandler<ListarTiposDecisionQuery, IReadOnlyList<TipoDecisionDto>>, IQueryHandler<ListarDecisionesQuery, IReadOnlyList<DecisionDto>>,
    IQueryHandler<DetalleDecisionQuery, DecisionDetalleDto>, IQueryHandler<PdfDecisionQuery, ArchivoAdjunto>
{
    /// <summary>La administración de tipos pide su permiso; para registrar una decisión basta poder registrarla (solo los activos).</summary>
    public async Task<Result<IReadOnlyList<TipoDecisionDto>>> Handle(ListarTiposDecisionQuery q, CancellationToken ct)
    {
        var permitido = q.SoloActivos
            ? await permisos.Tiene("tiposdec", "ver", ct) || await permisos.Tiene("decisiones", "crear", ct) || await permisos.Tiene("decisiones", "editar", ct)
            : await permisos.Tiene("tiposdec", "ver", ct);
        return permitido ? Result.Success(await repo.ListarTipos(q.SoloActivos, ct)) : Result.Failure<IReadOnlyList<TipoDecisionDto>>(Acceso.SinPermiso);
    }

    public async Task<Result<IReadOnlyList<DecisionDto>>> Handle(ListarDecisionesQuery q, CancellationToken ct)
    {
        var alcance = q.Alcance is "pendientes" or "notificadas" or "firmadas" ? q.Alcance : "todas";
        var permitido = alcance == "todas" ? await permisos.Tiene("decisiones", "ver", ct) || await permisos.Tiene("decisiones", "crear", ct)
            : await permisos.Tiene("pendientes", "ver", ct);
        return permitido ? Result.Success(await repo.Listar(alcance, null, Actor.De(user), ct)) : Result.Failure<IReadOnlyList<DecisionDto>>(Acceso.SinPermiso);
    }

    public async Task<Result<DecisionDetalleDto>> Handle(DetalleDecisionQuery q, CancellationToken ct) =>
        await repo.Detalle(q.Token, "Consulta", Actor.De(user), ct) is { } d ? Result.Success(d) : Result.Failure<DecisionDetalleDto>(Error.NotFound("La decisión"));

    /// <summary>
    /// DEC-04: lo exporta quien la registró, sus aprobadores o quien tiene el permiso de exportar (los notificados solo la ven);
    /// cada exportación queda en la trazabilidad y en la auditoría.
    /// </summary>
    public async Task<Result<ArchivoAdjunto>> Handle(PdfDecisionQuery q, CancellationToken ct)
    {
        if ((await repo.Listar("todas", q.Token, Actor.De(user), ct)).FirstOrDefault() is not { } x) return Result.Failure<ArchivoAdjunto>(Error.NotFound("La decisión"));
        if (!x.SoyCreador && !x.SoyAprobador && !await permisos.Tiene("decisiones", "exportar", ct))
            return Result.Failure<ArchivoAdjunto>(Acceso.SinPermiso);
        if (await repo.Detalle(q.Token, "Exportación PDF", Actor.De(user), ct) is not { } d) return Result.Failure<ArchivoAdjunto>(Error.NotFound("La decisión"));
        var pdf = await generador.Pdf(d, user.Nombre, clock.Ahora, ct);
        await audit.Registrar("Decisiones", "Exportación de la decisión " + d.Decision.Codigo, $"PDF firmado · versión {d.Decision.Version} · {d.Decision.Estado} · huella {d.Huella ?? "—"}", ct);
        return Result.Success(new ArchivoAdjunto($"{d.Decision.Codigo}.pdf", "application/pdf", new MemoryStream(pdf, writable: false)));
    }
}

internal sealed class DecisionesHandlers(IDecisionesRepository repo, IPermisos permisos, ICurrentUser user, ICredencialesRepository credenciales,
    IAuditLog audit, IHtmlSeguro html) :
    ICommandHandler<GuardarTipoDecisionCommand, string>, ICommandHandler<GuardarDecisionCommand, DecisionGuardadaDto>,
    ICommandHandler<EnviarDecisionCommand, string>, ICommandHandler<CambiarParticipanteCommand, string>,
    ICommandHandler<FirmarDecisionCommand, string>, ICommandHandler<ModificarDecisionCommand, string>, ICommandHandler<AnularDecisionCommand, string>
{
    public async Task<Result<string>> Handle(GuardarTipoDecisionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("tiposdec", c.Codigo is null ? "crear" : "editar", ct) is { } e) return Result.Failure<string>(e);
        var x = c.Request;
        var r = x with { Nombre = x.Nombre?.Trim() ?? "", Descripcion = string.IsNullOrWhiteSpace(x.Descripcion) ? null : x.Descripcion.Trim(), Aprobadores = x.Aprobadores ?? [] };
        if (ReglasDecisiones.ValidarTipo(r.Nombre, r.Descripcion, r.Aprobadores.Select(a => (a.UsuarioId, a.Orden)).ToList()) is { } v)
            return Result.Failure<string>(v);
        var codigo = await repo.GuardarTipo(c.Codigo, r, Actor.De(user), ct);
        return Result.Success($"Tipo de decisión {codigo} guardado");
    }

    public async Task<Result<DecisionGuardadaDto>> Handle(GuardarDecisionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("decisiones", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<DecisionGuardadaDto>(e);
        var x = c.Request;
        var (descripcion, texto) = html.Limpiar(x.Descripcion);
        var r = x with { Titulo = x.Titulo?.Trim() ?? "", Descripcion = descripcion, Area = x.Area?.Trim() ?? "", Notificados = (x.Notificados ?? []).Distinct().ToList() };
        if (ReglasDecisiones.ValidarDecision(r.Tipo, r.Titulo, texto, r.Area, descripcion.Length) is { } v) return Result.Failure<DecisionGuardadaDto>(v);
        return Result.Success(new DecisionGuardadaDto(await repo.Guardar(c.Token, r, Actor.De(user), ct)));
    }

    public async Task<Result<string>> Handle(EnviarDecisionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("decisiones", "editar", ct) is { } e) return Result.Failure<string>(e);
        await repo.Enviar(c.Token, Actor.De(user), ct);
        return Result.Success("Enviada a aprobación: su contenido queda sellado y cada aprobador la firma");
    }

    /// <summary>DEC-03 y DEC-05: la base aplica las reglas (los aprobadores no se quitan si ya hubo firmas; los notificados, si ya se envió).</summary>
    public async Task<Result<string>> Handle(CambiarParticipanteCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("decisiones", "editar", ct) is { } e) return Result.Failure<string>(e);
        if (c.Rol is not ("Aprobador" or "Notificado")) return Result.Failure<string>(Error.Validation("rol", "Indique si es aprobador o notificado."));
        await repo.CambiarParticipante(c.Token, c.Rol, c.Agregar, c.Request.UsuarioId, c.Request.Orden, Actor.De(user), ct);
        return Result.Success($"{c.Rol} {(c.Agregar ? "agregado" : "quitado")}");
    }

    /// <summary>DEC-03: la clave del usuario confirma que es él quien firma; un intento con la clave incorrecta queda en la auditoría.</summary>
    public async Task<Result<string>> Handle(FirmarDecisionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("pendientes", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        var r = c.Request;
        if (ReglasDecisiones.ValidarFirma(r.Aprobar, r.Clave, r.Comentario) is { } v) return Result.Failure<string>(v);
        if (await ClaveIncorrecta(r.Clave, "Firma", c.Token, ct) is { } error) return Result.Failure<string>(error);
        var estado = await repo.Firmar(c.Token, r.Aprobar, r.Comentario?.Trim(), Actor.De(user), ct);
        return Result.Success((r.Aprobar ? "Aprobación firmada" : "Rechazo firmado") + estado switch
        {
            "Aprobada" => ": la decisión quedó aprobada por todos",
            "Rechazada" => ": la decisión quedó rechazada",
            _ => ": queda pendiente de los demás aprobadores",
        });
    }

    /// <summary>DEC-07: el aprobador corrige el contenido con su clave; nace una versión nueva y todos vuelven a firmar.</summary>
    public async Task<Result<string>> Handle(ModificarDecisionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("pendientes", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        var x = c.Request;
        var (descripcion, texto) = html.Limpiar(x.Descripcion);
        var r = x with { Titulo = x.Titulo?.Trim() ?? "", Descripcion = descripcion, Area = x.Area?.Trim() ?? "", Motivo = x.Motivo?.Trim() ?? "" };
        if (ReglasDecisiones.ValidarModificacion(r.Titulo, texto, r.Area, r.Motivo, r.Clave, descripcion.Length) is { } v) return Result.Failure<string>(v);
        if (await ClaveIncorrecta(r.Clave, "Modificación", c.Token, ct) is { } error) return Result.Failure<string>(error);
        var version = await repo.Modificar(c.Token, r, Actor.De(user), ct);
        return Result.Success($"Versión {version} guardada: todos los aprobadores vuelven a firmar");
    }

    public async Task<Result<string>> Handle(AnularDecisionCommand c, CancellationToken ct)
    {
        if (ReglasDecisiones.ValidarAnulacion(c.Request.Motivo) is { } v) return Result.Failure<string>(v);
        await repo.Anular(c.Token, c.Request.Motivo.Trim(), Actor.De(user), ct);
        return Result.Success("Decisión anulada");
    }

    private async Task<Error?> ClaveIncorrecta(string clave, string accion, Guid token, CancellationToken ct)
    {
        var cuenta = await credenciales.Obtener(user.Usuario, ct);
        if (cuenta?.ClaveHash is not null && cuenta.ClaveSal is not null && Claves.Verificar(clave, cuenta.ClaveHash, cuenta.ClaveSal)) return null;
        await audit.Registrar("Decisiones", $"{accion} no realizada: clave incorrecta", $"Decisión {token}", ct);
        return Error.Validation("clave", $"La clave no es correcta: la {accion.ToLowerInvariant()} no se registró.");
    }
}

// ---------- DEC-08: archivos de respaldo ----------
public sealed record SubirArchivoDecisionCommand(Guid DecisionToken, string Nombre, byte[] Contenido) : ICommand<string>;
public sealed record EliminarArchivoDecisionCommand(Guid Token) : ICommand<string>;
public sealed record DescargarArchivoDecisionQuery(Guid Token) : IQuery<ArchivoAdjunto>;

/// <summary>
/// Se verifican tipo, tamaño y contenido, se calcula su huella SHA-256 (identifica el archivo; no forma parte de la huella de la
/// decisión) y se guarda
/// en el almacén; si el registro falla, el archivo se borra. La base exige que la decisión esté en borrador y sea de quien la registró.
/// </summary>
internal sealed class ArchivosDecision(IDecisionesRepository repo, IPermisos permisos, ICurrentUser user, IAlmacenArchivos almacen) :
    ICommandHandler<SubirArchivoDecisionCommand, string>, ICommandHandler<EliminarArchivoDecisionCommand, string>,
    IQueryHandler<DescargarArchivoDecisionQuery, ArchivoAdjunto>
{
    private const string Contenedor = "adjuntos";

    public async Task<Result<string>> Handle(SubirArchivoDecisionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("decisiones", "editar", ct) is { } e) return Result.Failure<string>(e);
        var cabecera = c.Contenido.AsSpan(0, Math.Min(ReglasAdjunto.BytesCabecera, c.Contenido.Length));
        var (nombre, tipo, error) = ReglasAdjunto.Validar(c.Nombre, c.Contenido.LongLength, cabecera, ReglasDecisiones.ExtensionesArchivo, ReglasDecisiones.MaximoMBArchivo);
        if (error is not null) return Result.Failure<string>(error);
        var token = Guid.NewGuid();
        var ruta = $"dec/{c.DecisionToken:N}/{token:N}{Path.GetExtension(nombre).ToLowerInvariant()}";
        using (var s = new MemoryStream(c.Contenido, writable: false)) await almacen.Guardar(Contenedor, ruta, s, tipo, ct);
        try
        {
            await repo.RegistrarAdjunto(token, c.DecisionToken, nombre, tipo, c.Contenido.LongLength, SHA256.HashData(c.Contenido), ruta,
                ReglasDecisiones.MaximoArchivos, Actor.De(user), ct);
        }
        catch { await almacen.Eliminar(Contenedor, ruta, CancellationToken.None); throw; }
        return Result.Success($"Archivo {nombre} agregado");
    }

    public async Task<Result<string>> Handle(EliminarArchivoDecisionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("decisiones", "editar", ct) is { } e) return Result.Failure<string>(e);
        var ruta = await repo.EliminarAdjunto(c.Token, Actor.De(user), ct);
        await almacen.Eliminar(Contenedor, ruta, CancellationToken.None);
        return Result.Success("Archivo quitado");
    }

    public async Task<Result<ArchivoAdjunto>> Handle(DescargarArchivoDecisionQuery q, CancellationToken ct)
    {
        if (await repo.ObtenerAdjunto(q.Token, Actor.De(user), ct) is not { } a) return Result.Failure<ArchivoAdjunto>(Error.NotFound("El archivo"));
        return await almacen.Abrir(Contenedor, a.Ruta, ct) is { } s
            ? Result.Success(new ArchivoAdjunto(a.Nombre, a.Tipo, s)) : Result.Failure<ArchivoAdjunto>(Error.NotFound("El archivo"));
    }
}
