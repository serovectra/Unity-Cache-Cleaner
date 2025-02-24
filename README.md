# Unity Cache Cleaner

A Windows desktop application to help Unity developers manage and clean their project caches efficiently.

## Features

- Clean Unity project temporary files
- Clean Unity Library cache
- Clean Unity Editor cache
- Build and run Unity projects
- Recent projects list with auto-discovery
- Progress tracking and cancellation support
- Detailed logging

## Version 1.2.6 (2025-02-24)
- Fixed menu positioning to prevent overlap with project dropdown
- Updated version display consistency across the application
- Added exit prompt after cleaning completion
- Improved UI spacing and visual consistency
- Added debug logging for better troubleshooting

## Requirements

- Windows operating system
- .NET 6.0 Runtime
- Unity (any recent version)

## Installation

1. Download the latest release from the [Releases](https://github.com/serovectra/Unity-Cache-Cleaner-v1.0.0/releases) page
2. Extract the zip file to your preferred location
3. Run `UnityCacheCleaner.exe`

## Usage

1. Select a Unity project directory using one of these methods:
   - Choose from the dropdown list of recently used projects
   - Click "Browse" to select a project folder
   - The app will automatically discover Unity projects in common locations

2. Select which caches to clean:
   - Temp Directory: Temporary files created during Unity Editor sessions
   - Library Cache: Cached data in the Library folder
   - Editor Cache: Unity Editor's global cache

3. Click "Clean" to start the cleaning process
   - Progress will be shown in real-time
   - You can cancel the operation at any time
   - Results and any errors will be logged in the application

4. Use "Build & Run" to compile and launch your Unity project
   - Requires a valid build.ps1 script in your project root
   - Build progress and errors will be displayed
   - The application will automatically launch after successful build

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
