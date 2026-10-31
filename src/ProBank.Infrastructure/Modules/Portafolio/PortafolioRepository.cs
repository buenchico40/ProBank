using System.Data;
using System.Text.Json;
using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Portafolio;
using ProBank.Contracts.Portafolio;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Portafolio;

/// <summary>Ítems, cronograma, acuerdos, informes y parametrización de Portafolio: solo procedimientos del esquema pry.</summary>
internal sealed class PortafolioRepository(ISqlConnectionFactory db) : IPortafolioRepository
{
    private static DateTime F(DateOnly d) => d.ToDateTime(TimeOnly.MinValue);
    private static DateOnly D(DateTime d) => DateOnly.FromDateTime(d);
    private static DateOnly? D(DateTime? d) => d is null ? null : DateOnly.FromDateTime(d.Value);
    private static IReadOnlyList<int> Ids(string? csv) => string.IsNullOrEmpty(csv) ? [] : csv.Split(',').Select(int.Parse).ToList();
    private static DynamicParameters Con(object datos, Actor a)
    {
        var p = new DynamicParameters(datos);
        p.AddDynamicParams(new { ActorId = a.UsuarioId, a.RolActivo, a.Ip });
        return p;
    }

    private async Task Ejecutar(string sp, DynamicParameters p, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition(sp, p, commandType: Sp.Tipo, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ItemPortafolioDto>> ListarItems(bool soloAbiertos, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var filas = await cn.QueryAsync<ItemFila>(new CommandDefinition("pry.usp_Item_Listar", new { SoloAbiertos = soloAbiertos }, commandType: Sp.Tipo, cancellationToken: ct));
        return filas.Select(f => new ItemPortafolioDto(f.Token, f.Codigo, f.Titulo, f.Tipo, f.Estado, f.Responsables, Ids(f.ResponsableIds), f.Tareas, f.Completadas, f.Avance)).ToList();
    }

    public async Task<ItemDetalleDto?> ObtenerItem(Guid token, DateOnly hoy, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("pry.usp_Item_Obtener", new { Token = token, Hoy = F(hoy) }, commandType: Sp.Tipo, cancellationToken: ct));
        var i = await m.ReadSingleOrDefaultAsync<ItemDetalleFila>();
        var responsables = (await m.ReadAsync<ResponsableDto>()).ToList();
        var hitos = (await m.ReadAsync<HitoFila>()).Select(h => new HitoDto(h.HitoId, h.Nombre, h.Orden, D(h.FechaInicio), D(h.FechaFin), h.Tareas)).ToList();
        var acuerdos = (await m.ReadAsync<AcuerdoFila>()).Select(Acuerdo).ToList();
        return i is null ? null : new ItemDetalleDto(i.Token, i.Codigo, i.Titulo, i.Tipo, i.Estado, i.DescripcionHtml, i.FechaRegistro, i.CreadoPor, responsables, hitos, acuerdos);
    }

    public async Task<ItemGuardadoDto> GuardarItem(Guid? token, GuardarItemRequest r, Actor a, CancellationToken ct)
    {
        var p = Con(new { Token = token, r.Tipo, r.Titulo, r.DescripcionHtml, Responsables = string.Join(',', r.ResponsableIds), r.Estado }, a);
        p.Add("TokenGuardado", dbType: DbType.Guid, direction: ParameterDirection.Output);
        p.Add("Codigo", dbType: DbType.String, size: 20, direction: ParameterDirection.Output);
        await Ejecutar("pry.usp_Item_Guardar", p, ct);
        return new ItemGuardadoDto(p.Get<Guid>("TokenGuardado"), p.Get<string>("Codigo"));
    }

    public Task EliminarItem(Guid token, Actor a, CancellationToken ct) => Ejecutar("pry.usp_Item_Eliminar", Con(new { Token = token }, a), ct);

    public Task GuardarHitos(Guid itemToken, IReadOnlyList<HitoRequest> hitos, Actor a, CancellationToken ct) =>
        Ejecutar("pry.usp_Hito_Guardar", Con(new
        {
            ItemToken = itemToken,
            Hitos = JsonSerializer.Serialize(hitos.Select(h => new { hitoId = h.HitoId, nombre = h.Nombre, orden = h.Orden, inicio = h.Inicio?.ToString("yyyy-MM-dd"), fin = h.Fin?.ToString("yyyy-MM-dd") }))
        }, a), ct);

    public async Task<CronogramaDto> Cronograma(Guid? itemToken, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("pry.usp_Cronograma_Listar", new { ItemToken = itemToken }, commandType: Sp.Tipo, cancellationToken: ct));
        var items = (await m.ReadAsync<CronoItemDto>()).ToList();
        var hitos = (await m.ReadAsync<HitoCronoFila>()).Select(h => new CronoHitoDto(h.HitoId, h.ItemId, h.Nombre, h.Orden, D(h.FechaInicio), D(h.FechaFin))).ToList();
        var tareas = (await m.ReadAsync<TareaCronoFila>()).Select(t => new CronoTareaDto(t.Token, t.Codigo, t.Titulo, t.ItemId, t.Item, t.HitoId, t.Area, t.Tecnico,
            D(t.FechaInicio), D(t.FechaTerminacion), t.Avance, t.Estado)).ToList();
        return new CronogramaDto(items, hitos, tareas);
    }

    public async Task<IReadOnlyList<AcuerdoDto>> ListarAcuerdos(DateOnly hoy, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<AcuerdoFila>(new CommandDefinition("pry.usp_Acuerdo_Listar", new { Hoy = F(hoy) }, commandType: Sp.Tipo, cancellationToken: ct))).Select(Acuerdo).ToList();
    }

    public async Task<int> RegistrarAcuerdos(RegistrarAcuerdosRequest r, Actor a, CancellationToken ct)
    {
        var p = Con(new
        {
            r.ItemToken, Fecha = F(r.Fecha), r.Temas,
            Acuerdos = JsonSerializer.Serialize(r.Acuerdos.Select(x => new { descripcion = x.Descripcion, responsableId = x.ResponsableId, fecha = x.Fecha.ToString("yyyy-MM-dd") }))
        }, a);
        p.Add("Registrados", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await Ejecutar("pry.usp_Acuerdo_Registrar", p, ct);
        return p.Get<int>("Registrados");
    }

    public Task ActualizarAcuerdo(int acuerdoId, ActualizarAcuerdoRequest r, Actor a, CancellationToken ct) =>
        Ejecutar("pry.usp_Acuerdo_Actualizar", Con(new { AcuerdoId = acuerdoId, r.Descripcion, r.ResponsableId, FechaCumplimiento = F(r.FechaCumplimiento) }, a), ct);

    public Task EliminarAcuerdo(int acuerdoId, Actor a, CancellationToken ct) => Ejecutar("pry.usp_Acuerdo_Eliminar", Con(new { AcuerdoId = acuerdoId }, a), ct);

    public Task CumplirAcuerdo(int acuerdoId, DateOnly hoy, Actor a, CancellationToken ct) =>
        Ejecutar("pry.usp_Acuerdo_Cumplir", Con(new { AcuerdoId = acuerdoId, Hoy = F(hoy) }, a), ct);

    public async Task<IReadOnlyList<InformeDto>> ListarInformes(CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<InformeFila>(new CommandDefinition("pry.usp_Informe_Listar", commandType: Sp.Tipo, cancellationToken: ct)))
            .Select(f => new InformeDto(f.InformeId, f.Codigo, f.Tipo, D(f.Desde), D(f.Hasta), f.Incluye, f.Estado, f.FechaGenerado, f.GeneradoPor)).ToList();
    }

    public async Task<int> GuardarInforme(int? informeId, GuardarInformeRequest r, Actor a, CancellationToken ct)
    {
        var p = Con(new { InformeId = informeId, r.Tipo, Desde = F(r.Desde), Hasta = F(r.Hasta) }, a);
        p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await Ejecutar("pry.usp_Informe_Guardar", p, ct);
        return p.Get<int>("Id");
    }

    public async Task<InformeContenidoDto?> ObtenerInforme(int informeId, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var f = await cn.QuerySingleOrDefaultAsync<InformeContenidoFila>(new CommandDefinition("pry.usp_Informe_Obtener", new { InformeId = informeId }, commandType: Sp.Tipo, cancellationToken: ct));
        return f is null ? null : new InformeContenidoDto(f.InformeId, f.Codigo, f.Tipo, D(f.Desde), D(f.Hasta), f.Estado, f.ContenidoHtml);
    }

    public async Task<InformeDatos> DatosInforme(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("pry.usp_Informe_Datos", new { Desde = F(desde), Hasta = F(hasta) }, commandType: Sp.Tipo, cancellationToken: ct));
        var resumen = (await m.ReadAsync<ResumenInformeDto>()).ToList();
        var bitacora = (await m.ReadAsync<BitacoraFila>()).ToList();
        var acuerdos = (await m.ReadAsync<AcuerdoInformeFila>()).ToList();
        var temas = (await m.ReadAsync<TemaRevisionFila>()).ToList();
        return new InformeDatos(resumen, bitacora, acuerdos, temas);
    }

    public Task GenerarInforme(int informeId, string html, Actor a, CancellationToken ct) =>
        Ejecutar("pry.usp_Informe_Generar", Con(new { InformeId = informeId, ContenidoHtml = html }, a), ct);

    public async Task<ParametrizacionDto> Parametrizacion(CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("pry.usp_AccionRol_Listar", commandType: Sp.Tipo, cancellationToken: ct));
        var acciones = (await m.ReadAsync<AccionFila>()).Select(a => new AccionRolDto(a.Clave, a.Nombre, a.Estado,
            string.IsNullOrEmpty(a.Roles) ? [] : a.Roles.Split(','))).ToList();
        var roles = (await m.ReadAsync<RolResumenDto>()).ToList();
        return new ParametrizacionDto(acciones, roles);
    }

    public Task GuardarAccionRol(string accion, IReadOnlyList<string> roles, Actor a, CancellationToken ct) =>
        Ejecutar("pry.usp_AccionRol_Guardar", Con(new { Accion = accion, Roles = string.Join(',', roles) }, a), ct);

    private static AcuerdoDto Acuerdo(AcuerdoFila f) => new(f.AcuerdoId, f.Codigo, f.ItemToken, f.Item, f.ItemTitulo, f.RevisionId, D(f.FechaRevision), f.Temas,
        f.Descripcion, f.ResponsableId, f.Responsable, D(f.FechaCumplimiento), D(f.FechaCumplido), f.Estado);

    private sealed record ItemFila(Guid Token, string Codigo, string Titulo, string Tipo, string Estado, string? Responsables, string? ResponsableIds,
        int Tareas, int Completadas, int Avance);
    private sealed record ItemDetalleFila(Guid Token, string Codigo, string Titulo, string Tipo, string Estado, string? DescripcionHtml, DateTime FechaRegistro, string? CreadoPor);
    private sealed record HitoFila(int HitoId, string Nombre, int Orden, DateTime? FechaInicio, DateTime? FechaFin, int Tareas);
    private sealed record HitoCronoFila(int HitoId, int ItemId, string Nombre, int Orden, DateTime? FechaInicio, DateTime? FechaFin);
    private sealed record TareaCronoFila(Guid Token, string Codigo, string Titulo, int ItemId, string Item, int? HitoId, string Area, string Tecnico,
        DateTime FechaInicio, DateTime FechaTerminacion, int Avance, string Estado);
    private sealed record AcuerdoFila(int AcuerdoId, string Codigo, Guid ItemToken, string Item, string ItemTitulo, int RevisionId, DateTime FechaRevision,
        string? Temas, string Descripcion, int ResponsableId, string Responsable, DateTime FechaCumplimiento, DateTime? FechaCumplido, string Estado);
    private sealed record InformeFila(int InformeId, string Codigo, string Tipo, DateTime Desde, DateTime Hasta, string Incluye, string Estado,
        DateTime? FechaGenerado, string? GeneradoPor);
    private sealed record InformeContenidoFila(int InformeId, string Codigo, string Tipo, DateTime Desde, DateTime Hasta, string Incluye, string Estado, string? ContenidoHtml);
    private sealed record AccionFila(string Clave, string Nombre, string Estado, string? Roles);
}
