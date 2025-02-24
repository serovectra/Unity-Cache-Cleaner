# Unity Cache Cleaner Installer Script

!define APPNAME "Unity Cache Cleaner"
!define COMPANYNAME "Serovectra"
!define DESCRIPTION "A tool for managing Unity project caches and credentials"
!define VERSIONMAJOR 1
!define VERSIONMINOR 3
!define VERSIONBUILD 0
!define VERSIONBETA "beta"

# Set compression
SetCompressor /SOLID lzma

# Include modern UI
!include "MUI2.nsh"

# Define name of installer
Name "${APPNAME}"
OutFile "UnityCacheCleaner-${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}-${VERSIONBETA}-setup.exe"

# Default installation folder
InstallDir "$LOCALAPPDATA\Unity Cache Cleaner"

# Get installation folder from registry if available
InstallDirRegKey HKCU "Software\${COMPANYNAME}\${APPNAME}" ""

# Request application privileges
RequestExecutionLevel user

# Interface Settings
!define MUI_ABORTWARNING
!define MUI_ICON "UnityCacheCleaner\Resources\AppIcon.ico"

# Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

# Languages
!insertmacro MUI_LANGUAGE "English"

# Installer sections
Section "Unity Cache Cleaner" SecMain
    SectionIn RO
    
    # Set output path to the installation directory
    SetOutPath $INSTDIR
    
    # Add files
    File /r "release\portable\bin\*.*"
    
    # Create desktop shortcut
    CreateShortcut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\UnityCacheCleaner.exe"
    
    # Create start menu shortcut
    CreateDirectory "$SMPROGRAMS\${APPNAME}"
    CreateShortcut "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\UnityCacheCleaner.exe"
    CreateShortcut "$SMPROGRAMS\${APPNAME}\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
    
    # Store installation folder
    WriteRegStr HKCU "Software\${COMPANYNAME}\${APPNAME}" "" $INSTDIR
    
    # Create uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"
    
    # Add uninstall information to Add/Remove Programs
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" \
                     "DisplayName" "${APPNAME}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" \
                     "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" \
                     "DisplayIcon" "$INSTDIR\UnityCacheCleaner.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" \
                     "Publisher" "${COMPANYNAME}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" \
                     "DisplayVersion" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}-${VERSIONBETA}"
SectionEnd

# Uninstaller section
Section "Uninstall"
    # Remove Start Menu shortcuts
    Delete "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk"
    Delete "$SMPROGRAMS\${APPNAME}\Uninstall.lnk"
    RMDir "$SMPROGRAMS\${APPNAME}"
    
    # Remove desktop shortcut
    Delete "$DESKTOP\${APPNAME}.lnk"
    
    # Remove files
    RMDir /r "$INSTDIR"
    
    # Remove registry keys
    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
    DeleteRegKey HKCU "Software\${COMPANYNAME}\${APPNAME}"
SectionEnd
