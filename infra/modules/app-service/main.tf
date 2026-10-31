variable "nombre" { type = string }
variable "ubicacion" { type = string }
variable "grupo" { type = string }
variable "sku" { type = string }
variable "red_privada" {
  type        = bool
  default     = true
  description = "true: integración con VNet y API solo por private endpoint (qa/prod). false: sin VNet (F1/dev de bajo costo); la API solo acepta las IP de salida de la Web."
}
variable "subnet_apps_id" {
  type    = string
  default = null
}
variable "subnet_privados_id" {
  type    = string
  default = null
}
variable "zona_dns_web_id" {
  type    = string
  default = null
}
variable "app_insights_connection_string" {
  type      = string
  sensitive = true
}
variable "api_settings" { type = map(string) }
variable "web_settings" { type = map(string) }
variable "ip_despliegue" {
  type        = string
  default     = ""
  description = "Solo durante un despliegue: permite llegar a la API (sitio y SCM) desde esta IP."
}
variable "etiquetas" { type = map(string) }

resource "azurerm_service_plan" "this" {
  name                = "asp-${var.nombre}"
  location            = var.ubicacion
  resource_group_name = var.grupo
  os_type             = "Linux"
  sku_name            = var.sku
  tags                = var.etiquetas
}

locals {
  gratis  = var.sku == "F1" # sin always_on ni health check
  abierta = var.ip_despliegue != ""
  comunes = {
    APPLICATIONINSIGHTS_CONNECTION_STRING = var.app_insights_connection_string
    WEBSITE_TIME_ZONE                     = "America/Guayaquil"
    TZ                                    = "America/Guayaquil" # Linux usa TZ; WEBSITE_TIME_ZONE es de Windows
  }
  # Quién puede llamar a la API por la red pública: la Web (sin red privada) y quien despliega.
  ips_api = concat(
    var.red_privada ? [] : [for ip in split(",", azurerm_linux_web_app.web.possible_outbound_ip_addresses) : trimspace(ip)],
    local.abierta ? [var.ip_despliegue] : []
  )
}

resource "azurerm_linux_web_app" "web" {
  name                      = "app-${var.nombre}-web"
  location                  = var.ubicacion
  resource_group_name       = var.grupo
  service_plan_id           = azurerm_service_plan.this.id
  https_only                = true
  virtual_network_subnet_id = var.red_privada ? var.subnet_apps_id : null
  identity { type = "SystemAssigned" }
  site_config {
    always_on              = !local.gratis
    minimum_tls_version    = "1.2"
    vnet_route_all_enabled = var.red_privada
    application_stack { dotnet_version = "10.0" }
  }
  app_settings = merge(local.comunes, var.web_settings)
  tags         = var.etiquetas
}

resource "azurerm_linux_web_app" "api" {
  name                          = "app-${var.nombre}-api"
  location                      = var.ubicacion
  resource_group_name           = var.grupo
  service_plan_id               = azurerm_service_plan.this.id
  https_only                    = true
  public_network_access_enabled = !var.red_privada || local.abierta
  virtual_network_subnet_id     = var.red_privada ? var.subnet_apps_id : null
  identity { type = "SystemAssigned" }
  site_config {
    always_on                         = !local.gratis # con F1 el programador de consultas solo corre mientras la API está activa
    minimum_tls_version               = "1.2"
    vnet_route_all_enabled            = var.red_privada
    health_check_path                 = local.gratis ? null : "/health"
    health_check_eviction_time_in_min = local.gratis ? null : 10
    ip_restriction_default_action     = "Deny"
    scm_ip_restriction_default_action = "Deny"
    application_stack { dotnet_version = "10.0" }
    dynamic "ip_restriction" {
      for_each = toset(local.ips_api)
      content {
        name       = ip_restriction.value == var.ip_despliegue ? "despliegue" : "web"
        ip_address = "${ip_restriction.value}/32"
        action     = "Allow"
        priority   = ip_restriction.value == var.ip_despliegue ? 100 : 200
      }
    }
    dynamic "scm_ip_restriction" {
      for_each = local.abierta ? [var.ip_despliegue] : []
      content {
        name       = "despliegue"
        ip_address = "${scm_ip_restriction.value}/32"
        action     = "Allow"
        priority   = 100
      }
    }
  }
  app_settings = merge(local.comunes, var.api_settings)
  tags         = var.etiquetas
}

# Con red privada la Web llega a la API por la zona privatelink.azurewebsites.net enlazada a la VNet
resource "azurerm_private_endpoint" "api" {
  count               = var.red_privada ? 1 : 0
  name                = "pe-api-${var.nombre}"
  location            = var.ubicacion
  resource_group_name = var.grupo
  subnet_id           = var.subnet_privados_id
  private_service_connection {
    name                           = "api"
    private_connection_resource_id = azurerm_linux_web_app.api.id
    subresource_names              = ["sites"]
    is_manual_connection           = false
  }
  private_dns_zone_group {
    name                 = "api"
    private_dns_zone_ids = [var.zona_dns_web_id]
  }
  tags = var.etiquetas
}

output "api_principal_id" { value = azurerm_linux_web_app.api.identity[0].principal_id }
output "web_principal_id" { value = azurerm_linux_web_app.web.identity[0].principal_id }
output "api_nombre" { value = azurerm_linux_web_app.api.name }
output "web_nombre" { value = azurerm_linux_web_app.web.name }
output "api_hostname" { value = azurerm_linux_web_app.api.default_hostname }
output "web_hostname" { value = azurerm_linux_web_app.web.default_hostname }
