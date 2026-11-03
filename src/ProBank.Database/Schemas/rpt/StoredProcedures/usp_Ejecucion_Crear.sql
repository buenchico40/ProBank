-- Registra una solicitud de ejecución con sus parámetros ya resueltos (texto auditable y valores en JSON) y sus aprobadores.
CREATE PROCEDURE rpt.usp_Ejecucion_Crear
    @ConsultaId INT, @ConexionCodigo VARCHAR(20), @SolicitanteId INT, @Aprobadores VARCHAR(400) = '', @ParametrosResueltos NVARCHAR(2000),
    @ParametrosJson NVARCHAR(2000), @Programada BIT = 0, @Estado VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @ConexionId INT = (SELECT ConexionId FROM rpt.Conexion WHERE Codigo = @ConexionCodigo AND Estado = 'Activo');
    IF @ConexionId IS NULL THROW 50422, N'La base de datos no existe o está inactiva.', 1;
    BEGIN TRAN;
    INSERT rpt.Ejecucion (ConsultaId, ConexionId, SolicitanteId, ParametrosResueltos, ParametrosJson, Programada, Estado)
    VALUES (@ConsultaId, @ConexionId, @SolicitanteId, @ParametrosResueltos, @ParametrosJson, @Programada, @Estado);
    DECLARE @Id BIGINT = SCOPE_IDENTITY();
    -- SQL-03: quien solicita no figura como aprobador de su propia ejecución
    INSERT rpt.EjecucionAprobador (EjecucionId, UsuarioId)
    SELECT DISTINCT @Id, CAST(value AS INT) FROM STRING_SPLIT(@Aprobadores, ',') WHERE LEN(value) > 0 AND CAST(value AS INT) <> @SolicitanteId;
    COMMIT;
    SELECT EjecucionId, Codigo FROM rpt.Ejecucion WHERE EjecucionId = @Id;
END;
