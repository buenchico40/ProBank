# Infraestructura (Terraform)

```
infra/
├─ modules/   network · app-service · sql · key-vault · storage · monitoring · entra-app
└─ envs/      dev · qa · prod (cada uno con su backend remoto)
```

1. Crear una vez la cuenta de almacenamiento del estado (`tfstate`) con bloqueo y versionado.
2. `cd envs/dev && terraform init && terraform plan -out=dev.tfplan`.
3. El `apply` de qa y prod se hace solo desde `.github/workflows/infra.yml`, con aprobación.

Los valores sensibles se pasan como variables del pipeline (`TF_VAR_*`); `terraform.tfvars` solo tiene valores no sensibles.
