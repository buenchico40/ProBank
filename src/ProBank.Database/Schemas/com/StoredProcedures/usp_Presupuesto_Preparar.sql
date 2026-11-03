-- COM-01: prepara el borrador de un área para el año siguiente a su último año con partidas activas, copiando sus rubros con
-- el asignado vigente (los rubros que el borrador ya tiene no se tocan). Devuelve el año y cuántas partidas copió.
CREATE PROCEDURE com.usp_Presupuesto_Preparar
    @Area VARCHAR(30), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Anio SMALLINT OUTPUT, @Copiadas INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'presupuesto', 'crear') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para preparar el presupuesto.', 1;
    IF NOT EXISTS (SELECT 1 FROM com.Area WHERE Codigo = @Area AND Activa = 1)
        THROW 50422, N'El área no existe o está inactiva.', 1;
    BEGIN TRAN;
    DECLARE @Vigente SMALLINT = (SELECT MAX(Anio) FROM com.Partida WITH (UPDLOCK, HOLDLOCK) WHERE Area = @Area AND Estado = 'Activo');
    IF @Vigente IS NULL THROW 50422, N'El área no tiene un presupuesto activo del cual copiar.', 1;
    SET @Anio = @Vigente + 1;
    DECLARE @Base INT = ISNULL((SELECT MAX(CAST(RIGHT(Codigo, 2) AS INT)) FROM com.Partida WHERE Anio = @Anio), 0);
    INSERT com.Partida (Codigo, Anio, Area, Rubro, Asignado, CreadoPorId)
    SELECT CONCAT('PRE-', @Anio, '-', RIGHT('0' + CAST(@Base + ROW_NUMBER() OVER (ORDER BY v.Codigo) AS VARCHAR(3)), 2)), @Anio, v.Area, v.Rubro, v.Asignado, @ActorId
    FROM com.Partida v
    WHERE v.Anio = @Vigente AND v.Area = @Area AND v.Estado = 'Activo'
      AND NOT EXISTS (SELECT 1 FROM com.Partida x WHERE x.Anio = @Anio AND x.Area = @Area AND x.Rubro = v.Rubro);
    SET @Copiadas = @@ROWCOUNT;
    IF @Base + @Copiadas > 99 THROW 50422, N'El borrador superaría las 99 partidas.', 1;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CONCAT(N'Borrador del presupuesto ', @Area, N' ', @Anio), CONCAT(@Copiadas, N' partida(s) copiadas de ', @Vigente), @Ip);
    COMMIT;
END;
