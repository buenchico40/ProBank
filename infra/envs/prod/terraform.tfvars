# Solo valores no sensibles. correo_alertas y admin_grupo_sql se pasan como TF_VAR_* (pipeline o scripts/azure/desplegar.ps1):
# si se definieran aquí, este archivo tendría prioridad sobre TF_VAR_* y se usarían valores de ejemplo.
ubicacion      = "eastus"
buzon_servicio = "probank-notificaciones@<dominio>"
