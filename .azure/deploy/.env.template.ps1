$env:RESOURCEGROUP = "Name of resource group"
$env:HUBNAME = "Name of your IoTHub"
$env:HUBCSTR = "IoTHub owner connection string"
$env:DPSNAME = "Name of device provisioning service instance"
$env:IDSCOPE = "ID Scope for this device provisioning service"
$env:DEVICEID = "Your assigned ID for the single device. I recommend using the MAC address"
$env:PK = "Primary Key for your enrollment group"
# Note that $env:DEVICEKEY expects to have the quotes IN the value, unlike the others
$env:DEVICEKEY = '"Device-specific key, generated by Compute-DeviceKey.ps1"' 
