$ErrorActionPreference = 'Stop'

$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url = 'https://github.com/serovectra/Unity-Cache-Cleaner-v1.0.0/releases/download/v1.3.0-beta/UnityCacheCleaner-1.3.0-beta.msi'
$checksum = '' # Will be updated after MSI creation

# Default features
$createDesktopShortcut = $true
$fileAssociations = $true
$autoUpdate = $true

# Get parameters
$pp = Get-PackageParameters

if ($pp) {
    if ($pp.NoDesktopShortcut) { $createDesktopShortcut = $false }
    if ($pp.NoFileAssoc) { $fileAssociations = $false }
    if ($pp.NoAutoUpdate) { $autoUpdate = $false }
}

# Build MSI arguments
$silentArgs = "/qn /norestart"
$silentArgs += " CREATEDESKTOPSHORTCUT=$([int]$createDesktopShortcut)"
$silentArgs += " FILEASSOCIATIONS=$([int]$fileAssociations)"
$silentArgs += " AUTOUPDATE=$([int]$autoUpdate)"

$packageArgs = @{
    packageName    = $env:ChocolateyPackageName
    fileType      = 'MSI'
    url           = $url
    silentArgs    = $silentArgs
    validExitCodes= @(0, 3010, 1641)
    softwareName  = 'Unity Cache Cleaner*'
    checksum      = $checksum
    checksumType  = 'sha256'
}

Install-ChocolateyPackage @packageArgs

# Output installation details
Write-Host "Unity Cache Cleaner has been installed with the following settings:"
Write-Host "Desktop Shortcut: $($createDesktopShortcut ? 'Yes' : 'No')"
Write-Host "File Associations: $($fileAssociations ? 'Yes' : 'No')"
Write-Host "Auto Updates: $($autoUpdate ? 'Yes' : 'No')"

Write-Host "`nTo customize these settings during installation, use parameters:"
Write-Host "choco install unity-cache-cleaner --params '/NoDesktopShortcut /NoFileAssoc /NoAutoUpdate'"
