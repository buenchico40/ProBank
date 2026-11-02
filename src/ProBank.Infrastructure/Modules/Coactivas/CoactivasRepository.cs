using System.Data;
using System.Text.Json;
using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Coactivas;
using ProBank.Contracts.Coactivas;
using ProBank.Domain.Modules.Coactivas;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Coactivas;

/// <summary>Repositorio de Coactivas: solo procedimientos almacenados del esquema coa.</summary>
internal sealed class CoactivasRepository(ISqlConnectionFactory db) : ICoactivasRepository
{
    private async Task<List<T>> Lista<T>(string sp, object? p, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<T>(new CommandDefinition(sp, p, commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    private async Task<DynamicParameters> Ejecutar(string sp, object p, Action<DynamicParameters>? salida, CancellationToken ct)
    {
        var dp = new DynamicParameters(p);
        salida?.Invoke(dp);
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition(sp, dp, commandType: Sp.Tipo, cancellationToken: ct));
        return dp;
    }

    private static object A(Actor a) => new { ActorId = a.UsuarioId, a.RolActivo, a.Ip };
    private static DynamicParameters Con(object datos, Actor a) { var p = new DynamicParameters(datos); p.AddDynamicParams(A(a)); return p; }
    private static DateTime F(DateOnly d) => d.ToDateTime(TimeOnly.MinValue);

    public async Task<IReadOnlyList<EntidadDto>> ListarEntidades(CancellationToken ct) => await Lista<EntidadDto>("coa.usp_Entidad_Listar", null, ct);

    public async Task<int> GuardarEntidad(int? entidadId, GuardarEntidadRequest r, Actor a, CancellationToken ct) =>
        (await Ejecutar("coa.usp_Entidad_Guardar", Con(new { EntidadId = entidadId, r.Ruc, r.Nombre, r.CuentaDestino, r.Estado }, a),
            p => p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output), ct)).Get<int>("Id");

    public async Task<IReadOnlyList<OficioDto>> ListarOficios(CancellationToken ct) => await Lista<OficioDto>("coa.usp_Oficio_Listar", null, ct);

    public async Task<OficioDetalleDto?> ObtenerOficio(Guid token, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("coa.usp_Oficio_Obtener", new { Token = token }, commandType: Sp.Tipo, cancellationToken: ct));
        var f = await m.ReadSingleOrDefaultAsync<OficioFila>();
        var ordenes = (await m.ReadAsync<OrdenDto>()).ToList();
        return f is null ? null : new OficioDetalleDto(f.Token, f.Numero, f.Recibido, f.RemitidoPor, f.ContenidoHtml, f.Ordenes, f.Entidades,
            f.MontoCoactivas, f.Ejecutado, f.Estado, f.RegistradoPor, f.FechaRegistro, ordenes);
    }

    public async Task<Guid> GuardarOficio(Guid? token, GuardarOficioRequest r, DateOnly hoy, Actor a, CancellationToken ct) =>
        (await Ejecutar("coa.usp_Oficio_Guardar", Con(new { Token = token, r.Numero, Recibido = F(r.Recibido), r.RemitidoPor, r.ContenidoHtml, Hoy = F(hoy) }, a),
            p => p.Add("TokenGuardado", dbType: DbType.Guid, direction: ParameterDirection.Output), ct)).Get<Guid>("TokenGuardado");

    public async Task<int> AnularOficio(Guid token, DateOnly hoy, Actor a, CancellationToken ct) =>
        (await Ejecutar("coa.usp_Oficio_Anular", Con(new { Token = token, Hoy = F(hoy) }, a),
            p => p.Add("Liberados", dbType: DbType.Int32, direction: ParameterDirection.Output), ct)).Get<int>("Liberados");

    public async Task<string> AgregarOrden(Guid oficioToken, AgregarOrdenRequest r, string? productos, Actor a, CancellationToken ct) =>
        (await Ejecutar("coa.usp_Orden_Agregar", Con(new
        {
            OficioToken = oficioToken, r.Tipo, r.NumeroProceso, r.EntidadId, r.TipoIdentificacion, r.Identificacion, r.Cliente, r.Monto,
            Productos = productos, r.CoactivaToken, r.MontoOrdenado, r.Observacion
        }, a), p => p.Add("Proceso", dbType: DbType.String, size: 50, direction: ParameterDirection.Output), ct)).Get<string>("Proceso");

    public async Task<IReadOnlyList<CoactivaDto>> ListarCoactivas(string? identificacion, CancellationToken ct) =>
        await Lista<CoactivaDto>("coa.usp_Coactiva_Listar", new { Identificacion = identificacion }, ct);

    public async Task<CoactivaDetalleDto?> ObtenerCoactiva(Guid token, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("coa.usp_Coactiva_Obtener", new { Token = token }, commandType: Sp.Tipo, cancellationToken: ct));
        var c = await m.ReadSingleOrDefaultAsync<CoactivaFila>();
        var productos = (await m.ReadAsync<ProductoClienteDto>()).ToList();
        var bloqueos = (await m.ReadAsync<BloqueoDto>()).ToList();
        var movimientos = (await m.ReadAsync<MovimientoDto>()).ToList();
        var ordenes = (await m.ReadAsync<OrdenCoactivaDto>()).ToList();
        return c is null ? null : new CoactivaDetalleDto(c.Token, c.NumeroProceso, c.Oficio, c.OficioToken, c.EntidadId, c.Entidad, c.CuentaDestino,
            c.TipoIdentificacion, c.Identificacion, c.Cliente, c.Monto, c.Bloqueado, c.Levantado, c.Transferido, c.Cobertura, c.Estado,
            c.ProductosPermitidos, c.Observacion, productos, bloqueos, movimientos, ordenes);
    }

    public Task ActualizarCoactiva(Guid token, decimal monto, string? productos, string? observacion, string? cliente, int? entidadId, Actor a, CancellationToken ct) =>
        Ejecutar("coa.usp_Coactiva_Actualizar", Con(new { Token = token, Monto = monto, Productos = productos, Observacion = observacion, Cliente = cliente, EntidadId = entidadId }, a), null, ct);

    public async Task<int> AnularCoactiva(Guid token, DateOnly hoy, Actor a, CancellationToken ct) =>
        (await Ejecutar("coa.usp_Coactiva_Anular", Con(new { Token = token, Hoy = F(hoy) }, a),
            p => p.Add("Liberados", dbType: DbType.Int32, direction: ParameterDirection.Output), ct)).Get<int>("Liberados");

    public Task GuardarProducto(Guid coactivaToken, GuardarProductoRequest r, Actor a, CancellationToken ct) =>
        Ejecutar("coa.usp_Producto_Guardar", Con(new { CoactivaToken = coactivaToken, r.ProductoId, r.Tipo, r.Numero, r.Saldo }, a), null, ct);

    public async Task<string> RegistrarBloqueo(Guid coactivaToken, RegistrarBloqueoRequest r, DateOnly hoy, Actor a, CancellationToken ct) =>
        (await Ejecutar("coa.usp_Bloqueo_Registrar", Con(new { CoactivaToken = coactivaToken, r.ProductoId, r.Monto, r.Observacion, Hoy = F(hoy) }, a),
            p => p.Add("Codigo", dbType: DbType.String, size: 20, direction: ParameterDirection.Output), ct)).Get<string>("Codigo");

    public async Task<decimal> EjecutarOrden(EjecutarOrdenRequest r, DateOnly hoy, Actor a, CancellationToken ct) =>
        (await Ejecutar("coa.usp_Orden_Ejecutar", Con(new
        {
            r.OrdenId, Fecha = F(r.Fecha), Montos = JsonSerializer.Serialize(r.Montos.Select(m => new { bloqueoId = m.BloqueoId, monto = m.Monto })),
            r.FormaPago, r.CuentaDestino, r.Comprobante, r.Observacion, Hoy = F(hoy)
        }, a), p => p.Add("Total", dbType: DbType.Decimal, precision: 18, scale: 2, direction: ParameterDirection.Output), ct)).Get<decimal>("Total");

    public async Task<IReadOnlyList<ClienteCoactivasDto>> ListarClientes(CancellationToken ct) => await Lista<ClienteCoactivasDto>("coa.usp_Cliente_Listar", null, ct);

    public async Task<IReadOnlyList<ProductoResumenDto>> ListarProductosCliente(string identificacion, CancellationToken ct) =>
        await Lista<ProductoResumenDto>("coa.usp_Cliente_ListarProductos", new { Identificacion = identificacion }, ct);

    public async Task<(int Oficios, int Ordenes, IReadOnlyList<ErrorCargaDto> Errores)> Cargar(IReadOnlyList<FilaValida> filas, Actor a, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(filas.Select(f => new
        {
            fila = f.Fila, oficio = f.Oficio, recibido = f.Recibido.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            remitidoPor = f.RemitidoPor, tipo = f.Tipo.ToString(), proceso = f.Proceso, ruc = f.Ruc, tipoIdentificacion = f.TipoIdentificacion,
            identificacion = f.Identificacion, cliente = f.Cliente, monto = f.Monto, productos = f.Productos, montoOrdenado = f.MontoOrdenado,
            observacion = f.Observacion
        }));
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("coa.usp_Carga_Procesar", Con(new { Filas = json }, a),
            commandType: Sp.Tipo, cancellationToken: ct, commandTimeout: 300));
        var errores = (await m.ReadAsync<ErrorCargaDto>()).ToList();
        var resumen = await m.ReadSingleAsync<(int Oficios, int Ordenes)>();
        return (resumen.Oficios, resumen.Ordenes, errores);
    }

    private sealed record OficioFila(Guid Token, string Numero, DateTime Recibido, string RemitidoPor, string? ContenidoHtml, int Ordenes, int Entidades,
        decimal MontoCoactivas, decimal Ejecutado, string Estado, string RegistradoPor, DateTime FechaRegistro);
    private sealed record CoactivaFila(Guid Token, string NumeroProceso, string Oficio, Guid OficioToken, int EntidadId, string Entidad, string? CuentaDestino,
        string TipoIdentificacion, string Identificacion, string Cliente, decimal Monto, decimal Bloqueado, decimal Levantado, decimal Transferido,
        string Cobertura, string Estado, string? ProductosPermitidos, string? Observacion);
}
