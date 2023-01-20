$Version = git describe --always
Write-Output "{ ""Version"": ""local-$Version"" }"