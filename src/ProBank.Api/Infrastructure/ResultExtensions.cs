using Microsoft.AspNetCore.Mvc;
using ProBank.Domain.Common;

namespace ProBank.Api.Infrastructure;

internal static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase c, Result<T> r) => r.IsSuccess
        ? c.Ok(r.Value)
        : c.Problem(statusCode: r.Error.Code switch { "not_found" => 404, "forbidden" => 403, _ => 422 }, title: r.Error.Code, detail: r.Error.Message);
}
