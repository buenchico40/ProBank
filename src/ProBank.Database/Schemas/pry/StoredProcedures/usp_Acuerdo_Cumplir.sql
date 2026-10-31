-- Marca un acuerdo como cumplido (PRY-09: «Marcar acuerdo cumplido»). El responsable del acuerdo también puede hacerlo.
CREATE PROCEDURE pry.usp_Acuerdo_Cumplir
    @AcuerdoId INT, @Hoy DATE, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20), @ResponsableId INT;
    SELECT @Codigo = Codigo, @Estado = Estado, @ResponsableId = ResponsableId FROM pry.Acuerdo WHERE AcuerdoId = @AcuerdoId;
    IF @Codigo IS NULL THROW 50404, N'El acuerdo no existe.', 1;
    IF @ResponsableId <> @ActorId AND (seg.fn_TienePermiso(@ActorId, @RolActivo, 'acuerdos', 'cumplir') = 0 OR pry.fn_PuedeAccion(@RolActivo, 'cumplir_acuerdo') = 0)
        THROW 50403, N'Tu rol activo no está habilitado para «Marcar acuerdo cumplido».', 1;
    IF @Estado = 'Cumplido' THROW 50422, N'El acuerdo ya está cumplido.', 1;
    BEGIN TRAN;
    UPDATE pry.Acuerdo SET Estado = 'Cumplido', FechaCumplido = @Hoy WHERE AcuerdoId = @AcuerdoId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Acuerdo ' + @Codigo + N' cumplido', NULL, @Ip);
    COMMIT;
END;
