param([Parameter(Mandatory=$true)][Guid]$AdapterGuid,
      [Parameter(Mandatory=$true)][ValidateSet(1,2)][int]$PcNumber)
$ErrorActionPreference='Stop'
$added=$false
try {
    $nic=Get-NetAdapter -Physical | Where-Object { [Guid]$_.InterfaceGuid -eq $AdapterGuid }
    if (-not $nic -or $nic.MediaType -ne '802.3') { throw 'Seleziona una scheda Ethernet fisica.' }
    if ($nic.Status -ne 'Up') { throw 'Cavo Ethernet non collegato: collega entrambi i PC e riprova.' }
    $index=$nic.ifIndex
    $local='192.168.250.'+$PcNumber
    $peer='192.168.250.'+(3-$PcNumber)
    $prefix='192.168.250.0/30'
    # Refuse existing networks instead of replacing their IP or default route.
    $addresses=@(Get-NetIPAddress -InterfaceIndex $index -AddressFamily IPv4 -ErrorAction SilentlyContinue)
    if ($addresses | Where-Object { $_.IPAddress -notlike '169.254.*' -and ($_.IPAddress -ne $local -or $_.PrefixLength -ne 30) }) { throw 'La scheda ha gia un IP configurato. Usa una scheda dedicata al cavo diretto.' }
    if (Get-NetRoute -InterfaceIndex $index -DestinationPrefix '0.0.0.0/0' -ErrorAction SilentlyContinue) { throw 'La scheda ha un gateway: non e un collegamento dedicato.' }
    foreach($route in Get-NetRoute -AddressFamily IPv4) {
        if($route.InterfaceIndex -eq $index -or $route.DestinationPrefix -eq '0.0.0.0/0'){continue}
        $parts=$route.DestinationPrefix.Split('/')
        $bits=[Math]::Min(30,[int]$parts[1])
        $a=[Net.IPAddress]::Parse($parts[0]).GetAddressBytes()
        $b=[Net.IPAddress]::Parse($local).GetAddressBytes()
        $overlap=$true
        for($i=0;$i -lt 4;$i++){
            $n=[Math]::Min(8,[Math]::Max(0,$bits-8*$i))
            $mask=if($n -eq 0){0}else{(255 -shl (8-$n)) -band 255}
            if(($a[$i] -band $mask) -ne ($b[$i] -band $mask)){$overlap=$false}
        }
        if($overlap){throw "Rete dedicata in conflitto con $($route.DestinationPrefix). Non applicato."}
    }
    $dhcp=(Get-NetIPInterface -InterfaceIndex $index -AddressFamily IPv4).Dhcp
    if(-not ($addresses | Where-Object IPAddress -eq $local)){
        New-NetIPAddress -InterfaceIndex $index -IPAddress $local -PrefixLength 30 | Out-Null
        $added=$true
    }
    Start-Sleep -Seconds 2
    $ip=Get-NetIPAddress -InterfaceIndex $index -IPAddress $local
    if($ip.AddressState -ne 'Preferred'){throw "IP non utilizzabile: $($ip.AddressState). Controlla che l'altro PC abbia il numero opposto."}
    $vm='{40E0AC32-46A5-438A-A0B2-2B479E8F2E90}'
    foreach($port in 9911,9912){
        $name='DS4Ethernet-'+$index+'-'+$port
        $ruleArgs=@{Direction='Inbound';Action='Allow';Protocol='TCP';LocalPort=$port;LocalAddress=$local;RemoteAddress=$peer;Profile='Any';InterfaceAlias=$nic.Name}
        if(Get-NetFirewallRule -Name $name -ErrorAction SilentlyContinue){Set-NetFirewallRule -Name $name @ruleArgs -Enabled True}
        else{New-NetFirewallRule -Name $name -DisplayName $name @ruleArgs | Out-Null}
        $hv=@{Name=($name+'-WSL');Direction='Inbound';Action='Allow';VMCreatorId=$vm;Protocol='TCP';LocalPorts=[string]$port;LocalAddresses=$local;RemoteAddresses=$peer}
        if(Get-NetFirewallHyperVRule -Name $hv.Name -ErrorAction SilentlyContinue){Set-NetFirewallHyperVRule @hv -Enabled True}
        else{New-NetFirewallHyperVRule @hv -DisplayName $hv.Name | Out-Null}
    }
    Write-Host "Configurato $local -> $peer. Link: $($nic.LinkSpeed). Ripeti sull'altro PC."
    exit 0
}catch{
    if($added){
        Remove-NetIPAddress -InterfaceIndex $index -IPAddress $local -Confirm:$false -ErrorAction SilentlyContinue
        if($dhcp -eq 'Enabled'){Set-NetIPInterface -InterfaceIndex $index -AddressFamily IPv4 -Dhcp Enabled -ErrorAction SilentlyContinue}
    }
    Write-Host $_ -ForegroundColor Red
    [void](Read-Host 'Invio per chiudere')
    exit 1
}
