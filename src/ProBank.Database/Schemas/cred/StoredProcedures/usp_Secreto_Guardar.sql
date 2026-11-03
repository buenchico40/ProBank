-- CRE-01: crea (@Token NULL) o edita un secreto; el valor se cambia aparte (usp_Credencial_Cambiar).
-- Cambiar el grupo de un secreto cambia quién ve su valor: solo lo hace un miembro del grupo actual (CRE-02).
CREATE PROCEDURE cred.usp_Secreto_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Nombre NVARCHAR(150), @Tipo NVARCHAR(60), @Almacen NVARCHAR(100), @Sistema NVARCHAR(150),
    @GrupoToken UNIQUEIDENTIFIER, @DiasAviso INT, @Expira DATE = NULL, @Activo BIT,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'secretos', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar secretos.', 1;
    DECLARE @GrupoId INT = (SELECT GrupoId FROM cred.GrupoAcceso WHERE Token = @GrupoToken AND Estado = 'Activo');
    IF @GrupoId IS NULL THROW 50422, N'Elija un grupo de acceso activo: solo sus miembros podrán ver el valor.', 1;

    BEGIN TRAN;
    IF EXISTS (SELECT 1 FROM cred.Secreto WITH (UPDLOCK, HOLDLOCK) WHERE Almacen = @Almacen AND Nombre = @Nombre AND (@Token IS NULL OR Token <> @Token))
        THROW 50409, N'Ya existe un secreto con ese nombre en ese almacén.', 1;
    IF @Token IS NULL
    BEGIN
        SET @TokenGuardado = NEWID();
        INSERT cred.Secreto (Token, Nombre, Tipo, Almacen, Sistema, GrupoId, DiasAviso, Expira, Activo)
        VALUES (@TokenGuardado, @Nombre, @Tipo, @Almacen, @Sistema, @GrupoId, @DiasAviso, @Expira, @Activo);
    END
    ELSE
    BEGIN
        DECLARE @Id INT, @GrupoActual INT;
        SELECT @Id = SecretoId, @GrupoActual = GrupoId FROM cred.Secreto WITH (UPDLOCK) WHERE Token = @Token;
        IF @Id IS NULL THROW 50404, N'El secreto no existe.', 1;
        IF @GrupoId <> @GrupoActual AND cred.fn_EsMiembro(@ActorId, @GrupoActual) = 0
            THROW 50403, N'Solo un miembro del grupo de acceso actual puede cambiar el grupo de este secreto.', 1;
        UPDATE cred.Secreto SET Nombre = @Nombre, Tipo = @Tipo, Almacen = @Almacen, Sistema = @Sistema, GrupoId = @GrupoId,
               DiasAviso = @DiasAviso, Expira = @Expira, Activo = @Activo
        WHERE SecretoId = @Id;
        SET @TokenGuardado = @Token;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Bóveda',
            CASE WHEN @Token IS NULL THEN N'Nuevo secreto ' ELSE N'Edición del secreto ' END + @Nombre,
            CONCAT(@Tipo, N' · ', @Almacen, N' · ', @Sistema, N' · grupo ', (SELECT Codigo FROM cred.GrupoAcceso WHERE GrupoId = @GrupoId),
                   N' · expira ', ISNULL(CONVERT(VARCHAR(10), @Expira, 103), N'—'), N' · aviso ', @DiasAviso, N' días',
                   CASE WHEN @Activo = 0 THEN N' · inactivo' END), @Ip);
    COMMIT;
END;
