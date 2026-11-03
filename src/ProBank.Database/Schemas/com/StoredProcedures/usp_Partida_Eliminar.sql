-- Solo se eliminan partidas en borrador que nadie usa (movimientos, renovaciones, solicitudes o pagos).
CREATE PROCEDURE com.usp_Partida_Eliminar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'presupuesto', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para eliminar partidas.', 1;
    BEGIN TRAN;
    DECLARE @PartidaId INT, @Codigo VARCHAR(20), @Estado VARCHAR(20), @Rubro NVARCHAR(150);
    SELECT @PartidaId = PartidaId, @Codigo = Codigo, @Estado = Estado, @Rubro = Rubro FROM com.Partida WITH (UPDLOCK) WHERE Token = @Token;
    IF @PartidaId IS NULL THROW 50404, N'La partida no existe.', 1;
    IF @Estado <> 'Borrador' THROW 50422, N'Solo se eliminan partidas en borrador.', 1;
    IF EXISTS (SELECT 1 FROM com.Movimiento WHERE PartidaOrigenId = @PartidaId OR PartidaDestinoId = @PartidaId)
       OR EXISTS (SELECT 1 FROM com.Renovacion WHERE PartidaId = @PartidaId)
       OR EXISTS (SELECT 1 FROM com.Solicitud WHERE PartidaId = @PartidaId)
       OR EXISTS (SELECT 1 FROM com.PagoDistribucion WHERE PartidaId = @PartidaId)
        THROW 50422, N'La partida ya se usa en movimientos, renovaciones, solicitudes o pagos.', 1;
    DELETE com.Partida WHERE PartidaId = @PartidaId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', N'Eliminación de la partida ' + @Codigo, @Rubro, @Ip);
    COMMIT;
END;
