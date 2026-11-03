-- Tipos de archivo y tamaño máximo de los adjuntos por ámbito (Item o Tarea). Se administran en Portafolio › Parametrización.
-- Extensiones: separadas por coma, del catálogo de ReglasAdjunto (por ejemplo ".pdf,.docx,.zip").
CREATE TABLE [pry].[ParametroAdjunto]
(
    Ambito      VARCHAR(10)  NOT NULL CONSTRAINT PK_ParametroAdjunto PRIMARY KEY CONSTRAINT CK_ParametroAdjunto_Ambito CHECK (Ambito IN ('Item', 'Tarea')),
    Extensiones VARCHAR(200) NOT NULL,
    MaximoMB    INT          NOT NULL CONSTRAINT CK_ParametroAdjunto_Maximo CHECK (MaximoMB BETWEEN 1 AND 20)
);
