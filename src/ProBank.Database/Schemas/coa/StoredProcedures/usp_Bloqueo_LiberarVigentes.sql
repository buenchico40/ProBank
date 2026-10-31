-- Levanta lo vigente de los bloqueos de las coactivas indicadas (al anular), sin orden judicial asociada.
-- Se llama dentro de la transacción de quien anula.
CREATE PROCEDURE coa.usp_Bloqueo_LiberarVigentes
    @Coactivas VARCHAR(MAX), @Motivo NVARCHAR(500), @Hoy DATE, @ActorId INT, @Liberados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT coa.Movimiento (BloqueoId, OrdenId, Tipo, Monto, Fecha, Detalle, UsuarioId)
    SELECT b.BloqueoId, NULL, 'Levantamiento', b.Vigente, @Hoy, @Motivo, @ActorId
    FROM coa.vw_Bloqueo b
    WHERE b.Vigente > 0 AND b.CoactivaId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@Coactivas, ',') WHERE value <> '');
    SET @Liberados = @@ROWCOUNT;
END;
