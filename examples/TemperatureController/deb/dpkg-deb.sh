#!/bin/bash
mkdir -p publish_output/etc/systemd/system
cp deb/tempcontroller.service publish_output/etc/systemd/system
mkdir -p publish_output/DEBIAN
cp deb/control publish_output/DEBIAN
cp deb/postinst publish_output/DEBIAN
cp deb/postrm publish_output/DEBIAN
cp deb/prerm publish_output/DEBIAN
chmod -R 0775 publish_output/DEBIAN
echo "Package: $2" >> publish_output/DEBIAN/control
echo "Version: $3" >> publish_output/DEBIAN/control
echo "Architecture: $4" >> publish_output/DEBIAN/control
dpkg-deb --build publish_output/ $1/$2_$3_$4.deb
