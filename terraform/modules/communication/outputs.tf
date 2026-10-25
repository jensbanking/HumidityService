output "communication_service_id" {
  description = "Resource ID of the Communication Services resource (scope for the Function App's send-email role assignment)."
  value       = azurerm_communication_service.this.id
}

output "endpoint" {
  description = "Communication Services endpoint (CommunicationServices__Endpoint), used with Managed Identity."
  value       = "https://${azurerm_communication_service.this.hostname}"
}

output "sender_address" {
  description = "Sender address on the Azure-managed email domain (HumidityAlert__SenderAddress)."
  value       = "DoNotReply@${azurerm_email_communication_service_domain.azure_managed.mail_from_sender_domain}"
}
