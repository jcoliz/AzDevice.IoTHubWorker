# Package a .NET Worker Service as a DEB

## Layout

Fundamentally, a DEB package is an archive file, with the files therein located where you want them on the target system, plus a packing list (here called a 'control file'), and some scripts. This is the file layout for the Temperature Controller example:

* /opt/TemperatureController: Output of dotnet publish
* /etc/systemd/system/tempcontroller.service: SystemD Unit file
* /DEBIAN/control: Control file
* /DEBIAN/{pre,post}{inst,rm}: Pre/post install/remove scripts

## Control File

The [control file](https://www.debian.org/doc/debian-policy/ch-controlfields.html) gives the packaging tools the necessary context
and details about your package.

Here's an example of what mine looks like for the Temperature Controller example:

```
Package: temperaturecontroller
Version: 0.0.0-local-1234abcd
Architecture: amd64
Description: Example temperature controller
 This is a sample for Azure IoT Device Worker
Maintainer: James Coliz <jcoliz@outlook.com>
Depends: dotnet-runtime-7.0
```

## Building the Package

There are two steps to building the package:
1. Build and publish the application
2. Create the package

Here is the script I use to control the whole package when building packages locally on my development machine. 
Note that I use the [Windows Subsystem for Linux](https://learn.microsoft.com/en-us/windows/wsl/install) 
to do this, which makes it super easy to run the `dpkg` tools on Linux. If you spend any
time developing on Windows to target Linux devices, I highly recommend having this set up.

This script expects to run in the root directory for the project being built. Thus it takes the application
name from the leaf node of the current directory.

```Powershell
$Application = Split-Path $pwd -Leaf

if (Test-Path publish_output)
{
    Remove-Item -Recurse publish_output    
}

dotnet publish --configuration Release --runtime linux-x64 --no-self-contained --output publish_output/opt/$Application
$Version = Get-Content .\version.txt
wsl -e deb/dpkg-deb.sh bin $Application 0.0.0-$Version amd64
```

Note that in this case, I am publishing it as `no-self-contained` (a.k.a "Framework-Dependent"), which greatly decreases the size of the resulting file.
To compensate for that, you'll notice that the control file adds this line: `Depends: dotnet-runtime-7.0`. The .NET runtime will need to be in place
on the target system in order for this package to run.

### deb/dpkg-deb.sh: Create the package

Here's the script which pulls everything else, besides the dotnet output, to make the package:

```
#!/bin/bash
mkdir -p publish_output/etc/systemd/system
cp deb/*.service publish_output/etc/systemd/system
cp -R deb/DEBIAN publish_output/
chmod -R 0775 publish_output/DEBIAN
echo "Package: $2" >> publish_output/DEBIAN/control
echo "Version: $3" >> publish_output/DEBIAN/control
echo "Architecture: $4" >> publish_output/DEBIAN/control
dpkg-deb --build publish_output/ $1/$2_$3_$4.deb
```

This is getting everything in place to match the file layout described above. It also generates the control file, starting with a
basic stub, and adding on the information we know just now during the build process.

### Build on Azure Pipelines

To build a DEB package out of a .NET application in Azure Pipelines, it is the same steps, just translated into Azure Pipelines YAML. You can check out the overall flow ([ci.yaml](/.azure/pipelines/ci.yaml)), and the DEB-packaging-specific steps ([publish-deb.yaml](/.azure/pipelines/steps/publish-deb.yaml)).

## See it in action

For a complete step-by-step guide to building an AzDevice example project into a DEB file, and running it on Raspberry Pi,
check out the * [Connect physical sensors on Raspberry Pi to Azure IoT](/docs/RunOnRPi.md) guide.

## Download from build server / Copy to target machine

Download it from build server
Copy it to target machine

(7) Install with sudo apt install

james@brewbox:~$ sudo apt install ./TemperatureController_0.0.0-ci-2459_amd64.deb
Reading package lists... Done
Building dependency tree
Reading state information... Done
Note, selecting 'temperaturecontroller' instead of './TemperatureController_0.0.0-ci-2459_amd64.deb'
The following NEW packages will be installed:
  temperaturecontroller
0 upgraded, 1 newly installed, 0 to remove and 13 not upgraded.
After this operation, 0 B of additional disk space will be used.
Get:1 /home/james/TemperatureController_0.0.0-ci-2459_amd64.deb temperaturecontroller amd64 0.0.0-ci-2459 [27.8 MB]
Selecting previously unselected package temperaturecontroller.
(Reading database ... 110601 files and directories currently installed.)
Preparing to unpack .../TemperatureController_0.0.0-ci-2459_amd64.deb ...
Unpacking temperaturecontroller (0.0.0-ci-2459) ...
Setting up temperaturecontroller (0.0.0-ci-2459) ...

(8) Copy over local config

(9) Start it as before

(10) Check the logs

Jan 25 20:18:54 brewbox systemd[1]: Starting Temperature Controller...
Jan 25 20:18:54 brewbox TemperatureController[3590]: AzDevice.IoTHubWorker[100] Started OK
Jan 25 20:18:54 brewbox TemperatureController[3590]: AzDevice.IoTHubWorker[200] Initial State: OK Applied 5 keys
Jan 25 20:18:54 brewbox TemperatureController[3590]: AzDevice.IoTHubWorker[101] Device: Example Thermostat Controller S/N:1234567890-ABCDEF ver:0.0.0-ci-2459 dtmi:com:example:TemperatureController;2
Jan 25 20:18:54 brewbox TemperatureController[3590]: Microsoft.Hosting.Lifetime[0] Application started. Hosting environment: Production; Content root path: /opt/TemperatureController
Jan 25 20:18:54 brewbox systemd[1]: Started Temperature Controller.
Jan 25 20:18:58 brewbox TemperatureController[3590]: AzDevice.IoTHubWorker[300] Provisioning: OK. Device tc-01 on Hub iothub-sp4mvwjwloirs.azure-devices.net
Jan 25 20:18:58 brewbox TemperatureController[3590]: AzDevice.IoTHubWorker[400] Connection: OK.
Jan 25 20:19:09 brewbox TemperatureController[3590]: AzDevice.IoTHubWorker[2300] Property: Component OK. Updated thermostat1.targetTemperature to 5000
Jan 25 20:19:09 brewbox TemperatureController[3590]: AzDevice.IoTHubWorker[2300] Property: Component OK. Updated thermostat2.targetTemperature to 100
Jan 25 20:19:10 brewbox TemperatureController[3590]: AzDevice.IoTHubWorker[500] Telemetry: OK 3 messages
Jan 25 20:19:20 brewbox TemperatureController[3590]: AzDevice.IoTHubWorker[500] Telemetry: OK 3 messages