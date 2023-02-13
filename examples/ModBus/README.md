# Temperature and Humidity Example using ModBus

![ModBus XY-MD02](/docs/images/modbus-xy-md02.jpg)

This example shows how to use the Azure IoT Device Worker to
model a ModBus-based temperature & humidity sensor with IoT Plug-and-Play, 
connected to Azure IoT Hub. While the example itself is focused
on one single ModBus-based sensor, the techniques here are broadly
applicable to any unit running on ModBus.

## Bill of Materials

* XY-MD02 ModBus temp/humidity sensor
* Waveshare USB to RS485 converter
* Power supply (DC 5-30V)

## Background Reading

ModBus is a pretty straightforward protocol. Here is some reading you may find useful
if you don't use ModBus every day. Please note that all my code and documentation
use the modern terms for ModBus components: "Client" and "Server".

* Siemens
* Simply ModBus tool
* Fluent ModBus package