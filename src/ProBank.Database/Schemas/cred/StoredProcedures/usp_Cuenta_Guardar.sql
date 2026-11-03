-- CRE-01: crea (@Token NULL) o edita una cuenta técnica; la contraseña se cambia aparte (usp_Credencial_Cambiar).
-- Cambiar el grupo de una cuenta cambia quién ve su contraseña: solo lo hace un miembro del grupo actual (CRE-02).
-- La identidad administrada de Azure no tiene contraseña, grupo ni rotación.
CREATE PROCEDURE cred.usp_Cuenta_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Nombre NVARCHAR(150), @Tipo NVARCHAR(60), @Sistema NVARCHAR(150), @Responsable NVARCHAR(100),
    @Privilegios NVARCHAR(300) = NULL, @GrupoToken UNIQUEIDENTIFIER = NULL, @ProximaRotacion DATE = NULL, @Activo BIT,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'cuentas', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar cuentas técnicas.', 1;
    DECLARE @Identidad BIT = CASE WHEN @Tipo = N'Identidad administrada de Azure' THEN 1 ELSE 0 END;
    DECLARE @GrupoId INT = (SELECT GrupoId FROM cred.GrupoAcceso WHERE Token = @GrupoToken AND Estado = 'Activo');
    IF @Identidad = 0 AND @GrupoId IS NULL THROW 50422, N'Elija un grupo de acceso activo: solo sus miembros podrán ver la contraseña.', 1;
    IF @Identidad = 1 SELECT @GrupoId = NULL, @ProximaRotacion = NULL;

    BEGIN TRAN;
    IF EXISTS (SELECT 1 FROM cred.CuentaTecnica WITH (UPDLOCK, HOLDLOCK) WHERE Nombre = @Nombre AND (@Token IS NULL OR Token <> @Token))
        THROW 50409, N'Ya existe una cuenta técnica con ese nombre.', 1;
    IF @Token IS NULL
    BEGIN
        SET @TokenGuardado = NEWID();
        INSERT cred.CuentaTecnica (Token, Nombre, Tipo, Sistema, Responsable, Privilegios, GrupoId, ProximaRotacion, Activo)
        VALUES (@TokenGuardado, @Nombre, @Tipo, @Sistema, @Responsable, @Privilegios, @GrupoId, @ProximaRotacion, @Activo);
    END
    ELSE
    BEGIN
        DECLARE @Id INT, @GrupoActual INT;
        SELECT @Id = CuentaId, @GrupoActual = GrupoId FROM cred.CuentaTecnica WITH (UPDLOCK) WHERE Token = @Token;
        IF @Id IS NULL THROW 50404, N'La cuenta técnica no existe.', 1;
        IF @GrupoActual IS NOT NULL AND ISNULL(@GrupoId, 0) <> @GrupoActual AND cred.fn_EsMiembro(@ActorId, @GrupoActual) = 0
            THROW 50403, N'Solo un miembro del grupo de acceso actual puede cambiar el grupo de esta cuenta.', 1;
        IF @Identidad = 1 AND EXISTS (SELECT 1 FROM cred.CredencialVersion WHERE CuentaId = @Id AND Valor IS NOT NULL)
            THROW 50409, N'La cuenta tiene una contraseña guardada: una identidad administrada no tiene contraseña.', 1;
        UPDATE cred.CuentaTecnica SET Nombre = @Nombre, Tipo = @Tipo, Sistema = @Sistema, Responsable = @Responsable, Privilegios = @Privilegios,
               GrupoId = @GrupoId, ProximaRotacion = @ProximaRotacion, Activo = @Activo
        WHERE CuentaId = @Id;
        SET @TokenGuardado = @Token;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Bóveda',
            CASE WHEN @Token IS NULL THEN N'Nueva cuenta técnica ' ELSE N'Edición de la cuenta técnica ' END + @Nombre,
            CONCAT(@Tipo, N' · ', @Sistema, N' · grupo ', ISNULL((SELECT Codigo FROM cred.GrupoAcceso WHERE GrupoId = @GrupoId), N'—'),
                   N' · próxima rotación ', ISNULL(CONVERT(VARCHAR(10), @ProximaRotacion, 103), N'no aplica'), CASE WHEN @Activo = 0 THEN N' · inactiva' END), @Ip);
    COMMIT;
END;
