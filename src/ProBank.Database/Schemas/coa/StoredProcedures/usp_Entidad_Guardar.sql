-- Catálogo de entidades (RUC único). @EntidadId NULL = nueva.
CREATE PROCEDURE coa.usp_Entidad_Guardar
    @EntidadId INT = NULL, @Ruc VARCHAR(13), @Nombre NVARCHAR(150), @CuentaDestino NVARCHAR(150) = NULL, @Estado VARCHAR(20),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'oficios', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para administrar entidades.', 1;
    IF EXISTS (SELECT 1 FROM coa.Entidad WHERE Ruc = @Ruc AND EntidadId <> ISNULL(@EntidadId, 0))
        THROW 50409, N'Ya existe una entidad con ese RUC.', 1;
    BEGIN TRAN;
    IF @EntidadId IS NULL
    BEGIN
        INSERT coa.Entidad (Ruc, Nombre, CuentaDestino, Estado) VALUES (@Ruc, @Nombre, NULLIF(@CuentaDestino, N''), @Estado);
        SET @Id = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE coa.Entidad SET Ruc = @Ruc, Nombre = @Nombre, CuentaDestino = NULLIF(@CuentaDestino, N''), Estado = @Estado WHERE EntidadId = @EntidadId;
        IF @@ROWCOUNT = 0 THROW 50404, N'La entidad no existe.', 1;
        SET @Id = @EntidadId;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Coactivas', CASE WHEN @EntidadId IS NULL THEN N'Nueva entidad' ELSE N'Edición de entidad' END,
            CONCAT(@Ruc, N' · ', @Nombre), @Ip);
    COMMIT;
END;
