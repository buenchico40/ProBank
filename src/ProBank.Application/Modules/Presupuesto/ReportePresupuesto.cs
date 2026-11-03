using System.Globalization;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Portafolio;
using ProBank.Contracts.Presupuesto;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Presupuesto;

namespace ProBank.Application.Modules.Presupuesto;

/// <summary>Arma el reporte de presupuesto en PDF (DevExpress Reporting en Infraestructura).</summary>
public interface IGeneradorReportePresupuesto
{
    byte[] Pdf(ReportePresupuestoModelo modelo);
}

// Datos que devuelve com.usp_Presupuesto_Reporte
public sealed record ServicioReporte(string Codigo, string Servicio, string Proveedor, DateOnly Vence, decimal MontoEstimado, bool Presupuestado,
    string? Partida, string? PartidaRubro, string Estado, int DiasAviso, decimal Pagado, string? Solicitud);
public sealed record PagoReporte(string Codigo, DateOnly Fecha, string Proveedor, string Solicitud, string Descripcion, string? Servicio,
    bool Presupuestado, string? Partidas, decimal MontoPago, decimal Monto, string Estado);
public sealed record ReportePresupuestoDatos(string Area, string AreaNombre, IReadOnlyList<PartidaDto> Partidas,
    IReadOnlyList<ServicioReporte> Servicios, IReadOnlyList<PagoReporte> Pagos);

public sealed record IndicadorReporte(string Titulo, string Valor, string Nota);
/// <summary>Una barra por partida: lo pagado, lo comprometido y lo que queda disponible suman lo presupuestado.</summary>
public sealed record PartidaSerie(string Partida, decimal Pagado, decimal Comprometido, decimal Disponible);
public sealed record MesSerie(string Mes, decimal Pagado, decimal Programado);
/// <summary>Servicio con su estado calculado (COM-03) y lo que falta pagar de lo estimado.</summary>
public sealed record ServicioDelReporte(ServicioReporte Servicio, string Estado, decimal Diferencia);
/// <summary>Pago con su estado para el reporte: Pagado, Programado (fecha futura) o Pendiente.</summary>
public sealed record PagoDelReporte(PagoReporte Pago, string Estado);

/// <summary>
/// Lo que muestra el reporte de presupuesto de un área y un año: indicadores, gráficos y el detalle de partidas, servicios y
/// pagos con sus totales. Se calcula aquí (sin DevExpress) para probarlo; el reporte solo lo dibuja.
/// </summary>
public sealed record ReportePresupuestoModelo(
    string Codigo, string Titulo, string Subtitulo, string Generado,
    IReadOnlyList<IndicadorReporte> Indicadores,
    IReadOnlyList<PartidaSerie> PorPartida,
    IReadOnlyList<MesSerie> PorMes,
    IReadOnlyList<PartidaDto> Partidas,
    IReadOnlyList<ServicioDelReporte> Servicios,
    IReadOnlyList<PagoDelReporte> Pagos)
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-EC");

    public static string Dinero(decimal v) => (v < 0 ? "-$ " : "$ ") + Math.Abs(v).ToString("N2", Es);

    public static ReportePresupuestoModelo Armar(int anio, ReportePresupuestoDatos d, string generadoPor, DateTime ahora)
    {
        var hoy = DateOnly.FromDateTime(ahora);
        var asignado = d.Partidas.Sum(p => p.Asignado);
        var ajustes = d.Partidas.Sum(p => p.Ajustes);
        var presupuestado = asignado + ajustes;
        var comprometido = d.Partidas.Sum(p => p.Comprometido);
        var pagado = d.Partidas.Sum(p => p.Pagado);
        var disponible = d.Partidas.Sum(p => p.Disponible);
        var sinPartida = d.Pagos.Where(p => !p.Presupuestado && p.Estado == "Pagado").ToList();

        var servicios = d.Servicios.Select(s => new ServicioDelReporte(s, ReglasPresupuesto.EstadoRenovacion(s.Estado, s.Vence, s.DiasAviso, hoy), s.MontoEstimado - s.Pagado)).ToList();
        var pagos = d.Pagos.Select(p => new PagoDelReporte(p, p.Estado == "Pendiente" && p.Fecha > hoy ? "Programado" : p.Estado)).ToList();

        IndicadorReporte[] indicadores =
        [
            new("Presupuestado", Dinero(presupuestado), ajustes == 0 ? "Asignado, sin ajustes" : $"Incluye ajustes por {Dinero(ajustes)}"),
            new("Comprometido", Dinero(comprometido), "Solicitudes aprobadas por pagar"),
            new("Pagado", Dinero(pagado), $"Ejecución {ReglasPresupuesto.Ejecucion(asignado, ajustes, comprometido, pagado)} %"),
            new("Disponible", Dinero(disponible), $"{d.Partidas.Count(p => p.Estado == "Activo")} de {d.Partidas.Count} partidas activas"),
            new("No presupuestado", Dinero(sinPartida.Sum(p => p.Monto)), $"{sinPartida.Count} pago(s) sin partida"),
        ];

        var meses = Enumerable.Range(1, 12).Select(m => new MesSerie(
            Es.DateTimeFormat.GetAbbreviatedMonthName(m).TrimEnd('.'),
            pagos.Where(p => p.Pago.Fecha.Month == m && p.Estado == "Pagado").Sum(p => p.Pago.Monto),
            pagos.Where(p => p.Pago.Fecha.Month == m && p.Estado != "Pagado").Sum(p => p.Pago.Monto))).ToList();

        return new ReportePresupuestoModelo(
            $"PRE-{d.Area}-{anio}",
            $"Presupuesto {d.AreaNombre} {anio}",
            "Servicios, pagos y presupuestado por partida",
            $"Generado el {ahora.ToString("dd/MM/yyyy HH:mm", Es)} por {generadoPor}",
            indicadores,
            d.Partidas.Select(p => new PartidaSerie($"{p.Codigo} · {p.Rubro}", p.Pagado, p.Comprometido, Math.Max(0, p.Disponible))).ToList(),
            meses,
            d.Partidas.OrderBy(p => p.Codigo).ToList(),
            servicios,
            pagos);
    }
}

/// <summary>Reporte de presupuesto de un área y un año en PDF (se genera al pedirlo; no se guarda).</summary>
public sealed record ReportePresupuestoQuery(string Area, int Anio) : IQuery<ArchivoAdjunto>;

internal sealed class ReportePresupuestoHandler(IPresupuestoRepository repo, IPermisos permisos, ICurrentUser user, IClock clock,
    IGeneradorReportePresupuesto generador) : IQueryHandler<ReportePresupuestoQuery, ArchivoAdjunto>
{
    public async Task<Result<ArchivoAdjunto>> Handle(ReportePresupuestoQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("presupuesto", "exportar", ct) is { } e) return Result.Failure<ArchivoAdjunto>(e);
        if (string.IsNullOrWhiteSpace(q.Area)) return Result.Failure<ArchivoAdjunto>(Error.Validation("area", "Elija el área del presupuesto."));
        if (q.Anio is < 2000 or > 2100) return Result.Failure<ArchivoAdjunto>(Error.Validation("anio", "El año no es válido."));
        var area = q.Area.Trim().ToUpperInvariant();
        var datos = await repo.DatosReporte(area, q.Anio, Actor.De(user), ct);
        var pdf = generador.Pdf(ReportePresupuestoModelo.Armar(q.Anio, datos, user.Nombre, clock.Ahora));
        return Result.Success(new ArchivoAdjunto($"Presupuesto-{area}-{q.Anio}.pdf", "application/pdf", new MemoryStream(pdf, writable: false)));
    }
}
