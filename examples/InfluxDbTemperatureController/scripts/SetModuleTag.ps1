# Ensure needed env vars are set
$vars = @{
    ACRSERVER = "Logon server for container registry"
}

foreach( $var in $vars.GetEnumerator() )
{
    if (-not (Test-Path env:$($var.key))) 
    { 
        Write-Output "Please set env:$($var.Key) to $($var.Value)"
        Exit 
    }
}

$ModuleName = "tempcontroller"
$ModuleVer = $(Get-Date -Format "yyyyMMdd-HHmmss")
$ModuleTags = "${env:ACRSERVER}/${ModuleName}:local-$ModuleVer-amd64"
$ModuleTags > obj\tags.txt