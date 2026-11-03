namespace ProBank.Contracts.Boveda;

// Grupos de acceso (CRE-02): solo sus miembros ven, copian y cambian las credenciales del grupo. EsMiembro: el usuario conectado.
public sealed record GrupoAccesoDto(Guid Token, string Codigo, string Nombre, string? Descripcion, string Estado, int Miembros, int Credenciales, bool EsMiembro);
public sealed record GrupoDetalleDto(IReadOnlyList<MiembroGrupoDto> Miembros, IReadOnlyList<CredencialGrupoDto> Credenciales);
public sealed record MiembroGrupoDto(int UsuarioId, string Usuario, string Nombre, string Estado, string Roles, DateTime Desde);
/// <summary>Clase: «cuenta» o «secreto».</summary>
public sealed record CredencialGrupoDto(string Clase, Guid Token, string Nombre, string Tipo, string Sistema, bool Activo);
public sealed record GuardarGrupoRequest(string Nombre, string? Descripcion, string Estado, IReadOnlyList<int> Miembros);

// Certificados digitales (CRE-01): ProBank solo registra su vigencia; el certificado queda en su almacén.
public sealed record CertificadoDto(Guid Token, string Codigo, string NombreComun, string Tipo, string Emisor, DateOnly Vence, int DiasParaVencer,
    string Almacen, string Responsable, int DiasAviso, string? Huella, string? NombresAlternativos, string? UsadoPor, bool Activo, string Estado,
    DateTime? UltimaRenovacion);
public sealed record RenovacionCertificadoDto(DateTime Fecha, DateOnly VenceAnterior, DateOnly VenceNuevo, string? HuellaAnterior, string HuellaNueva, string RegistradoPor);
public sealed record GuardarCertificadoRequest(string NombreComun, string Tipo, string Emisor, DateOnly Vence, string Almacen, string Responsable, int DiasAviso,
    string? Huella, string? NombresAlternativos, string? UsadoPor, bool Activo);
public sealed record RenovarCertificadoRequest(DateOnly Vence, string Huella);

// Cuentas técnicas y secretos (CRE-01): los listados nunca traen el valor; Version 0 = aún sin valor guardado.
public sealed record CuentaTecnicaDto(Guid Token, string Nombre, string Tipo, string Sistema, string Responsable, string? Privilegios,
    DateOnly? UltimaRotacion, DateOnly? ProximaRotacion, int? DiasParaRotar, string Estado, Guid? GrupoToken, string? GrupoCodigo, string? GrupoNombre,
    int Version, DateTime? FechaVersion, bool TieneValor, bool EsMiembro, bool Activo);
public sealed record GuardarCuentaRequest(string Nombre, string Tipo, string Sistema, string Responsable, string? Privilegios, Guid? Grupo,
    DateOnly? ProximaRotacion, bool Activo);

public sealed record SecretoDto(Guid Token, string Nombre, string Tipo, string Almacen, string Sistema, int Version, DateOnly? UltimaRotacion,
    DateOnly? Expira, int? DiasParaVencer, int DiasAviso, string Estado, Guid GrupoToken, string GrupoCodigo, string GrupoNombre, DateTime? FechaVersion,
    bool TieneValor, bool EsMiembro, bool Activo);
public sealed record GuardarSecretoRequest(string Nombre, string Tipo, string Almacen, string Sistema, Guid Grupo, int DiasAviso, DateOnly? Expira, bool Activo);

// Valor de una credencial (CRE-02 y CRE-03): versiones sin valor, accesos registrados, entrega auditada y cambio.
public sealed record CredencialHistorialDto(IReadOnlyList<VersionCredencialDto> Versiones, IReadOnlyList<AccesoCredencialDto> Accesos);
public sealed record VersionCredencialDto(int Version, DateTime Fecha, DateOnly? Vence, string RegistradoPor, string? Nota, bool Vigente);
public sealed record AccesoCredencialDto(DateTime Fecha, string Usuario, string Nombre, string Accion, int Version, string? Ip);
/// <summary>Dias: próxima rotación (cuenta) o vigencia (secreto) desde hoy.</summary>
public sealed record CambiarCredencialRequest(string Valor, int Dias, string? Nota);
public sealed record CredencialValorDto(string Valor, int Version);
public sealed record CredencialCambiadaDto(int Version, DateOnly Vence);

public sealed record BovedaGuardadoDto(Guid Token);
