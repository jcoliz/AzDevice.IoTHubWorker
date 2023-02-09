dotnet publish --configuration Release --runtime linux-arm64 --no-self-contained --output publish_output/opt/TempHumidityRpi
$Version = Get-Content .\version.txt
wsl -e deb/dpkg-deb.sh bin TempHumidityRpi 0.0.0-$Version arm64
