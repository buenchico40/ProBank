-- CRE-03: llave maestra de Always Encrypted. Solo de referencia para compilar el modelo: cada ambiente crea la suya con
-- «ProBank.Setup llaves» (Key Vault en Azure, certificado en la máquina local) y el .dacpac se publica con
-- /p:ExcludeObjectTypes="ColumnMasterKeys;ColumnEncryptionKeys" para no tocarla.
CREATE COLUMN MASTER KEY [CMK_ProBank]
WITH (KEY_STORE_PROVIDER_NAME = N'AZURE_KEY_VAULT', KEY_PATH = N'https://referencia.vault.azure.net/keys/cmk-always-encrypted');
