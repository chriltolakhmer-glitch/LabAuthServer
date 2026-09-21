#requires -Version 5.1
<# Read-only local IIS maintenance probe. Exit: 0 PASS, 1 FAIL, 2 WARN.
Health is liveness only; no login, restart, deployment, or secret inspection.
#>
[CmdletBinding()]
param(
    [string]$SiteName = 'LabAuthServer',
    [string]$AppPoolName = 'LabAuthServerAppPool',
    [Parameter(Mandatory)][uri]$HealthUrl,
    [Parameter(Mandatory)][string]$BackupPath,
    [string]$ExpectedDeploymentPath,
    [string]$ArtifactPath,
    [ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$ExpectedSha256,
    [ValidateRange(1,365)][int]$CertificateWarningDays = 30
)
$ErrorActionPreference = 'Stop'
$results = [System.Collections.Generic.List[object]]::new()
function Add-Result([string]$Check, [string]$Status, [string]$Detail) {
    $row = [pscustomobject]@{ Check = $Check; Status = $Status; Detail = $Detail }
    $results.Add($row)
    Write-Output $row
}

$site = $null
try {
    Import-Module WebAdministration -ErrorAction Stop
    $site = Get-Website -Name $SiteName
    if (-not $site) { throw 'Site missing' }
    $poolState = (Get-WebAppPoolState -Name $AppPoolName).Value
    $status = if ($poolState -eq 'Started' -and $site.State -eq 'Started' -and $site.ApplicationPool -eq $AppPoolName) { 'PASS' } else { 'FAIL' }
    Add-Result 'IIS' $status "Site=$($site.State); pool=$poolState; assigned pool=$($site.ApplicationPool)"
    $deploymentPath = [Environment]::ExpandEnvironmentVariables($site.PhysicalPath)
    if ($ExpectedDeploymentPath -and [IO.Path]::GetFullPath($deploymentPath).TrimEnd('\') -ne [IO.Path]::GetFullPath($ExpectedDeploymentPath).TrimEnd('\')) {
        Add-Result 'Deployment path' 'FAIL' 'IIS physical path differs from the supplied expected path.'
    } else { Add-Result 'Deployment path' 'PASS' $deploymentPath }
    $dll = Get-Item -LiteralPath (Join-Path $deploymentPath 'LabAuthServer.Api.dll')
    $version = $dll.VersionInfo.ProductVersion
    if ([string]::IsNullOrWhiteSpace($version)) { throw 'Version unavailable' }
    Add-Result 'Application version' 'PASS' "ProductVersion=$version; API SHA256=$((Get-FileHash -LiteralPath $dll.FullName -Algorithm SHA256).Hash)"
} catch { Add-Result 'IIS/application inspection' 'FAIL' 'Unable to inspect site, pool or application version. Check local IIS access and paths.' }

$safeUrl = $HealthUrl.IsAbsoluteUri -and $HealthUrl.Scheme -eq 'https' -and -not $HealthUrl.UserInfo -and -not $HealthUrl.Query -and -not $HealthUrl.Fragment
if (-not $safeUrl) {
    Add-Result 'HTTPS health' 'FAIL' 'Supply an absolute HTTPS health URL without credentials, query or fragment.'
} else {
    $client = $null; $handler = $null; $response = $null
    try {
        Add-Type -AssemblyName System.Net.Http
        $handler = [System.Net.Http.HttpClientHandler]::new()
        $handler.AllowAutoRedirect = $false
        $handler.CheckCertificateRevocationList = $true
        $client = [System.Net.Http.HttpClient]::new($handler)
        $client.Timeout = [TimeSpan]::FromSeconds(20)
        $response = $client.GetAsync($HealthUrl).GetAwaiter().GetResult()
        $status = if ([int]$response.StatusCode -eq 200) { 'PASS' } else { 'FAIL' }
        Add-Result 'HTTPS health' $status "HTTP $([int]$response.StatusCode); normal certificate validation; redirects disabled. Liveness only."
    } catch { Add-Result 'HTTPS health' 'FAIL' 'HTTPS request failed. Check reachability, hostname, certificate trust and application diagnostics; no trust bypass used.' }
    finally {
        if ($response) { $response.Dispose() }
        if ($client) { $client.Dispose() }
        if ($handler) { $handler.Dispose() }
    }
}

try {
    if (-not $site -or -not $safeUrl) { throw 'Binding unavailable' }
    $bindings = @(Get-WebBinding -Name $SiteName -Protocol https | Where-Object {
        $parts = $_.bindingInformation -split ':'
        $parts[-2] -eq [string]$HealthUrl.Port -and ($parts[-1] -eq $HealthUrl.DnsSafeHost -or $parts[-1] -eq '')
    })
    if ($bindings.Count -ne 1) { throw 'Binding ambiguous' }
    $binding = $bindings[0]
    $thumbprint = [string]$binding.certificateHash
    if ($binding.certificateHash -is [byte[]]) { $thumbprint = ([BitConverter]::ToString($binding.certificateHash)).Replace('-','') }
    if (-not $thumbprint -or -not $binding.certificateStoreName) { throw 'Certificate reference unavailable' }
    $cert = Get-Item -LiteralPath ('Cert:\LocalMachine\' + $binding.certificateStoreName + '\' + $thumbprint)
    $now = Get-Date
    $days = [math]::Floor(($cert.NotAfter - $now).TotalDays)
    $status = if ($cert.NotBefore -gt $now -or $cert.NotAfter -le $now -or -not $cert.HasPrivateKey) { 'FAIL' } elseif ($days -le $CertificateWarningDays) { 'WARN' } else { 'PASS' }
    Add-Result 'HTTPS certificate' $status "ExpiresUTC=$($cert.NotAfter.ToUniversalTime().ToString('o')); days remaining=$days; private key associated=$($cert.HasPrivateKey). Pool key permissions are not tested."
} catch { Add-Result 'HTTPS certificate' 'FAIL' 'Cannot resolve one local HTTPS binding/certificate for the URL. Inspect binding/store metadata manually.' }

if ($ArtifactPath) {
    try {
        $actualHash = (Get-FileHash -LiteralPath $ArtifactPath -Algorithm SHA256).Hash
        $status = if (-not $ExpectedSha256) { 'WARN' } elseif ($actualHash -eq $ExpectedSha256) { 'PASS' } else { 'FAIL' }
        Add-Result 'Artifact SHA256' $status "SHA256=$actualHash; expected supplied=$([bool]$ExpectedSha256). This does not compare every deployed file to the package."
    } catch { Add-Result 'Artifact SHA256' 'FAIL' 'Artifact is missing or unreadable.' }
} elseif ($ExpectedSha256) { Add-Result 'Artifact SHA256' 'FAIL' 'An expected hash was supplied without an artifact path.' }
else { Add-Result 'Artifact SHA256' 'WARN' 'Not checked: supply ArtifactPath and ExpectedSha256 for comparison.' }

try {
    if (-not (Test-Path -LiteralPath $BackupPath -PathType Container)) { throw 'Backup missing' }
    $required = @('LabAuthServer.Api.dll','LabAuthServer.Application.dll','LabAuthServer.Infrastructure.dll','LabAuthServer.Domain.dll','LabAuthServer.Api.deps.json','LabAuthServer.Api.runtimeconfig.json','web.config','appsettings.json')
    foreach ($name in $required) {
        $file = Get-Item -LiteralPath (Join-Path $BackupPath $name)
        if ($file.Length -eq 0) { throw 'Empty required file' }
    }
    $files = @(Get-ChildItem -LiteralPath $BackupPath -File -Recurse -Force)
    foreach ($file in $files) { $null = Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256 }
    $backupVersion = (Get-Item -LiteralPath (Join-Path $BackupPath 'LabAuthServer.Api.dll')).VersionInfo.ProductVersion
    Add-Result 'Backup contents' 'PASS' "$($files.Count) files readable; required runtime/configuration files present; version=$backupVersion. Plausibility only: confirm recovery record, provenance and hash manifest before restore."
} catch { Add-Result 'Backup contents' 'FAIL' 'Backup is missing, incomplete or unreadable; supply the application backup directory, not its parent.' }

try {
    $runtimes = @(& dotnet --list-runtimes 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $runtimes.Count) { throw 'Runtime inventory unavailable' }
    Add-Result 'Runtime inventory' 'PASS' ($runtimes -join '; ')
} catch { Add-Result 'Runtime inventory' 'WARN' 'Runtime inventory unavailable; inspect installed runtimes manually. No update attempted.' }
$failures = @($results | Where-Object Status -eq 'FAIL').Count
$warnings = @($results | Where-Object Status -eq 'WARN').Count
$summary = if ($failures) { 'FAIL' } elseif ($warnings) { 'WARN' } else { 'PASS' }
Add-Result 'Summary' $summary "PASS=$(@($results | Where-Object Status -eq 'PASS').Count); WARN=$warnings; FAIL=$failures. No configuration changes performed."
if ($failures) { exit 1 }
if ($warnings) { exit 2 }
exit 0
