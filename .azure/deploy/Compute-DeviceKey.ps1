###
#
# Compute a device-specific key for one device
#
##

# Ensure needed env vars are set
$vars = @{
    DEVICEID = "Your assigned ID for the single device. I recommend using the MAC address"
    PK = "Primary Key for your enrollment group"
}

foreach( $var in $vars.GetEnumerator() )
{
    if (-not (Test-Path env:$($var.key))) 
    { 
        Write-Output "Please set env:$($var.Key) to $($var.Value)"
        Exit 
    }
}

# Create the device key
$env:DEVICEKEY = az iot dps enrollment-group compute-device-key --key $env:PK --registration-id $env:DEVICEID
$env:DEVICEKEY
