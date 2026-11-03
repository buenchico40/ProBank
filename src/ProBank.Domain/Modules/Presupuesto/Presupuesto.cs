using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.Presupuesto;

/// <summary>
/// Reglas de Presupuesto por área (COM-01 a COM-04) que no dependen de la base. La base valida permisos, estados, partidas activas
/// y que ninguna operación deje una partida con disponible negativo (com.usp_Partida_ExigirDisponible).
/// </summary>
public static class ReglasPresupuesto
{
    public static readonly IReadOnlyList<string> TiposMovimiento = ["Reasignación", "Incremento", "Reducción"];
    /// <summary>Áreas solicitantes de una compra.</summary>
    public static readonly IReadOnlyList<string> Areas = ["Gerencia de TI", "Aplicaciones", "Base de datos", "Core", "Soporte de aplicaciones", "Redes", "Servidores", "Helpdesk"];
    public static readonly IReadOnlyList<int> DiasAviso = [15, 30, 45, 60, 90, 120];

    /// <summary>Documentos de compras: tipo → extensiones permitidas (se verifica el contenido) y hasta 10 MB.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Documentos = new Dictionary<string, IReadOnlyList<string>>
    {
        ["Contrato"] = [".pdf", ".doc", ".docx"],
        ["Factura"] = [".pdf", ".xml"],
        ["Distribución"] = [".xlsx", ".xls", ".csv", ".pdf"],
    };
    public const int MaximoMBDocumento = 10;

    /// <summary>
    /// COM-03 (igual que com.usp_Renovacion_Listar): Retirado y Requerimiento enviado se muestran tal cual; lo activo es Vencido,
    /// Por vencer (dentro de los días de aviso) o Vigente.
    /// </summary>
    public static string EstadoRenovacion(string estado, DateOnly vence, int diasAviso, DateOnly hoy) =>
        estado != "Activo" ? estado : vence < hoy ? "Vencido" : vence.DayNumber - hoy.DayNumber <= diasAviso ? "Por vencer" : "Vigente";

    /// <summary>Ejecución de una partida (igual que com.vw_Partida): (pagado + comprometido) ÷ (asignado + ajustes), en %.</summary>
    public static int Ejecucion(decimal asignado, decimal ajustes, decimal comprometido, decimal pagado) =>
        asignado + ajustes > 0 ? (int)Math.Round(100m * (pagado + comprometido) / (asignado + ajustes), MidpointRounding.AwayFromZero) : 0;

    public static Error? ValidarPartida(int anio, string? area, string? rubro, decimal asignado)
    {
        if (anio is < 2000 or > 2100) return Error.Validation("anio", "El año no es válido.");
        // Que el área exista y esté activa lo valida la base (com.Area)
        if (string.IsNullOrWhiteSpace(area)) return Error.Validation("area", "Elija el área del presupuesto.");
        if (string.IsNullOrWhiteSpace(rubro) || rubro.Trim().Length > 150) return Error.Validation("rubro", "El rubro es obligatorio (máximo 150 caracteres).");
        return Monto(asignado, "El asignado", permitirCero: true);
    }

    /// <summary>Área del presupuesto (com.Area): código de 2 a 30 letras mayúsculas o números (TI, MKT) y nombre de hasta 100 caracteres.</summary>
    public static Error? ValidarArea(string? codigo, string? nombre)
    {
        if (codigo is null || codigo.Length is < 2 or > 30 || !codigo.All(c => c is (>= 'A' and <= 'Z') or (>= '0' and <= '9')))
            return Error.Validation("codigo", "El código tiene de 2 a 30 letras o números, sin espacios ni tildes (por ejemplo TI o MKT).");
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100) return Error.Validation("nombre", "El nombre es obligatorio (máximo 100 caracteres).");
        return null;
    }

    /// <summary>Reasignación: de una partida a otra distinta; Incremento: solo destino; Reducción: solo origen.</summary>
    public static Error? ValidarMovimiento(string? tipo, Guid? origen, Guid? destino, decimal monto, string? motivo)
    {
        if (tipo is null || !TiposMovimiento.Contains(tipo)) return Error.Validation("tipo", "El tipo es Reasignación, Incremento o Reducción.");
        var partidas = tipo switch
        {
            "Reasignación" when origen is null || destino is null || origen == destino => "Indique una partida origen y una destino distintas.",
            "Incremento" when destino is null || origen is not null => "Un incremento solo tiene partida destino.",
            "Reducción" when origen is null || destino is not null => "Una reducción solo tiene partida origen.",
            _ => null,
        };
        if (partidas is not null) return Error.Validation("partidas", partidas);
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 500) return Error.Validation("motivo", "El motivo es obligatorio (máximo 500 caracteres).");
        return Monto(monto, "El monto");
    }

    public static Error? ValidarRenovacion(string? servicio, string? proveedor, decimal montoEstimado, bool presupuestado, Guid? partida, int diasAviso)
    {
        if (string.IsNullOrWhiteSpace(servicio) || servicio.Trim().Length > 150) return Error.Validation("servicio", "El servicio es obligatorio (máximo 150 caracteres).");
        if (string.IsNullOrWhiteSpace(proveedor) || proveedor.Trim().Length > 150) return Error.Validation("proveedor", "El proveedor es obligatorio (máximo 150 caracteres).");
        if (diasAviso is < 1 or > 365) return Error.Validation("aviso", "Los días de aviso van de 1 a 365.");
        return Imputacion(presupuestado, partida) ?? Monto(montoEstimado, "El monto estimado", permitirCero: true);
    }

    public static Error? ValidarSolicitud(string? descripcion, string? area, string? proveedor, bool presupuestado, Guid? partida, decimal monto)
    {
        if (string.IsNullOrWhiteSpace(descripcion) || descripcion.Trim().Length > 300) return Error.Validation("descripcion", "La descripción es obligatoria (máximo 300 caracteres).");
        if (area is null || !Areas.Contains(area)) return Error.Validation("area", "Elija el área solicitante.");
        if (proveedor?.Trim().Length > 150) return Error.Validation("proveedor", "El proveedor admite hasta 150 caracteres.");
        return Imputacion(presupuestado, partida) ?? Monto(monto, "El monto");
    }

    /// <summary>COM-04: la distribución no repite partidas, cada monto es positivo y no supera el pago; una solicitud no presupuestada no se distribuye.</summary>
    public static Error? ValidarPago(string? proveedor, decimal monto, bool presupuestado, IReadOnlyCollection<(Guid Partida, decimal Monto)> distribucion)
    {
        if (string.IsNullOrWhiteSpace(proveedor) || proveedor.Trim().Length > 150) return Error.Validation("proveedor", "El proveedor es obligatorio (máximo 150 caracteres).");
        if (Monto(monto, "El monto") is { } m) return m;
        if (!presupuestado && distribucion.Count > 0) return Error.Validation("distribucion", "Una solicitud no presupuestada no descuenta de partidas: el pago va sin distribución.");
        if (distribucion.Select(d => d.Partida).Distinct().Count() != distribucion.Count) return Error.Validation("distribucion", "Una partida aparece dos veces en la distribución.");
        if (distribucion.Any(d => d.Monto <= 0 || decimal.Round(d.Monto, 2) != d.Monto)) return Error.Validation("distribucion", "Cada monto de la distribución debe ser mayor que 0, con hasta 2 decimales.");
        if (distribucion.Sum(d => d.Monto) > monto) return Error.Validation("distribucion", "La distribución supera el monto del pago.");
        return null;
    }

    /// <summary>Al marcarlo Pagado, la distribución de un pago presupuestado debe cuadrar con su monto.</summary>
    public static bool DistribucionCuadra(decimal monto, IEnumerable<decimal> distribucion) => distribucion.Sum() == monto;

    // COM-02: presupuestado con partida, o no presupuestado sin partida
    private static Error? Imputacion(bool presupuestado, Guid? partida) =>
        presupuestado && partida is null ? Error.Validation("partida", "Elija la partida o márquela como no presupuestada.")
        : !presupuestado && partida is not null ? Error.Validation("partida", "Lo no presupuestado no lleva partida.")
        : null;

    private static Error? Monto(decimal monto, string nombre, bool permitirCero = false) =>
        (permitirCero ? monto < 0 : monto <= 0) ? Error.Validation("monto", $"{nombre} debe ser {(permitirCero ? "0 o mayor" : "mayor que 0")}.")
        : decimal.Round(monto, 2) != monto ? Error.Validation("monto", $"{nombre} admite hasta 2 decimales.")
        : monto > 999_999_999m ? Error.Validation("monto", $"{nombre} es demasiado alto.")
        : null;
}
