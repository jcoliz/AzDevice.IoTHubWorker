#!/bin/bash
mkdir -p publish_output/etc/systemd/system
cp deb/tempcontroller.service publish_output/etc/systemd/system
mkdir -p publish_output/DEBIAN
chmod 0775 publish_output/DEBIAN
cp deb/control publish_output/DEBIAN
echo "Package: $1" >> publish_output/DEBIAN/control
echo "Version: $2" >> publish_output/DEBIAN/control
echo "Architecture: $3" >> publish_output/DEBIAN/control
dpkg-deb --build publish_output/ $1-$2-$3.deb
