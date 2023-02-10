#
# Build a DEB package locally
#
# NOTE: Run from project root directory
#

$Application = Split-Path $pwd -Leaf

if (Test-Path publish_output)
{
    Remove-Item -Recurse publish_output    
}

dotnet publish --configuration Release --runtime linux-x64 --no-self-contained --output publish_output/opt/$Application
$Version = Get-Content .\version.txt
wsl -e deb/dpkg-deb.sh bin $Application 0.0.0-$Version amd64
