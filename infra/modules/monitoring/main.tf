variable "nombre" { type = string }
variable "ubicacion" { type = string }
variable "grupo" { type = string }
variable "correo_alertas" { type = string }
variable "retencion_dias" {
  type    = number
  default = 90
}
variable "etiquetas" { type = map(string) }

resource "azurerm_log_analytics_workspace" "this" {
  name                = "log-${var.nombre}"
  location            = var.ubicacion
  resource_group_name = var.grupo
  sku                 = "PerGB2018"
  retention_in_days   = var.retencion_dias
  tags                = var.etiquetas
}

resource "azurerm_application_insights" "this" {
  name                = "appi-${var.nombre}"
  location            = var.ubicacion
  resource_group_name = var.grupo
  workspace_id        = azurerm_log_analytics_workspace.this.id
  application_type    = "web"
  tags                = var.etiquetas
}

resource "azurerm_monitor_action_group" "ti" {
  name                = "ag-${var.nombre}"
  resource_group_name = var.grupo
  short_name          = "probank"
  email_receiver {
    name          = "gerencia-ti"
    email_address = var.correo_alertas
  }
}

output "log_analytics_id" { value = azurerm_log_analytics_workspace.this.id }
output "app_insights_connection_string" {
  value     = azurerm_application_insights.this.connection_string
  sensitive = true
}
output "action_group_id" { value = azurerm_monitor_action_group.ti.id }
