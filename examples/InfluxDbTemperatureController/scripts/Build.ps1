# Set project-local vars
$ModuleTags = Get-Content -Path obj\tags.txt

docker build --rm -f .\docker\Dockerfile.amd64 -t $ModuleTags .
