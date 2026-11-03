-- Retira un servicio que ya no se renovará: deja de avisar.
CREATE PROCEDURE com.usp_Renovacion_Retirar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'renovaciones', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para retirar renovaciones.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(30), @Servicio NVARCHAR(150);
    SELECT @Codigo = Codigo, @Estado = Estado, @Servicio = Servicio FROM com.Renovacion WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'La renovación no existe.', 1;
    IF @Estado = 'Retirado' THROW 50422, N'La renovación ya está retirada.', 1;
    UPDATE com.Renovacion SET Estado = 'Retirado' WHERE Token = @Token;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', N'Retiro de la renovación ' + @Codigo, @Servicio, @Ip);
    COMMIT;
END;
