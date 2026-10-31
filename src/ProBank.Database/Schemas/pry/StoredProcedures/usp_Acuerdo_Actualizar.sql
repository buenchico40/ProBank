-- Edita un acuerdo pendiente (descripción, responsable y fecha de cumplimiento).
CREATE PROCEDURE pry.usp_Acuerdo_Actualizar
    @AcuerdoId INT, @Descripcion NVARCHAR(500), @ResponsableId INT, @FechaCumplimiento DATE, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'acuerdos', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar acuerdos.', 1;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20);
    SELECT @Codigo = Codigo, @Estado = Estado FROM pry.Acuerdo WHERE AcuerdoId = @AcuerdoId;
    IF @Codigo IS NULL THROW 50404, N'El acuerdo no existe.', 1;
    IF @Estado = 'Cumplido' THROW 50422, N'El acuerdo ya está cumplido.', 1;
    IF NOT EXISTS (SELECT 1 FROM seg.Usuario WHERE UsuarioId = @ResponsableId AND Estado = 'Activo') THROW 50422, N'El responsable no existe o está inactivo.', 1;
    BEGIN TRAN;
    UPDATE pry.Acuerdo SET Descripcion = @Descripcion, ResponsableId = @ResponsableId, FechaCumplimiento = @FechaCumplimiento WHERE AcuerdoId = @AcuerdoId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Edición del acuerdo ' + @Codigo, @Descripcion, @Ip);
    COMMIT;
END;
