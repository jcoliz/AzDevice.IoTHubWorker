#!/bin/bash
mkdir -p publish_output/etc/systemd/system
cp deb/*.service publish_output/etc/systemd/system
cp -R deb/DEBIAN publish_output/
chmod -R 0775 publish_output/DEBIAN
echo "Package: $2" >> publish_output/DEBIAN/control
echo "Version: $3" >> publish_output/DEBIAN/control
echo "Architecture: $4" >> publish_output/DEBIAN/control
dpkg-deb -Zxz --build publish_output/ $1/$2_$3_$4.deb
