# Azure Communication Services Email for the humidity alerts (Feature 5). An Azure-managed email
# domain (DoNotReply@<guid>.azurecomm.net) needs no DNS setup and its low sending limits are far
# above the at-most-one-email-per-location-per-day the alerts send.
resource "azurerm_communication_service" "this" {
  name                = "acs-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  data_location       = var.data_location

  tags = var.tags
}

resource "azurerm_email_communication_service" "this" {
  name                = "ecs-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  data_location       = var.data_location

  tags = var.tags
}

resource "azurerm_email_communication_service_domain" "azure_managed" {
  name              = "AzureManagedDomain"
  email_service_id  = azurerm_email_communication_service.this.id
  domain_management = "AzureManaged"

  tags = var.tags
}

resource "azurerm_communication_service_email_domain_association" "this" {
  communication_service_id = azurerm_communication_service.this.id
  email_service_domain_id  = azurerm_email_communication_service_domain.azure_managed.id
}
