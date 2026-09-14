# Runs the real configuration script against mock network cmdlets; no network writes.
param([ValidateSet('pc1','pc2','existing','conflict','occupied','unplugged')][string]$Case='pc1')
$global:ds4Case=$Case
$global:ds4Pc=if($Case -eq 'pc2'){2}else{1}
$global:ds4Writes=0
function Get-NetAdapter { [pscustomobject]@{InterfaceGuid=[Guid]'11111111-1111-1111-1111-111111111111';MediaType='802.3';ifIndex=7;Name='Ethernet test';Status=$(if($global:ds4Case -eq 'unplugged'){'Disconnected'}else{'Up'});LinkSpeed='2.5 Gbps'} }
function Get-NetIPAddress {
    param($InterfaceIndex,$AddressFamily,$IPAddress,$ErrorAction)
    if($IPAddress){return [pscustomobject]@{IPAddress=$IPAddress;AddressState='Preferred';PrefixLength=30}}
    if($global:ds4Case -eq 'occupied'){return [pscustomobject]@{IPAddress='192.168.1.20';PrefixLength=24}}
    if($global:ds4Case -eq 'existing'){return [pscustomobject]@{IPAddress='192.168.250.1';PrefixLength=30}}
}
function Get-NetRoute {
    param($InterfaceIndex,$DestinationPrefix,$AddressFamily,$ErrorAction)
    if($DestinationPrefix){return}
    if($global:ds4Case -eq 'conflict'){[pscustomobject]@{InterfaceIndex=9;DestinationPrefix='192.168.250.2/32'}}
}
function Get-NetIPInterface { [pscustomobject]@{Dhcp='Enabled'} }
function New-NetIPAddress {
    param($InterfaceIndex,$IPAddress,$PrefixLength)
    if($global:ds4Case -eq 'existing' -or $InterfaceIndex -ne 7 -or $PrefixLength -ne 30 -or $IPAddress -ne "192.168.250.$global:ds4Pc"){throw 'Unexpected IP mutation'}
    $global:ds4Writes++
}
function Get-NetFirewallRule {if($global:ds4Case -eq 'existing'){@{Name='existing'}}}
function Get-NetFirewallHyperVRule {if($global:ds4Case -eq 'existing'){@{Name='existing'}}}
function New-NetFirewallRule {
    param($Name,$DisplayName,$Direction,$Action,$Protocol,$LocalPort,$LocalAddress,$RemoteAddress,$Profile,$InterfaceAlias,$Enabled)
    if($Protocol -ne 'TCP' -or $LocalPort -notin @(9911,9912) -or $LocalAddress -ne "192.168.250.$global:ds4Pc" -or $RemoteAddress -ne ('192.168.250.'+(3-$global:ds4Pc)) -or $InterfaceAlias -ne 'Ethernet test'){throw 'Unsafe Windows firewall scope'}
    $global:ds4Writes++
}
function Set-NetFirewallRule {New-NetFirewallRule @args}
function New-NetFirewallHyperVRule {
    param($Name,$DisplayName,$Direction,$Action,$Protocol,$LocalPorts,$LocalAddresses,$RemoteAddresses,$VMCreatorId,$Enabled)
    if($Name -notlike '*-WSL' -or $Protocol -ne 'TCP' -or $LocalPorts -notin @('9911','9912') -or $LocalAddresses -ne "192.168.250.$global:ds4Pc" -or $RemoteAddresses -ne ('192.168.250.'+(3-$global:ds4Pc))){throw 'Unsafe Hyper-V firewall scope'}
    $global:ds4Writes++
}
function Set-NetFirewallHyperVRule {New-NetFirewallHyperVRule @args}
function Start-Sleep {}
function Read-Host {return ''}
function Remove-NetIPAddress {throw 'Unexpected rollback in fixture'}
function Set-NetIPInterface {throw 'Unexpected DHCP mutation in fixture'}
& "$PSScriptRoot\Ethernet.ps1" -AdapterGuid '11111111-1111-1111-1111-111111111111' -PcNumber $global:ds4Pc
exit $LASTEXITCODE
