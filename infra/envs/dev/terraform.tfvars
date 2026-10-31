# Solo valores no sensibles. correo_alertas y admin_grupo_sql se pasan como TF_VAR_* (pipeline o scripts/azure/desplegar.ps1):
# si se definieran aquí, este archivo tendría prioridad sobre TF_VAR_* y se usarían valores de ejemplo.
ubicacion      = "eastus"
buzon_servicio = "probank-notificaciones@<dominio>"
# Restricciones de esta suscripción: sin cuota de App Service (F1) en eastus/centralus y sin aprovisionamiento de
# Azure SQL en eastus/centralus. westcentralus permite ambos.
ubicacion_apps = "westcentralus"
ubicacion_sql  = "westcentralus"
