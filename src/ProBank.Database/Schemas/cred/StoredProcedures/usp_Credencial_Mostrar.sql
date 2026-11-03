-- CRE-02: entrega el valor vigente de una cuenta técnica (@Clase 'cuenta') o de un secreto ('secreto') para mostrarlo o copiarlo.
-- Exige el permiso propio (mostrar o copiar) y ser miembro del grupo de acceso; cada entrega queda en cred.CredencialAcceso y en la
-- auditoría antes de devolver el valor. El valor sale cifrado (Always Encrypted): solo la API, que tiene la llave, lo descifra.
CREATE PROCEDURE cred.usp_Credencial_Mostrar
    @Clase VARCHAR(10), @Token UNIQUEIDENTIFIER, @Accion VARCHAR(10),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @Clase NOT IN ('cuenta', 'secreto') OR @Accion NOT IN ('Mostrar', 'Copiar') THROW 50422, N'Solicitud no válida.', 1;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, CASE @Clase WHEN 'cuenta' THEN 'cuentas' ELSE 'secretos' END, LOWER(@Accion)) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para esta acción sobre la credencial.', 1;

    DECLARE @CuentaId INT, @SecretoId INT, @GrupoId INT, @Nombre NVARCHAR(150), @Grupo VARCHAR(10);
    IF @Clase = 'cuenta'
        SELECT @CuentaId = c.CuentaId, @GrupoId = c.GrupoId, @Nombre = c.Nombre FROM cred.CuentaTecnica c WHERE c.Token = @Token;
    ELSE
        SELECT @SecretoId = s.SecretoId, @GrupoId = s.GrupoId, @Nombre = s.Nombre FROM cred.Secreto s WHERE s.Token = @Token;
    IF @Nombre IS NULL THROW 50404, N'La credencial no existe.', 1;
    SET @Grupo = (SELECT Codigo FROM cred.GrupoAcceso WHERE GrupoId = @GrupoId);
    IF @GrupoId IS NULL OR cred.fn_EsMiembro(@ActorId, @GrupoId) = 0
    BEGIN
        INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
        VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Bóveda', N'Acceso denegado a la credencial ' + @Nombre,
                CONCAT(@Accion, N' · no es miembro del grupo ', ISNULL(@Grupo, N'—')), @Ip);
        DECLARE @Msg NVARCHAR(200) = CONCAT(N'Solo los miembros del grupo ', ISNULL(@Grupo, N'de acceso'), N' pueden ver o copiar esta credencial.');
        THROW 50403, @Msg, 1;
    END;

    DECLARE @VersionId BIGINT, @Version INT;
    SELECT TOP (1) @VersionId = VersionId, @Version = Version FROM cred.CredencialVersion
    WHERE ((@CuentaId IS NOT NULL AND CuentaId = @CuentaId) OR (@SecretoId IS NOT NULL AND SecretoId = @SecretoId)) AND Valor IS NOT NULL
    ORDER BY Version DESC;
    IF @VersionId IS NULL THROW 50404, N'La credencial aún no tiene un valor guardado.', 1;

    BEGIN TRAN;
    INSERT cred.CredencialAcceso (CuentaId, SecretoId, Version, Accion, UsuarioId, Ip) VALUES (@CuentaId, @SecretoId, @Version, @Accion, @ActorId, @Ip);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Bóveda',
            CASE @Accion WHEN 'Mostrar' THEN N'Consulta de credencial ' ELSE N'Copia de credencial ' END + @Nombre,
            CONCAT(CASE @Clase WHEN 'cuenta' THEN N'Cuenta técnica' ELSE N'Secreto' END, N' · versión ', @Version, N' · grupo ', @Grupo), @Ip);
    COMMIT;

    SELECT Valor, Version FROM cred.CredencialVersion WHERE VersionId = @VersionId;
END;
