# Temperature and Humidity Example for Raspberry Pi

![RPi Zero 2W with SHTC3](/docs/images/thrpi.jpg)

This example shows how to use the Azure IoT Device Worker to
model a physical temperature & humidity sensor with IoT Plug-and-Play, 
run on a Raspberry Pi, connected to Azure IoT Hub.

Along the way, it also serves as example for how to construct a new
model using the [Digital Twins Definition Language](https://github.com/Azure/opendigitaltwins-dtdl)
to meet the use cases posed by a custom scenario.

## Bill of Materials

The SparkFun Qwiic system is a handy way to quickly and reliably connect parts which use
the I<sup>2</sup>C bus. I highly recommend it for getting started. Here are the parts I'm using in this
example. While I like the Pi Zero for its price and compact form factor, any RPi should 
work fine for this.

* Raspberry Pi Zero 2 W (DEV-18713) $15
* SparkFun Qwiic HAT for Raspberry Pi (DEV-14459) $6.50
* SparkFun Humidity Sensor Breakout - SHTC3 (Qwiic) (SEN-16467) $11
* Raspberry Pi Power Supply, SoulBay 5V 3A Micro USB AC Adapter $11
* Flexible Qwiic Cable - 100mm (PRT-17259) $1.60

## Detailed Instructions

Please follow the [Run on Raspberry Pi](/docs/RunOnRPi.md) guide for detailed instructions on how
to use this example to send data from a physical sensor through to Azure IoT Hub.