-- Registra la renovación del servicio con su nueva fecha de vencimiento: vuelve a Activo (y a avisar según sus días de aviso).
CREATE PROCEDURE com.usp_Renovacion_Renovar
    @Token UNIQUEIDENTIFIER, @NuevoVence DATE, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'renovaciones', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar renovaciones.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(30), @Vence DATE;
    SELECT @Codigo = Codigo, @Estado = Estado, @Vence = Vence FROM com.Renovacion WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'La renovación no existe.', 1;
    IF @Estado = 'Retirado' THROW 50422, N'La renovación está retirada.', 1;
    IF @NuevoVence <= @Vence THROW 50422, N'El nuevo vencimiento debe ser posterior al actual.', 1;
    UPDATE com.Renovacion SET Vence = @NuevoVence, Estado = 'Activo' WHERE Token = @Token;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', N'Renovación registrada de ' + @Codigo,
            CONCAT(N'Vence ', CONVERT(CHAR(10), @Vence, 103), N' → ', CONVERT(CHAR(10), @NuevoVence, 103)), @Ip);
    COMMIT;
END;
