using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Api.Seguridad;
using ProBank.Contracts.Seguridad;

namespace ProBank.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ServicioIngreso servicio) : ControllerBase
{
    /// <summary>SEG-03: valida usuario y contraseña y devuelve el token de la sesión.</summary>
    [HttpPost("ingresar"), AllowAnonymous]
    public async Task<IActionResult> Ingresar([FromBody] IngresarRequest r, CancellationToken ct)
    {
        var resultado = await servicio.Ingresar(r, ct);
        return resultado.IsFailure && resultado.Error.Code is "credenciales" or "bloqueado"
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: resultado.Error.Code, detail: resultado.Error.Message)
            : this.ToActionResult(resultado);
    }
}
