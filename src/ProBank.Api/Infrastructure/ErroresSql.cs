using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ProBank.Api.Infrastructure;

/// <summary>
/// Los SP señalan reglas de negocio con THROW 50xxx (50403 sin permiso, 50404 no existe, 50409 duplicado, otro 50xxx regla);
/// se responden como ProblemDetails con su mensaje. Cualquier otro error de SQL sigue siendo un 500 sin detalles.
/// </summary>
internal sealed class ErroresSql(IProblemDetailsService problemas) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is not SqlException { Number: >= 50000 and < 51000 } sql) return false;
        var (status, codigo) = sql.Number switch
        {
            50403 => (StatusCodes.Status403Forbidden, "forbidden"),
            50404 => (StatusCodes.Status404NotFound, "not_found"),
            50409 => (StatusCodes.Status409Conflict, "conflicto"),
            _ => (StatusCodes.Status422UnprocessableEntity, "regla"),
        };
        ctx.Response.StatusCode = status;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            ProblemDetails = new ProblemDetails { Status = status, Title = codigo, Detail = sql.Message },
        });
    }
}
