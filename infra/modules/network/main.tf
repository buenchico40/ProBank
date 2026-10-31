variable "nombre" { type = string }
variable "ubicacion" { type = string }
variable "grupo" { type = string }
variable "espacio" { type = list(string) }
variable "etiquetas" { type = map(string) }

resource "azurerm_virtual_network" "this" {
  name                = "vnet-${var.nombre}"
  location            = var.ubicacion
  resource_group_name = var.grupo
  address_space       = var.espacio
  tags                = var.etiquetas
}

resource "azurerm_subnet" "apps" {
  name                 = "snet-apps"
  resource_group_name  = var.grupo
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = [cidrsubnet(var.espacio[0], 8, 1)]
  delegation {
    name = "appservice"
    service_delegation { name = "Microsoft.Web/serverFarms" }
  }
}

resource "azurerm_subnet" "privados" {
  name                 = "snet-private-endpoints"
  resource_group_name  = var.grupo
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = [cidrsubnet(var.espacio[0], 8, 2)]
}

locals {
  zonas = {
    sql   = "privatelink.database.windows.net"
    vault = "privatelink.vaultcore.azure.net"
    blob  = "privatelink.blob.core.windows.net"
    web   = "privatelink.azurewebsites.net"
  }
}

resource "azurerm_private_dns_zone" "this" {
  for_each            = local.zonas
  name                = each.value
  resource_group_name = var.grupo
  tags                = var.etiquetas
}

resource "azurerm_private_dns_zone_virtual_network_link" "this" {
  for_each              = local.zonas
  name                  = "link-${each.key}"
  resource_group_name   = var.grupo
  private_dns_zone_name = azurerm_private_dns_zone.this[each.key].name
  virtual_network_id    = azurerm_virtual_network.this.id
}

output "subnet_apps_id" { value = azurerm_subnet.apps.id }
output "subnet_privados_id" { value = azurerm_subnet.privados.id }
output "zonas_dns" { value = { for k, z in azurerm_private_dns_zone.this : k => z.id } }
