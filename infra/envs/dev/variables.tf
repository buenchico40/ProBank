variable "ubicacion" {
  type    = string
  default = "eastus"
}
variable "correo_alertas" { type = string }
variable "buzon_servicio" { type = string }
variable "admin_grupo_sql" {
  type        = object({ nombre = string, object_id = string })
  description = "Administrador Entra ID del servidor SQL (grupo de DBA o la cuenta que despliega)"
}
variable "ip_despliegue" {
  type        = string
  default     = ""
  description = "Solo durante un despliegue (scripts/azure/desplegar.ps1): IP que puede llegar a SQL, Key Vault y la API. Vacío = todo privado."
}
variable "ubicacion_apps" {
  type        = string
  default     = ""
  description = "Región del App Service si difiere de ubicacion (p. ej. por cuota de la suscripción). Vacío = ubicacion."
}

variable "ubicacion_sql" {
  type        = string
  default     = ""
  description = "Región de Azure SQL si difiere de ubicacion (algunas suscripciones tienen el aprovisionamiento restringido por región). Vacío = ubicacion."
}

variable "devextreme_licencia" {
  type        = string
  default     = ""
  sensitive   = true
  description = "Clave de licencia de DevExtreme (DevExpress Download Manager). No va en el repositorio: la pasa scripts/azure/desplegar.ps1. Vacío = modo de evaluación."
}
