[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$executablePath,

    [Parameter(Mandatory=$true)]
    [string]$serverType,
	
	[Parameter(Mandatory=$true)]
    [string]$serverName
)

Write-Host "Executing the start server script on machine '$serverName'"

if ($serverType -eq "IIS")
{
	throw [System.IO.NotImplementedException] "IIS deployment scenarios not yet implemented."
}
elseif ($serverType -eq "Kestrel")
{
	Write-Host "Starting the process '$executablePath'"
	& $executablePath --server.urls http://$($serverName):5000/   
}
elseif ($serverType -eq "WebListener")
{
	Write-Host "Starting the process '$executablePath'"
	& $executablePath --server.urls http://$($serverName):5000/ --server "Microsoft.AspNetCore.Server.WebListener"
}
else
{
	throw [System.IO.InvalidOperationException] "Server type '$serverType' is not supported."
}