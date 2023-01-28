if (Test-Path env:APPLICATION_VERSION) 
{
    $Version = "$env:APPLICATION_VERSION"
}
else 
{    
    $Commit = git describe --always
    $Version = "local-$Commit"
}

Write-Output $Version