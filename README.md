# AzDevice IoT Hub Worker

This is a toolkit for quickly creating IoT Plug and Play compliant control software for Azure IoT Hub-connected devices using .NET.

## Why?

The basic work of connecting with Azure IoT Hub, following the IoT Plug and Play conventions, formatting messages, receiving commands or property updates--this all tends to look the same in each solution. 
My goal with the IoT Hub Worker is to write all that stuff just once. 
This leaves the application to focus on only the solution-specific work of implementing the telemetry, properties, and commands of a particular interface.

Ultimately this makes it much faster to bring up a new proof of concept connected to Azure IoT.

## Basic Idea

The IoT Hub Worker is an Inversion of Control framework, which handles the communication between the device and IoT Hub. It will provision the device with DPS,
then handle the initial IoT Hub setup and ongoing communication.

You set this up by defining a `Program.cs` which looks like the example below. The `ControllerModel` class in this example contains the application-specific logic, which will
be called by the IoT Hub Worker as needed. The [IRootModel](./src/AzDevice.IoTHubWorker/Models/IRootModel.cs) and [IComponentModel](./src/AzDevice.IoTHubWorker/Models/IComponentModel.cs) interfaces define the points of interaction where the application-specific logic can expect control. 

```c#
IHost host = Host.CreateDefaultBuilder(args)
    .UseSystemd() 
    .ConfigureServices(services =>
    {
        services.AddHostedService<IoTHubWorker>();
        services.AddSingleton<IRootModel,ControllerModel>();
    })
    .ConfigureAppConfiguration(config =>
    {
        config.AddTomlFile("config.toml", optional: true, reloadOnChange: true);
    })
    .Build();

host.Run();
```

To take control of the inner loop, while still taking advantage of the individual functions of the base class,  
implement a derived class and override `ExecuteAsync()`. The individual components called from there could
be invidually used by a derived class.

## Feature Set

* Automatically provision device using Device Provisioning Service
* Make connection with Azure IoT Hub
* Read initial configuration state from standard .NET configuration sources
* Send telemetry automatically on a configurable period
* Report actual properties automatically at increasingly longer intervals
* Receive desired property updates, passing them through to component implementation
* Receive commands from IoT Hub, passing them through to component implementation
* Follows IoT Plug and Play conventions to enable a model-based solution
* Automatically supply a standard DeviceInformation model
* Detailed multi-level logging to help with troubleshooting
* Integrates with systemd to run as a background service on Linux
* Copious examples showing usage with simulated and phyiscal devices on a variety of busses

## How To...

* [Get Started with Azure IoT](/docs/GettingStarted.md). This guide presents the PowerShell-based workflow I use to quickly bring up a new set of services and provision new devices. We can create and configure an IoT hub, as well as set up the device provisioning service, in seconds.
* [Install a .NET Worker Service on Linux](/docs/InstallOnLinux.md). The primary real-life usage of the IoT Hub Worker Service is running on Linux devices in the field. This guide shows how to install any .NET Worker Service into a running Linux system.
* [Package a .NET Worker Service as a DEB](/docs/PackageAsDeb.md). Running on a Debian-based OS like Rasperry Pi OS or Ubuntu? The easiest way to move your code in place, and keep it up to date, is to package it as a DEB.
* [Interact with physical sensors using .NET](/docs/DotNetIot.md)
* [Interact with a Modbus sensor using .NET](/docs/FluentModBus.md)
* [Connect physical sensors on Raspberry Pi to Azure IoT](/docs/RunOnRPi.md). This guide demonstrates how to model a physical temperature & humidity sensor with IoT Plug and Play, run on a Raspberry Pi, and send data from this sensor to Azure IoT Hub. 
* [Add IoT Plug and Play capabilities to a device in a model-driven solution](/docs/CustomDtmi.md). Why I love IoT Plug and Play, and you should too!

## Examples

There are some examples which help show how to use the IoT Hub Worker.

* [TemperatureContoller](/examples/TemperatureController/). Canonical example, simulates a "dtmi:com:example:Thermostat;2" model, with all telemetry, properties, and commands.
* [ClimateMontitor](/examples/ClimateMonitor/). Another example of implementing an existing model with simulated data. This one simulates "dtmi:com:example:climatemonitor;1".
* [I2cTempHumidityMonitor](/examples/I2cTempHumidityMonitor/). This example is targeted toward a real physical sensor running on a Raspberry Pi. For easy testing, you can run this on a Windows PC and generate simulated data.
* [ModbusTempHumidityMonitor](/examples/ModbusTempHumidityMonitor/). This example shows how to connect to a sensor via Modbus. It will run on Windows or Linux, as you can easily use a USB-to-RS485 dongle to make the physical connection on any machine.

