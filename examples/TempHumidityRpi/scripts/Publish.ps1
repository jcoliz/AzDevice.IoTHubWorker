dotnet publish --configuration Release --runtime linux-arm64 --no-self-contained --output publish_output
Compress-Archive -Force -Path .\publish_output\* -DestinationPath .\bin\TempHumidityRpi.zip
