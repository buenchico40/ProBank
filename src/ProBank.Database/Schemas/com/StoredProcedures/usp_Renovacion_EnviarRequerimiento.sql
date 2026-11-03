-- Envía el requerimiento de compra de una renovación: crea la solicitud (con su partida o no presupuestada) y la renovación
-- queda «Requerimiento enviado». Devuelve el código de la solicitud.
CREATE PROCEDURE com.usp_Renovacion_EnviarRequerimiento
    @Token UNIQUEIDENTIFIER, @Area NVARCHAR(60), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Solicitud VARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'renovaciones', 'crear') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para enviar requerimientos de compra.', 1;
    BEGIN TRAN;
    DECLARE @Id INT, @Codigo VARCHAR(20), @Estado VARCHAR(30), @Servicio NVARCHAR(150), @Proveedor NVARCHAR(150), @Monto DECIMAL(18,2),
            @Presupuestado BIT, @PartidaId INT;
    SELECT @Id = RenovacionId, @Codigo = Codigo, @Estado = Estado, @Servicio = Servicio, @Proveedor = Proveedor, @Monto = MontoEstimado,
           @Presupuestado = Presupuestado, @PartidaId = PartidaId
    FROM com.Renovacion WITH (UPDLOCK) WHERE Token = @Token;
    IF @Id IS NULL THROW 50404, N'La renovación no existe.', 1;
    IF @Estado <> 'Activo' THROW 50422, N'La renovación ya tiene requerimiento enviado o está retirada.', 1;
    IF @Monto <= 0 THROW 50422, N'Indique el monto estimado de la renovación.', 1;
    IF @Presupuestado = 1 AND NOT EXISTS (SELECT 1 FROM com.Partida WHERE PartidaId = @PartidaId AND Estado = 'Activo')
        THROW 50422, N'La partida de la renovación debe estar activa para pedir la compra.', 1;
    INSERT com.Solicitud (Descripcion, Area, Proveedor, Presupuestado, PartidaId, Monto, RenovacionId, SolicitanteId)
    VALUES (N'Renovación: ' + @Servicio, @Area, @Proveedor, @Presupuestado, @PartidaId, @Monto, @Id, @ActorId);
    SET @Solicitud = (SELECT Codigo FROM com.Solicitud WHERE SolicitudId = SCOPE_IDENTITY());
    UPDATE com.Renovacion SET Estado = 'Requerimiento enviado' WHERE RenovacionId = @Id;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', N'Requerimiento de compra de ' + @Codigo, CONCAT(@Solicitud, N' · ', @Servicio, N' · ', @Monto), @Ip);
    COMMIT;
END;
