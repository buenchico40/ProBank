-- Devuelve solo el nombre del secreto de Key Vault de una base activa (la API lee la cadena; nunca sale al navegador).
CREATE PROCEDURE rpt.usp_Conexion_ObtenerSecreto @Codigo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SecretoKeyVault FROM rpt.Conexion WHERE Codigo = @Codigo AND Estado = 'Activo';
END;
