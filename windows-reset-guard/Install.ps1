#Requires -RunAsAdministrator
$ErrorActionPreference='Stop'
$dest=Join-Path $env:ProgramFiles 'EkremAmdResetGuard'
$name='EkremAmdResetGuard'
$created=$false
try {
 if(Get-Service $name -ErrorAction SilentlyContinue){throw 'Already installed. Uninstall the existing guard first.'}
 $other=@(Get-CimInstance Win32_Service | Where-Object {$_.PathName -match 'RadeonResetBugFix'})
 if($other.Count){throw 'Remove the previous RadeonResetBugFix service before installing this guard.'}
 $gpu=@(Get-PnpDevice -Class Display -PresentOnly | Where-Object {$_.InstanceId -match '^PCI\\VEN_1002&DEV_164E&'})
 if($gpu.Count -ne 1 -or $gpu[0].Status -ne 'OK'){throw 'Exactly one healthy Raphael AMD GPU is required. Recover it first.'}
 New-Item -ItemType Directory -Force $dest | Out-Null
 Copy-Item (Join-Path $PSScriptRoot 'Device.ps1') $dest -Force
 Copy-Item (Join-Path $PSScriptRoot 'Uninstall.ps1') $dest -Force
 $gpu[0].InstanceId | Set-Content (Join-Path $dest 'device.txt') -Encoding ASCII
 $compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
 & $compiler /nologo /target:exe /platform:x64 ("/out:"+(Join-Path $dest 'Guard.exe')) (Join-Path $PSScriptRoot 'Guard.cs')
 if($LASTEXITCODE -ne 0){throw 'Service compilation failed.'}
 # Program Files supplies administrator/SYSTEM-only write access.
 New-Service -Name $name -BinaryPathName ('"'+(Join-Path $dest 'Guard.exe')+'"') -DisplayName 'Raphael AMD reset guard' -StartupType Automatic | Out-Null
 $created=$true
 Add-Type -Path (Join-Path $PSScriptRoot 'ServiceConfig.cs')
 [ResetGuardServiceConfig]::SetAndVerify($name,150000)
 Write-Host 'Preshutdown timeout verified: 150 seconds.'
 & sc.exe description $name 'Enables Raphael at startup and disables it before graceful shutdown; No fallback display required.'
 Start-Service $name
 Write-Host 'Installed. Log: ' (Join-Path $dest 'guard.log')
 Write-Host 'Test a normal shutdown and start without rebooting Arch. Display capture stops while AMD is disabled.'
} catch {
 Write-Host ('INSTALL FAILED: '+$_) -ForegroundColor Red
 if($created){Stop-Service $name -ErrorAction SilentlyContinue;& sc.exe delete $name | Out-Null}
 exit 1
}
