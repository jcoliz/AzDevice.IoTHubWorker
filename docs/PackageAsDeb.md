# Package a .NET Worker Service as a DEB

## Why?

Let's say you've built a .NET Worker Service, and already have it [installed on Linux](/docs/InstallOnLinux.md). 
Now you need an easy way to quickly get all the files in the right place every time.
You need to build a DEB package!

## Layout

Fundamentally, a DEB package is an archive file, with the files therein located where you want them on the target system, plus a packing list (here called a 'control file'), and some scripts. 

Let's use the [Temperature Controller](/examples/TemperatureController/) example from `AzDevice.IoTHubWorker`. Here is the file layout of the DEB package:

* /opt/TemperatureController: Output of dotnet publish
* /etc/systemd/system/tempcontroller.service: SystemD Unit file
* /DEBIAN/control: Control file
* /DEBIAN/{pre,post}{inst,rm}: Pre/post install/remove scripts

The files in the `DEBIAN` directory won't be installed. They're giving instruction to the package manager toolchain.

## Control File

The [control file](https://www.debian.org/doc/debian-policy/ch-controlfields.html) gives the packaging tools the necessary context
and details about your package.

Using the Temperature Controller example again, here's an example of what mine looks like:

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

To build a DEB package out of a .NET application in Azure Pipelines, it is the same steps described above, just translated into Azure Pipelines YAML. You can check out the overall flow I use ([ci.yaml](/.azure/pipelines/ci.yaml)), along with the DEB-packaging-specific steps ([publish-deb.yaml](/.azure/pipelines/steps/publish-deb.yaml)).

## See it in action

For a complete step-by-step guide to building an AzDevice example project into a DEB file, and running it on Raspberry Pi,
check out the [Connect physical sensors on Raspberry Pi to Azure IoT](/docs/RunOnRPi.md) guide.

## Download from build server / Copy to target machine

Download it from build server
Copy it to target machine

## Install with sudo apt install

```
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
```

## You're done!

At this point, you've packaged your .NET Worker Service as a DEB package, and installed it on the target machine. 
From here, everything should work just as if you'd copied the files over directly.

Start the service, and check status:

```
$ sudo systemctl start tempcontroller
$ sudo systemctl status tempcontroller

● tempcontroller.service - Temperature Controller
     Loaded: loaded (/etc/systemd/system/tempcontroller.service; enabled; vendor preset: enabled)
     Active: active (running) since Thu 2023-01-19 20:10:35 PST; 31s ago
   Main PID: 17626 (TemperatureCont)
      Tasks: 26 (limit: 9228)
     Memory: 35.0M
     CGroup: /system.slice/tempcontroller.service
             └─17626 /opt/TemperatureController/TemperatureController

Jan 19 20:10:34 brewbox TemperatureController[17626]: AzDevice.IoTHubWorker[102] Model: dtmi:com:example:TemperatureController;2
Jan 19 20:10:34 brewbox TemperatureController[17626]: AzDevice.IoTHubWorker[200] Initial State: OK Applied 6 keys
Jan 19 20:10:34 brewbox TemperatureController[17626]: AzDevice.IoTHubWorker[101] Device: Example.json Thermostat Controller.json 0.0.2
Jan 19 20:10:35 brewbox TemperatureController[17626]: Microsoft.Hosting.Lifetime[0] Application started. Hosting environment: Production; Content root path: /opt/TemperatureController
Jan 19 20:10:35 brewbox systemd[1]: Started Temperature Controller.
```

All finished for now? Stop the service and remove the package

```
$ sudo systemctl stop tempcontroller
$ sudo apt remove temperaturecontroller
```
