terraform {
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 3.116" }
  }
}
provider "azurerm" { features {} }

resource "azurerm_resource_group" "platform" {
  name     = "rg-analytics-platform"
  location = "Southeast Asia"
}

# Placeholder: Power BI Embedded capacity, App Service / Container Apps,
# Azure SQL, Key Vault, and Fabric workspace resources go here.
