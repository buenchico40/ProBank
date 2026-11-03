-- Crea (@Token NULL) una partida en Borrador con su código PRE-<año>-NN, o edita rubro y asignado de un borrador.
-- El rubro es único por año y área; el año y el área de una partida no cambian. Solo se crean partidas de áreas activas.
-- Una partida activa no cambia su asignado: se ajusta con un movimiento (com.usp_Movimiento_Guardar).
CREATE PROCEDURE com.usp_Partida_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Anio SMALLINT, @Area VARCHAR(30), @Rubro NVARCHAR(150), @Asignado DECIMAL(18,2),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'presupuesto', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar partidas.', 1;
    IF @Token IS NULL AND NOT EXISTS (SELECT 1 FROM com.Area WHERE Codigo = @Area AND Activa = 1)
        THROW 50422, N'El área no existe o está inactiva.', 1;
    BEGIN TRAN;
    IF EXISTS (SELECT 1 FROM com.Partida WITH (UPDLOCK, HOLDLOCK) WHERE Anio = @Anio AND Area = @Area AND Rubro = @Rubro AND Token <> ISNULL(@Token, '00000000-0000-0000-0000-000000000000'))
        THROW 50409, N'Ese rubro ya tiene una partida del área en el año.', 1;
    IF @Token IS NULL
    BEGIN
        DECLARE @n INT = 1 + ISNULL((SELECT MAX(CAST(RIGHT(Codigo, 2) AS INT)) FROM com.Partida WHERE Anio = @Anio), 0);
        IF @n > 99 THROW 50422, N'El año ya tiene 99 partidas.', 1;
        SET @TokenGuardado = NEWID();
        INSERT com.Partida (Token, Codigo, Anio, Area, Rubro, Asignado, CreadoPorId)
        VALUES (@TokenGuardado, CONCAT('PRE-', @Anio, '-', RIGHT('0' + CAST(@n AS VARCHAR(3)), 2)), @Anio, @Area, @Rubro, @Asignado, @ActorId);
    END
    ELSE
    BEGIN
        DECLARE @Estado VARCHAR(20), @AnioActual SMALLINT, @AreaActual VARCHAR(30);
        SELECT @Estado = Estado, @AnioActual = Anio, @AreaActual = Area FROM com.Partida WITH (UPDLOCK) WHERE Token = @Token;
        IF @Estado IS NULL THROW 50404, N'La partida no existe.', 1;
        IF @Estado <> 'Borrador' THROW 50422, N'Solo se editan partidas en borrador; una partida activa se ajusta con un movimiento.', 1;
        IF @AnioActual <> @Anio THROW 50422, N'El año de una partida no cambia.', 1;
        IF @AreaActual <> @Area THROW 50422, N'El área de una partida no cambia.', 1;
        UPDATE com.Partida SET Rubro = @Rubro, Asignado = @Asignado WHERE Token = @Token;
        SET @TokenGuardado = @Token;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CASE WHEN @Token IS NULL THEN N'Nueva partida ' ELSE N'Edición de la partida ' END + Codigo,
           CONCAT(Area, N' · ', Rubro, N' · asignado ', Asignado), @Ip
    FROM com.Partida WHERE Token = @TokenGuardado;
    COMMIT;
END;
