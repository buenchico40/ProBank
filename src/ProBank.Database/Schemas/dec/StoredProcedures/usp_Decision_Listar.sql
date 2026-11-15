-- DEC-01: decisiones visibles para el usuario (dec.fn_PuedeVer). @Alcance 'todas'; 'pendientes': solo las que esperan su firma
-- ahora (en secuencia, cuando llega su turno); 'notificadas': las que le notificaron; 'firmadas': las que aprobó o rechazó (en
-- cualquier versión). MiResultado, MiFechaFirma y MiVersionFirma: su última firma. @Token: solo esa (si puede verla).
CREATE PROCEDURE dec.usp_Decision_Listar
    @ActorId INT, @RolActivo VARCHAR(60), @Alcance VARCHAR(12) = 'todas', @Token UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.Token, d.Codigo, t.Codigo AS TipoCodigo, t.Nombre AS Tipo, d.Titulo, d.Area, d.Fecha, d.Estado,
           CAST(CASE WHEN d.Estado = N'Borrador' THEN t.Secuencial ELSE d.Secuencial END AS BIT) AS Secuencial, -- en borrador, el flujo de su tipo
           c.Nombre AS CreadoPor, d.FechaRegistro, d.FechaEnvio, d.FechaCierre,
           (SELECT COUNT(*) FROM dec.DecisionAprobador a WHERE a.DecisionId = d.DecisionId AND a.Resultado = N'Aprobada') AS Aprobadas,
           (SELECT COUNT(*) FROM dec.DecisionAprobador a WHERE a.DecisionId = d.DecisionId) AS TotalAprobadores,
           CAST(CASE WHEN turno.UsuarioId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS EsMiTurno,
           CAST(CASE WHEN d.CreadoPorId = @ActorId THEN 1 ELSE 0 END AS BIT) AS SoyCreador,
           CAST(CASE WHEN n.UsuarioId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS SoyNotificado,
           CAST(CASE WHEN EXISTS (SELECT 1 FROM dec.DecisionAprobador a WHERE a.DecisionId = d.DecisionId AND a.UsuarioId = @ActorId) THEN 1 ELSE 0 END AS BIT) AS SoyAprobador,
           d.Version, COALESCE(mi.Resultado, ant.Resultado) AS MiResultado, COALESCE(mi.Fecha, ant.Fecha) AS MiFechaFirma,
           CASE WHEN mi.Resultado IS NOT NULL THEN d.Version ELSE ant.Version END AS MiVersionFirma
    FROM dec.Decision d
    JOIN dec.TipoDecision t ON t.TipoId = d.TipoId
    JOIN seg.Usuario c ON c.UsuarioId = d.CreadoPorId
    LEFT JOIN dec.DecisionNotificado n ON n.DecisionId = d.DecisionId AND n.UsuarioId = @ActorId
    -- Mi firma: la de la versión vigente o, si la decisión se modificó después de firmarla, la última de una versión anterior
    LEFT JOIN dec.DecisionAprobador mi ON mi.DecisionId = d.DecisionId AND mi.UsuarioId = @ActorId AND mi.Resultado <> N'Pendiente'
    OUTER APPLY (SELECT TOP (1) x.Resultado, x.Fecha, x.Version FROM dec.FirmaAnterior x
                 WHERE x.DecisionId = d.DecisionId AND x.UsuarioId = @ActorId ORDER BY x.Fecha DESC) ant
    OUTER APPLY (SELECT a.UsuarioId FROM dec.DecisionAprobador a
                 WHERE a.DecisionId = d.DecisionId AND a.UsuarioId = @ActorId AND a.Resultado = N'Pendiente' AND d.Estado = N'En aprobación'
                   AND (d.Secuencial = 0 OR NOT EXISTS (SELECT 1 FROM dec.DecisionAprobador x
                                                         WHERE x.DecisionId = d.DecisionId AND x.Resultado = N'Pendiente' AND x.Orden < a.Orden))) turno
    WHERE (@Token IS NULL OR d.Token = @Token)
      AND (@Alcance <> 'pendientes' OR turno.UsuarioId IS NOT NULL)
      AND (@Alcance <> 'notificadas' OR n.UsuarioId IS NOT NULL)
      AND (@Alcance <> 'firmadas' OR mi.Resultado IS NOT NULL OR ant.Resultado IS NOT NULL)
      AND dec.fn_PuedeVer(@ActorId, @RolActivo, d.DecisionId) = 1
    ORDER BY d.DecisionId DESC;
END;
