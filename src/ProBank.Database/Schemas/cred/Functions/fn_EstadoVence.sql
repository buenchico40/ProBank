-- CRE-01: estado de un certificado o un secreto según su vencimiento y los días de aviso (igual que ReglasBoveda.EstadoVence).
CREATE FUNCTION cred.fn_EstadoVence (@Activo BIT, @Vence DATE, @DiasAviso INT, @Hoy DATE)
RETURNS VARCHAR(20)
AS
BEGIN
    RETURN CASE WHEN @Activo = 0 THEN 'Inactivo'
                WHEN @Vence IS NULL THEN 'Vigente'
                WHEN @Vence < @Hoy THEN 'Vencido'
                WHEN DATEDIFF(DAY, @Hoy, @Vence) <= @DiasAviso THEN 'Por vencer'
                ELSE 'Vigente' END;
END;
