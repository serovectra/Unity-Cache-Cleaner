using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UnityCacheCleanerBuildManager
{
    public class BuildManager
    {
        private readonly string projectPath;
        private readonly string logDirectory = "logs";
        private string currentLogFile;
        private string currentErrorFile;

        public BuildManager(string projectPath)
        {
            this.projectPath = projectPath;
            Directory.CreateDirectory(logDirectory);
            currentLogFile = Path.Combine(logDirectory, $"build_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            currentErrorFile = Path.Combine(logDirectory, $"error_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        }

        public async Task BuildAndRun()
        {
            try
            {
                string buildScript = Path.Combine(projectPath, "build.ps1");
                if (!File.Exists(buildScript))
                {
                    throw new FileNotFoundException("Build script not found. Please ensure build.ps1 exists in the project root.");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{buildScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = projectPath
                };

                using var process = new Process { StartInfo = startInfo };
                using var outputWriter = new StreamWriter(currentLogFile, true);
                using var errorWriter = new StreamWriter(currentErrorFile, true);

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputWriter.WriteLine(e.Data);
                        outputWriter.Flush();
                        Debug.WriteLine($"[Build Output] {e.Data}");
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorWriter.WriteLine(e.Data);
                        errorWriter.Flush();
                        Debug.WriteLine($"[Build Error] {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    Debug.WriteLine("Build completed successfully!");
                    string exePath = Path.Combine(projectPath, "Build", "YourGame.exe");
                    if (File.Exists(exePath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        throw new FileNotFoundException("Built executable not found.");
                    }
                }
                else
                {
                    throw new Exception($"Build failed with exit code: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Build error: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
