variable "nombre" { type = string }
variable "ubicacion" { type = string }
variable "grupo" { type = string }
variable "tenant_id" { type = string }
variable "subnet_id" {
  type    = string
  default = null
}
variable "zona_dns_id" {
  type    = string
  default = null
}
variable "desplegador_object_id" {
  type        = string
  description = "Identidad que ejecuta Terraform: necesita el plano de datos para crear la CMK y los secretos."
}
variable "red_privada" {
  type        = bool
  default     = true
  description = "true: solo private endpoint (qa/prod). false: acceso público protegido por RBAC (dev de bajo costo)."
}
variable "sku" {
  type    = string
  default = "premium" # llaves HSM para la CMK de Always Encrypted
}
variable "purge_protection" {
  type    = bool
  default = true
}
variable "ip_despliegue" {
  type        = string
  default     = ""
  description = "Con red privada, solo durante un despliegue: permite el acceso público únicamente desde esta IP."
}
variable "etiquetas" { type = map(string) }

locals { abierta = var.ip_despliegue != "" }

resource "azurerm_key_vault" "this" {
  name                          = "kv-${var.nombre}"
  location                      = var.ubicacion
  resource_group_name           = var.grupo
  tenant_id                     = var.tenant_id
  sku_name                      = var.sku
  rbac_authorization_enabled    = true
  purge_protection_enabled      = var.purge_protection
  soft_delete_retention_days    = var.purge_protection ? 90 : 7
  public_network_access_enabled = !var.red_privada || local.abierta
  network_acls {
    default_action = var.red_privada ? "Deny" : "Allow"
    bypass         = "AzureServices"
    ip_rules       = var.red_privada && local.abierta ? [var.ip_despliegue] : []
  }
  tags = var.etiquetas
}

resource "azurerm_role_assignment" "desplegador" {
  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Administrator"
  principal_id         = var.desplegador_object_id
}

# Los permisos RBAC tardan en propagarse; sin esta espera la primera creación de la llave falla con 403.
resource "time_sleep" "rbac" {
  depends_on      = [azurerm_role_assignment.desplegador]
  create_duration = "90s"
}

# CMK de Always Encrypted (columnas sensibles de cred y seg). HSM solo en premium.
resource "azurerm_key_vault_key" "cmk" {
  name         = "cmk-always-encrypted"
  key_vault_id = azurerm_key_vault.this.id
  key_type     = var.sku == "premium" ? "RSA-HSM" : "RSA"
  key_size     = 3072
  key_opts     = ["wrapKey", "unwrapKey", "sign", "verify"]
  depends_on   = [time_sleep.rbac]
}

resource "azurerm_private_endpoint" "this" {
  count               = var.red_privada ? 1 : 0
  name                = "pe-kv-${var.nombre}"
  location            = var.ubicacion
  resource_group_name = var.grupo
  subnet_id           = var.subnet_id
  private_service_connection {
    name                           = "kv"
    private_connection_resource_id = azurerm_key_vault.this.id
    subresource_names              = ["vault"]
    is_manual_connection           = false
  }
  private_dns_zone_group {
    name                 = "kv"
    private_dns_zone_ids = [var.zona_dns_id]
  }
  tags = var.etiquetas
}

output "id" { value = azurerm_key_vault.this.id }
output "uri" { value = azurerm_key_vault.this.vault_uri }
output "cmk_id" { value = azurerm_key_vault_key.cmk.id }
output "listo" {
  value       = time_sleep.rbac.id
  description = "Para que los secretos creados fuera del módulo esperen la propagación de RBAC."
}
