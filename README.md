# Unity Cache Cleaner v1.3.0-beta

A modern tool for managing Unity project caches and credentials. Helps improve build times and manage Unity sign-in state.

## New in 1.3.0-beta

- Modern bottom-aligned menu with theme support
- Unity sign-out functionality with safety checks
- Enhanced Unity process detection and handling
- Improved Unity directory auto-detection
- Real-time progress tracking
- Detailed operation logging
- Color scheme customization
- Splash screen and modern UI

## Features

- Clean Unity project caches:
  - Temporary cache
  - Library cache
  - Editor cache
- Manage Unity credentials:
  - Safe sign-out with warnings
  - Credential state management
- Modern UI:
  - Theme support with color schemes
  - Real-time progress tracking
  - Detailed logging
  - Process management
- Safety features:
  - Unity process detection
  - Operation confirmations
  - Detailed warnings
  - Safe exit handling

## Requirements

- Windows 10/11
- .NET 6.0 or later
- Unity 2019.4 or later (for cache cleaning)
- Unity Hub 3.0 or later (for sign-out feature)

## Installation

Choose your preferred installation method:

### Option 1: Windows Installer (Recommended)
1. Download `UnityCacheCleaner-1.3.0-beta.msi` from the latest release
2. Run the installer
3. Launch from Start Menu

### Option 2: Portable ZIP
1. Download `UnityCacheCleaner-1.3.0-beta-portable.zip`
2. Extract to any location
3. Run `UnityCacheCleaner.exe`

### Option 3: Chocolatey
```powershell
choco install unity-cache-cleaner --version 1.3.0-beta
```

### Prerequisites
- Windows 10/11
- .NET 6.0 Runtime (automatically installed by Windows Installer and Chocolatey)
- Unity 2019.4 or later (for cache cleaning)
- Unity Hub 3.0 or later (for sign-out feature)

## Usage

1. Select your Unity project folder
2. Choose which caches to clean
3. Optionally select Unity sign-out
4. Click "Clean" to start
5. Follow the prompts and warnings

## Beta Notes

This is a beta release with significant new features. Please report any issues or feedback on GitHub.

## Development

### Prerequisites

- Visual Studio 2022 or later
- .NET 6.0 SDK
- Windows Forms development workload

### Building from Source

1. Clone the repository:
```bash
git clone https://github.com/serovectra/Unity-Cache-Cleaner-v1.0.0.git
```

2. Open `UnityCacheCleaner.sln` in Visual Studio

3. Build the solution:
```bash
dotnet build
```

4. Run the application:
```bash
dotnet run
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Unity Technologies for their amazing game engine
- The .NET community for Windows Forms
- All contributors who help improve this tool
- Unity Technologies
- The Unity development community
- All contributors and testers
