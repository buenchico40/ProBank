-- DEC-03: sello SHA-256 de una firma: huella del contenido, usuario, resultado, fecha y hora, comentario e IP.
CREATE FUNCTION dec.fn_Sello (@Huella VARBINARY(32), @Usuario VARCHAR(60), @Resultado NVARCHAR(10), @Fecha DATETIME2(0),
                              @Comentario NVARCHAR(500), @Ip VARCHAR(45))
RETURNS VARBINARY(32)
AS
BEGIN
    RETURN HASHBYTES('SHA2_256', CONCAT_WS(N'|', CONVERT(VARCHAR(64), @Huella, 2), @Usuario, @Resultado,
                                           CONVERT(VARCHAR(19), @Fecha, 126), ISNULL(@Comentario, N''), ISNULL(@Ip, '')));
END;
