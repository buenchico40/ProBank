-- Anula una solicitud sin pagos realizados ni pendientes: libera su comprometido. Si venía de una renovación, la renovación
-- vuelve a Activo para poder pedir la compra de nuevo.
CREATE PROCEDURE com.usp_Solicitud_Anular
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'solicitudes', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para anular solicitudes de compra.', 1;
    BEGIN TRAN;
    DECLARE @Id INT, @Codigo VARCHAR(20), @Anulada BIT, @RenovacionId INT;
    SELECT @Id = SolicitudId, @Codigo = Codigo, @Anulada = Anulada, @RenovacionId = RenovacionId FROM com.Solicitud WITH (UPDLOCK) WHERE Token = @Token;
    IF @Id IS NULL THROW 50404, N'La solicitud no existe.', 1;
    IF @Anulada = 1 THROW 50422, N'La solicitud ya está anulada.', 1;
    IF EXISTS (SELECT 1 FROM com.Pago WHERE SolicitudId = @Id AND Estado <> 'Anulado')
        THROW 50422, N'La solicitud tiene pagos registrados: anúlelos antes.', 1;
    UPDATE com.Solicitud SET Anulada = 1 WHERE SolicitudId = @Id;
    IF @RenovacionId IS NOT NULL
        UPDATE com.Renovacion SET Estado = 'Activo' WHERE RenovacionId = @RenovacionId AND Estado = 'Requerimiento enviado';
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', N'Anulación de la solicitud ' + @Codigo, NULL, @Ip);
    COMMIT;
END;
