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
Write-Host "Cleaning directories..."
Remove-Item -Path $releaseDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $portableDir, $installerDir, $chocolateyDir -Force | Out-Null

# Build the project
Write-Host "Building project..."
dotnet publish $sourceDir -c $configuration -f $framework --self-contained false -o "$portableDir/bin"

# Create portable ZIP
Write-Host "Creating portable ZIP..."
$portableZip = "$releaseDir/UnityCacheCleaner-$version-portable.zip"
Compress-Archive -Path "$portableDir/bin/*" -DestinationPath $portableZip -Force

# Build MSI installer
Write-Host "Building MSI installer..."
$wixBinPath = "${env:WIX}bin"
$msiPath = "$releaseDir/UnityCacheCleaner-$version.msi"

if (Test-Path $wixBinPath) {
    # Copy required files
    Copy-Item "$portableDir/bin/*" "$installerDir/bin/" -Recurse -Force
    
    # Build MSI
    & "$wixBinPath\candle.exe" -dBinDir="$installerDir/bin" "$installerDir/Product.wxs" -out "$installerDir/Product.wixobj"
    if ($LASTEXITCODE -eq 0) {
        & "$wixBinPath\light.exe" -ext WixUIExtension "$installerDir/Product.wixobj" -out $msiPath
        if ($LASTEXITCODE -eq 0) {
            Write-Host "MSI created successfully: $msiPath"
            
            # Calculate checksum for Chocolatey
            $checksum = (Get-FileHash $msiPath -Algorithm SHA256).Hash
            
            # Update Chocolatey install script with checksum
            $installScript = Join-Path $chocolateyDir "tools\chocolateyinstall.ps1"
            (Get-Content $installScript) -replace "`$checksum = ''", "`$checksum = '$checksum'" | Set-Content $installScript
        } else {
            Write-Error "Failed to create MSI"
            exit 1
        }
    } else {
        Write-Error "Failed to compile WiX source"
        exit 1
    }
} else {
    Write-Warning "WiX Toolset not found. Skipping MSI creation."
}

# Build Chocolatey package
Write-Host "Building Chocolatey package..."
if (Get-Command choco -ErrorAction SilentlyContinue) {
    Push-Location $chocolateyDir
    choco pack
    if ($LASTEXITCODE -eq 0) {
        Move-Item "unity-cache-cleaner.$version.nupkg" $releaseDir -Force
        Write-Host "Chocolatey package created successfully"
    } else {
        Write-Error "Failed to create Chocolatey package"
    }
    Pop-Location
} else {
    Write-Warning "Chocolatey not found. Skipping package creation."
}

# Generate SHA256 checksums for all packages
Write-Host "`nGenerating checksums..."
Get-ChildItem $releaseDir -File | ForEach-Object {
    $hash = Get-FileHash $_.FullName -Algorithm SHA256
    Add-Content "$releaseDir/checksums.txt" "$($hash.Hash) $($_.Name)"
}

Write-Host "`nBuild complete! The following packages were created:"
Get-ChildItem $releaseDir -File | ForEach-Object {
    Write-Host "- $($_.Name) ($('{0:N2}' -f ($_.Length / 1MB)) MB)"
}

Write-Host "`nInstallation options:"
Write-Host "1. Windows Installer (MSI):"
Write-Host "   - Standard installation: Double-click the MSI"
Write-Host "   - Silent installation: msiexec /i UnityCacheCleaner-$version.msi /qn"
Write-Host "   - Custom installation: msiexec /i UnityCacheCleaner-$version.msi CREATEDESKTOPSHORTCUT=0 FILEASSOCIATIONS=0"

Write-Host "`n2. Chocolatey:"
Write-Host "   - Standard installation: choco install unity-cache-cleaner -y"
Write-Host "   - Custom installation: choco install unity-cache-cleaner --params '/NoDesktopShortcut /NoFileAssoc /NoAutoUpdate'"

Write-Host "`n3. Portable:"
Write-Host "   - Extract the ZIP and run UnityCacheCleaner.exe"
