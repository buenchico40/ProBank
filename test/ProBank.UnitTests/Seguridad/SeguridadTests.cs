using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Seguridad;
using ProBank.Contracts.Seguridad;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Seguridad;

namespace ProBank.UnitTests.Seguridad;

public sealed class ReglasSeguridadTests
{
    [Theory(DisplayName = "Formato del nombre de usuario")]
    [InlineData("ana.perez", true)]
    [InlineData("a_1-b", true)]
    [InlineData("Ana.Perez", false)]
    [InlineData("ab", false)]
    [InlineData("ana perez", false)]
    [InlineData(".ana", false)]
    public void Usuario(string usuario, bool valido) => Assert.Equal(valido, ReglasSeguridad.ValidarUsuario(usuario) is null);

    [Fact(DisplayName = "SEG-01 · el rol predeterminado debe estar entre los asignados")]
    public void Predeterminado()
    {
        Assert.Null(ReglasSeguridad.ValidarDatos("Ana", null, ["Consulta", "Gerencia de TI"], "Consulta", "Activo"));
        Assert.Equal("rol_predeterminado", ReglasSeguridad.ValidarDatos("Ana", null, ["Consulta"], "Administrador", "Activo")!.Code);
        Assert.Equal("roles", ReglasSeguridad.ValidarDatos("Ana", null, [], null, "Activo")!.Code);
    }

    [Theory(DisplayName = "Correo y estado válidos")]
    [InlineData("ana@banco.ec", "Activo", true)]
    [InlineData("no-es-correo", "Activo", false)]
    [InlineData(null, "Suspendido", false)]
    public void CorreoEstado(string? correo, string estado, bool valido) =>
        Assert.Equal(valido, ReglasSeguridad.ValidarDatos("Ana", correo, ["Consulta"], null, estado) is null);
}

public sealed class SeguridadHandlersTests
{
    private const string Clave = "Clave-Actual-2026!";

    private sealed class Conectado(bool permitido = true) : ICurrentUser, IPermisos
    {
        public int UsuarioId => 7;
        public string Usuario => "ana.perez";
        public string Nombre => "Ana Pérez";
        public string RolActivo => "Administrador";
        public string Ip => "10.0.0.5";
        public List<string> Pedidos { get; } = [];
        public Task<bool> Tiene(string opcion, string accion, CancellationToken ct) { Pedidos.Add(opcion + ":" + accion); return Task.FromResult(permitido); }
    }

    private sealed class Sesiones : ISesiones
    {
        public List<int> Invalidadas { get; } = [];
        public Task<SesionVigente?> Obtener(int usuarioId, CancellationToken ct) => Task.FromResult<SesionVigente?>(new SesionVigente(1, "Activo"));
        public void Invalidar(int usuarioId) => Invalidadas.Add(usuarioId);
    }

    private sealed class Credenciales : ICredencialesRepository
    {
        private static readonly (byte[] Hash, byte[] Sal) Guardada = Claves.Generar(Clave);
        public Task<CredencialUsuario?> Obtener(string usuario, CancellationToken ct) => Task.FromResult<CredencialUsuario?>(
            usuario == "ana.perez" ? new CredencialUsuario(7, "ana.perez", "Ana Pérez", Guardada.Hash, Guardada.Sal, null, "Activo", false, ["Consulta"]) : null);
        public Task RegistrarIngreso(string usuario, bool exitoso, string? motivo, string ip, CancellationToken ct) => Task.CompletedTask;
    }

    /// <summary>Solo registra lo que los manejadores le piden; lo demás no se usa en estas pruebas.</summary>
    private sealed class Repo : ISeguridadRepository
    {
        public CrearUsuarioRequest? Creado { get; private set; }
        public byte[]? Hash { get; private set; }
        public byte[]? Sal { get; private set; }
        public Actor? Actor { get; private set; }
        public int ClavesCambiadas { get; private set; }

        public Task<Guid> CrearUsuario(CrearUsuarioRequest r, byte[] hash, byte[] sal, Actor actor, CancellationToken ct)
        { Creado = r; Hash = hash; Sal = sal; Actor = actor; return Task.FromResult(Guid.NewGuid()); }
        public Task CambiarClave(int usuarioId, byte[] hash, byte[] sal, string rolActivo, string ip, CancellationToken ct) { ClavesCambiadas++; return Task.CompletedTask; }
        public Task<int> RevocarSesiones(Guid token, Actor actor, CancellationToken ct) => Task.FromResult(42);

        public Task<IReadOnlyList<UsuarioDto>> ListarUsuarios(CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<IngresoUsuarioDto>> ListarIngresos(Guid token, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> ActualizarUsuario(Guid token, ActualizarUsuarioRequest r, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> RestablecerClave(Guid token, byte[] hash, byte[] sal, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task Desbloquear(Guid token, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<PerfilDto?> ObtenerPerfil(int usuarioId, string rolActivo, CancellationToken ct) => throw new NotSupportedException();
        public Task CambiarRol(int usuarioId, string rol, bool predeterminado, string ip, CancellationToken ct) => throw new NotSupportedException();
        public Task<SesionVigente?> ObtenerSello(int usuarioId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<RolDto>> ListarRoles(CancellationToken ct) => throw new NotSupportedException();
        public Task<int> GuardarRol(int? rolId, GuardarRolRequest r, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<PermisoDto>> ListarPermisos(int rolId, CancellationToken ct) => throw new NotSupportedException();
        public Task GuardarPermisos(int rolId, IReadOnlyList<string> permisos, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<AuditoriaDto>> ListarAuditoria(DateOnly desde, DateOnly hasta, string? modulo, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<ParametroDto>> ListarParametros(CancellationToken ct) => throw new NotSupportedException();
        public Task GuardarParametros(IReadOnlyList<ValorParametro> valores, Actor actor, CancellationToken ct) => throw new NotSupportedException();
    }

    private static readonly CrearUsuarioRequest Nuevo = new(" Luis.Mora ", "Luis Mora", "luis@banco.ec", ["Consulta", "Compras TI"], null);

    [Fact(DisplayName = "SEG-03 · sin permiso no se crea el usuario")]
    public async Task CrearSinPermiso()
    {
        var repo = new Repo(); var u = new Conectado(permitido: false);
        var r = await new CrearUsuarioHandler(repo, u, u).Handle(new CrearUsuarioCommand(Nuevo), default);
        Assert.Equal("forbidden", r.Error.Code);
        Assert.Equal(["usuarios:crear"], u.Pedidos);
        Assert.Null(repo.Creado);
    }

    [Fact(DisplayName = "SEG-07 · el usuario nuevo recibe una clave temporal que coincide con el hash guardado")]
    public async Task CrearConClaveTemporal()
    {
        var repo = new Repo(); var u = new Conectado();
        var r = await new CrearUsuarioHandler(repo, u, u).Handle(new CrearUsuarioCommand(Nuevo), default);

        Assert.True(r.IsSuccess);
        Assert.Equal("luis.mora", r.Value.Usuario);
        Assert.Equal("Consulta", repo.Creado!.RolPredeterminado);
        Assert.True(Claves.Verificar(r.Value.ClaveTemporal, repo.Hash!, repo.Sal!));
        Assert.Equal(new Actor(7, "Administrador", "10.0.0.5"), repo.Actor);
    }

    [Fact(DisplayName = "SEG-01 · datos inválidos no llegan a la base")]
    public async Task CrearInvalido()
    {
        var repo = new Repo(); var u = new Conectado();
        var r = await new CrearUsuarioHandler(repo, u, u).Handle(new CrearUsuarioCommand(Nuevo with { Usuario = "x" }), default);
        Assert.Equal("usuario", r.Error.Code);
        Assert.Null(repo.Creado);
    }

    [Fact(DisplayName = "SEG-07 · revocar sesiones invalida la caché del usuario afectado")]
    public async Task Revocar()
    {
        var sesiones = new Sesiones(); var u = new Conectado();
        var r = await new RevocarSesionesHandler(new Repo(), u, u, sesiones).Handle(new RevocarSesionesCommand(Guid.NewGuid()), default);
        Assert.True(r.IsSuccess);
        Assert.Equal([42], sesiones.Invalidadas);
    }

    [Fact(DisplayName = "Cambiar la clave exige la actual")]
    public async Task CambiarClaveActualIncorrecta()
    {
        var repo = new Repo();
        var r = await new CambiarClaveHandler(new Credenciales(), repo, new Conectado(), new Sesiones())
            .Handle(new CambiarClaveCommand(new CambiarClaveRequest("otra-clave-cualquiera", "Nueva-Clave-Segura-2026")), default);
        Assert.Equal(CambiarClaveHandler.ActualIncorrecta, r.Error);
        Assert.Equal(0, repo.ClavesCambiadas);
    }

    [Theory(DisplayName = "SEG-06 · la nueva clave cumple la política y es distinta de la actual")]
    [InlineData("corta")]
    [InlineData("ana.perez-2026-nueva")]
    [InlineData(Clave)]
    public async Task CambiarClaveRechazada(string nueva)
    {
        var repo = new Repo();
        var r = await new CambiarClaveHandler(new Credenciales(), repo, new Conectado(), new Sesiones())
            .Handle(new CambiarClaveCommand(new CambiarClaveRequest(Clave, nueva)), default);
        Assert.Equal("clave_nueva", r.Error.Code);
        Assert.Equal(0, repo.ClavesCambiadas);
    }

    [Fact(DisplayName = "SEG-07 · al cambiar la clave se revocan las sesiones del usuario")]
    public async Task CambiarClave()
    {
        var repo = new Repo(); var sesiones = new Sesiones();
        var r = await new CambiarClaveHandler(new Credenciales(), repo, new Conectado(), sesiones)
            .Handle(new CambiarClaveCommand(new CambiarClaveRequest(Clave, "Nueva-Clave-Segura-2026")), default);
        Assert.True(r.IsSuccess);
        Assert.Equal(1, repo.ClavesCambiadas);
        Assert.Equal([7], sesiones.Invalidadas);
    }
}
