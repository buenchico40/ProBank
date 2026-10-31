using Microsoft.Extensions.DependencyInjection;
using ProBank.Domain.Common;

namespace ProBank.Application.Abstractions;

public interface ICommand<TResult> { }
public interface IQuery<TResult> { }

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> Handle(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken ct);
}

/// <summary>Despachador mínimo (ADR 0004): resuelve el handler desde el contenedor.</summary>
public interface IDispatcher
{
    Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default);
    Task<Result<TResult>> Query<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}

internal static class Acceso
{
    public static readonly Error SinPermiso = Error.Forbidden("Tu rol activo no tiene permiso para esta acción.");

    /// <summary>SEG-03: el rol activo debe tener la acción sobre la opción; los SP lo vuelven a validar.</summary>
    public static async Task<Error?> Exigir(this IPermisos permisos, string opcion, string accion, CancellationToken ct) =>
        await permisos.Tiene(opcion, accion, ct) ? null : SinPermiso;
}

internal sealed class Dispatcher(IServiceProvider sp) : IDispatcher
{
    public Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        var type = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = sp.GetRequiredService(type);
        return (Task<Result<TResult>>)type.GetMethod("Handle")!.Invoke(handler, [command, ct])!;
    }

    public Task<Result<TResult>> Query<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        var type = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = sp.GetRequiredService(type);
        return (Task<Result<TResult>>)type.GetMethod("Handle")!.Invoke(handler, [query, ct])!;
    }
}
