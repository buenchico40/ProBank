-- CRE-03: llave de columna de la Bóveda, cifrada con CMK_ProBank. Valor de referencia: la real la crea «ProBank.Setup llaves»
-- en cada ambiente (ver CMK_ProBank.sql).
CREATE COLUMN ENCRYPTION KEY [CEK_ProBank]
WITH VALUES (COLUMN_MASTER_KEY = [CMK_ProBank], ALGORITHM = 'RSA_OAEP', ENCRYPTED_VALUE = 0x01);
