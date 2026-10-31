-- Devuelve solo el nombre del secreto de Key Vault (la API lee la cadena; nunca sale al navegador).
CREATE PROCEDURE rpt.usp_Conexion_ObtenerSecreto @ConexionId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SecretoKeyVault FROM rpt.Conexion WHERE ConexionId = @ConexionId AND Estado = 'Activo';
END;
