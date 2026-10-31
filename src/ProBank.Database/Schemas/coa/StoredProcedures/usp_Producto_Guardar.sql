-- Registra o actualiza un producto del cliente de la coactiva con su saldo referencial (sin core bancario en esta fase).
-- El saldo no puede quedar por debajo de lo bloqueado vigente en la cuenta.
CREATE PROCEDURE coa.usp_Producto_Guardar
    @CoactivaToken UNIQUEIDENTIFIER, @ProductoId INT = NULL, @Tipo VARCHAR(40), @Numero VARCHAR(30), @Saldo DECIMAL(18,2),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'coactivas', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar productos del cliente.', 1;
    DECLARE @ClienteId INT, @Cliente NVARCHAR(150);
    SELECT @ClienteId = c.ClienteId, @Cliente = cl.Nombre FROM coa.Coactiva c JOIN coa.Cliente cl ON cl.ClienteId = c.ClienteId WHERE c.Token = @CoactivaToken;
    IF @ClienteId IS NULL THROW 50404, N'La coactiva no existe.', 1;
    IF EXISTS (SELECT 1 FROM coa.Producto WHERE ClienteId = @ClienteId AND Numero = @Numero AND ProductoId <> ISNULL(@ProductoId, 0))
        THROW 50409, N'El cliente ya tiene un producto con ese número de cuenta.', 1;
    BEGIN TRAN;
    IF @ProductoId IS NULL
        INSERT coa.Producto (ClienteId, Tipo, Numero, SaldoReferencial) VALUES (@ClienteId, @Tipo, @Numero, @Saldo);
    ELSE
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM coa.Producto WITH (UPDLOCK) WHERE ProductoId = @ProductoId AND ClienteId = @ClienteId)
            THROW 50404, N'El producto no existe.', 1;
        IF @Saldo < ISNULL((SELECT SUM(Vigente) FROM coa.vw_Bloqueo WHERE ProductoId = @ProductoId), 0)
            THROW 50422, N'El saldo no puede ser menor que lo bloqueado en la cuenta.', 1;
        UPDATE coa.Producto SET Tipo = @Tipo, Numero = @Numero, SaldoReferencial = @Saldo WHERE ProductoId = @ProductoId;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Coactivas', CASE WHEN @ProductoId IS NULL THEN N'Registro de producto del cliente' ELSE N'Edición de producto del cliente' END,
            CONCAT(@Cliente, N' · ', @Tipo, N' ', @Numero, N' · saldo ', @Saldo), @Ip);
    COMMIT;
END;
