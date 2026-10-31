namespace ProBank.Domain.Common;

public enum Decision { Aprobada, Rechazada }

public sealed record DecisionAprobador(int AprobadorId, Decision Decision, DateTime Fecha, string? Comentario);

/// <summary>
/// Regla común de aprobación (tareas, ejecuciones de consultas, pasos a Producción):
/// ninguno, uno o varios aprobadores; si son varios, aprueban todos en cualquier orden;
/// un rechazo exige comentario; quien solicita no aprueba.
/// </summary>
public sealed class CircuitoAprobacion
{
    private readonly List<DecisionAprobador> _decisiones = [];

    public CircuitoAprobacion(IEnumerable<int> aprobadores, int solicitanteId)
    {
        Aprobadores = aprobadores.Distinct().ToList();
        SolicitanteId = solicitanteId;
    }

    public IReadOnlyList<int> Aprobadores { get; }
    public int SolicitanteId { get; }
    public IReadOnlyList<DecisionAprobador> Decisiones => _decisiones;

    public bool RequiereAprobacion => Aprobadores.Count > 0;
    public int Aprobadas => _decisiones.Count(d => d.Decision == Decision.Aprobada);
    public bool Completa => RequiereAprobacion && Aprobadores.All(a => _decisiones.Any(d => d.AprobadorId == a && d.Decision == Decision.Aprobada));
    public bool Rechazada => _decisiones.Any(d => d.Decision == Decision.Rechazada);
    public IEnumerable<int> Faltantes => Aprobadores.Where(a => !_decisiones.Any(d => d.AprobadorId == a && d.Decision == Decision.Aprobada));

    public void Cargar(DecisionAprobador d) => _decisiones.Add(d);

    public Result Aprobar(int aprobadorId, DateTime ahora) => Registrar(aprobadorId, Decision.Aprobada, null, ahora);

    public Result Rechazar(int aprobadorId, string? comentario, DateTime ahora)
    {
        if (string.IsNullOrWhiteSpace(comentario))
            return Result.Failure(Error.Validation("comentario_obligatorio", "El rechazo exige un comentario."));
        return Registrar(aprobadorId, Decision.Rechazada, comentario.Trim(), ahora);
    }

    private Result Registrar(int aprobadorId, Decision decision, string? comentario, DateTime ahora)
    {
        if (!Aprobadores.Contains(aprobadorId))
            return Result.Failure(Error.Forbidden("El usuario no es aprobador de este registro."));
        if (aprobadorId == SolicitanteId)
            return Result.Failure(Error.Forbidden("Quien solicita no puede aprobar."));
        if (Rechazada)
            return Result.Failure(Error.Validation("ya_rechazada", "El registro ya fue rechazado."));
        if (_decisiones.Any(d => d.AprobadorId == aprobadorId))
            return Result.Failure(Error.Validation("ya_decidio", "El aprobador ya registró su decisión."));
        _decisiones.Add(new DecisionAprobador(aprobadorId, decision, ahora, comentario));
        return Result.Success();
    }
}
