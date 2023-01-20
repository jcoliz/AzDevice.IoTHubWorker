###
#
# Create a multiple-device enrollment group using symmetric keys
#
##

# Ensure needed env vars are set
$vars = @{
    RESOURCEGROUP = "Name of resource group for all resources"
    DPSNAME = "Name of device provisioning service instance"
}

foreach( $var in $vars.GetEnumerator() )
{
    if (-not (Test-Path env:$($var.key))) 
    { 
        Write-Output "Please set env:$($var.Key) to $($var.Value)"
        Exit 
    }
}

# Generate keys
$env:PK = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes("$(Get-Random)/$(Get-Random)"))
$env:SK = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes("$(Get-Random)/$(Get-Random)"))

# Create the enrollment group
$EnrollmentId = "devices"
az iot dps enrollment-group create -g $env:RESOURCEGROUP --dps-name $env:DPSNAME --enrollment-id $EnrollmentId --primary-key $env:PK --secondary-key $env:SK
