-- SQL-07 y SQL-08: crea (@Codigo NULL, PRG-NNNN) o edita un envío programado de una consulta publicada sin aprobadores,
-- con su base, frecuencia, formato, parámetros (fijos o variables) y destinatarios ([{"correo","entraObjectId"}]).
-- La próxima ejecución la calcula la API (CalendarioProgramacion).
CREATE PROCEDURE rpt.usp_Programacion_Guardar
    @Codigo VARCHAR(10) = NULL, @ConsultaCodigo VARCHAR(20), @ConexionCodigo VARCHAR(20), @Frecuencia VARCHAR(10), @Hora TIME(0),
    @Dias VARCHAR(20) = NULL, @DiaDelMes TINYINT = NULL, @Formato VARCHAR(5), @ParametrosJson NVARCHAR(2000),
    @Destinatarios NVARCHAR(MAX), @Estado VARCHAR(25), @ProximaEjecucion DATETIME2(0),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @CodigoGuardado VARCHAR(10) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'consultas', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para programar envíos de consultas.', 1;
    DECLARE @ConsultaId INT, @EstadoConsulta VARCHAR(20), @ConexionId INT;
    SELECT @ConsultaId = ConsultaId, @EstadoConsulta = Estado FROM rpt.Consulta WHERE Codigo = @ConsultaCodigo;
    IF @ConsultaId IS NULL THROW 50404, N'La consulta no existe.', 1;
    IF @EstadoConsulta <> 'Publicada' THROW 50422, N'Solo se programan consultas publicadas.', 1;
    IF EXISTS (SELECT 1 FROM rpt.ConsultaAprobador WHERE ConsultaId = @ConsultaId)
        THROW 50422, N'La consulta requiere aprobación en cada ejecución: no se puede enviar programada.', 1;
    SELECT @ConexionId = c.ConexionId FROM rpt.ConsultaConexion cc JOIN rpt.Conexion c ON c.ConexionId = cc.ConexionId AND c.Estado = 'Activo'
    WHERE cc.ConsultaId = @ConsultaId AND c.Codigo = @ConexionCodigo;
    IF @ConexionId IS NULL THROW 50422, N'La base de datos no está permitida para esta consulta o está inactiva.', 1;
    IF NOT EXISTS (SELECT 1 FROM OPENJSON(@Destinatarios)) THROW 50422, N'Indique al menos un destinatario.', 1;

    BEGIN TRAN;
    DECLARE @Id INT;
    IF @Codigo IS NULL
    BEGIN
        INSERT rpt.Programacion (ConsultaId, ConexionId, Frecuencia, Hora, Dias, DiaDelMes, Formato, ParametrosJson, ResponsableId, ProximaEjecucion, Estado)
        VALUES (@ConsultaId, @ConexionId, @Frecuencia, @Hora, NULLIF(@Dias, ''), @DiaDelMes, @Formato, @ParametrosJson, @ActorId, @ProximaEjecucion, @Estado);
        SET @Id = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @Id = ProgramacionId FROM rpt.Programacion WITH (UPDLOCK) WHERE Codigo = @Codigo AND ConsultaId = @ConsultaId;
        IF @Id IS NULL THROW 50404, N'El envío programado no existe.', 1;
        UPDATE rpt.Programacion SET ConexionId = @ConexionId, Frecuencia = @Frecuencia, Hora = @Hora, Dias = NULLIF(@Dias, ''), DiaDelMes = @DiaDelMes,
               Formato = @Formato, ParametrosJson = @ParametrosJson, ProximaEjecucion = @ProximaEjecucion, Estado = @Estado
        WHERE ProgramacionId = @Id;
        DELETE rpt.ProgramacionDestinatario WHERE ProgramacionId = @Id;
    END;
    INSERT rpt.ProgramacionDestinatario (ProgramacionId, Correo, EntraObjectId)
    SELECT DISTINCT @Id, LOWER(LTRIM(RTRIM(j.correo))), TRY_CAST(j.entraObjectId AS UNIQUEIDENTIFIER)
    FROM OPENJSON(@Destinatarios) WITH (correo VARCHAR(256), entraObjectId VARCHAR(40)) j WHERE NULLIF(LTRIM(RTRIM(j.correo)), '') IS NOT NULL;
    SET @CodigoGuardado = (SELECT Codigo FROM rpt.Programacion WHERE ProgramacionId = @Id);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Consultas dinámicas',
            CASE WHEN @Codigo IS NULL THEN N'Nuevo envío programado ' ELSE N'Edición del envío programado ' END + @CodigoGuardado + N' de ' + @ConsultaCodigo,
            CONCAT(@Frecuencia, N' a las ', CONVERT(VARCHAR(5), @Hora, 108), N' · ', UPPER(@Formato), N' · ', @Estado, N' · a ',
                   (SELECT STRING_AGG(Correo, ', ') FROM rpt.ProgramacionDestinatario WHERE ProgramacionId = @Id)), @Ip);
    COMMIT;
END;
