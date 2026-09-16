variable "environment" {
  type        = string
  description = "Deployment environment (dev, staging, prod)"
  default     = "prod"
}

variable "location" {
  type        = string
  description = "Azure region for resources"
  default     = "Southeast Asia"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group"
  default     = "rg-analytics-platform-prod"
}

variable "sql_admin_username" {
  type        = string
  description = "Administrator username for Azure SQL Server"
  default     = "analyticsadmin"
}

variable "sql_admin_password" {
  type        = string
  description = "Administrator password for Azure SQL Server"
  sensitive   = true
}

variable "api_image_tag" {
  type        = string
  description = "Docker image tag for AnalyticsPlatform.Api"
  default     = "latest"
}

variable "key_vault_sku" {
  type        = string
  description = "SKU for Azure Key Vault"
  default     = "standard"
}

variable "fabric_capacity_sku" {
  type        = string
  description = "Fabric / Power BI Embedded capacity SKU"
  default     = "F64"
}

