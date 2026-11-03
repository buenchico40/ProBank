using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Boveda;
using ProBank.Contracts.Boveda;

namespace ProBank.Api.Controllers.Boveda;

/// <summary>Bóveda: grupos de acceso, certificados, cuentas técnicas y secretos (CRE-01 a CRE-03).</summary>
[ApiController]
[Route("api/boveda")]
public sealed class BovedaController(IDispatcher dispatcher) : ControllerBase
{
    // ---------- Grupos de acceso ----------
    [HttpGet("grupos")]
    public async Task<IActionResult> Grupos(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarGruposQuery(), ct));

    [HttpGet("grupos/{token:guid}")]
    public async Task<IActionResult> Grupo(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerGrupoQuery(token), ct));

    [HttpGet("grupos/{token:guid}/detalle")]
    public async Task<IActionResult> DetalleGrupo(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new DetalleGrupoQuery(token), ct));

    [HttpPost("grupos")]
    public async Task<IActionResult> CrearGrupo(GuardarGrupoRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarGrupoCommand(null, r), ct));

    [HttpPut("grupos/{token:guid}")]
    public async Task<IActionResult> EditarGrupo(Guid token, GuardarGrupoRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarGrupoCommand(token, r), ct));

    // ---------- Certificados ----------
    [HttpGet("certificados")]
    public async Task<IActionResult> Certificados(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarCertificadosQuery(), ct));

    [HttpGet("certificados/{token:guid}")]
    public async Task<IActionResult> Certificado(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerCertificadoQuery(token), ct));

    [HttpGet("certificados/{token:guid}/renovaciones")]
    public async Task<IActionResult> Renovaciones(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new RenovacionesCertificadoQuery(token), ct));

    [HttpPost("certificados")]
    public async Task<IActionResult> CrearCertificado(GuardarCertificadoRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarCertificadoCommand(null, r), ct));

    [HttpPut("certificados/{token:guid}")]
    public async Task<IActionResult> EditarCertificado(Guid token, GuardarCertificadoRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarCertificadoCommand(token, r), ct));

    [HttpPost("certificados/{token:guid}/renovar")]
    public async Task<IActionResult> Renovar(Guid token, RenovarCertificadoRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new RenovarCertificadoCommand(token, r), ct));

    // ---------- Cuentas técnicas ----------
    [HttpGet("cuentas")]
    public async Task<IActionResult> Cuentas(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarCuentasQuery(), ct));

    [HttpGet("cuentas/{token:guid}")]
    public async Task<IActionResult> Cuenta(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerCuentaQuery(token), ct));

    [HttpPost("cuentas")]
    public async Task<IActionResult> CrearCuenta(GuardarCuentaRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarCuentaCommand(null, r), ct));

    [HttpPut("cuentas/{token:guid}")]
    public async Task<IActionResult> EditarCuenta(Guid token, GuardarCuentaRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarCuentaCommand(token, r), ct));

    // ---------- Secretos ----------
    [HttpGet("secretos")]
    public async Task<IActionResult> Secretos(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarSecretosQuery(), ct));

    [HttpGet("secretos/{token:guid}")]
    public async Task<IActionResult> Secreto(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerSecretoQuery(token), ct));

    [HttpPost("secretos")]
    public async Task<IActionResult> CrearSecreto(GuardarSecretoRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarSecretoCommand(null, r), ct));

    [HttpPut("secretos/{token:guid}")]
    public async Task<IActionResult> EditarSecreto(Guid token, GuardarSecretoRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarSecretoCommand(token, r), ct));

    // ---------- Valor de una cuenta técnica o un secreto ({clase}: cuentas o secretos) ----------
    [HttpGet("{clase:regex(^(cuentas|secretos)$)}/{token:guid}/historial")]
    public async Task<IActionResult> Historial(string clase, Guid token, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Query(new HistorialCredencialQuery(Tipo(clase), token), ct));

    /// <summary>CRE-02: entrega el valor para mostrarlo; queda registrado. Nunca se guarda en caché.</summary>
    [HttpPost("{clase:regex(^(cuentas|secretos)$)}/{token:guid}/mostrar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Mostrar(string clase, Guid token, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new EntregarCredencialCommand(Tipo(clase), token, Copiar: false), ct));

    /// <summary>CRE-02: entrega el valor para copiarlo al portapapeles (permiso propio); queda registrado.</summary>
    [HttpPost("{clase:regex(^(cuentas|secretos)$)}/{token:guid}/copiar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Copiar(string clase, Guid token, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new EntregarCredencialCommand(Tipo(clase), token, Copiar: true), ct));

    [HttpPost("{clase:regex(^(cuentas|secretos)$)}/{token:guid}/cambiar")]
    public async Task<IActionResult> Cambiar(string clase, Guid token, CambiarCredencialRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new CambiarCredencialCommand(Tipo(clase), token, r), ct));

    private static TipoCredencial Tipo(string clase) => clase == "cuentas" ? TipoCredencial.Cuenta : TipoCredencial.Secreto;
}
