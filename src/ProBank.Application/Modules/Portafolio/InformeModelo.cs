using System.Globalization;
using ProBank.Contracts.Portafolio;

namespace ProBank.Application.Modules.Portafolio;

/// <summary>Arma el informe de seguimiento en PDF (DevExpress Reporting en Infraestructura).</summary>
public interface IGeneradorInforme
{
    byte[] Pdf(InformeModelo modelo);
}

public sealed record IndicadorInforme(string Titulo, string Valor, string Nota);
public sealed record AvanceItemSerie(string Item, int Avance, int Completadas, int Pendientes);
public sealed record ConteoSerie(string Nombre, int Cantidad);

/// <summary>
/// Lo que muestra el informe: encabezado, indicadores, series de los gráficos y el detalle. Se calcula aquí (sin
/// dependencias de DevExpress) para probarlo; el reporte solo lo dibuja.
/// </summary>
public sealed record InformeModelo(
    string Codigo, string Titulo, string Periodo, string Generado,
    IReadOnlyList<IndicadorInforme> Indicadores,
    IReadOnlyList<AvanceItemSerie> AvancePorItem,
    IReadOnlyList<ConteoSerie> AcuerdosPorEstado,
    IReadOnlyList<ConteoSerie> MovimientosPorDia,
    IReadOnlyList<ConteoSerie> EventosPorTipo,
    IReadOnlyList<ResumenInformeDto> Resumen,
    IReadOnlyList<BitacoraFila> Bitacora,
    IReadOnlyList<AcuerdoInformeFila> Acuerdos,
    IReadOnlyList<TemaRevisionFila> Temas)
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-EC");

    public static InformeModelo Armar(InformeContenidoDto i, InformeDatos d, string generadoPor, DateTime ahora)
    {
        var tareas = d.Resumen.Sum(r => r.Tareas);
        var completadas = d.Resumen.Sum(r => r.Completadas);
        var acuerdos = d.Acuerdos.Count;
        var cumplidos = d.Acuerdos.Count(a => a.Estado == "Cumplido");
        var vencidos = d.Acuerdos.Count(a => a.Estado == "Vencido");
        var avance = tareas == 0 ? 0 : (int)Math.Round(100m * completadas / tareas);

        IndicadorInforme[] indicadores =
        [
            new("Ítems con actividad", d.Resumen.Count.ToString(Es), $"{d.Resumen.Count(r => r.TareasConMovimiento > 0)} con movimiento de tareas"),
            new("Avance global", $"{avance} %", $"{completadas} de {tareas} tareas completadas"),
            new("Movimientos de tareas", d.Bitacora.Count.ToString(Es), $"{d.Bitacora.Select(b => b.Tarea).Distinct().Count()} tareas distintas"),
            new("Acuerdos del periodo", acuerdos.ToString(Es), $"{cumplidos} cumplidos · {vencidos} vencidos"),
        ];

        // Un punto por día del periodo (con ceros) para que la tendencia no salte días sin movimientos
        var porDia = d.Bitacora.GroupBy(b => DateOnly.FromDateTime(b.Fecha)).ToDictionary(g => g.Key, g => g.Count());
        var dias = Enumerable.Range(0, i.Hasta.DayNumber - i.Desde.DayNumber + 1).Select(n => i.Desde.AddDays(n))
            .Select(f => new ConteoSerie(f.ToString("ddd dd/MM", Es), porDia.GetValueOrDefault(f))).ToList();

        string[] ordenAcuerdos = ["Cumplido", "Pendiente", "Vencido"];
        return new InformeModelo(
            i.Codigo,
            $"Informe {i.Tipo.ToLowerInvariant()} de seguimiento",
            $"Del {i.Desde.ToString("dd/MM/yyyy", Es)} al {i.Hasta.ToString("dd/MM/yyyy", Es)}",
            $"Generado el {ahora.ToString("dd/MM/yyyy HH:mm", Es)} por {generadoPor}",
            indicadores,
            d.Resumen.OrderByDescending(r => r.Avance).ThenBy(r => r.Item)
                .Select(r => new AvanceItemSerie(r.Item, r.Avance, r.Completadas, r.Tareas - r.Completadas)).ToList(),
            d.Acuerdos.GroupBy(a => a.Estado).OrderBy(g => Array.IndexOf(ordenAcuerdos, g.Key) is var x && x < 0 ? 99 : x)
                .Select(g => new ConteoSerie(g.Key, g.Count())).ToList(),
            dias,
            d.Bitacora.GroupBy(b => b.Evento).OrderByDescending(g => g.Count()).Select(g => new ConteoSerie(g.Key, g.Count())).ToList(),
            d.Resumen, d.Bitacora.OrderBy(b => b.Fecha).ToList(), d.Acuerdos, d.Temas);
    }
}
