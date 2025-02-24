# Build configuration
$version = "1.3.0-beta"
$configuration = "Release"
$framework = "net6.0-windows"

# Directory setup
$rootDir = $PSScriptRoot
$sourceDir = Join-Path $rootDir "UnityCacheCleaner"
$releaseDir = Join-Path $rootDir "release"
$portableDir = Join-Path $releaseDir "portable"
$installerDir = Join-Path $releaseDir "installer"
$chocolateyDir = Join-Path $releaseDir "chocolatey"

# Clean and create directories
Remove-Item -Path $releaseDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $portableDir, $installerDir, $chocolateyDir -Force

# Build the project
Write-Host "Building project..."
dotnet publish $sourceDir -c $configuration -f $framework --self-contained false -o "$portableDir/bin"

# Create portable ZIP
Write-Host "Creating portable ZIP..."
Compress-Archive -Path "$portableDir/bin/*" -DestinationPath "$releaseDir/UnityCacheCleaner-$version-portable.zip" -Force

# Build MSI installer (requires WiX toolset)
Write-Host "Building MSI installer..."
$wixBinPath = "${env:WIX}bin"
if (Test-Path $wixBinPath) {
    & "$wixBinPath\candle.exe" -dBinDir="$portableDir/bin" "$installerDir/Product.wxs" -out "$installerDir/Product.wixobj"
    & "$wixBinPath\light.exe" -ext WixUIExtension "$installerDir/Product.wixobj" -out "$releaseDir/UnityCacheCleaner-$version.msi"
} else {
    Write-Warning "WiX Toolset not found. Skipping MSI creation."
}

# Build Chocolatey package
Write-Host "Building Chocolatey package..."
if (Get-Command choco -ErrorAction SilentlyContinue) {
    Set-Location $chocolateyDir
    choco pack
    Move-Item "unity-cache-cleaner.$version.nupkg" $releaseDir
} else {
    Write-Warning "Chocolatey not found. Skipping package creation."
}

Write-Host "`nBuild complete! Packages created in the 'release' directory:"
Get-ChildItem $releaseDir -File | ForEach-Object {
    Write-Host "- $($_.Name)"
}
