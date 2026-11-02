-- Edita monto, productos permitidos (COA-03) y observación. El monto no puede quedar por debajo de lo ya bloqueado y transferido.
-- @Cliente: corrige el nombre del cliente (aplica a todas sus coactivas). @EntidadId: corrige la entidad (activa y sin repetir el proceso).
CREATE PROCEDURE coa.usp_Coactiva_Actualizar
    @Token UNIQUEIDENTIFIER, @Monto DECIMAL(18,2), @Productos VARCHAR(200) = NULL, @Observacion NVARCHAR(500) = NULL,
    @Cliente NVARCHAR(150) = NULL, @EntidadId INT = NULL,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'coactivas', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar coactivas.', 1;
    BEGIN TRAN;
    DECLARE @CoactivaId INT, @Proceso VARCHAR(50), @Anulada BIT, @Anterior DECIMAL(18,2), @ClienteId INT, @EntidadAnterior INT;
    SELECT @CoactivaId = CoactivaId, @Proceso = NumeroProceso, @Anulada = Anulada, @Anterior = Monto, @ClienteId = ClienteId, @EntidadAnterior = EntidadId
    FROM coa.Coactiva WITH (UPDLOCK) WHERE Token = @Token;
    IF @CoactivaId IS NULL THROW 50404, N'La coactiva no existe.', 1;
    IF @Anulada = 1 THROW 50422, N'La coactiva está anulada.', 1;
    IF @Monto < (SELECT Bloqueado + Transferido FROM coa.vw_Coactiva WHERE CoactivaId = @CoactivaId)
        THROW 50422, N'El monto no puede ser menor que lo bloqueado y transferido.', 1;
    SET @EntidadId = ISNULL(@EntidadId, @EntidadAnterior);
    IF @EntidadId <> @EntidadAnterior
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM coa.Entidad WHERE EntidadId = @EntidadId AND Estado = 'Activo') THROW 50422, N'La entidad no existe o está inactiva.', 1;
        IF EXISTS (SELECT 1 FROM coa.Coactiva WHERE EntidadId = @EntidadId AND NumeroProceso = @Proceso)
            THROW 50409, N'Ese número de proceso ya existe para la entidad.', 1;
    END;
    UPDATE coa.Coactiva SET Monto = @Monto, ProductosPermitidos = NULLIF(@Productos, ''), Observacion = @Observacion, EntidadId = @EntidadId
    WHERE CoactivaId = @CoactivaId;
    IF NULLIF(@Cliente, N'') IS NOT NULL UPDATE coa.Cliente SET Nombre = @Cliente WHERE ClienteId = @ClienteId AND Nombre <> @Cliente;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Coactivas', N'Edición de coactiva ' + @Proceso,
            CONCAT(N'Monto ', @Anterior, N' → ', @Monto, N' · productos: ', ISNULL(NULLIF(@Productos, ''), N'todos'),
                   CASE WHEN @EntidadId <> @EntidadAnterior THEN CONCAT(N' · entidad ', @EntidadAnterior, N' → ', @EntidadId) END,
                   CASE WHEN NULLIF(@Cliente, N'') IS NOT NULL THEN N' · cliente: ' + @Cliente END), @Ip);
    COMMIT;
END;
