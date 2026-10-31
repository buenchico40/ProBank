terraform {
  required_version = ">= 1.9"
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 4.81" }
    azapi   = { source = "Azure/azapi", version = "~> 2.0" }
    random  = { source = "hashicorp/random", version = "~> 3.6" }
    time    = { source = "hashicorp/time", version = "~> 0.12" }
  }
}

provider "azurerm" {
  features {
    key_vault { purge_soft_delete_on_destroy = false }
  }
  storage_use_azuread = true
}
provider "azapi" {}

data "azurerm_client_config" "actual" {}

locals {
  nombre    = "probank-prod"
  etiquetas = { sistema = "ProBank", ambiente = "prod", responsable = "Gerencia de TI" }
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${local.nombre}"
  location = var.ubicacion
  tags     = local.etiquetas
}

module "network" {
  source    = "../../modules/network"
  nombre    = local.nombre
  ubicacion = var.ubicacion
  grupo     = azurerm_resource_group.this.name
  espacio   = ["10.42.0.0/16"]
  etiquetas = local.etiquetas
}

module "monitoring" {
  source         = "../../modules/monitoring"
  nombre         = local.nombre
  ubicacion      = var.ubicacion
  grupo          = azurerm_resource_group.this.name
  correo_alertas = var.correo_alertas
  etiquetas      = local.etiquetas
}

module "key_vault" {
  source                = "../../modules/key-vault"
  nombre                = local.nombre
  ubicacion             = var.ubicacion
  grupo                 = azurerm_resource_group.this.name
  tenant_id             = data.azurerm_client_config.actual.tenant_id
  desplegador_object_id = data.azurerm_client_config.actual.object_id
  subnet_id             = module.network.subnet_privados_id
  zona_dns_id           = module.network.zonas_dns["vault"]
  ip_despliegue         = var.ip_despliegue
  etiquetas             = local.etiquetas
}

module "sql" {
  source            = "../../modules/sql"
  nombre            = local.nombre
  ubicacion         = coalesce(var.ubicacion_sql, var.ubicacion)
  grupo             = azurerm_resource_group.this.name
  admin_grupo_entra = var.admin_grupo_sql
  sku               = "GP_Gen5_4"
  subnet_id         = module.network.subnet_privados_id
  zona_dns_id       = module.network.zonas_dns["sql"]
  log_analytics_id  = module.monitoring.log_analytics_id
  ip_despliegue     = var.ip_despliegue
  etiquetas         = local.etiquetas
}

module "storage" {
  source      = "../../modules/storage"
  nombre      = local.nombre
  ubicacion   = var.ubicacion
  grupo       = azurerm_resource_group.this.name
  subnet_id   = module.network.subnet_privados_id
  zona_dns_id = module.network.zonas_dns["blob"]
  etiquetas   = local.etiquetas
}

# Clave de firma del token de sesión (SEG-03). Solo vive en Key Vault; App Service la lee por referencia.
resource "random_password" "jwt" {
  length  = 64
  special = false
}
resource "azurerm_key_vault_secret" "jwt" {
  name         = "jwt-clave"
  value        = random_password.jwt.result
  key_vault_id = module.key_vault.id
  content_type = "Clave HMAC del token de ProBank"
  depends_on   = [module.key_vault]
}

module "apps" {
  source                         = "../../modules/app-service"
  nombre                         = local.nombre
  ubicacion                      = coalesce(var.ubicacion_apps, var.ubicacion)
  grupo                          = azurerm_resource_group.this.name
  sku                            = "P2v3"
  subnet_apps_id                 = module.network.subnet_apps_id
  subnet_privados_id             = module.network.subnet_privados_id
  zona_dns_web_id                = module.network.zonas_dns["web"]
  ip_despliegue                  = var.ip_despliegue
  app_insights_connection_string = module.monitoring.app_insights_connection_string
  api_settings = {
    # Run From Package: la app lee su .zip del contenedor despliegues con su identidad (scripts/azure/desplegar.ps1)
    "WEBSITE_RUN_FROM_PACKAGE"                     = "${module.storage.blob_endpoint}despliegues/ProBank.Api.zip"
    "WEBSITE_RUN_FROM_PACKAGE_BLOB_MI_RESOURCE_ID" = "SystemAssigned"
    "Jwt__Clave"                                   = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.jwt.versionless_id})"
    "KeyVault__Uri"                                = module.key_vault.uri
    "Correo__BuzonServicio"                        = var.buzon_servicio
    "ConnectionStrings__ProBank"                   = "Server=tcp:${module.sql.servidor_fqdn},1433;Database=${module.sql.base};Authentication=Active Directory Managed Identity;Encrypt=True;Column Encryption Setting=Enabled"
  }
  web_settings = {
    "WEBSITE_RUN_FROM_PACKAGE"                     = "${module.storage.blob_endpoint}despliegues/ProBank.Web.zip"
    "WEBSITE_RUN_FROM_PACKAGE_BLOB_MI_RESOURCE_ID" = "SystemAssigned"
    "ProBankApi__BaseUrl"                          = "https://app-${local.nombre}-api.azurewebsites.net/"
    "ASPNETCORE_FORWARDEDHEADERS_ENABLED"          = "true" # IP real del usuario para auditoría y SEG-04
    "DevExtreme__LicenseKey"                       = var.devextreme_licencia
  }
  etiquetas = local.etiquetas
}

# La API lee secretos y usa la CMK; la Web no toca Key Vault
resource "azurerm_role_assignment" "api_kv_secretos" {
  scope                = module.key_vault.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = module.apps.api_principal_id
}
resource "azurerm_role_assignment" "api_kv_crypto" {
  scope                = module.key_vault.id
  role_definition_name = "Key Vault Crypto User"
  principal_id         = module.apps.api_principal_id
}
resource "azurerm_role_assignment" "api_blob" {
  scope                = module.storage.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = module.apps.api_principal_id
}

# Paquetes de despliegue: quien despliega los sube; la Web solo lee el suyo (la API ya es Contributor de la cuenta)
resource "azurerm_role_assignment" "desplegador_paquetes" {
  scope                = module.storage.contenedor_despliegues_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.actual.object_id
}
resource "azurerm_role_assignment" "web_paquetes" {
  scope                = module.storage.contenedor_despliegues_id
  role_definition_name = "Storage Blob Data Reader"
  principal_id         = module.apps.web_principal_id
}

output "paquetes_url" { value = "${module.storage.blob_endpoint}despliegues/" }
output "web_url" { value = "https://${module.apps.web_hostname}" }
output "sql_servidor" { value = module.sql.servidor_fqdn }
output "sql_base" { value = module.sql.base }
output "api_nombre" { value = module.apps.api_nombre }
output "web_nombre" { value = module.apps.web_nombre }
output "grupo" { value = azurerm_resource_group.this.name }
