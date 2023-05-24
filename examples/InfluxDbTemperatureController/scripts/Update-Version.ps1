#
# Used by the build process to inject the current version of software
# being built as an assembly resource
#

if (Test-Path env:SOLUTION_VERSION) 
{
    $Version = "$env:SOLUTION_VERSION"
}
else 
{    
    $Commit = git describe --always
    $Version = "local-$Commit"
}

Write-Output $Version