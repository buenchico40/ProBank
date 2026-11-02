using ProBank.Application.Abstractions;
using ProBank.Contracts.Coactivas;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Coactivas;

namespace ProBank.Application.Modules.Coactivas;

public interface ICoactivasRepository
{
    Task<IReadOnlyList<EntidadDto>> ListarEntidades(CancellationToken ct);
    Task<int> GuardarEntidad(int? entidadId, GuardarEntidadRequest r, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<OficioDto>> ListarOficios(CancellationToken ct);
    Task<OficioDetalleDto?> ObtenerOficio(Guid token, CancellationToken ct);
    Task<Guid> GuardarOficio(Guid? token, GuardarOficioRequest r, DateOnly hoy, Actor actor, CancellationToken ct);
    Task<int> AnularOficio(Guid token, DateOnly hoy, Actor actor, CancellationToken ct);
    Task<string> AgregarOrden(Guid oficioToken, AgregarOrdenRequest r, string? productos, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<CoactivaDto>> ListarCoactivas(string? identificacion, CancellationToken ct);
    Task<CoactivaDetalleDto?> ObtenerCoactiva(Guid token, CancellationToken ct);
    Task ActualizarCoactiva(Guid token, decimal monto, string? productos, string? observacion, string? cliente, int? entidadId, Actor actor, CancellationToken ct);
    Task<int> AnularCoactiva(Guid token, DateOnly hoy, Actor actor, CancellationToken ct);
    Task GuardarProducto(Guid coactivaToken, GuardarProductoRequest r, Actor actor, CancellationToken ct);
    Task<string> RegistrarBloqueo(Guid coactivaToken, RegistrarBloqueoRequest r, DateOnly hoy, Actor actor, CancellationToken ct);
    Task<decimal> EjecutarOrden(EjecutarOrdenRequest r, DateOnly hoy, Actor actor, CancellationToken ct);

    Task<IReadOnlyList<ClienteCoactivasDto>> ListarClientes(CancellationToken ct);
    Task<IReadOnlyList<ProductoResumenDto>> ListarProductosCliente(string identificacion, CancellationToken ct);

    Task<(int Oficios, int Ordenes, IReadOnlyList<ErrorCargaDto> Errores)> Cargar(IReadOnlyList<FilaValida> filas, Actor actor, CancellationToken ct);
}

// ---------- Consultas ----------
public sealed record ListarEntidadesQuery : IQuery<IReadOnlyList<EntidadDto>>;
public sealed record ListarOficiosQuery : IQuery<IReadOnlyList<OficioDto>>;
public sealed record ObtenerOficioQuery(Guid Token) : IQuery<OficioDetalleDto>;
public sealed record ListarCoactivasQuery(string? Identificacion) : IQuery<IReadOnlyList<CoactivaDto>>;
public sealed record ObtenerCoactivaQuery(Guid Token) : IQuery<CoactivaDetalleDto>;
public sealed record ListarClientesQuery : IQuery<IReadOnlyList<ClienteCoactivasDto>>;
public sealed record ListarProductosClienteQuery(string Identificacion) : IQuery<IReadOnlyList<ProductoResumenDto>>;

// ---------- Comandos ----------
public sealed record GuardarEntidadCommand(int? EntidadId, GuardarEntidadRequest Request) : ICommand<int>;
public sealed record GuardarOficioCommand(Guid? Token, GuardarOficioRequest Request) : ICommand<Guid>;
public sealed record AnularOficioCommand(Guid Token) : ICommand<string>;
public sealed record AgregarOrdenCommand(Guid OficioToken, AgregarOrdenRequest Request) : ICommand<string>;
public sealed record ActualizarCoactivaCommand(Guid Token, ActualizarCoactivaRequest Request) : ICommand<string>;
public sealed record AnularCoactivaCommand(Guid Token) : ICommand<string>;
public sealed record GuardarProductoCommand(Guid CoactivaToken, GuardarProductoRequest Request) : ICommand<string>;
public sealed record RegistrarBloqueoCommand(Guid CoactivaToken, RegistrarBloqueoRequest Request) : ICommand<string>;
public sealed record EjecutarOrdenCommand(EjecutarOrdenRequest Request) : ICommand<decimal>;
public sealed record CargaMasivaCommand(CargaMasivaRequest Request) : ICommand<CargaMasivaResultadoDto>;

internal sealed class CoactivasConsultas(ICoactivasRepository repo, IPermisos permisos) :
    IQueryHandler<ListarEntidadesQuery, IReadOnlyList<EntidadDto>>,
    IQueryHandler<ListarOficiosQuery, IReadOnlyList<OficioDto>>,
    IQueryHandler<ObtenerOficioQuery, OficioDetalleDto>,
    IQueryHandler<ListarCoactivasQuery, IReadOnlyList<CoactivaDto>>,
    IQueryHandler<ObtenerCoactivaQuery, CoactivaDetalleDto>,
    IQueryHandler<ListarClientesQuery, IReadOnlyList<ClienteCoactivasDto>>,
    IQueryHandler<ListarProductosClienteQuery, IReadOnlyList<ProductoResumenDto>>
{
    // Las entidades se usan al registrar coactivas y en la carga masiva
    public async Task<Result<IReadOnlyList<EntidadDto>>> Handle(ListarEntidadesQuery q, CancellationToken ct) =>
        await permisos.Tiene("oficios", "ver", ct) || await permisos.Tiene("coactivas", "ver", ct)
            ? Result.Success(await repo.ListarEntidades(ct))
            : Result.Failure<IReadOnlyList<EntidadDto>>(Acceso.SinPermiso);

    public async Task<Result<IReadOnlyList<OficioDto>>> Handle(ListarOficiosQuery q, CancellationToken ct) =>
        await permisos.Exigir("oficios", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<OficioDto>>(e) : Result.Success(await repo.ListarOficios(ct));

    public async Task<Result<OficioDetalleDto>> Handle(ObtenerOficioQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("oficios", "ver", ct) is { } e) return Result.Failure<OficioDetalleDto>(e);
        return await repo.ObtenerOficio(q.Token, ct) is { } o ? Result.Success(o) : Result.Failure<OficioDetalleDto>(Error.NotFound("El oficio"));
    }

    public async Task<Result<IReadOnlyList<CoactivaDto>>> Handle(ListarCoactivasQuery q, CancellationToken ct) =>
        await permisos.Tiene("coactivas", "ver", ct) || (q.Identificacion is not null && await permisos.Tiene("clientes", "ver", ct))
            ? Result.Success(await repo.ListarCoactivas(q.Identificacion, ct))
            : Result.Failure<IReadOnlyList<CoactivaDto>>(Acceso.SinPermiso);

    public async Task<Result<CoactivaDetalleDto>> Handle(ObtenerCoactivaQuery q, CancellationToken ct)
    {
        if (!(await permisos.Tiene("coactivas", "ver", ct) || await permisos.Tiene("oficios", "ver", ct)))
            return Result.Failure<CoactivaDetalleDto>(Acceso.SinPermiso);
        return await repo.ObtenerCoactiva(q.Token, ct) is { } c ? Result.Success(c) : Result.Failure<CoactivaDetalleDto>(Error.NotFound("La coactiva"));
    }

    public async Task<Result<IReadOnlyList<ClienteCoactivasDto>>> Handle(ListarClientesQuery q, CancellationToken ct) =>
        await permisos.Exigir("clientes", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<ClienteCoactivasDto>>(e) : Result.Success(await repo.ListarClientes(ct));

    public async Task<Result<IReadOnlyList<ProductoResumenDto>>> Handle(ListarProductosClienteQuery q, CancellationToken ct) =>
        await permisos.Exigir("clientes", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<ProductoResumenDto>>(e) : Result.Success(await repo.ListarProductosCliente(q.Identificacion, ct));
}

internal sealed class GuardarEntidadHandler(ICoactivasRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<GuardarEntidadCommand, int>
{
    public async Task<Result<int>> Handle(GuardarEntidadCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("oficios", "editar", ct) is { } e) return Result.Failure<int>(e);
        var r = c.Request with { Ruc = c.Request.Ruc.Trim(), Nombre = c.Request.Nombre.Trim(), CuentaDestino = c.Request.CuentaDestino?.Trim() };
        if (ReglasCoactiva.ValidarRuc(r.Ruc) is { } er) return Result.Failure<int>(Error.Validation("ruc", er));
        if (r.Nombre.Length is 0 or > 150) return Result.Failure<int>(Error.Validation("nombre", "El nombre de la entidad es obligatorio (máximo 150 caracteres)."));
        if (r.Estado is not ("Activo" or "Inactivo")) return Result.Failure<int>(Error.Validation("estado", "El estado debe ser Activo o Inactivo."));
        return Result.Success(await repo.GuardarEntidad(c.EntidadId, r, Actor.De(user), ct));
    }
}

internal sealed class GuardarOficioHandler(ICoactivasRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) : ICommandHandler<GuardarOficioCommand, Guid>
{
    public async Task<Result<Guid>> Handle(GuardarOficioCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("oficios", c.Token is null ? "crear" : "editar", ct) is { } e) return Result.Failure<Guid>(e);
        var r = c.Request with { Numero = c.Request.Numero.Trim(), RemitidoPor = c.Request.RemitidoPor.Trim() };
        if (r.Numero.Length is 0 or > 30) return Result.Failure<Guid>(Error.Validation("numero", "El N.º de oficio es obligatorio (máximo 30 caracteres)."));
        if (r.RemitidoPor.Length is 0 or > 150) return Result.Failure<Guid>(Error.Validation("remitido", "Indica quién remite el oficio (máximo 150 caracteres)."));
        if (r.Recibido > clock.Hoy) return Result.Failure<Guid>(Error.Validation("recibido", "La fecha de recepción no puede ser posterior a hoy."));
        return Result.Success(await repo.GuardarOficio(c.Token, r, clock.Hoy, Actor.De(user), ct));
    }
}

internal sealed class AnularOficioHandler(ICoactivasRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) : ICommandHandler<AnularOficioCommand, string>
{
    public async Task<Result<string>> Handle(AnularOficioCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("oficios", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        var liberados = await repo.AnularOficio(c.Token, clock.Hoy, Actor.De(user), ct);
        return Result.Success(liberados > 0 ? $"{liberados} bloqueo(s) liberado(s)" : "Sin bloqueos vigentes");
    }
}

/// <summary>COA-01, COA-02 y COA-03: agrega al oficio una coactiva nueva (bloqueo) o una orden sobre una coactiva bloqueada.</summary>
internal sealed class AgregarOrdenHandler(ICoactivasRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<AgregarOrdenCommand, string>
{
    public async Task<Result<string>> Handle(AgregarOrdenCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("oficios", "editar", ct) is { } e) return Result.Failure<string>(e);
        var r = c.Request;
        if (!Enum.TryParse<TipoOrden>(r.Tipo, out var tipo)) return Result.Failure<string>(Error.Validation("tipo", "La orden debe ser Bloqueo, Levantamiento o Transferencia."));
        string? productos = null;
        if (tipo == TipoOrden.Bloqueo)
        {
            var errores = new[]
            {
                string.IsNullOrWhiteSpace(r.NumeroProceso) || r.NumeroProceso.Trim().Length > 50 ? "El N.º de proceso es obligatorio (máximo 50 caracteres)." : null,
                r.EntidadId is null ? "Selecciona la entidad." : null,
                ReglasCoactiva.ValidarIdentificacion(r.TipoIdentificacion ?? "", r.Identificacion?.Trim() ?? ""),
                string.IsNullOrWhiteSpace(r.Cliente) || r.Cliente.Trim().Length > 150 ? "El cliente o empresa es obligatorio (máximo 150 caracteres)." : null,
                ReglasCoactiva.ValidarMonto(r.Monto, "El monto de la coactiva"),
            }.Where(x => x is not null).ToList();
            var (p, ep) = ReglasCoactiva.NormalizarProductos(r.Productos is null ? null : string.Join(",", r.Productos));
            if (ep is not null) errores.Add(ep);
            if (errores.Count > 0) return Result.Failure<string>(Error.Validation("coactiva", errores[0]!));
            productos = p;
            r = r with { NumeroProceso = r.NumeroProceso!.Trim(), Identificacion = r.Identificacion!.Trim(), Cliente = r.Cliente!.Trim() };
        }
        else
        {
            if (r.CoactivaToken is null) return Result.Failure<string>(Error.Validation("coactiva", "Selecciona la coactiva a la que se refiere la orden."));
            if (r.MontoOrdenado is not null && ReglasCoactiva.ValidarMonto(r.MontoOrdenado, "El monto ordenado") is { } em)
                return Result.Failure<string>(Error.Validation("monto", em));
        }
        return Result.Success(await repo.AgregarOrden(c.OficioToken, r with { Tipo = tipo.ToString() }, productos, Actor.De(user), ct));
    }
}

internal sealed class ActualizarCoactivaHandler(ICoactivasRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<ActualizarCoactivaCommand, string>
{
    public async Task<Result<string>> Handle(ActualizarCoactivaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("coactivas", "editar", ct) is { } e) return Result.Failure<string>(e);
        if (ReglasCoactiva.ValidarMonto(c.Request.Monto, "El monto de la coactiva") is { } em) return Result.Failure<string>(Error.Validation("monto", em));
        var (p, ep) = ReglasCoactiva.NormalizarProductos(c.Request.Productos is null ? null : string.Join(",", c.Request.Productos));
        if (ep is not null) return Result.Failure<string>(Error.Validation("productos", ep));
        var cliente = c.Request.Cliente?.Trim();
        if (cliente is not null && cliente.Length is 0 or > 150) return Result.Failure<string>(Error.Validation("cliente", "El cliente o empresa es obligatorio (máximo 150 caracteres)."));
        await repo.ActualizarCoactiva(c.Token, c.Request.Monto, p, c.Request.Observacion?.Trim(), cliente, c.Request.EntidadId, Actor.De(user), ct);
        return Result.Success("Coactiva actualizada");
    }
}

internal sealed class AnularCoactivaHandler(ICoactivasRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) : ICommandHandler<AnularCoactivaCommand, string>
{
    public async Task<Result<string>> Handle(AnularCoactivaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("coactivas", "eliminar", ct) is { } e) return Result.Failure<string>(e);
        var liberados = await repo.AnularCoactiva(c.Token, clock.Hoy, Actor.De(user), ct);
        return Result.Success(liberados > 0 ? $"{liberados} bloqueo(s) liberado(s)" : "Sin bloqueos vigentes");
    }
}

internal sealed class GuardarProductoHandler(ICoactivasRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<GuardarProductoCommand, string>
{
    public async Task<Result<string>> Handle(GuardarProductoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("coactivas", "editar", ct) is { } e) return Result.Failure<string>(e);
        var r = c.Request with { Numero = c.Request.Numero.Trim() };
        if (!ReglasCoactiva.TiposProducto.Contains(r.Tipo)) return Result.Failure<string>(Error.Validation("tipo", "Tipo de producto no válido."));
        if (r.Numero.Length is 0 or > 30) return Result.Failure<string>(Error.Validation("numero", "El número de cuenta es obligatorio (máximo 30 caracteres)."));
        if (r.Saldo < 0 || decimal.Round(r.Saldo, 2) != r.Saldo) return Result.Failure<string>(Error.Validation("saldo", "El saldo debe ser 0 o mayor, con hasta 2 decimales."));
        await repo.GuardarProducto(c.CoactivaToken, r, Actor.De(user), ct);
        return Result.Success("Producto guardado");
    }
}

internal sealed class RegistrarBloqueoHandler(ICoactivasRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) : ICommandHandler<RegistrarBloqueoCommand, string>
{
    public async Task<Result<string>> Handle(RegistrarBloqueoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("coactivas", "bloquear", ct) is { } e) return Result.Failure<string>(e);
        if (ReglasCoactiva.ValidarMonto(c.Request.Monto, "El monto a bloquear") is { } em) return Result.Failure<string>(Error.Validation("monto", em));
        return Result.Success(await repo.RegistrarBloqueo(c.CoactivaToken, c.Request, clock.Hoy, Actor.De(user), ct));
    }
}

/// <summary>COA-01: levantamiento o transferencia sobre los bloqueos vigentes, con el oficio que lo ordena.</summary>
internal sealed class EjecutarOrdenHandler(ICoactivasRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) : ICommandHandler<EjecutarOrdenCommand, decimal>
{
    public async Task<Result<decimal>> Handle(EjecutarOrdenCommand c, CancellationToken ct)
    {
        // El permiso exacto (levantar o transferir) depende del tipo de la orden y lo valida el SP.
        if (!(await permisos.Tiene("coactivas", "levantar", ct) || await permisos.Tiene("coactivas", "transferir", ct)))
            return Result.Failure<decimal>(Acceso.SinPermiso);
        var r = c.Request;
        if (r.Fecha > clock.Hoy) return Result.Failure<decimal>(Error.Validation("fecha", "La fecha no puede ser posterior a hoy."));
        if (r.Montos.Any(m => m.Monto < 0 || decimal.Round(m.Monto, 2) != m.Monto) || r.Montos.Sum(m => m.Monto) <= 0)
            return Result.Failure<decimal>(Error.Validation("montos", "Indica montos positivos (hasta 2 decimales) en al menos una cuenta."));
        return Result.Success(await repo.EjecutarOrden(r, clock.Hoy, Actor.De(user), ct));
    }
}

/// <summary>COA-04: valida el formato de cada fila; la base valida catálogos, procesos existentes y saldos, y carga los oficios sin errores.</summary>
internal sealed class CargaMasivaHandler(ICoactivasRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) : ICommandHandler<CargaMasivaCommand, CargaMasivaResultadoDto>
{
    public const int MaximoFilas = 5000;

    public async Task<Result<CargaMasivaResultadoDto>> Handle(CargaMasivaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("oficios", "carga", ct) is { } e) return Result.Failure<CargaMasivaResultadoDto>(e);
        if (c.Request.Filas.Count == 0) return Result.Failure<CargaMasivaResultadoDto>(Error.Validation("filas", "El archivo no tiene filas para cargar (desde la fila 6 de la hoja Coactivas)."));
        if (c.Request.Filas.Count > MaximoFilas) return Result.Failure<CargaMasivaResultadoDto>(Error.Validation("filas", $"El archivo supera las {MaximoFilas} filas."));

        static string S(string? s) => s ?? "";
        var filas = c.Request.Filas.Select(f => new FilaCarga(f.Fila, S(f.Oficio), S(f.Recibido), S(f.RemitidoPor), S(f.Orden), S(f.Proceso), S(f.Ruc),
            S(f.TipoIdentificacion), S(f.Identificacion), S(f.Cliente), S(f.Monto), S(f.Productos), S(f.MontoOrdenado), S(f.Observacion))).ToList();
        var (validas, errores) = CargaMasiva.Validar(filas, clock.Hoy);

        var (oficios, ordenes, erroresBase) = validas.Count > 0 ? await repo.Cargar(validas, Actor.De(user), ct) : (0, 0, []);
        var todos = errores.Select(x => new ErrorCargaDto(x.Fila, x.Oficio, x.Motivo)).Concat(erroresBase).OrderBy(x => x.Fila).ToList();
        return Result.Success(new CargaMasivaResultadoDto(oficios, ordenes, todos));
    }
}
