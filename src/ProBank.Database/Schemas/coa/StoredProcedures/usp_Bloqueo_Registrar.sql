-- Bloqueo de fondos en un producto del cliente (orden de bloqueo del oficio que trajo la coactiva).
-- COA-03: solo en los tipos de producto permitidos. El monto no supera lo que falta cubrir ni lo disponible en la cuenta.
CREATE PROCEDURE coa.usp_Bloqueo_Registrar
    @CoactivaToken UNIQUEIDENTIFIER, @ProductoId INT, @Monto DECIMAL(18,2), @Observacion NVARCHAR(500) = NULL, @Hoy DATE,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Codigo VARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'coactivas', 'bloquear') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para bloquear fondos.', 1;
    BEGIN TRAN;
    DECLARE @CoactivaId INT, @ClienteId INT, @OficioId INT, @Proceso VARCHAR(50), @Anulada BIT, @Montocoa DECIMAL(18,2), @Permitidos VARCHAR(200);
    SELECT @CoactivaId = CoactivaId, @ClienteId = ClienteId, @OficioId = OficioId, @Proceso = NumeroProceso, @Anulada = Anulada,
           @Montocoa = Monto, @Permitidos = ProductosPermitidos
    FROM coa.Coactiva WITH (UPDLOCK, HOLDLOCK) WHERE Token = @CoactivaToken;
    IF @CoactivaId IS NULL THROW 50404, N'La coactiva no existe.', 1;
    IF @Anulada = 1 THROW 50422, N'La coactiva está anulada.', 1;

    DECLARE @Tipo VARCHAR(40), @Numero VARCHAR(30), @Saldo DECIMAL(18,2);
    SELECT @Tipo = Tipo, @Numero = Numero, @Saldo = SaldoReferencial FROM coa.Producto WITH (UPDLOCK, HOLDLOCK) WHERE ProductoId = @ProductoId AND ClienteId = @ClienteId;
    IF @Tipo IS NULL THROW 50404, N'El producto no pertenece al cliente de la coactiva.', 1;
    IF @Permitidos IS NOT NULL AND @Tipo NOT IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@Permitidos, ','))
        THROW 50422, N'La orden no permite bloquear ese tipo de producto.', 1;

    DECLARE @Falta DECIMAL(18,2) = @Montocoa - (SELECT Bloqueado + Transferido FROM coa.vw_Coactiva WHERE CoactivaId = @CoactivaId);
    DECLARE @Disponible DECIMAL(18,2) = @Saldo - ISNULL((SELECT SUM(Vigente) FROM coa.vw_Bloqueo WHERE ProductoId = @ProductoId), 0);
    IF @Monto > @Falta THROW 50422, N'El monto supera lo que falta cubrir de la coactiva.', 1;
    IF @Monto > @Disponible THROW 50422, N'El monto supera lo disponible en la cuenta.', 1;

    INSERT coa.Bloqueo (CoactivaId, ProductoId, Monto, Fecha, UsuarioId) VALUES (@CoactivaId, @ProductoId, @Monto, @Hoy, @ActorId);
    DECLARE @BloqueoId INT = SCOPE_IDENTITY();
    INSERT coa.Movimiento (BloqueoId, OrdenId, Tipo, Monto, Fecha, Detalle, UsuarioId)
    SELECT @BloqueoId, OrdenId, 'Bloqueo', @Monto, @Hoy, @Observacion, @ActorId
    FROM coa.OrdenCoactiva WHERE OficioId = @OficioId AND CoactivaId = @CoactivaId AND Tipo = 'Bloqueo';
    SET @Codigo = (SELECT Codigo FROM coa.Bloqueo WHERE BloqueoId = @BloqueoId);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Coactivas', N'Bloqueo de fondos ' + @Codigo, CONCAT(@Proceso, N' · ', @Tipo, N' ', @Numero, N' · ', @Monto), @Ip);
    COMMIT;
END;
