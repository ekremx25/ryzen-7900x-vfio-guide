#Requires -RunAsAdministrator
$ErrorActionPreference='Stop'
$dest=Join-Path $env:ProgramFiles 'EkremAmdResetGuard'
try {
 Stop-Service 'EkremAmdResetGuard' -ErrorAction SilentlyContinue
 & "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File (Join-Path $dest 'Device.ps1') -Action Enable
 if($LASTEXITCODE -ne 0){throw 'AMD could not be enabled. Files retained for recovery.'}
 & sc.exe delete EkremAmdResetGuard
 if($LASTEXITCODE -ne 0){throw 'Could not delete service.'}
 Write-Host 'Service removed. Files and logs retained in' $dest
} catch {Write-Host $_ -ForegroundColor Red;exit 1}
