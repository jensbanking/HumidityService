# Provides the VNet used to firewall Azure SQL: the Function App regional-VNet-integrates
# into the delegated subnet below, and the SQL module allows traffic only from that subnet
# (via a service-endpoint VNet rule) instead of allowing the public internet.

resource "azurerm_virtual_network" "this" {
  name                = "vnet-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  address_space       = var.address_space

  tags = var.tags
}

resource "azurerm_subnet" "function" {
  name                 = "snet-function"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = [var.function_subnet_prefix]

  # Required for regional VNet Integration (Premium/Elastic Premium Function App plans).
  delegation {
    name = "function-delegation"

    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }

  # Lets the delegated subnet reach Azure SQL and Storage over the Microsoft backbone
  # instead of the public endpoint, which is what the SQL virtual-network-rule firewalls on.
  service_endpoints = ["Microsoft.Sql", "Microsoft.Storage"]
}
