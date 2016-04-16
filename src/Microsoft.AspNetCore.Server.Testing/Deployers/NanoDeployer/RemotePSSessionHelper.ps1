[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$serverName,

	[Parameter(Mandatory=$true)]
	[string]$accountName,

    [Parameter(Mandatory=$true)]
    [string]$accountPassword,

    [Parameter(Mandatory=$true)]
    [string]$executablePath,

    [Parameter(Mandatory=$true)]
    [string]$serverType,
	
	[Parameter(Mandatory=$true)]
	[string]$serverAction
)

Write-Host "`nExecuting deployment helper script on machine '$serverName'"
Write-Host "`nSarting a powershell session to machine '$serverName'"

$securePassword = ConvertTo-SecureString $accountPassword -AsPlainText -Force
$credentials= New-Object System.Management.Automation.PSCredential ($accountName, $securePassword)
$psSession = New-PSSession -ComputerName $serverName -credential $credentials

if ($serverAction -eq "StartServer")
{
	Write-Host "Starting the application on machine '$serverName'"
	$startServerScriptPath = "$PSScriptRoot\StartServer.ps1"
	Invoke-Command -Session $psSession -FilePath $startServerScriptPath -ArgumentList $executablePath, $serverType, $serverName
	Remove-PSSession $psSession
}
else
{
	Write-Host "Stopping the application on machine '$serverName'"
	$stopServerScriptPath = "$PSScriptRoot\StopServer.ps1"
	$serverProcessName = [System.IO.Path]::GetFileNameWithoutExtension($executablePath)
	Invoke-Command -Session $psSession -FilePath $stopServerScriptPath -ArgumentList $serverProcessName, $serverType, $serverName
	Remove-PSSession $psSession
}

