$ErrorActionPreference = 'Stop'

$packageName = 'unity-cache-cleaner'
$toolsDir    = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url         = 'https://github.com/serovectra/Unity-Cache-Cleaner-v1.0.0/releases/download/v1.3.0-beta/UnityCacheCleaner-1.3.0-beta.msi'
$checksum    = '' # Will be updated after MSI creation

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  fileType      = 'MSI'
  url           = $url
  silentArgs    = "/qn /norestart"
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Unity Cache Cleaner*'
  checksum      = $checksum
  checksumType  = 'sha256'
}

Install-ChocolateyPackage @packageArgs
