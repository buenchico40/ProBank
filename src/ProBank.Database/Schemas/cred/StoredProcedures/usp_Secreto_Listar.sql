-- CRE-01: secretos con su versión vigente, vencimiento, estado y grupo (o solo uno con @Token). No devuelve valores:
-- EsMiembro indica si quien consulta integra el grupo y por tanto puede mostrarlo o cambiarlo (CRE-02).
CREATE PROCEDURE cred.usp_Secreto_Listar @Hoy DATE, @ActorId INT, @Token UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.Token, s.Nombre, s.Tipo, s.Almacen, s.Sistema, ISNULL(v.Version, 0) AS Version, s.UltimaRotacion, s.Expira,
           DATEDIFF(DAY, @Hoy, s.Expira) AS DiasParaVencer, s.DiasAviso, cred.fn_EstadoVence(s.Activo, s.Expira, s.DiasAviso, @Hoy) AS Estado,
           g.Token AS GrupoToken, g.Codigo AS GrupoCodigo, g.Nombre AS GrupoNombre, v.Fecha AS FechaVersion,
           CAST(CASE WHEN v.Version IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS TieneValor,
           cred.fn_EsMiembro(@ActorId, s.GrupoId) AS EsMiembro, s.Activo
    FROM cred.Secreto s
    JOIN cred.GrupoAcceso g ON g.GrupoId = s.GrupoId
    OUTER APPLY (SELECT TOP (1) x.Version, x.Fecha FROM cred.CredencialVersion x WHERE x.SecretoId = s.SecretoId AND x.Valor IS NOT NULL ORDER BY x.Version DESC) v
    WHERE @Token IS NULL OR s.Token = @Token
    ORDER BY s.Activo DESC, s.Nombre;
END;
