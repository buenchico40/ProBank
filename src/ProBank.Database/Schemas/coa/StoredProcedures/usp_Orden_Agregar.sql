-- COA-01 y COA-02: agrega una coactiva al oficio con su orden.
-- Bloqueo: coactiva nueva (proceso único por entidad); el cliente se crea si su identificación no existe.
-- Levantamiento o transferencia: sobre una coactiva con fondos bloqueados; monto ordenado NULL = total, nunca mayor que lo vigente.
CREATE PROCEDURE coa.usp_Orden_Agregar
    @OficioToken UNIQUEIDENTIFIER, @Tipo VARCHAR(20),
    @NumeroProceso VARCHAR(50) = NULL, @EntidadId INT = NULL, @TipoIdentificacion VARCHAR(10) = NULL, @Identificacion VARCHAR(20) = NULL,
    @Cliente NVARCHAR(150) = NULL, @Monto DECIMAL(18,2) = NULL, @Productos VARCHAR(200) = NULL,
    @CoactivaToken UNIQUEIDENTIFIER = NULL, @MontoOrdenado DECIMAL(18,2) = NULL, @Observacion NVARCHAR(500) = NULL,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Proceso VARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'oficios', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para agregar coactivas al oficio.', 1;
    DECLARE @OficioId INT, @Oficio VARCHAR(30), @Anulado BIT;
    SELECT @OficioId = OficioId, @Oficio = Numero, @Anulado = Anulado FROM coa.Oficio WITH (UPDLOCK) WHERE Token = @OficioToken;
    IF @OficioId IS NULL THROW 50404, N'El oficio no existe.', 1;
    IF @Anulado = 1 THROW 50422, N'El oficio está anulado.', 1;

    BEGIN TRAN;
    DECLARE @CoactivaId INT;
    IF @Tipo = 'Bloqueo'
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM coa.Entidad WHERE EntidadId = @EntidadId AND Estado = 'Activo') THROW 50422, N'La entidad no existe o está inactiva.', 1;
        IF EXISTS (SELECT 1 FROM coa.Coactiva WHERE EntidadId = @EntidadId AND NumeroProceso = @NumeroProceso)
            THROW 50409, N'Ese número de proceso ya existe para la entidad.', 1;
        DECLARE @ClienteId INT = (SELECT ClienteId FROM coa.Cliente WITH (UPDLOCK) WHERE Identificacion = @Identificacion);
        IF @ClienteId IS NULL
        BEGIN
            INSERT coa.Cliente (TipoIdentificacion, Identificacion, Nombre) VALUES (@TipoIdentificacion, @Identificacion, @Cliente);
            SET @ClienteId = SCOPE_IDENTITY();
        END;
        INSERT coa.Coactiva (OficioId, EntidadId, ClienteId, NumeroProceso, Monto, ProductosPermitidos, Observacion)
        VALUES (@OficioId, @EntidadId, @ClienteId, @NumeroProceso, @Monto, NULLIF(@Productos, ''), @Observacion);
        SET @CoactivaId = SCOPE_IDENTITY();
        INSERT coa.OrdenCoactiva (OficioId, CoactivaId, Tipo, MontoOrdenado, Observacion) VALUES (@OficioId, @CoactivaId, 'Bloqueo', NULL, @Observacion);
        SET @Proceso = @NumeroProceso;
    END
    ELSE
    BEGIN
        DECLARE @Bloqueado DECIMAL(18,2), @Anulada BIT;
        SELECT @CoactivaId = CoactivaId, @Proceso = NumeroProceso, @Bloqueado = Bloqueado, @Anulada = Anulada FROM coa.vw_Coactiva WHERE Token = @CoactivaToken;
        IF @CoactivaId IS NULL THROW 50404, N'La coactiva no existe.', 1;
        IF @Anulada = 1 OR @Bloqueado <= 0 THROW 50422, N'La coactiva no tiene fondos bloqueados.', 1;
        IF @MontoOrdenado > @Bloqueado THROW 50422, N'El monto ordenado supera lo bloqueado vigente.', 1;
        IF EXISTS (SELECT 1 FROM coa.OrdenCoactiva WHERE OficioId = @OficioId AND CoactivaId = @CoactivaId AND Tipo = @Tipo)
            THROW 50409, N'El oficio ya tiene esa orden para la coactiva.', 1;
        INSERT coa.OrdenCoactiva (OficioId, CoactivaId, Tipo, MontoOrdenado, Observacion) VALUES (@OficioId, @CoactivaId, @Tipo, @MontoOrdenado, @Observacion);
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Coactivas', CONCAT(N'Orden de ', LOWER(@Tipo), N' en el oficio ', @Oficio),
            CONCAT(@Proceso, CASE WHEN @Tipo = 'Bloqueo' THEN CONCAT(N' · monto ', @Monto) WHEN @MontoOrdenado IS NULL THEN N' · total' ELSE CONCAT(N' · ', @MontoOrdenado) END), @Ip);
    COMMIT;
END;
