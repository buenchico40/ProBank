using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Boveda;
using ProBank.Contracts.Boveda;
using static ProBank.Domain.Modules.Boveda.ReglasBoveda;

namespace ProBank.UnitTests.Boveda;

public sealed class ReglasBovedaTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 3);

    [Fact(DisplayName = "CRE-01 · Certificados y secretos: vigente, por vencer dentro del aviso, vencido o inactivo")]
    public void EstadoDeVencimiento()
    {
        Assert.Equal("Vigente", EstadoVence(true, Hoy.AddDays(61), 60, Hoy));
        Assert.Equal("Por vencer", EstadoVence(true, Hoy.AddDays(60), 60, Hoy));
        Assert.Equal("Por vencer", EstadoVence(true, Hoy, 60, Hoy));
        Assert.Equal("Vencido", EstadoVence(true, Hoy.AddDays(-1), 60, Hoy));
        Assert.Equal("Vigente", EstadoVence(true, null, 30, Hoy));
        Assert.Equal("Inactivo", EstadoVence(false, Hoy.AddDays(-10), 30, Hoy));
    }

    [Fact(DisplayName = "CRE-01 · Cuentas técnicas: por rotar 15 días antes, rotación vencida; sin rotación (identidad administrada) activa")]
    public void EstadoDeRotacion()
    {
        Assert.Equal("Activo", EstadoCuenta(true, Hoy.AddDays(16), Hoy));
        Assert.Equal("Por rotar", EstadoCuenta(true, Hoy.AddDays(15), Hoy));
        Assert.Equal("Rotación vencida", EstadoCuenta(true, Hoy.AddDays(-1), Hoy));
        Assert.Equal("Activo", EstadoCuenta(true, null, Hoy));
        Assert.Equal("Inactivo", EstadoCuenta(false, Hoy.AddDays(-1), Hoy));
    }

    [Fact(DisplayName = "CRE-01 · La huella digital se normaliza y es SHA-1 o SHA-256 en hexadecimal")]
    public void Huella()
    {
        Assert.Equal("AABBCCDDEEFF00112233445566778899AABBCCDD", NormalizarHuella("aa:bb:cc:dd:ee:ff:00:11:22:33:44:55:66:77:88:99:aa:bb:cc:dd"));
        Assert.True(HuellaValida(NormalizarHuella("aabbccddeeff00112233445566778899aabbccdd")));
        Assert.True(HuellaValida(new string('A', 64)));
        Assert.False(HuellaValida(new string('A', 50)));
        Assert.False(HuellaValida("ZZBBCCDDEEFF00112233445566778899AABBCCDD"));
        Assert.Null(NormalizarHuella("  "));
        Assert.Equal("huella", ValidarRenovacion(Hoy.AddDays(365), "1234", Hoy)!.Code);
        Assert.Equal("vence", ValidarRenovacion(Hoy, new string('A', 40), Hoy)!.Code);
        Assert.Null(ValidarRenovacion(Hoy.AddDays(1), new string('A', 40), Hoy));
    }

    [Fact(DisplayName = "CRE-02 · Las cuentas con contraseña y los grupos necesitan grupo y miembros; la identidad administrada no")]
    public void GrupoObligatorio()
    {
        Assert.Equal("grupo", ValidarCuenta("svc_api", "Cuenta de servicio de Entra ID", "ProBank API", "Aplicaciones", null, null)!.Code);
        Assert.Null(ValidarCuenta("mi-probank-web", IdentidadAdministrada, "ProBank Web", "Servidores", null, null));
        Assert.Equal("tipo", ValidarCuenta("x", "Cuenta compartida", "S", "R", null, Guid.NewGuid())!.Code);
        Assert.Equal("miembros", ValidarGrupo("Bóveda · Aplicaciones", null, "Activo", [])!.Code);
        Assert.Null(ValidarGrupo("Bóveda · Aplicaciones", null, "Activo", [1]));
    }

    [Fact(DisplayName = "CRE-03 · El valor tiene al menos 16 caracteres, hasta 4000, y una vigencia de las ofrecidas")]
    public void Valor()
    {
        Assert.Equal("valor", ValidarValor("corto", 90)!.Code);
        Assert.Equal("valor", ValidarValor(new string('x', 4001), 90)!.Code);
        Assert.Equal("dias", ValidarValor(new string('x', 16), 45)!.Code);
        Assert.Null(ValidarValor(new string('x', 16), 90));
    }
}

public sealed class CredencialHandlersTests
{
    private static readonly Guid Token = Guid.NewGuid();

    private sealed class Conectado(params string[] permisos) : ICurrentUser, IPermisos, IClock
    {
        public int UsuarioId => 7;
        public string Usuario => "ana.perez";
        public string Nombre => "Ana Pérez";
        public string RolActivo => "Seguridad de la información";
        public string Ip => "10.0.0.5";
        public DateTime Ahora => new(2026, 11, 3, 10, 0, 0);
        public DateOnly Hoy => new(2026, 11, 3);
        public Task<bool> Tiene(string opcion, string accion, CancellationToken ct) => Task.FromResult(permisos.Contains(opcion + ":" + accion));
    }

    /// <summary>Solo registra lo que se le pidió: las reglas de grupo y la auditoría las aplica la base.</summary>
    private sealed class Repo : IBovedaRepository
    {
        public List<string> Llamadas { get; } = [];
        public Task<CredencialValorDto> EntregarCredencial(string clase, Guid token, string accion, Actor actor, CancellationToken ct)
        { Llamadas.Add($"{accion} {clase}"); return Task.FromResult(new CredencialValorDto("valor-secreto-0123456789", 3)); }
        public Task<int> CambiarCredencial(string clase, Guid token, string valor, DateOnly vence, string? nota, DateOnly hoy, Actor actor, CancellationToken ct)
        { Llamadas.Add($"Cambiar {clase} {vence:yyyy-MM-dd}"); return Task.FromResult(4); }

        public Task<IReadOnlyList<GrupoAccesoDto>> ListarGrupos(Guid? token, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<GrupoDetalleDto> DetalleGrupo(Guid token, CancellationToken ct) => throw new NotSupportedException();
        public Task<Guid> GuardarGrupo(Guid? token, GuardarGrupoRequest r, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<CertificadoDto>> ListarCertificados(DateOnly hoy, Guid? token, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<RenovacionCertificadoDto>> RenovacionesCertificado(Guid token, CancellationToken ct) => throw new NotSupportedException();
        public Task<Guid> GuardarCertificado(Guid? token, GuardarCertificadoRequest r, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task RenovarCertificado(Guid token, DateOnly vence, string huella, DateOnly hoy, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<CuentaTecnicaDto>> ListarCuentas(DateOnly hoy, Guid? token, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<Guid> GuardarCuenta(Guid? token, GuardarCuentaRequest r, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<SecretoDto>> ListarSecretos(DateOnly hoy, Guid? token, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<Guid> GuardarSecreto(Guid? token, GuardarSecretoRequest r, Actor actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<CredencialHistorialDto> HistorialCredencial(string clase, Guid token, CancellationToken ct) => throw new NotSupportedException();
    }

    private static CredencialHandlers Crear(Repo repo, params string[] permisos)
    {
        var u = new Conectado(permisos);
        return new CredencialHandlers(repo, u, u, u);
    }

    [Fact(DisplayName = "CRE-02 · Mostrar y copiar son permisos propios, distintos de ver")]
    public async Task MostrarYCopiar()
    {
        var repo = new Repo();
        var soloVer = Crear(repo, "secretos:ver");
        Assert.Equal("forbidden", (await soloVer.Handle(new EntregarCredencialCommand(TipoCredencial.Secreto, Token, Copiar: false), default)).Error.Code);

        var mostrar = Crear(repo, "secretos:ver", "secretos:mostrar");
        Assert.True((await mostrar.Handle(new EntregarCredencialCommand(TipoCredencial.Secreto, Token, Copiar: false), default)).IsSuccess);
        Assert.Equal("forbidden", (await mostrar.Handle(new EntregarCredencialCommand(TipoCredencial.Secreto, Token, Copiar: true), default)).Error.Code);

        var cuentas = Crear(repo, "cuentas:copiar");
        Assert.True((await cuentas.Handle(new EntregarCredencialCommand(TipoCredencial.Cuenta, Token, Copiar: true), default)).IsSuccess);
        Assert.Equal(["Mostrar secreto", "Copiar cuenta"], repo.Llamadas);
    }

    [Fact(DisplayName = "CRE-01 · Cambiar el valor exige editar y calcula el vencimiento desde hoy")]
    public async Task Cambiar()
    {
        var repo = new Repo();
        Assert.Equal("forbidden", (await Crear(repo, "cuentas:mostrar").Handle(
            new CambiarCredencialCommand(TipoCredencial.Cuenta, Token, new CambiarCredencialRequest("Clave-Nueva-0123456789", 90, null)), default)).Error.Code);
        Assert.Equal("valor", (await Crear(repo, "cuentas:editar").Handle(
            new CambiarCredencialCommand(TipoCredencial.Cuenta, Token, new CambiarCredencialRequest("corta", 90, null)), default)).Error.Code);
        var r = await Crear(repo, "cuentas:editar").Handle(
            new CambiarCredencialCommand(TipoCredencial.Cuenta, Token, new CambiarCredencialRequest("Clave-Nueva-0123456789", 90, " Rotación ")), default);
        Assert.Equal(new CredencialCambiadaDto(4, new DateOnly(2027, 2, 1)), r.Value);
        Assert.Equal(["Cambiar cuenta 2027-02-01"], repo.Llamadas);
    }
}
