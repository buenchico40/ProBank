-- Edita monto, productos permitidos (COA-03) y observación. El monto no puede quedar por debajo de lo ya bloqueado y transferido.
CREATE PROCEDURE coa.usp_Coactiva_Actualizar
    @Token UNIQUEIDENTIFIER, @Monto DECIMAL(18,2), @Productos VARCHAR(200) = NULL, @Observacion NVARCHAR(500) = NULL,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'coactivas', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar coactivas.', 1;
    BEGIN TRAN;
    DECLARE @CoactivaId INT, @Proceso VARCHAR(50), @Anulada BIT, @Anterior DECIMAL(18,2);
    SELECT @CoactivaId = CoactivaId, @Proceso = NumeroProceso, @Anulada = Anulada, @Anterior = Monto FROM coa.Coactiva WITH (UPDLOCK) WHERE Token = @Token;
    IF @CoactivaId IS NULL THROW 50404, N'La coactiva no existe.', 1;
    IF @Anulada = 1 THROW 50422, N'La coactiva está anulada.', 1;
    IF @Monto < (SELECT Bloqueado + Transferido FROM coa.vw_Coactiva WHERE CoactivaId = @CoactivaId)
        THROW 50422, N'El monto no puede ser menor que lo bloqueado y transferido.', 1;
    UPDATE coa.Coactiva SET Monto = @Monto, ProductosPermitidos = NULLIF(@Productos, ''), Observacion = @Observacion WHERE CoactivaId = @CoactivaId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Coactivas', N'Edición de coactiva ' + @Proceso,
            CONCAT(N'Monto ', @Anterior, N' → ', @Monto, N' · productos: ', ISNULL(NULLIF(@Productos, ''), N'todos')), @Ip);
    COMMIT;
END;
