if (Test-Path env:BUILD_BUILDID) 
{
    $Version = "0.0.0-ci-$env:BUILD_BUILDID"
}
else 
{    
    $Commit = git describe --always
    $Version = "local-$Commit"
}

Write-Output $Version