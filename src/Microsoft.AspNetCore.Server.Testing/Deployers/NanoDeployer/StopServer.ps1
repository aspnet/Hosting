[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$serverProcessName,

    [Parameter(Mandatory=$true)]
    [string]$serverType,

	[Parameter(Mandatory=$true)]
	[string]$serverName
)

Write-Host "Executing the stop server script on machine '$serverName'"

if($serverType -eq "IIS")
{
	throw [System.IO.NotImplementedException] "IIS deployment scenarios not yet implemented." 
}
else
{
	Write-Host "Stopping the process '$serverProcessName'"
	Stop-Process -Name "$serverProcessName"
}