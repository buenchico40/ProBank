-- CRE-01: cuentas técnicas con su rotación, estado, grupo y versión vigente (o solo una con @Token). No devuelve contraseñas:
-- EsMiembro indica si quien consulta integra el grupo y por tanto puede mostrarla o cambiarla (CRE-02).
CREATE PROCEDURE cred.usp_Cuenta_Listar @Hoy DATE, @ActorId INT, @Token UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Token, c.Nombre, c.Tipo, c.Sistema, c.Responsable, c.Privilegios, c.UltimaRotacion, c.ProximaRotacion,
           DATEDIFF(DAY, @Hoy, c.ProximaRotacion) AS DiasParaRotar, cred.fn_EstadoCuenta(c.Activo, c.ProximaRotacion, @Hoy) AS Estado,
           g.Token AS GrupoToken, g.Codigo AS GrupoCodigo, g.Nombre AS GrupoNombre, ISNULL(v.Version, 0) AS Version, v.Fecha AS FechaVersion,
           CAST(CASE WHEN v.Version IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS TieneValor,
           CAST(ISNULL(cred.fn_EsMiembro(@ActorId, c.GrupoId), 0) AS BIT) AS EsMiembro, c.Activo
    FROM cred.CuentaTecnica c
    LEFT JOIN cred.GrupoAcceso g ON g.GrupoId = c.GrupoId
    OUTER APPLY (SELECT TOP (1) x.Version, x.Fecha FROM cred.CredencialVersion x WHERE x.CuentaId = c.CuentaId AND x.Valor IS NOT NULL ORDER BY x.Version DESC) v
    WHERE @Token IS NULL OR c.Token = @Token
    ORDER BY c.Activo DESC, c.Nombre;
END;
