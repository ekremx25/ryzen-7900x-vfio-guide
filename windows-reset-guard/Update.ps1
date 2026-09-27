#Requires -RunAsAdministrator
$ErrorActionPreference='Stop'
$dest=Join-Path $env:ProgramFiles 'EkremAmdResetGuard'
try {
 $svc=Get-Service EkremAmdResetGuard -ErrorAction Stop
 Stop-Service $svc.Name -ErrorAction Stop
 $svc.WaitForStatus('Stopped',[TimeSpan]::FromSeconds(70))
 $backup=Join-Path $dest ('Device.ps1.backup-'+(Get-Date -Format yyyyMMdd-HHmmss))
 Copy-Item (Join-Path $dest 'Device.ps1') $backup
 Copy-Item (Join-Path $PSScriptRoot 'Device.ps1') $dest -Force
 Write-Host 'Updated device handling. Diagnostic output:'
 & "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File (Join-Path $dest 'Device.ps1') -Action Check
 if($LASTEXITCODE -ne 0){throw 'Diagnostic failed; inspect output.'}
 Start-Service $svc.Name
 Write-Host 'Service restarted. QXL is no longer required or automatically enabled. Check guard.log after a normal shutdown/start test.'
} catch {
 Write-Host ('UPDATE ERROR: '+$_) -ForegroundColor Red
 Start-Service EkremAmdResetGuard -ErrorAction SilentlyContinue
 exit 1
}
