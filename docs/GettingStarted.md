# Getting started with Azure IoT Hub and IoT Plug-and-Play

## Prerequisites

* Powershell
* .NET 7 SDK
* Azure Subscription
* Azure CLI
* Azure IoT CLI Add-in
* Azure IoT Explorer

## Bring up Services

Can I give you a pro tip for dealing with Azure services? **Always** bring up services
using an Azure Resource Manager (ARM) template. This way you'll always have a record
of what you tried, what worked, what didn't work, and what is the configuration you
finally wanted to run with. Check these into source control for an unlimited history
of your experiments. Once you're finished, you can be certain the services will
always be created exactly correctly, no guessing.

### Create Resource Group

1. Open a powershell window, and change to the `.azure/deploy` directory.
2. Set `$env:RESOURCEGROUP` to the name of a resource group which does not yet exist in your Azure subscription
3. Run `Create-ResourceGroup.ps1`

This uses the Azure CLI to create a new resource group:
```powershell
az group create --name $env:RESOURCEGROUP --location "West US 2"
```

### Deploy IoT Hub & DPS

1. Run `Deploy-Services.ps1`

This deploys an ARM template to create a new Azure IoT Hub and Device Provisioning Services instances 
in your resource group:
```powershell
az deployment group create --name "HubDps-$(Get-Random)" --resource-group $env:RESOURCEGROUP --template-file "azuredeploy.json"
```

The outputs from this deployment will include several configuration values which you'll
need to make note of for further steps.

```json
    "outputs": {
      "$env:DPSNAME": {
        "type": "String",
        "value": "dps-redacted"
      },
      "$env:HUBCSTR": {
        "type": "String",
        "value": "HostName=iothub-redacted.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=redacted/redacted="
      },
      "$env:HUBNAME": {
        "type": "String",
        "value": "iothub-redacted"
      },
      "$env:IDSCOPE": {
        "type": "String",
        "value": "0ne00redacted"
      }
    },
```

### Save Environment Vars

This is a great time to keep track of your environment configuration by creating an 
environment variables file. Copy the `.env.template.ps1` file over to your own file,
e.g. `env.ps1`. Then fill in the values with the ones shown as `outputs` from your
deployment.

The remaining scripts rely on these variables being set, as well as contributing
new variables.

### Create Enrollment Group

1. Run `Create-EnrollmentGroup.ps1` 

This creates an enrollment group in the Device Provisioning Service. This will make
it super easy to add new devices later:
```powershell
# Generate keys
$env:PK = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes("$(Get-Random)/$(Get-Random)"))
$env:SK = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes("$(Get-Random)/$(Get-Random)"))

# Create the enrollment group
$EnrollmentId = "devices"
az iot dps enrollment-group create -g $env:RESOURCEGROUP --dps-name $env:DPSNAME --enrollment-id $EnrollmentId --primary-key $env:PK --secondary-key $env:SK
```

Be sure to add `$env:PK` and `$env:SK` to your environment variables file.

## Enroll Device

At this point, our services are all set up and ready to go. What's left is to enroll the specific device
we're using (our development PC in this case) into the service.

### Compute Device Key

1. Set `$env:DEVICEID` to the name you'd like to use for the device you're enrolling
2. Run `Compute-DeviceKey.ps1`

This creates a unique key for this device to connect to the enrollment group you've already
created:
```powershell
az iot dps enrollment-group compute-device-key --key $env:PK --registration-id $env:DEVICEID
```

### Generate Device Config (config.toml)

1. Run `Generate-DeviceConfig.ps1`

This script uses `config.template.toml` as a template to generate a unique `config.toml` for this device,
by replacing the needed tokens with correct values based on your solution.

## Build/Run Device Software

To make things easy, you can follow along with the TemperatureController example device software.
This implements the example TemeratureController device model from the DTMI repo.
Along the way, you can see how IoT Plug-and-Play is a powerful tool for IoT application
development.

### Place config file

1. Change directory to `examples/TemperatureController` for the remaining steps
2. Copy the config file from the previous steps, `.azure/deploy/config.toml`, to this directory.

### Build & Run

Once the device configuration is in place, simply run the app. It will pick up the configuration,
connect, and immediately start working with IoT Hub.

```
PS AzDevice-IotHubWorker\examples\TemperatureController> dotnet run
Building...
<6>AzDevice.IoTHubWorker[100] Started OK
<6>AzDevice.IoTHubWorker[200] Initial State: OK Applied 5 keys
<6>AzDevice.IoTHubWorker[101] Device: Example Thermostat Controller S/N:1234567890-ABCDEF ver:local-6759b72 dtmi:com:example:TemperatureController;2
<6>Microsoft.Hosting.Lifetime[0] Application started. Press Ctrl+C to shut down.
<6>Microsoft.Hosting.Lifetime[0] Hosting environment: Production
<6>Microsoft.Hosting.Lifetime[0] Content root path: \AzDevice-IotHubWorker\examples\TemperatureController
<6>AzDevice.IoTHubWorker[300] Provisioning: OK. Device redacted-01 on Hub iothub-redacted.azure-devices.net
<6>AzDevice.IoTHubWorker[400] Connection: OK.
<6>AzDevice.IoTHubWorker[500] Telemetry: OK 3 messages
```

If everything was configured correctly, you'll see your device name connected to the hub you just created.

## View Device in IoT Explorer

The Azure IoT Explorer makes it easy to find our devices and communicate with them. Because we're using
an IoT Plug-and-Play model, the Explorer automatically creates UI to interact with our device in the
way we've described in the DTMI model. It knows what telemetry to expect, which commmands we can send,
which properties we can view, and which properties we can change. 

This all makes it an incredibly powerful tool for deployment. We can first create our interface based
on the problem domain. Then develop to fulfill that interface while using IoT Explorer as a front-end.
Finally, we can integrate the back-end services once we have a device we know is working correctly.

To get started:

1. Launch the Azure IoT Explorer
2. Grab your IoT Hub Connection String `$env:HUBCSTR` from your environment vars
3. Choose `Add connection`
4. Enter the IoT Hub Connection String into the `Connection string` text box.
5. Press `Save`
6. Then, back on the main page, `-> View devices in this hub`

## View Telemetry

## View/Set Properties

## Send Commands

## What's next

* Try different device models
* Create your OWN device model
* Deploy to your Linux-based IoT device
* Use Azure Pipelines to create a Continous Integration workflow deploying deb packages to a private deb repository
* Deploy to Raspberry Pi
* Send data from real sensors
* Use Azure IoT Edge as an intelligent gateway