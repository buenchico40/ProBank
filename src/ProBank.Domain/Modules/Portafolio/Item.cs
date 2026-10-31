using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.Portafolio;

public enum TipoItem { Proyecto, Mejora, Bug }

public static class AvanceItem
{
    /// <summary>PRY-06: avance = tareas completadas ÷ total de tareas (0 si no hay tareas).</summary>
    public static decimal Calcular(IReadOnlyCollection<Tarea> tareas) =>
        tareas.Count == 0 ? 0m : Math.Round(100m * tareas.Count(t => t.Estado == EstadoTarea.Completada) / tareas.Count, 0);
}

public static class ReglasItem
{
    public static readonly IReadOnlyList<string> Estados = ["Borrador", "Pendiente", "En curso", "En revisión", "Completado", "Anulado"];

    /// <summary>PRY-02: cada ítem tiene uno o varios responsables.</summary>
    public static Error? Validar(string tipo, string titulo, IReadOnlyCollection<int> responsables, string estado)
    {
        if (!Enum.TryParse<TipoItem>(tipo, out _)) return Error.Validation("tipo", "El tipo debe ser Proyecto, Mejora o Bug.");
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Trim().Length > 200) return Error.Validation("titulo", "El título es obligatorio (máximo 200 caracteres).");
        if (responsables.Count == 0) return Error.Validation("responsables", "Asigna al menos un responsable.");
        if (!Estados.Contains(estado)) return Error.Validation("estado", "Estado no válido.");
        return null;
    }
}
