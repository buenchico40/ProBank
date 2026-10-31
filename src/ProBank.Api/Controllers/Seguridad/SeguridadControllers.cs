using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Seguridad;
using ProBank.Contracts.Seguridad;

namespace ProBank.Api.Controllers.Seguridad;

/// <summary>Seguridad › Usuarios (SEG-01, SEG-06, SEG-07). Los usuarios se identifican por su token opaco.</summary>
[ApiController]
[Route("api/seguridad/usuarios")]
public sealed class UsuariosController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarUsuariosQuery(), ct));

    [HttpGet("{token:guid}/ingresos")]
    public async Task<IActionResult> Ingresos(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarIngresosQuery(token), ct));

    /// <summary>Crea el usuario y devuelve su clave temporal (se muestra una sola vez).</summary>
    [HttpPost]
    public async Task<IActionResult> Crear(CrearUsuarioRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new CrearUsuarioCommand(r), ct));

    [HttpPut("{token:guid}")]
    public async Task<IActionResult> Actualizar(Guid token, ActualizarUsuarioRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new ActualizarUsuarioCommand(token, r), ct));

    [HttpPost("{token:guid}/clave")]
    public async Task<IActionResult> RestablecerClave(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new RestablecerClaveCommand(token), ct));

    [HttpPost("{token:guid}/desbloquear")]
    public async Task<IActionResult> Desbloquear(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new DesbloquearUsuarioCommand(token), ct));

    [HttpPost("{token:guid}/revocar")]
    public async Task<IActionResult> Revocar(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new RevocarSesionesCommand(token), ct));
}

/// <summary>Seguridad › Roles y permisos (SEG-03): opción del menú × acción y funciones propias.</summary>
[ApiController]
[Route("api/seguridad/roles")]
public sealed class RolesController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarRolesQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Crear(GuardarRolRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarRolCommand(null, r), ct));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, GuardarRolRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarRolCommand(id, r), ct));

    /// <summary>Matriz de permisos; id 0 devuelve la matriz vacía para un rol nuevo.</summary>
    [HttpGet("{id:int}/permisos")]
    public async Task<IActionResult> Permisos(int id, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarPermisosQuery(id), ct));

    [HttpPut("{id:int}/permisos")]
    public async Task<IActionResult> GuardarPermisos(int id, GuardarPermisosRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarPermisosCommand(id, r.Permisos), ct));
}

/// <summary>Seguridad › Auditoría (SEG-05): solo lectura.</summary>
[ApiController]
[Route("api/seguridad/auditoria")]
public sealed class AuditoriaController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] string? modulo, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Query(new ListarAuditoriaQuery(desde, hasta, modulo), ct));
}

[ApiController]
[Route("api/seguridad/parametros")]
public sealed class ParametrosController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarParametrosQuery(), ct));

    [HttpPut]
    public async Task<IActionResult> Guardar(GuardarParametrosRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarParametrosCommand(r.Valores), ct));
}

/// <summary>Perfil del usuario conectado. Es lo único que se permite con una clave temporal (SEG-07).</summary>
[ApiController]
[Route("api/seguridad/perfil")]
public sealed class PerfilController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerPerfilQuery(), ct));

    [HttpPost("clave")]
    public async Task<IActionResult> CambiarClave(CambiarClaveRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new CambiarClaveCommand(r), ct));

    [HttpPost("rol")]
    public async Task<IActionResult> CambiarRol(CambiarRolRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new CambiarRolCommand(r), ct));
}
