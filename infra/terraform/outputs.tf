output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.platform.name
}

output "api_fqdn" {
  description = "Fully Qualified Domain Name of the API Container App"
  value       = azurerm_container_app.api.latest_revision_fqdn
}

output "sql_server_fqdn" {
  description = "Fully Qualified Domain Name of the SQL Server"
  value       = azurerm_mssql_server.sql.fully_qualified_domain_name
}

output "key_vault_uri" {
  description = "URI of the Azure Key Vault"
  value       = azurerm_key_vault.kv.vault_uri
}
