variable "nombre" { type = string }
variable "ubicacion" { type = string }
variable "grupo" { type = string }
variable "subnet_id" {
  type    = string
  default = null
}
variable "zona_dns_id" {
  type    = string
  default = null
}
variable "red_privada" {
  type        = bool
  default     = true
  description = "true: solo private endpoint (qa/prod). false: acceso público con identidad (sin claves compartidas) para dev."
}
variable "replicacion" {
  type    = string
  default = "ZRS"
}
variable "etiquetas" { type = map(string) }

resource "azurerm_storage_account" "this" {
  name                            = replace("st${var.nombre}", "-", "")
  location                        = var.ubicacion
  resource_group_name             = var.grupo
  account_tier                    = "Standard"
  account_replication_type        = var.replicacion
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = false
  public_network_access_enabled   = !var.red_privada
  blob_properties {
    versioning_enabled = true
    delete_retention_policy { days = 30 }
  }
  tags = var.etiquetas
}

resource "azurerm_storage_container" "this" {
  for_each              = toset(["oficios", "adjuntos", "exportaciones", "marketing", "despliegues"])
  name                  = each.key
  storage_account_id    = azurerm_storage_account.this.id
  container_access_type = "private"
}

# Las exportaciones grandes y los enlaces de correo vencen solos
resource "azurerm_storage_management_policy" "this" {
  storage_account_id = azurerm_storage_account.this.id
  rule {
    name    = "exportaciones-7-dias"
    enabled = true
    filters {
      blob_types   = ["blockBlob"]
      prefix_match = ["exportaciones/"]
    }
    actions {
      base_blob { delete_after_days_since_creation_greater_than = 7 }
    }
  }
}

resource "azurerm_private_endpoint" "this" {
  count               = var.red_privada ? 1 : 0
  name                = "pe-st-${var.nombre}"
  location            = var.ubicacion
  resource_group_name = var.grupo
  subnet_id           = var.subnet_id
  private_service_connection {
    name                           = "blob"
    private_connection_resource_id = azurerm_storage_account.this.id
    subresource_names              = ["blob"]
    is_manual_connection           = false
  }
  private_dns_zone_group {
    name                 = "blob"
    private_dns_zone_ids = [var.zona_dns_id]
  }
}

output "id" { value = azurerm_storage_account.this.id }
output "blob_endpoint" { value = azurerm_storage_account.this.primary_blob_endpoint }

output "contenedor_despliegues_id" { value = "${azurerm_storage_account.this.id}/blobServices/default/containers/despliegues" }
