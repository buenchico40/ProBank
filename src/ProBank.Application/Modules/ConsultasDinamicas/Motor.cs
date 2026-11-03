using System.Globalization;
using System.Text.Json;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Portafolio;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.ConsultasDinamicas;

namespace ProBank.Application.Modules.ConsultasDinamicas;

/// <summary>Parámetros listos para ejecutar: valores tipados, texto auditable y los valores en JSON (lo que se aprueba).</summary>
public sealed record ParametrosResueltos(IReadOnlyDictionary<string, object?> Valores, string Texto, string Json);

public static class ResolverParametros
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-EC");

    /// <summary>Aplica valores enviados, valores por defecto y variables (SQL-04); valida obligatorios, tipos y opciones de las listas.</summary>
    public static Result<ParametrosResueltos> Resolver(IReadOnlyList<ParametroConsulta> definicion, IReadOnlyList<ParametroValor> enviados, DateOnly hoy)
    {
        var valores = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var json = new Dictionary<string, string?>();
        var partes = new List<string>();
        foreach (var p in definicion)
        {
            var e = enviados.FirstOrDefault(x => string.Equals(x.Nombre, p.Nombre, StringComparison.OrdinalIgnoreCase));
            var origen = e is null ? p.ValorDefecto : NoVacio(e.Variable) ?? NoVacio(e.Valor);
            var resuelto = VariablesParametro.Resolver(NoVacio(origen), hoy);
            if (resuelto is null)
            {
                if (p.Obligatorio) return Result.Failure<ParametrosResueltos>(Error.Validation("parametro", $"{p.Etiqueta} es obligatorio."));
                valores[p.Nombre] = null; json[p.Nombre] = null;
                partes.Add($"{p.Etiqueta}=(vacío)");
                continue;
            }
            var tipado = Convertir(resuelto, p.Tipo);
            if (tipado is null)
                return Result.Failure<ParametrosResueltos>(Error.Validation("parametro", $"{p.Etiqueta}: el valor no es {(p.Tipo == TipoParametro.Fecha ? "una fecha" : "un número")} válido."));
            if (p.Tipo == TipoParametro.Lista && !p.ListaOpciones.Contains(tipado.ToString()))
                return Result.Failure<ParametrosResueltos>(Error.Validation("parametro", $"{p.Etiqueta}: elija una de sus opciones."));
            valores[p.Nombre] = tipado;
            json[p.Nombre] = Invariante(tipado);
            var mostrado = Mostrar(tipado);
            partes.Add(VariablesParametro.EsVariable(origen) ? $"{p.Etiqueta}={origen} → {mostrado}" : $"{p.Etiqueta}={mostrado}");
        }
        return Result.Success(new ParametrosResueltos(valores, string.Join("; ", partes), JsonSerializer.Serialize(json)));
    }

    /// <summary>Los valores guardados al solicitar (ya resueltos y aprobados), tipados según la definición actual.</summary>
    public static IReadOnlyDictionary<string, object?> DesdeJson(IReadOnlyList<ParametroConsulta> definicion, string json)
    {
        var guardados = JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? [];
        var valores = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in definicion)
            valores[p.Nombre] = guardados.TryGetValue(p.Nombre, out var v) && v is not null ? Convertir(v, p.Tipo) : null;
        return valores;
    }

    private static string? NoVacio(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static object? Convertir(object v, TipoParametro tipo) => (v, tipo) switch
    {
        (DateOnly d, TipoParametro.Fecha) => d,
        (int i, TipoParametro.Numero) => (decimal)i,
        (string s, TipoParametro.Fecha) when DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) => d,
        (string s, TipoParametro.Fecha) when DateOnly.TryParseExact(s, "dd/MM/yyyy", Es, DateTimeStyles.None, out var d) => d,
        (string s, TipoParametro.Numero) when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) => n,
        (string, TipoParametro.Fecha or TipoParametro.Numero) => null,
        (DateOnly or int, _) => v.ToString(),
        _ => v,
    };

    private static string Invariante(object v) => v switch
    {
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        decimal n => n.ToString(CultureInfo.InvariantCulture),
        _ => v.ToString() ?? "",
    };

    private static string Mostrar(object v) => v switch
    {
        DateOnly d => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        decimal n => n.ToString("0.##", Es),
        _ => v.ToString() ?? "",
    };

    /// <summary>Variables de parámetro con su valor de hoy, para mostrarlas en los formularios.</summary>
    public static IReadOnlyList<VariableDto> Variables(DateOnly hoy) =>
        VariablesParametro.Fechas.Select(kv => new VariableDto(kv.Key, "Fecha", kv.Value(hoy).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)))
            .Concat(VariablesParametro.Numeros.Select(kv => new VariableDto(kv.Key, "Numero", kv.Value(hoy).ToString(CultureInfo.InvariantCulture))))
            .ToList();
}

/// <summary>
/// Corre el procedimiento de una ejecución ya registrada (inmediata, aprobada o programada): guarda el resultado en el almacén
/// para verlo y exportarlo sin reejecutar, cierra la ejecución y deja el evento en la auditoría (SQL-06).
/// </summary>
public static class MotorConsultas
{
    private static string Ruta(string codigo) => $"rpt/ejecuciones/{codigo}.json";

    public static async Task<Result<ResultadoEjecucion>> Correr(IConsultaRepository repo, IConsultaExecutor executor, IAlmacenArchivos almacen,
        Consulta consulta, long ejecucionId, string codigo, string conexion, IReadOnlyDictionary<string, object?> valores, string parametrosTexto, CancellationToken ct)
    {
        try
        {
            var r = await executor.Ejecutar(conexion, consulta.Procedimiento, valores, consulta.TiempoMaximoSeg, consulta.LimiteFilas, ct);
            var ruta = Ruta(codigo);
            using (var s = new MemoryStream(Serializar(r), writable: false)) await almacen.Guardar(Adjuntos.Contenedor, ruta, s, "application/json", ct);
            await repo.CerrarEjecucion(ejecucionId, r.Filas.Count, r.DuracionMs, r.Truncado, ruta, null, "Ejecutada", ct);
            await repo.RegistrarEvento(ejecucionId, "Ejecución", $"EXEC {consulta.Procedimiento} · {parametrosTexto}" + (r.Truncado ? $" · truncado a {consulta.LimiteFilas} filas" : ""),
                "Exitosa", r.Filas.Count, r.DuracionMs, ct);
            return Result.Success(r);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            var motivo = ex is OperationCanceledException or TimeoutException || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                ? $"Tiempo máximo excedido ({consulta.TiempoMaximoSeg} s): la consulta se canceló"
                : ex.Message.Length > 450 ? ex.Message[..450] : ex.Message;
            await repo.CerrarEjecucion(ejecucionId, null, null, false, null, motivo, "Fallida", CancellationToken.None);
            await repo.RegistrarEvento(ejecucionId, "Ejecución", motivo, "Fallida", null, null, CancellationToken.None);
            return Result.Failure<ResultadoEjecucion>(new Error("ejecucion_fallida", "La consulta no se pudo ejecutar: " + motivo + ". Quedó registrado en la auditoría."));
        }
    }

    /// <summary>CFG-04: borra del almacén un resultado guardado (conservación vencida).</summary>
    public static Task Eliminar(IAlmacenArchivos almacen, string ruta, CancellationToken ct) => almacen.Eliminar(Adjuntos.Contenedor, ruta, ct);

    public static async Task<ResultadoEjecucion?> Leer(IAlmacenArchivos almacen, string ruta, CancellationToken ct)
    {
        await using var s = await almacen.Abrir(Adjuntos.Contenedor, ruta, ct);
        if (s is null) return null;
        using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);
        var raiz = doc.RootElement;
        var columnas = raiz.GetProperty("columnas").EnumerateArray().Select(x => x.GetString()!).ToList();
        var tipos = raiz.GetProperty("tipos").EnumerateArray().Select(x => x.GetString()!).ToList();
        var filas = raiz.GetProperty("filas").EnumerateArray().Select(f =>
        {
            var d = new Dictionary<string, object?>(columnas.Count);
            var i = 0;
            foreach (var v in f.EnumerateArray()) { d[columnas[i]] = Valor(v, tipos[i]); i++; }
            return (IReadOnlyDictionary<string, object?>)d;
        }).ToList();
        return new ResultadoEjecucion(columnas, tipos, filas, raiz.GetProperty("duracionMs").GetInt32(), raiz.GetProperty("truncado").GetBoolean());
    }

    private static byte[] Serializar(ResultadoEjecucion r) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        columnas = r.Columnas, tipos = r.Tipos, duracionMs = r.DuracionMs, truncado = r.Truncado,
        filas = r.Filas.Select(f => r.Columnas.Select(c => f.GetValueOrDefault(c)).ToArray()),
    });

    private static object? Valor(JsonElement v, string tipo) => v.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.True or JsonValueKind.False => v.GetBoolean(),
        JsonValueKind.Number => tipo == "entero" && v.TryGetInt64(out var l) ? l : v.GetDecimal(),
        JsonValueKind.String when tipo is "fecha" or "fechahora" && v.TryGetDateTime(out var d) => d,
        _ => v.ToString(),
    };

    /// <summary>Estado para mostrar.</summary>
    public static string Estado(string estado) => estado == "PendienteAprobacion" ? "Pendiente de aprobación" : estado;
}
