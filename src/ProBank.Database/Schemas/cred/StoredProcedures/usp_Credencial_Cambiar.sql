-- CRE-01 y CRE-03: guarda una versión nueva de la contraseña de una cuenta técnica (@Clase 'cuenta') o del valor de un secreto
-- ('secreto'). @Valor llega cifrado por el driver (Always Encrypted): el SP solo lo inserta, nunca lo lee ni lo compara.
-- Solo un miembro del grupo de acceso lo cambia; la versión anterior queda en el historial sin su valor.
-- @Vence: próxima rotación (cuenta) o expiración (secreto).
CREATE PROCEDURE cred.usp_Credencial_Cambiar
    @Clase VARCHAR(10), @Token UNIQUEIDENTIFIER, @Valor NVARCHAR(4000), @Vence DATE = NULL, @Nota NVARCHAR(200) = NULL, @Hoy DATE,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @VersionNueva INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @Clase NOT IN ('cuenta', 'secreto') THROW 50422, N'Solicitud no válida.', 1;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, CASE @Clase WHEN 'cuenta' THEN 'cuentas' ELSE 'secretos' END, 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para cambiar credenciales.', 1;
    IF @Vence IS NOT NULL AND @Vence <= @Hoy THROW 50422, N'La fecha de vencimiento debe ser posterior a hoy.', 1;

    BEGIN TRAN;
    DECLARE @CuentaId INT, @SecretoId INT, @GrupoId INT, @Nombre NVARCHAR(150), @Activo BIT, @Tipo NVARCHAR(60);
    IF @Clase = 'cuenta'
        SELECT @CuentaId = CuentaId, @GrupoId = GrupoId, @Nombre = Nombre, @Activo = Activo, @Tipo = Tipo FROM cred.CuentaTecnica WITH (UPDLOCK) WHERE Token = @Token;
    ELSE
        SELECT @SecretoId = SecretoId, @GrupoId = GrupoId, @Nombre = Nombre, @Activo = Activo, @Tipo = Tipo FROM cred.Secreto WITH (UPDLOCK) WHERE Token = @Token;
    IF @Nombre IS NULL THROW 50404, N'La credencial no existe.', 1;
    IF @Tipo = N'Identidad administrada de Azure' THROW 50409, N'Las identidades administradas no tienen contraseña que guardar.', 1;
    IF @Activo = 0 THROW 50409, N'La credencial está inactiva: actívela antes de cambiar su valor.', 1;
    IF cred.fn_EsMiembro(@ActorId, @GrupoId) = 0
    BEGIN
        DECLARE @Msg NVARCHAR(200) = CONCAT(N'Solo los miembros del grupo ', (SELECT Codigo FROM cred.GrupoAcceso WHERE GrupoId = @GrupoId), N' pueden cambiar esta credencial.');
        THROW 50403, @Msg, 1;
    END;

    SET @VersionNueva = 1 + ISNULL((SELECT MAX(Version) FROM cred.CredencialVersion WITH (UPDLOCK, HOLDLOCK)
                                    WHERE (@CuentaId IS NOT NULL AND CuentaId = @CuentaId) OR (@SecretoId IS NOT NULL AND SecretoId = @SecretoId)), 0);
    UPDATE cred.CredencialVersion SET Valor = NULL
    WHERE ((@CuentaId IS NOT NULL AND CuentaId = @CuentaId) OR (@SecretoId IS NOT NULL AND SecretoId = @SecretoId)) AND Valor IS NOT NULL;
    INSERT cred.CredencialVersion (CuentaId, SecretoId, Version, Valor, Vence, UsuarioId, Nota)
    VALUES (@CuentaId, @SecretoId, @VersionNueva, @Valor, @Vence, @ActorId, @Nota);
    IF @Clase = 'cuenta' UPDATE cred.CuentaTecnica SET UltimaRotacion = @Hoy, ProximaRotacion = @Vence WHERE CuentaId = @CuentaId;
    ELSE UPDATE cred.Secreto SET UltimaRotacion = @Hoy, Expira = @Vence WHERE SecretoId = @SecretoId;

    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Bóveda', CONCAT(N'Cambio de credencial ', @Nombre, N' (versión ', @VersionNueva, N')'),
            CONCAT(CASE @Clase WHEN 'cuenta' THEN N'Cuenta técnica · próxima rotación ' ELSE N'Secreto · expira ' END,
                   ISNULL(CONVERT(VARCHAR(10), @Vence, 103), N'—'), N' · ' + @Nota), @Ip);
    COMMIT;
END;
