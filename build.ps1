# Build configuration
$version = "1.3.0-beta"
$configuration = "Release"

Write-Host "Starting Unity Cache Cleaner build process..."

# Install Node.js dependencies
Write-Host "Installing Node.js dependencies..."
npm install

# Build .NET applications for Windows and Linux
Write-Host "Building .NET applications..."
npm run build

# Create distribution packages
Write-Host "Creating distribution packages..."
if ($args.Contains("--windows")) {
    Write-Host "Building Windows packages..."
    npm run dist:windows
}
elseif ($args.Contains("--linux")) {
    Write-Host "Building Linux packages..."
    npm run dist:linux
}
else {
    Write-Host "Building all packages..."
    npm run dist
}

Write-Host "`nBuild complete! Check the 'releases' directory for:"
Write-Host "Windows:"
Write-Host "- UnityCacheCleaner-$version-setup.exe (Installer)"
Write-Host "- UnityCacheCleaner-$version-portable.exe (Portable)"
Write-Host "Linux:"
Write-Host "- UnityCacheCleaner-$version.AppImage"
Write-Host "- unity-cache-cleaner_$version_amd64.deb"

Write-Host "`nInstallation options:"
Write-Host "Windows:"
Write-Host "1. Installer (recommended):"
Write-Host "   - Run UnityCacheCleaner-$version-setup.exe"
Write-Host "   - Choose installation directory"
Write-Host "   - Select additional options (shortcuts, file associations)"
Write-Host "2. Portable:"
Write-Host "   - Extract UnityCacheCleaner-$version-portable.exe to any location"
Write-Host "   - Run directly, no installation needed"

Write-Host "`nLinux:"
Write-Host "1. AppImage (universal):"
Write-Host "   - Make executable: chmod +x UnityCacheCleaner-$version.AppImage"
Write-Host "   - Run directly: ./UnityCacheCleaner-$version.AppImage"
Write-Host "2. Debian/Ubuntu:"
Write-Host "   - Install: sudo dpkg -i unity-cache-cleaner_$version_amd64.deb"
Write-Host "   - Run from menu or: unity-cache-cleaner"
