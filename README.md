# AzDevice IoT Hub Worker

This is a toolkit for quickly creating IoT Plug-and-Play compliant control software for Azure IoT Hub-connected devices using .NET.

## Why?

The basic work of connecting with Azure IoT Hub, following the IoT Plug and Play conventions, formatting messages, receiving commands or property updates--this all tends to look the same in each solution. 
My goal with the IoT Hub Worker is to write all that stuff just once. 
This leaves the application to focus on only the solution-specific work of implementing the telemetry, properties, and commands of a particular interface.

## How To...

* [Get Started with Azure IoT](/docs/GettingStarted.md). This guide presents the PowerShell-based workflow I use to quickly bring up a new set of services and provision new devices. We can create and configure an IoT hub, as well as set up the device provisioning service, in seconds.
* [Install a .NET Worker Service on Linux](/docs/InstallOnLinux.md). The primary real-life usage of the IoT Hub Worker Service is running on Linux devices in the field. This guide shows how to install any .NET Worker Service into a running Linux system.
* [Package a .NET Worker Service as a DEB](/docs/PackageAsDeb.md). Running on a Debian-based OS like Rasperry Pi OS or Ubuntu? The easiest way to move your code in place, and keep it up to date, is to package it as a DEB.
* [Interact with physical sensors using .NET](/docs/DotNetIot.md)
* [Interact with a ModBus sensor using .NET](/docs/FluentModBus.md)
* [Connect physical sensors on Raspberry Pi to Azure IoT](/docs/RunOnRPi.md). This guide demonstrates how to model a physical temperature & humidity sensor with IoT Plug and Play, run on a Raspberry Pi, and send data from this sensor to Azure IoT Hub. 
* [Add IoT Plug and Play capabilities to a device in a model-driven solution](/docs/CustomDtmi.md). Why I love IoT Plug and Play, and you should too!

## Examples

There are some examples which help show how to use the IoT Hub Worker.

* [TemperatureContoller](/examples/TemperatureController/). Canonical example, simulates a "dtmi:com:example:Thermostat;2" model, with all telemetry, properties, and commands.
* [ClimateMontitor](/examples/ClimateMonitor/). Another example of implementing an existing model with simulated data. This one simulates "dtmi:com:example:climatemonitor;1".
* [TempHumidityRpi](/examples/TempHumidityRpi/). This example is targeted toward a real physical sensor running on a Raspberry Pi. It will still simulate data running on a Windows PC as well.
* [ModBus](/examples/ModBus/). This example shows how to connect to a sensor via ModBus. It will run on Windows or Linux, as it you can easily use a USB-to-RS485 dongle to make the physical connection on any machine.
 