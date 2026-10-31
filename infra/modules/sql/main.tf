terraform {
  required_providers {
    azurerm = { source = "hashicorp/azurerm" }
    azapi   = { source = "Azure/azapi" }
  }
}

variable "nombre" { type = string }
variable "ubicacion" { type = string }
variable "grupo" { type = string }
variable "admin_grupo_entra" {
  type        = object({ nombre = string, object_id = string })
  description = "Grupo de Entra ID administrador del servidor (DBA)"
}
variable "sku" {
  type        = string
  description = "SKU de la base (se ignora con oferta_gratuita)"
}
variable "oferta_gratuita" {
  type        = bool
  default     = false
  description = "Base serverless de la oferta gratuita de Azure SQL (100 000 vCore·s y 32 GB al mes); al agotarse se pausa hasta el mes siguiente."
}
variable "red_privada" {
  type        = bool
  default     = true
  description = "true: solo private endpoint (qa/prod). false: acceso público con firewall para servicios de Azure (dev de bajo costo)."
}
variable "subnet_id" {
  type    = string
  default = null
}
variable "zona_dns_id" {
  type    = string
  default = null
}
variable "log_analytics_id" { type = string }
variable "ip_despliegue" {
  type        = string
  default     = ""
  description = "Solo durante un despliegue (publicar el .dacpac, permisos): acceso desde esta IP."
}
variable "nombre_servidor" {
  type        = string
  default     = ""
  description = "Nombre del servidor si no es sql-<nombre> (p. ej. cuando el nombre quedó reservado en otra región)."
}
variable "etiquetas" { type = map(string) }

resource "azurerm_mssql_server" "this" {
  name                          = coalesce(var.nombre_servidor, "sql-${var.nombre}")
  location                      = var.ubicacion
  resource_group_name           = var.grupo
  version                       = "12.0"
  minimum_tls_version           = "1.2"
  public_network_access_enabled = !var.red_privada || var.ip_despliegue != ""
  azuread_administrator {
    login_username              = var.admin_grupo_entra.nombre
    object_id                   = var.admin_grupo_entra.object_id
    azuread_authentication_only = true
  }
  identity { type = "SystemAssigned" }
  tags = var.etiquetas
}

# Sin red privada, App Service (sin VNet) llega por la regla "servicios de Azure"; la autenticación sigue siendo solo Entra ID.
resource "azurerm_mssql_firewall_rule" "servicios_azure" {
  count            = var.red_privada ? 0 : 1
  name             = "AllowAllWindowsAzureIps"
  server_id        = azurerm_mssql_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_firewall_rule" "despliegue" {
  count            = var.ip_despliegue != "" ? 1 : 0
  name             = "despliegue"
  server_id        = azurerm_mssql_server.this.id
  start_ip_address = var.ip_despliegue
  end_ip_address   = var.ip_despliegue
}

resource "azurerm_mssql_database" "probank" {
  count          = var.oferta_gratuita ? 0 : 1
  name           = "ProBank"
  server_id      = azurerm_mssql_server.this.id
  sku_name       = var.sku
  collation      = "Modern_Spanish_CI_AS"
  zone_redundant = false
  ledger_enabled = false
  short_term_retention_policy { retention_days = 14 }
  tags = var.etiquetas
}

# azurerm no expone useFreeLimit; se crea con azapi.
resource "azapi_resource" "probank_gratis" {
  count     = var.oferta_gratuita ? 1 : 0
  type      = "Microsoft.Sql/servers/databases@2024-05-01-preview"
  name      = "ProBank"
  parent_id = azurerm_mssql_server.this.id
  location  = var.ubicacion
  tags      = var.etiquetas
  body = {
    sku = { name = "GP_S_Gen5_2", tier = "GeneralPurpose", family = "Gen5", capacity = 2 }
    properties = {
      collation                        = "Modern_Spanish_CI_AS"
      maxSizeBytes                     = 34359738368
      autoPauseDelay                   = 60
      minCapacity                      = 0.5
      zoneRedundant                    = false
      requestedBackupStorageRedundancy = "Local"
      useFreeLimit                     = true
      freeLimitExhaustionBehavior      = "AutoPause"
    }
  }
}

locals {
  base_id = var.oferta_gratuita ? azapi_resource.probank_gratis[0].id : azurerm_mssql_database.probank[0].id
}

resource "azurerm_mssql_server_extended_auditing_policy" "this" {
  server_id              = azurerm_mssql_server.this.id
  log_monitoring_enabled = true
}

resource "azurerm_monitor_diagnostic_setting" "db" {
  name                       = "diag-sql"
  target_resource_id         = local.base_id
  log_analytics_workspace_id = var.log_analytics_id
  enabled_log { category = "SQLSecurityAuditEvents" }
  enabled_metric { category = "Basic" }
}

resource "azurerm_private_endpoint" "this" {
  count               = var.red_privada ? 1 : 0
  name                = "pe-sql-${var.nombre}"
  location            = var.ubicacion
  resource_group_name = var.grupo
  subnet_id           = var.subnet_id
  private_service_connection {
    name                           = "sql"
    private_connection_resource_id = azurerm_mssql_server.this.id
    subresource_names              = ["sqlServer"]
    is_manual_connection           = false
  }
  private_dns_zone_group {
    name                 = "sql"
    private_dns_zone_ids = [var.zona_dns_id]
  }
}

output "servidor_fqdn" { value = azurerm_mssql_server.this.fully_qualified_domain_name }
output "base" { value = "ProBank" }
output "servidor_nombre" { value = azurerm_mssql_server.this.name }
