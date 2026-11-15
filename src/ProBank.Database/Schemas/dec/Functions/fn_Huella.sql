-- DEC-03: huella SHA-256 de la información registrada en una decisión: código, versión, tipo, título, descripción, área y fecha
-- (los archivos de respaldo no forman parte de ella). Se fija al enviarla y en cada versión nueva; si alguien cambiara esa
-- información por fuera de ProBank, la huella actual dejaría de coincidir con la guardada y con cada sello.
CREATE FUNCTION dec.fn_Huella (@DecisionId INT)
RETURNS VARBINARY(32)
AS
BEGIN
    RETURN (SELECT HASHBYTES('SHA2_256', CONCAT_WS(N'|', d.Codigo, d.Version, t.Codigo, d.Titulo, d.Descripcion, d.Area, CONVERT(CHAR(10), d.Fecha, 23)))
            FROM dec.Decision d JOIN dec.TipoDecision t ON t.TipoId = d.TipoId
            WHERE d.DecisionId = @DecisionId);
END;
