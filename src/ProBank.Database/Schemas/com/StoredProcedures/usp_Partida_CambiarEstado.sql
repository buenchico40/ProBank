-- COM-01: Aprobar (Borrador → Activo: ya recibe solicitudes, renovaciones y pagos) o Cerrar (Activo → Cerrado, fin del año).
CREATE PROCEDURE com.usp_Partida_CambiarEstado
    @Token UNIQUEIDENTIFIER, @Accion VARCHAR(10), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'presupuesto', 'aprobar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para aprobar o cerrar partidas.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20), @Rubro NVARCHAR(150);
    SELECT @Codigo = Codigo, @Estado = Estado, @Rubro = Rubro FROM com.Partida WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'La partida no existe.', 1;
    IF @Accion = 'Aprobar'
    BEGIN
        IF @Estado <> 'Borrador' THROW 50422, N'Solo se aprueban partidas en borrador.', 1;
        UPDATE com.Partida SET Estado = 'Activo', AprobadoPorId = @ActorId, FechaAprobacion = SYSDATETIME() WHERE Token = @Token;
    END
    ELSE IF @Accion = 'Cerrar'
    BEGIN
        IF @Estado <> 'Activo' THROW 50422, N'Solo se cierran partidas activas.', 1;
        UPDATE com.Partida SET Estado = 'Cerrado' WHERE Token = @Token;
    END
    ELSE THROW 50422, N'Acción no válida.', 1;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CASE @Accion WHEN 'Aprobar' THEN N'Aprobación de la partida ' ELSE N'Cierre de la partida ' END + @Codigo, @Rubro, @Ip);
    COMMIT;
END;
