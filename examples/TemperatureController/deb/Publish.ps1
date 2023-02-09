dotnet publish --configuration Release --runtime linux-x64 --no-self-contained --output publish_output/opt/TemperatureController
$Version = Get-Content .\version.txt
wsl -e deb/dpkg-deb.sh bin TemperatureController 0.0.0-$Version arm64
