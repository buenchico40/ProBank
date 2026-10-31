using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.Portafolio;

public enum EstadoTarea { Pendiente, EnRevision, EnCurso, Completada }

public sealed class Tarea
{
    private Tarea() { }

    public int TareaId { get; private set; }
    public Guid Token { get; private set; }
    public int ItemId { get; private set; }
    public int? HitoId { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Area { get; private set; } = string.Empty;
    public int TecnicoId { get; private set; }
    public int Orden { get; private set; }
    public DateOnly FechaInicio { get; private set; }
    public DateOnly FechaTerminacion { get; private set; }
    public int Avance { get; private set; }
    public EstadoTarea Estado { get; private set; }
    public CircuitoAprobacion Aprobacion { get; private set; } = new([], 0);

    public static readonly IReadOnlyList<string> Areas = ["Base de datos", "Aplicaciones", "Core", "Soporte de aplicaciones", "Redes", "Servidores", "Helpdesk"];

    /// <summary>PRY-03: se crea dentro de un ítem y se asigna a un técnico de cualquier área.</summary>
    public static Result<Tarea> Crear(int itemId, int? hitoId, string titulo, string area, int tecnicoId, int orden,
        DateOnly fechaInicio, DateOnly fechaTerminacion, IEnumerable<int> aprobadores, int creadorId, DateOnly hoy)
    {
        if (Validar(titulo, area, orden, fechaInicio, fechaTerminacion) is { } e) return Result.Failure<Tarea>(e);
        if (fechaTerminacion < hoy) return Result.Failure<Tarea>(Error.Validation("fecha", "La fecha de terminación no puede ser pasada."));
        return Result.Success(new Tarea
        {
            Token = Guid.NewGuid(), ItemId = itemId, HitoId = hitoId, Titulo = titulo.Trim(), Area = area, TecnicoId = tecnicoId,
            Orden = orden, FechaInicio = fechaInicio, FechaTerminacion = fechaTerminacion, Estado = EstadoTarea.Pendiente,
            Aprobacion = new CircuitoAprobacion(aprobadores, creadorId)
        });
    }

    /// <summary>Reglas comunes al crear y al editar.</summary>
    public static Error? Validar(string titulo, string area, int orden, DateOnly inicio, DateOnly fin)
    {
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Trim().Length > 200) return Error.Validation("titulo", "El título es obligatorio (máximo 200 caracteres).");
        if (!Areas.Contains(area)) return Error.Validation("area", "Selecciona un área válida.");
        if (orden < 1) return Error.Validation("orden", "El orden debe ser 1 o mayor.");
        if (fin < inicio) return Error.Validation("fecha", "La fecha de terminación no puede ser anterior a la de inicio.");
        return null;
    }

    public static Tarea Rehidratar(int id, Guid token, int itemId, int? hitoId, string titulo, string area, int tecnicoId, int orden,
        DateOnly inicio, DateOnly fecha, int avance, EstadoTarea estado, CircuitoAprobacion aprobacion) => new()
    {
        TareaId = id, Token = token, ItemId = itemId, HitoId = hitoId, Titulo = titulo, Area = area, TecnicoId = tecnicoId,
        Orden = orden, FechaInicio = inicio, FechaTerminacion = fecha, Avance = avance, Estado = estado, Aprobacion = aprobacion
    };

    /// <summary>Sin aprobadores pasa directo a En curso; con aprobadores queda En revisión.</summary>
    public Result EnviarAAprobar()
    {
        if (Estado is EstadoTarea.EnRevision or EstadoTarea.Completada)
            return Result.Failure(Error.Validation("estado", "La tarea ya fue enviada o está completada."));
        Estado = Aprobacion.RequiereAprobacion ? EstadoTarea.EnRevision : EstadoTarea.EnCurso;
        return Result.Success();
    }

    /// <summary>PRY-04: si son varios aprobadores, aprueban todos en cualquier orden.</summary>
    public Result Aprobar(int aprobadorId, DateTime ahora)
    {
        if (Estado != EstadoTarea.EnRevision) return Result.Failure(Error.Validation("estado", "La tarea no está en revisión."));
        var r = Aprobacion.Aprobar(aprobadorId, ahora);
        if (r.IsSuccess && Aprobacion.Completa) Estado = EstadoTarea.EnCurso;
        return r;
    }

    /// <summary>PRY-05: el rechazo exige comentario y devuelve la tarea al técnico.</summary>
    public Result Rechazar(int aprobadorId, string? comentario, DateTime ahora)
    {
        if (Estado != EstadoTarea.EnRevision) return Result.Failure(Error.Validation("estado", "La tarea no está en revisión."));
        var r = Aprobacion.Rechazar(aprobadorId, comentario, ahora);
        if (r.IsSuccess) Estado = EstadoTarea.Pendiente;
        return r;
    }

    /// <summary>Solo se completa una tarea en curso (aprobada o sin aprobadores); su avance queda en 100 %.</summary>
    public Result Completar()
    {
        if (Estado != EstadoTarea.EnCurso) return Result.Failure(Error.Validation("estado", "Solo se completa una tarea en curso."));
        Estado = EstadoTarea.Completada;
        Avance = 100;
        return Result.Success();
    }
}
