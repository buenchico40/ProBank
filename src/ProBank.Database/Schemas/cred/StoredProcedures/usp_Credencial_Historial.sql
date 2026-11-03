-- CRE-01 y CRE-02: versiones (sin valores) y últimos 100 accesos de una cuenta técnica (@Clase 'cuenta') o de un secreto ('secreto').
CREATE PROCEDURE cred.usp_Credencial_Historial @Clase VARCHAR(10), @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CuentaId INT = CASE WHEN @Clase = 'cuenta' THEN (SELECT CuentaId FROM cred.CuentaTecnica WHERE Token = @Token) END;
    DECLARE @SecretoId INT = CASE WHEN @Clase = 'secreto' THEN (SELECT SecretoId FROM cred.Secreto WHERE Token = @Token) END;

    SELECT v.Version, v.Fecha, v.Vence, u.Nombre AS RegistradoPor, v.Nota, CAST(CASE WHEN v.Valor IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS Vigente
    FROM cred.CredencialVersion v JOIN seg.Usuario u ON u.UsuarioId = v.UsuarioId
    WHERE (@CuentaId IS NOT NULL AND v.CuentaId = @CuentaId) OR (@SecretoId IS NOT NULL AND v.SecretoId = @SecretoId)
    ORDER BY v.Version DESC;

    SELECT TOP (100) a.Fecha, u.Usuario, u.Nombre, a.Accion, a.Version, a.Ip
    FROM cred.CredencialAcceso a JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId
    WHERE (@CuentaId IS NOT NULL AND a.CuentaId = @CuentaId) OR (@SecretoId IS NOT NULL AND a.SecretoId = @SecretoId)
    ORDER BY a.AccesoId DESC;
END;
