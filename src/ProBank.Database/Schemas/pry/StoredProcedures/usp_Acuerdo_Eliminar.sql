-- Elimina un acuerdo pendiente (registrado por error). Los cumplidos se conservan para los informes.
CREATE PROCEDURE pry.usp_Acuerdo_Eliminar
    @AcuerdoId INT, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'acuerdos', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para eliminar acuerdos.', 1;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20), @Descripcion NVARCHAR(500);
    SELECT @Codigo = Codigo, @Estado = Estado, @Descripcion = Descripcion FROM pry.Acuerdo WHERE AcuerdoId = @AcuerdoId;
    IF @Codigo IS NULL THROW 50404, N'El acuerdo no existe.', 1;
    IF @Estado = 'Cumplido' THROW 50422, N'Un acuerdo cumplido no se elimina.', 1;
    BEGIN TRAN;
    DELETE pry.Acuerdo WHERE AcuerdoId = @AcuerdoId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Eliminación del acuerdo ' + @Codigo, @Descripcion, @Ip);
    COMMIT;
END;
