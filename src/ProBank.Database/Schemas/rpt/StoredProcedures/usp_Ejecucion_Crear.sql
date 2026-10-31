CREATE PROCEDURE rpt.usp_Ejecucion_Crear
    @ConsultaId INT, @ConexionId INT, @SolicitanteId INT, @Aprobadores VARCHAR(400) = '', @ParametrosResueltos NVARCHAR(2000), @Estado VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    INSERT rpt.Ejecucion (ConsultaId, ConexionId, SolicitanteId, ParametrosResueltos, Estado)
    VALUES (@ConsultaId, @ConexionId, @SolicitanteId, @ParametrosResueltos, @Estado);
    DECLARE @Id BIGINT = SCOPE_IDENTITY();
    -- SQL-03: quien solicita no figura como aprobador de su propia ejecución
    INSERT rpt.EjecucionAprobador (EjecucionId, UsuarioId)
    SELECT DISTINCT @Id, CAST(value AS INT) FROM STRING_SPLIT(@Aprobadores, ',') WHERE LEN(value) > 0 AND CAST(value AS INT) <> @SolicitanteId;
    COMMIT;
    SELECT EjecucionId, Codigo FROM rpt.Ejecucion WHERE EjecucionId = @Id;
END;
