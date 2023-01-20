# Create the deployment group
az deployment group create --name "HubDps-$(Get-Random)" --resource-group $env:RESOURCEGROUP --template-file "azuredeploy.json"

#
# Review the outputs of the deployment, then set these environment variables:
#

# HUBNAME = "Name of your IoTHub"
# HUBCSTR = "IoTHub owner connection string"
# DPSNAME = "Name of device provisioning service instance"
# IDSCOPE = "ID Scope for this device provisioning service"