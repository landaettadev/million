locals {
  name_prefix = "${var.project_name}-${var.environment}"
}

resource "azurerm_resource_group" "rg" {
  name     = "${local.name_prefix}-rg"
  location = var.location
}

resource "random_string" "suffix" {
  length  = 6
  lower   = true
  upper   = false
  numeric = true
  special = false
}

# Storage account for images
resource "azurerm_storage_account" "sa" {
  name                     = replace(substr("${var.project_name}${var.environment}${random_string.suffix.result}", 0, 24), "-", "")
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  allow_blob_public_access = false
  min_tls_version          = "TLS1_2"
}

resource "azurerm_storage_container" "images" {
  name                  = "property-images"
  storage_account_name  = azurerm_storage_account.sa.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "thumbs" {
  name                  = "thumbnails"
  storage_account_name  = azurerm_storage_account.sa.name
  container_access_type = "private"
}

# Application Insights
resource "azurerm_application_insights" "appi" {
  name                = "${local.name_prefix}-appi"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
}

# Key Vault
resource "azurerm_key_vault" "kv" {
  name                       = "${replace(local.name_prefix, "-", "")}${random_string.suffix.result}kv"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_enabled        = true
  purge_protection_enabled   = true
  enable_rbac_authorization  = true
  public_network_access_enabled = true
}

data "azurerm_client_config" "current" {}

resource "azurerm_key_vault_secret" "jwt" {
  name         = "JWT--KEY"
  value        = var.jwt_secret
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "mongo" {
  name         = "MONGO--CONNECTION--STRING"
  value        = var.mongo_connection_string
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "storage_conn" {
  name         = "AZURESTORAGE--CONNECTIONSTRING"
  value        = azurerm_storage_account.sa.primary_connection_string
  key_vault_id = azurerm_key_vault.kv.id
}

# App Service Plan + Web App (API)
resource "azurerm_service_plan" "asp" {
  name                = "${local.name_prefix}-asp"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Linux"
  sku_name            = "B1"
}

resource "azurerm_linux_web_app" "api" {
  name                = "${local.name_prefix}-api"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  service_plan_id     = azurerm_service_plan.asp.id

  https_only = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
    ftps_state = "Disabled"
    minimum_tls_version = "1.2"
  }

  app_settings = {
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.appi.connection_string
    "AzureStorage__ConnectionString"        = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=${azurerm_key_vault_secret.storage_conn.name})"
    "AzureStorage__ContainerName"           = azurerm_storage_container.images.name
    "JWT__KEY"                               = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=${azurerm_key_vault_secret.jwt.name})"
    "ConnectionStrings__Mongo"              = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=${azurerm_key_vault_secret.mongo.name})"
  }
}

# Grant Web App access to Key Vault (RBAC)
resource "azurerm_role_assignment" "kv_reader" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_linux_web_app.api.identity[0].principal_id
}

output "api_url" { value = azurerm_linux_web_app.api.default_hostname }
output "storage_account_name" { value = azurerm_storage_account.sa.name }
output "key_vault_name" { value = azurerm_key_vault.kv.name }
output "app_insights_connection_string" { value = azurerm_application_insights.appi.connection_string }


