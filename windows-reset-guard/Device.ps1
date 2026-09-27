param([ValidateSet('Enable','Disable','Check')][string]$Action='Check')
$ErrorActionPreference='Stop'
try {
 $id=(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'device.txt') -Raw).Trim()
 if ($id -notmatch '^PCI\\VEN_1002&DEV_164E&') {throw 'Unexpected device ID; refusing operation.'}
 $d=Get-PnpDevice -InstanceId $id -PresentOnly -ErrorAction Stop
 if($d.Class -ne 'Display'){throw 'Target is not a display adapter.'}
 $displays=@(Get-PnpDevice -Class Display -PresentOnly)
 foreach($display in $displays){
  $problem=(Get-PnpDeviceProperty -InstanceId $display.InstanceId -KeyName 'DEVPKEY_Device_ProblemCode').Data
  Write-Output ("Display: {0}; status={1}; problem={2}; id={3}" -f $display.FriendlyName,$display.Status,$problem,$display.InstanceId)
 }
 if($Action -eq 'Check'){exit 0}
 if($Action -eq 'Disable') {
  Write-Output 'Disabling AMD for shutdown; no fallback display is required. Capture will stop.'
  Disable-PnpDevice -InstanceId $id -Confirm:$false -ErrorAction Stop
  $wanted=22
 } else {Enable-PnpDevice -InstanceId $id -Confirm:$false -ErrorAction Stop;$wanted=0}
 for($i=0;$i -lt 15;$i++){
  $code=(Get-PnpDeviceProperty -InstanceId $id -KeyName 'DEVPKEY_Device_ProblemCode' -ErrorAction Stop).Data
  if($code -eq $wanted){Write-Output "$Action verified, problem code=$code";exit 0}
  Start-Sleep -Seconds 1
 }
 throw "$Action not verified; problem code=$code"
} catch {Write-Error $_;exit 1}
