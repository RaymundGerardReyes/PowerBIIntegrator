terraform {
  required_version = ">= 1.5.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.116"
    }
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy    = false
      recover_soft_deleted_key_vaults = true
    }
  }
}

data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "platform" {
  name     = var.resource_group_name
  location = var.location

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
    Project     = "AnalyticsPlatform"
  }
}

# 1. Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "logs" {
  name                = "log-analytics-${var.environment}"
  location            = azurerm_resource_group.platform.location
  resource_group_name = azurerm_resource_group.platform.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = azurerm_resource_group.platform.tags
}

# 2. Azure Container Apps Environment & Host
resource "azurerm_container_app_environment" "env" {
  name                       = "cae-analytics-${var.environment}"
  location                   = azurerm_resource_group.platform.location
  resource_group_name        = azurerm_resource_group.platform.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.logs.id

  tags = azurerm_resource_group.platform.tags
}

resource "azurerm_container_app" "api" {
  name                         = "ca-analytics-api-${var.environment}"
  container_app_environment_id = azurerm_container_app_environment.env.id
  resource_group_name          = azurerm_resource_group.platform.name
  revision_mode                = "Single"

  template {
    container {
      name   = "analytics-api"
      image  = "ghcr.io/raymundgerardreyes/analyticsplatform-api:${var.api_image_tag}"
      cpu    = 1.0
      memory = "2.0Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "prod" ? "Production" : "Development"
      }
      env {
        name  = "ConnectionStrings__Default"
        value = "Server=tcp:${azurerm_mssql_server.sql.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.db.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
      }

      liveness_probe {
        path             = "/health/live"
        port             = 8080
        transport        = "HTTP"
        interval_seconds = 15
      }

      readiness_probe {
        path             = "/health/live"
        port             = 8080
        transport        = "HTTP"
        interval_seconds = 10
      }
    }
  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = true
    target_port                = 8080

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  tags = azurerm_resource_group.platform.tags
}

# 3. Azure SQL Server & Database
resource "azurerm_mssql_server" "sql" {
  name                         = "sql-analytics-${var.environment}"
  resource_group_name          = azurerm_resource_group.platform.name
  location                     = azurerm_resource_group.platform.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_username
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"

  tags = azurerm_resource_group.platform.tags
}

resource "azurerm_mssql_database" "db" {
  name           = "sqldb-analytics-${var.environment}"
  server_id      = azurerm_mssql_server.sql.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  max_size_gb    = 50
  sku_name       = "GP_Gen5_2"
  zone_redundant = var.environment == "prod"

  tags = azurerm_resource_group.platform.tags
}

resource "azurerm_mssql_firewall_rule" "azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sql.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# 4. Azure Key Vault
resource "azurerm_key_vault" "kv" {
  name                       = "kv-analytics-${var.environment}"
  location                   = azurerm_resource_group.platform.location
  resource_group_name        = azurerm_resource_group.platform.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = var.key_vault_sku
  soft_delete_retention_days = 90
  purge_protection_enabled   = var.environment == "prod"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get", "List", "Set", "Delete", "Purge", "Recover"
    ]
  }

  tags = azurerm_resource_group.platform.tags
}

resource "azurerm_key_vault_secret" "sql_connection" {
  name         = "ConnectionStrings--Default"
  value        = "Server=tcp:${azurerm_mssql_server.sql.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.db.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = azurerm_key_vault.kv.id
}
