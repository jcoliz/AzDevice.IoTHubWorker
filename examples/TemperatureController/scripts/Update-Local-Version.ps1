if (Test-Path env:BUILD_BUILDID) 
{
    $Version = "ci-$env:BUILD_BUILDID"
}
else 
{    
    $Commit = git describe --always
    $Version = "local-$Commit"
}

Write-Output "{ ""Version"": ""$Version"" }"