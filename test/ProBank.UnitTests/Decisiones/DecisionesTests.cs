using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Decisiones;
using ProBank.Contracts.Decisiones;
using ProBank.Domain.Common;
using static ProBank.Domain.Modules.Decisiones.ReglasDecisiones;

namespace ProBank.UnitTests.Decisiones;

public sealed class ReglasDecisionesTests
{
    [Fact(DisplayName = "DEC-02 · El tipo necesita aprobadores distintos, con orden de 1 a 99")]
    public void Tipo()
    {
        Assert.Equal("aprobadores", ValidarTipo("Compra mayor", null, [])!.Code);
        Assert.Equal("aprobadores", ValidarTipo("Compra mayor", null, [(1, 1), (1, 2)])!.Code);
        Assert.Equal("orden", ValidarTipo("Compra mayor", null, [(1, 0)])!.Code);
        Assert.Null(ValidarTipo("Compra mayor", "Sobre el umbral", [(1, 1), (2, 2)]));
    }

    [Fact(DisplayName = "DEC-01 · La decisión tiene tipo, título, descripción (con alcance y montos) y área")]
    public void Decision()
    {
        Assert.Equal("tipo", ValidarDecision("", "Título", "Descripción", "TI")!.Code);
        Assert.Equal("descripcion", ValidarDecision("TDE-01", "Título", " ", "TI")!.Code);
        Assert.Equal("descripcion", ValidarDecision("TDE-01", "Título", new string('x', MaximoDescripcion + 1), "TI")!.Code);
        Assert.Equal("area", ValidarDecision("TDE-01", "Título", "Descripción", "")!.Code);
        Assert.Null(ValidarDecision("TDE-01", "Título", "Descripción", "TI"));
    }

    [Fact(DisplayName = "DEC-03 · Firmar pide la clave; rechazar pide además el motivo")]
    public void Firma()
    {
        Assert.Equal("clave", ValidarFirma(true, "", null)!.Code);
        Assert.Equal("comentario", ValidarFirma(false, "clave", " ")!.Code);
        Assert.Null(ValidarFirma(true, "clave", null));
        Assert.Null(ValidarFirma(false, "clave", "No corresponde"));
    }

    [Fact(DisplayName = "DEC-07 · Modificar pide el contenido válido, el motivo y la clave")]
    public void Modificacion()
    {
        Assert.Equal("motivo", ValidarModificacion("Título", "Descripción", "TI", "", "clave")!.Code);
        Assert.Equal("clave", ValidarModificacion("Título", "Descripción", "TI", "Se corrige el monto", "")!.Code);
        Assert.Equal("titulo", ValidarModificacion("", "Descripción", "TI", "Motivo", "clave")!.Code);
        Assert.Null(ValidarModificacion("Título", "Descripción", "TI", "Se corrige el monto", "clave"));
    }
}

public sealed class DecisionesHandlersTests
{
    private const string Clave = "Clave-Correcta-2026!";
    private static readonly Guid Token = Guid.NewGuid();

    private sealed class Conectado(params string[] permisos) : ICurrentUser, IPermisos
    {
        public int UsuarioId => 3;
        public string Usuario => "aprobador.local";
        public string Nombre => "Aprobador de prueba";
        public string RolActivo => "Gerencia de TI";
        public string Ip => "10.0.0.7";
        public Task<bool> Tiene(string opcion, string accion, CancellationToken ct) => Task.FromResult(permisos.Contains(opcion + ":" + accion));
    }

    private sealed class Credenciales : ICredencialesRepository
    {
        private static readonly (byte[] Hash, byte[] Sal) Guardada = Claves.Generar(Clave);
        public Task<CredencialUsuario?> Obtener(string usuario, CancellationToken ct) =>
            Task.FromResult<CredencialUsuario?>(new CredencialUsuario(3, usuario, "Aprobador de prueba", Guardada.Hash, Guardada.Sal, null, "Activo", false, ["Gerencia de TI"]));
        public Task RegistrarIngreso(string usuario, bool exitoso, string? motivo, string ip, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class Auditoria : IAuditLog
    {
        public List<string> Acciones { get; } = [];
        public Task Registrar(string modulo, string accion, string detalle, CancellationToken ct) { Acciones.Add(accion); return Task.CompletedTask; }
    }

    /// <summary>La limpieza real del HTML se prueba en ProBank.IntegrationTests (HtmlSeguroTests).</summary>
    private sealed class HtmlTal : IHtmlSeguro
    {
        public (string Html, string Texto) Limpiar(string? html) => (html ?? "", (html ?? "").Replace("<p>", "").Replace("</p>", "").Trim());
    }

    /// <summary>Solo registra lo que se le pidió: estados, turnos, huellas y sellos los aplica la base.</summary>
    private sealed class Repo : IDecisionesRepository
    {
        public List<string> Llamadas { get; } = [];
        public Task<string> Firmar(Guid token, bool aprobar, string? comentario, Actor actor, CancellationToken ct)
        { Llamadas.Add(aprobar ? "Aprobar" : "Rechazar:" + comentario); return Task.FromResult(aprobar ? "Aprobada" : "Rechazada"); }
        public Task<int> Modificar(Guid token, ModificarDecisionRequest r, Actor actor, CancellationToken ct) { Llamadas.Add("Modificar:" + r.Motivo); return Task.FromResult(2); }
        public Task CambiarParticipante(Guid token, string rol, bool agregar, int usuarioId, int? orden, Actor actor, CancellationToken ct)
        { Llamadas.Add($"{rol}:{(agregar ? "+" : "-")}{usuarioId}"); return Task.CompletedTask; }

        public Task<IReadOnlyList<TipoDecisionDto>> ListarTipos(bool soloActivos, CancellationToken ct) => throw new NotSupportedException();
        public Task<string> GuardarTipo(string? codigo, GuardarTipoDecisionRequest r, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<DecisionDto>> Listar(string alcance, Guid? token, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<DecisionDetalleDto?> Detalle(Guid token, string evento, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<Guid> Guardar(Guid? token, GuardarDecisionRequest r, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task Enviar(Guid token, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task Anular(Guid token, string motivo, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task RegistrarAdjunto(Guid token, Guid decisionToken, string nombre, string tipo, long tamano, byte[] hash, string ruta, int maximo, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<string> EliminarAdjunto(Guid token, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<(string Nombre, string Tipo, string Ruta)?> ObtenerAdjunto(Guid token, Actor actor, CancellationToken ct) => throw new NotSupportedException();
    }

    private static (DecisionesHandlers H, Repo R, Auditoria A) Crear(params string[] permisos)
    {
        var u = new Conectado(permisos); var repo = new Repo(); var audit = new Auditoria();
        return (new DecisionesHandlers(repo, u, u, new Credenciales(), audit, new HtmlTal()), repo, audit);
    }

    [Fact(DisplayName = "DEC-03 · Sin la clave correcta no hay firma y el intento queda en la auditoría")]
    public async Task ClaveIncorrecta()
    {
        var (h, repo, audit) = Crear("pendientes:aprobar");
        var r = await h.Handle(new FirmarDecisionCommand(Token, new FirmarDecisionRequest(true, "otra-clave", null)), default);
        Assert.Equal("clave", r.Error.Code);
        Assert.Empty(repo.Llamadas);
        Assert.Contains("Firma no realizada: clave incorrecta", audit.Acciones);
    }

    [Fact(DisplayName = "DEC-03 · Con la clave correcta firma; rechazar exige motivo; sin permiso no firma")]
    public async Task Firmar()
    {
        var (h, repo, _) = Crear("pendientes:aprobar");
        Assert.Equal("comentario", (await h.Handle(new FirmarDecisionCommand(Token, new FirmarDecisionRequest(false, Clave, null)), default)).Error.Code);
        Assert.Contains("aprobada por todos", (await h.Handle(new FirmarDecisionCommand(Token, new FirmarDecisionRequest(true, Clave, null)), default)).Value);
        Assert.Contains("rechazada", (await h.Handle(new FirmarDecisionCommand(Token, new FirmarDecisionRequest(false, Clave, " No corresponde ")), default)).Value);
        Assert.Equal(["Aprobar", "Rechazar:No corresponde"], repo.Llamadas);
        var (sin, _, _) = Crear("pendientes:ver");
        Assert.Equal("forbidden", (await sin.Handle(new FirmarDecisionCommand(Token, new FirmarDecisionRequest(true, Clave, null)), default)).Error.Code);
    }

    [Fact(DisplayName = "DEC-07 · Modificar también se firma con la clave y genera una versión nueva")]
    public async Task Modificar()
    {
        var (h, repo, audit) = Crear("pendientes:aprobar");
        var mala = new ModificarDecisionRequest("Título", "Descripción", "TI", new DateOnly(2026, 11, 14), "Se corrige", "otra-clave");
        Assert.Equal("clave", (await h.Handle(new ModificarDecisionCommand(Token, mala), default)).Error.Code);
        Assert.Contains("Modificación no realizada: clave incorrecta", audit.Acciones);
        var r = await h.Handle(new ModificarDecisionCommand(Token, mala with { Clave = Clave, Motivo = " Se corrige el monto " }), default);
        Assert.Equal("Versión 2 guardada: todos los aprobadores vuelven a firmar", r.Value);
        Assert.Equal(["Modificar:Se corrige el monto"], repo.Llamadas);
    }

    [Fact(DisplayName = "DEC-05 · Agregar o quitar aprobadores y notificados exige editar decisiones")]
    public async Task Participantes()
    {
        var (sin, _, _) = Crear("decisiones:ver");
        Assert.Equal("forbidden", (await sin.Handle(new CambiarParticipanteCommand(Token, "Notificado", true, new ParticipanteRequest(2, null)), default)).Error.Code);
        var (h, repo, _) = Crear("decisiones:editar");
        Assert.Equal("rol", (await h.Handle(new CambiarParticipanteCommand(Token, "Otro", true, new ParticipanteRequest(2, null)), default)).Error.Code);
        await h.Handle(new CambiarParticipanteCommand(Token, "Aprobador", true, new ParticipanteRequest(2, 3)), default);
        await h.Handle(new CambiarParticipanteCommand(Token, "Notificado", false, new ParticipanteRequest(5, null)), default);
        Assert.Equal(["Aprobador:+2", "Notificado:-5"], repo.Llamadas);
    }
}
