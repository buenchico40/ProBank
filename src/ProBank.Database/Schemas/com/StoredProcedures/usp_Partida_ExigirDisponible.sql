-- Regla general del presupuesto: ninguna operación deja una partida con disponible negativo. Se llama dentro de la
-- transacción de quien cambió los montos (con XACT_ABORT, el THROW la revierte). @Partidas: ids separados por coma.
CREATE PROCEDURE com.usp_Partida_ExigirDisponible @Partidas VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Codigo VARCHAR(20), @Disponible DECIMAL(18,2);
    SELECT TOP (1) @Codigo = v.Codigo, @Disponible = v.Disponible
    FROM com.vw_Partida v
    WHERE v.PartidaId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@Partidas, ',')) AND v.Disponible < 0
    ORDER BY v.Disponible;
    IF @Codigo IS NOT NULL
    BEGIN
        DECLARE @m NVARCHAR(300) = CONCAT(N'La partida ', @Codigo, N' no tiene disponible suficiente (quedaría en ', FORMAT(@Disponible, 'N2', 'es-EC'), N').');
        THROW 50422, @m, 1;
    END;
END;
