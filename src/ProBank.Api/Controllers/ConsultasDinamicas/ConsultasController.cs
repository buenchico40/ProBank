using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Contracts.ConsultasDinamicas;

namespace ProBank.Api.Controllers.ConsultasDinamicas;

/// <summary>Consultas dinámicas (SQL-01 a SQL-08): bases de datos, catálogo, ejecuciones con aprobación, auditoría y envíos programados.</summary>
[ApiController]
[Route("api/consultas")]
public sealed class ConsultasController(IDispatcher dispatcher) : ControllerBase
{
    // ---------- Bases de datos ----------
    [HttpGet("conexiones")]
    public async Task<IActionResult> Conexiones(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarConexionesQuery(), ct));

    [HttpGet("conexiones/{codigo}")]
    public async Task<IActionResult> Conexion(string codigo, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new DetalleConexionQuery(codigo), ct));

    [HttpPost("conexiones")]
    public async Task<IActionResult> CrearConexion(GuardarConexionRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarConexionCommand(null, r), ct));

    [HttpPut("conexiones/{codigo}")]
    public async Task<IActionResult> EditarConexion(string codigo, GuardarConexionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarConexionCommand(codigo, r), ct));

    [HttpDelete("conexiones/{codigo}")]
    public async Task<IActionResult> EliminarConexion(string codigo, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EliminarConexionCommand(codigo), ct));

    /// <summary>Abre la conexión con la cadena de Key Vault y mide la respuesta.</summary>
    [HttpPost("conexiones/{codigo}/probar")]
    public async Task<IActionResult> ProbarConexion(string codigo, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new ProbarConexionCommand(codigo), ct));

    // ---------- Catálogo ----------
    [HttpGet("catalogo")]
    public async Task<IActionResult> Catalogo(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarConsultasQuery(), ct));

    [HttpGet("catalogo/opciones")]
    public async Task<IActionResult> Opciones(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new OpcionesConsultaQuery(), ct));

    [HttpGet("catalogo/{codigo}")]
    public async Task<IActionResult> Consulta(string codigo, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new DetalleConsultaQuery(codigo), ct));

    [HttpPost("catalogo")]
    public async Task<IActionResult> CrearConsulta(GuardarConsultaRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarConsultaCommand(null, r), ct));

    [HttpPut("catalogo/{codigo}")]
    public async Task<IActionResult> EditarConsulta(string codigo, GuardarConsultaRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarConsultaCommand(codigo, r), ct));

    [HttpDelete("catalogo/{codigo}")]
    public async Task<IActionResult> EliminarConsulta(string codigo, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EliminarConsultaCommand(codigo), ct));

    /// <summary>SQL-01: lee parámetros (sys.parameters) y columnas (sp_describe_first_result_set) del procedimiento en una base permitida.</summary>
    [HttpPost("catalogo/{codigo}/definicion")]
    public async Task<IActionResult> LeerDefinicion(string codigo, [FromQuery] string conexion, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new LeerDefinicionCommand(codigo, conexion), ct));

    [HttpPut("catalogo/{codigo}/parametros")]
    public async Task<IActionResult> GuardarParametros(string codigo, GuardarParametrosRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarParametrosCommand(codigo, r), ct));

    [HttpPost("catalogo/{codigo}/{accion:regex(^(Publicar|Retirar)$)}")]
    public async Task<IActionResult> CambiarEstado(string codigo, string accion, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new CambiarEstadoConsultaCommand(codigo, accion), ct));

    // ---------- Envíos programados ----------
    [HttpPost("catalogo/{codigo}/programaciones")]
    public async Task<IActionResult> CrearProgramacion(string codigo, GuardarProgramacionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarProgramacionCommand(codigo, null, r), ct));

    [HttpPut("catalogo/{codigo}/programaciones/{programacion}")]
    public async Task<IActionResult> EditarProgramacion(string codigo, string programacion, GuardarProgramacionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarProgramacionCommand(codigo, programacion, r), ct));

    [HttpDelete("programaciones/{programacion}")]
    public async Task<IActionResult> EliminarProgramacion(string programacion, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new EliminarProgramacionCommand(programacion), ct));

    // ---------- Ejecución ----------
    /// <summary>Consultas publicadas que el rol activo puede ejecutar.</summary>
    [HttpGet("disponibles")]
    public async Task<IActionResult> Disponibles(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ConsultasDisponiblesQuery(), ct));

    [HttpGet("disponibles/{codigo}")]
    public async Task<IActionResult> ParaEjecutar(string codigo, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ConsultaParaEjecutarQuery(codigo), ct));

    /// <summary>SQL-01..06: ejecuta (o deja pendiente de aprobación) una consulta publicada.</summary>
    [HttpPost("ejecutar")]
    public async Task<IActionResult> Ejecutar(EjecutarConsultaRequest request, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new EjecutarConsultaCommand(request), ct));

    // ---------- Ejecuciones y aprobaciones ----------
    [HttpGet("ejecuciones")]
    public async Task<IActionResult> Ejecuciones(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarEjecucionesQuery(), ct));

    [HttpGet("ejecuciones/{codigo}")]
    public async Task<IActionResult> Ejecucion(string codigo, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new DetalleEjecucionQuery(codigo), ct));

    /// <summary>SQL-03: aprobar o rechazar (con motivo).</summary>
    [HttpPost("ejecuciones/{codigo}/resolver")]
    public async Task<IActionResult> Resolver(string codigo, ResolverEjecucionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new ResolverEjecucionCommand(codigo, r), ct));

    /// <summary>El solicitante ejecuta su ejecución aprobada por todos.</summary>
    [HttpPost("ejecuciones/{codigo}/ejecutar")]
    public async Task<IActionResult> EjecutarAprobada(string codigo, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EjecutarAprobadaCommand(codigo), ct));

    /// <summary>Resultado guardado de la ejecución (solo para quien la solicitó).</summary>
    [HttpGet("ejecuciones/{codigo}/resultado")]
    public async Task<IActionResult> Resultado(string codigo, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ResultadoEjecucionQuery(codigo), ct));

    /// <summary>SQL-05: Excel, CSV, PDF, JSON o texto delimitado.</summary>
    [HttpGet("ejecuciones/{codigo}/exportar")]
    public async Task<IActionResult> Exportar(string codigo, [FromQuery] string formato, CancellationToken ct)
    {
        var r = await dispatcher.Send(new ExportarEjecucionCommand(codigo, formato), ct);
        return r.IsSuccess ? File(r.Value.Contenido, r.Value.TipoContenido, r.Value.Nombre) : this.ToActionResult(r);
    }

    // ---------- Auditoría de ejecuciones ----------
    [HttpGet("auditoria")]
    public async Task<IActionResult> Auditoria([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Query(new AuditoriaEjecucionesQuery(desde, hasta), ct));
}
