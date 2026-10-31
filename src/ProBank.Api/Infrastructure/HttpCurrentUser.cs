using System.Security.Claims;
using ProBank.Application.Abstractions;

namespace ProBank.Api.Infrastructure;

/// <summary>
/// Usuario actual desde el token de ProBank (api/auth/ingresar). El rol activo llega en el encabezado X-ProBank-Rol y se valida
/// contra los roles asignados (claim "roles"); los SP lo vuelven a validar.
/// Sin petición HTTP (procesos en segundo plano) actúa como el usuario de sistema.
/// </summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private HttpContext? Ctx => accessor.HttpContext;

    public int UsuarioId => int.TryParse(Ctx?.User.FindFirstValue("probank_uid"), out var id) ? id : 0;
    public string Usuario => Ctx?.User.FindFirstValue("preferred_username") ?? "";
    public string Nombre =>Ctx is null ? "Sistema" : Ctx.User.FindFirstValue("name") ?? Ctx.User.Identity?.Name ?? "desconocido";

    public string RolActivo
    {
        get
        {
            if (Ctx is null) return "Sistema";
            var pedido = Ctx.Request.Headers["X-ProBank-Rol"].ToString();
            var roles = Ctx.User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(Ctx.User.FindAll("roles").Select(c => c.Value)).ToHashSet();
            return roles.Contains(pedido) ? pedido : roles.FirstOrDefault() ?? "Consulta";
        }
    }

    public string Ip => Ctx?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
}
