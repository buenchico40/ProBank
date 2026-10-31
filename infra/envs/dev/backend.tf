terraform {
  backend "azurerm" {
    resource_group_name  = "rg-probank-tfstate"
    storage_account_name = "stprobanktfstate"
    container_name       = "tfstate"
    key                  = "probank-dev.tfstate"
    use_azuread_auth     = true
  }
}
