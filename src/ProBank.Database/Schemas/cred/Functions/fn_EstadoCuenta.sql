-- CRE-01: estado de una cuenta técnica según su próxima rotación: «Por rotar» 15 días antes (igual que ReglasBoveda.EstadoCuenta).
CREATE FUNCTION cred.fn_EstadoCuenta (@Activo BIT, @ProximaRotacion DATE, @Hoy DATE)
RETURNS VARCHAR(20)
AS
BEGIN
    RETURN CASE WHEN @Activo = 0 THEN 'Inactivo'
                WHEN @ProximaRotacion IS NULL THEN 'Activo'
                WHEN @ProximaRotacion < @Hoy THEN 'Rotación vencida'
                WHEN DATEDIFF(DAY, @Hoy, @ProximaRotacion) <= 15 THEN 'Por rotar'
                ELSE 'Activo' END;
END;
