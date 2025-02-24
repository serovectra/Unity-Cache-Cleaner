using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;
using System.ComponentModel;
using UnityCacheCleanerBuildManager;

namespace UnityCacheCleaner
{
    public partial class MainForm : Form
    {
        // UI Controls
        private Button cleanButton = new();
        private RichTextBox logTextBox = new();
        private Label statusLabel = new();
        private Button browseButton = new();
        private TextBox projectPathTextBox = new();
        private Label projectLabel = new();
        private Button refreshButton = new();
        private ListView projectListView = new();
        private RichTextBox debugOutputBox = new();
        private TabControl tabControl = new();
        private TabPage logTab = new("Log");
        private TabPage debugTab = new("Debug Output");
        private ProgressBar progressBar = new();
        private Label percentLabel = new();
        private CheckBox cleanLibraryCache = new();
        private CheckBox cleanTempCache = new();
        private CheckBox cleanEditorCache = new();
        private ComboBox projectDirComboBox = new();
        private Button buildAndRunButton = new();
        private Button cancelButton = new();
        private MenuStrip mainMenu = new();

        // Constants and settings
        private const string RECENT_PROJECTS_FILE = "recent_projects.txt";
        private const int MAX_RECENT_PROJECTS = 10;
        
        // State
        private string projectPath = string.Empty;
        private readonly List<string> recentProjects = new();
        private Action<int>? updateProgressUI;
        private CancellationTokenSource? cancellationTokenSource;

        // Safe directories to clean within Library
        private readonly string[] safeTempPaths = new[]
        {
            "Temp",
            "Library/ShaderCache",
            "Library/BuildCache",
            "Library/ArtifactDB",
            "Library/SourceAssetDB",
            "Library/TempArtifacts",
            "Library/APIUpdater",
            "Library/PackageCache",
            "Library/StateCache"
        };

        // Directories that should never be deleted
        private readonly string[] protectedPaths = new[]
        {
            "Assets",
            "Packages",
            "ProjectSettings",
            "Library/LastSceneManagerSetup.txt",
            "Library/EditorUserBuildSettings.asset",
            "Library/EditorUserSettings.asset",
            "Library/ProjectSettings.asset",
            "Library/ScriptAssemblies"
        };

        // Expected Unity project structure
        private readonly string[] expectedFolders = new[]
        {
            "Assets",
            "Packages",
            "ProjectSettings",
            "Library"
        };

        // Common Unity project structure
        private readonly string[] recommendedAssetFolders = new[]
        {
            "Assets/Animations",
            "Assets/Audio",
            "Assets/Materials",
            "Assets/Prefabs",
            "Assets/Resources",
            "Assets/Scenes",
            "Assets/Scripts",
            "Assets/Sprites",
            "Assets/UI"
        };

        // Common Unity issues to check for
        private readonly (string Path, string Issue, string Recommendation)[] commonIssueChecks = new[]
        {
            ("Library/PackageCache", "Package cache might be corrupted", "Try deleting the PackageCache folder and let Unity rebuild it"),
            ("Library/ScriptAssemblies", "Script assembly issues", "Check for script compilation errors in the console"),
            ("Library/ShaderCache", "Shader compilation issues", "Clear the ShaderCache if you're experiencing material issues"),
            ("ProjectSettings/ProjectVersion.txt", "Unity version mismatch", "Ensure all team members use the same Unity version"),
            ("Assets/Plugins", "Plugin conflicts", "Check for duplicate or conflicting plugin versions"),
            ("Packages/manifest.json", "Package dependency issues", "Verify package versions in manifest.json")
        };

        private string GetCleaningSummary()
        {
            var summary = new StringBuilder();
            summary.AppendLine("Cleaning Summary:");
            summary.AppendLine("----------------");
            
            if (cleanLibraryCache.Checked)
                summary.AppendLine("✓ Library Cache");
            if (cleanTempCache.Checked)
                summary.AppendLine("✓ Temp Cache");
            if (cleanEditorCache.Checked)
                summary.AppendLine("✓ Editor Cache");

            summary.AppendLine();
            summary.AppendLine($"Project: {projectPath}");
            
            return summary.ToString();
        }

        public MainForm()
        {
            InitializeUI();
            LoadRecentProjects();
            SetupEventHandlers();
            updateProgressUI = UpdateProgress;
        }

        private void SetupEventHandlers()
        {
            cleanButton.Click += CleanButton_Click!;
            browseButton.Click += (sender, e) => BrowseForProjectDirectory();
            buildAndRunButton.Click += BuildAndRunButton_Click!;
            cancelButton.Click += CancelButton_Click!;
            projectDirComboBox.SelectedIndexChanged += ProjectDirComboBox_SelectedIndexChanged!;
            cleanTempCache.CheckedChanged += CheckBox_CheckedChanged!;
            cleanLibraryCache.CheckedChanged += CheckBox_CheckedChanged!;
            cleanEditorCache.CheckedChanged += CheckBox_CheckedChanged!;
        }

        private void InitializeUI()
        {
            // Initialize form properties
            this.Text = "Unity Cache Cleaner";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(600, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Get Unity project path from command line or current directory
            projectPath = Environment.GetCommandLineArgs().Length > 1 
                ? Environment.GetCommandLineArgs()[1]
                : Directory.GetCurrentDirectory();

            // Create main menu
            mainMenu = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Close());
            fileMenu.DropDownItems.Add(exitItem);

            var helpMenu = new ToolStripMenuItem("Help");
            var aboutItem = new ToolStripMenuItem("About", null, AboutItem_Click);
            var versionItem = new ToolStripMenuItem("Version", null, VersionItem_Click);
            var documentationItem = new ToolStripMenuItem("Documentation", null, DocumentationItem_Click);
            helpMenu.DropDownItems.AddRange(new[] { aboutItem, versionItem, documentationItem });

            mainMenu.Items.AddRange(new[] { fileMenu, helpMenu });
            this.MainMenuStrip = mainMenu;
            this.Controls.Add(mainMenu);

            // Initialize tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Location = new Point(10, 30),
                Size = new Size(ClientSize.Width - 20, ClientSize.Height - 40)
            };

            // Initialize log tab
            logTab = new TabPage("Log");
            logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 9F),
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            logTab.Controls.Add(logTextBox);

            // Initialize debug tab
            debugTab = new TabPage("Debug Output");
            debugOutputBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9F),
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            debugTab.Controls.Add(debugOutputBox);

            // Add tabs to tab control
            tabControl.TabPages.Add(logTab);
            tabControl.TabPages.Add(debugTab);

            // Initialize project controls
            var projectLabel = new Label
            {
                Text = "Unity Project:",
                Location = new Point(10, 40),
                AutoSize = true
            };

            projectDirComboBox = new ComboBox
            {
                Location = new Point(100, 37),
                Width = ClientSize.Width - 200,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(ClientSize.Width - 90, 35),
                Width = 80,
                Height = 23,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // Initialize checkboxes
            cleanLibraryCache = new CheckBox
            {
                Text = "Clean Library Cache",
                Location = new Point(10, 70),
                AutoSize = true,
                Checked = true
            };

            cleanTempCache = new CheckBox
            {
                Text = "Clean Temp Cache",
                Location = new Point(150, 70),
                AutoSize = true,
                Checked = true
            };

            cleanEditorCache = new CheckBox
            {
                Text = "Clean Editor Cache",
                Location = new Point(290, 70),
                AutoSize = true,
                Checked = true
            };

            // Initialize buttons
            cleanButton = new Button
            {
                Text = "Clean Cache",
                Location = new Point(10, 100),
                Width = 150,
                Height = 30
            };

            buildAndRunButton = new Button
            {
                Text = "Build && Run",
                Location = new Point(170, 100),
                Width = 150,
                Height = 30
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(330, 100),
                Width = 100,
                Height = 30
            };

            // Initialize progress controls
            progressBar = new ProgressBar
            {
                Location = new Point(10, 140),
                Width = ClientSize.Width - 90,
                Height = 23,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            percentLabel = new Label
            {
                Location = new Point(ClientSize.Width - 70, 145),
                Width = 60,
                Text = "0%",
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            statusLabel = new Label
            {
                Location = new Point(10, ClientSize.Height - 30),
                Width = ClientSize.Width - 20,
                Height = 20,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Add controls to form
            this.Controls.AddRange(new Control[]
            {
                projectLabel,
                projectDirComboBox,
                browseButton,
                cleanLibraryCache,
                cleanTempCache,
                cleanEditorCache,
                cleanButton,
                buildAndRunButton,
                cancelButton,
                progressBar,
                percentLabel,
                statusLabel,
                tabControl
            });

            // Set initial status
            statusLabel.Text = "Ready";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateDebugState();
        }

        private void UpdateProgress(int value)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int>(UpdateProgress), value);
                return;
            }
            progressBar.Value = Math.Min(100, Math.Max(0, value));
            percentLabel.Text = string.Format("{0}%", progressBar.Value);
        }

        private async void CleanButton_Click(object sender, EventArgs e)
        {
            if (!IsValidUnityProject(projectPath))
            {
                MessageBox.Show("Please select a valid Unity project directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                cleanButton.Enabled = false;
                cancelButton.Enabled = true;
                statusLabel.Text = "Cleaning...";
                progressBar.Value = 0;
                progressBar.Style = ProgressBarStyle.Continuous;

                // Create new cancellation token source
                cancellationTokenSource = new CancellationTokenSource();

                // Log cleaning start
                Debug.WriteLine($"Starting cache clean for project: {projectPath}");
                LogMessage(GetCleaningSummary());

                // Count total files to process
                int totalFiles = await CountFilesToProcess();
                if (totalFiles == 0)
                {
                    LogMessage("No files to clean.");
                    return;
                }

                Debug.WriteLine($"Total files to process: {totalFiles}");
                int processedFiles = 0;

                // Clean temp directory
                if (cleanTempCache.Checked)
                {
                    Debug.WriteLine("Cleaning temp directory...");
                    await CleanTempDirectory(progress =>
                    {
                        processedFiles += progress;
                        UpdateProgress((int)((float)processedFiles / totalFiles * 100));
                    }, cancellationTokenSource.Token);
                }

                // Clean library cache
                if (cleanLibraryCache.Checked)
                {
                    Debug.WriteLine("Cleaning library cache...");
                    await CleanLibraryCache(progress =>
                    {
                        processedFiles += progress;
                        UpdateProgress((int)((float)processedFiles / totalFiles * 100));
                    }, cancellationTokenSource.Token);
                }

                // Clean editor cache
                if (cleanEditorCache.Checked)
                {
                    Debug.WriteLine("Cleaning editor cache...");
                    await CleanEditorCache(progress =>
                    {
                        processedFiles += progress;
                        UpdateProgress((int)((float)processedFiles / totalFiles * 100));
                    }, cancellationTokenSource.Token);
                }

                LogSuccess("Cache cleaning completed successfully!");
            }
            catch (OperationCanceledException)
            {
                LogMessage("Operation cancelled by user.");
                Debug.WriteLine("Cache cleaning cancelled by user");
            }
            catch (Exception ex)
            {
                LogError($"Error cleaning cache: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                cleanButton.Enabled = true;
                cancelButton.Enabled = false;
                statusLabel.Text = "Ready";
                progressBar.Value = 0;
                
                // Dispose cancellation token source
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }
            }
        }

        private async Task<int> CountFilesToProcess()
        {
            return await Task.Run(() =>
            {
                int totalFiles = 0;

                // First check if any directories exist to clean
                bool hasDirectoriesToClean = false;

                if (cleanTempCache.Checked)
                {
                    string tempPath = Path.Combine(projectPath, "Temp");
                    if (Directory.Exists(tempPath))
                    {
                        hasDirectoriesToClean = true;
                        totalFiles += CountFilesInDirectory(tempPath);
                    }
                }

                if (cleanLibraryCache.Checked)
                {
                    foreach (string relativePath in safeTempPaths.Where(p => p.StartsWith("Library")))
                    {
                        string fullPath = Path.Combine(projectPath, relativePath);
                        if (Directory.Exists(fullPath))
                        {
                            hasDirectoriesToClean = true;
                            totalFiles += CountFilesInDirectory(fullPath);
                        }
                    }
                }

                if (cleanEditorCache.Checked)
                {
                    string editorPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Unity", "Editor", "Cache"
                    );
                    if (Directory.Exists(editorPath))
                    {
                        hasDirectoriesToClean = true;
                        totalFiles += CountFilesInDirectory(editorPath);
                    }
                }

                if (!hasDirectoriesToClean)
                {
                    LogMessage("All cache directories are already clean!");
                    return 0;
                }

                return totalFiles;
            });
        }

        private async Task CleanTempDirectory(Action<int> progress, CancellationToken cancellationToken)
        {
            var tempPath = Path.Combine(projectPath, "Temp");
            if (Directory.Exists(tempPath))
            {
                await Task.Run(() =>
                {
                    foreach (var file in Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            File.Delete(file);
                            progress(1);
                            Debug.WriteLine($"Deleted temp file: {file}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to delete temp file {file}: {ex.Message}");
                        }
                    }
                }, cancellationToken);
            }
        }

        private async Task CleanLibraryCache(Action<int> progress, CancellationToken cancellationToken)
        {
            var libraryPath = Path.Combine(projectPath, "Library");
            if (Directory.Exists(libraryPath))
            {
                await Task.Run(() =>
                {
                    foreach (var dir in safeTempPaths)
                    {
                        var path = Path.Combine(libraryPath, dir);
                        if (Directory.Exists(path))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            try
                            {
                                Directory.Delete(path, true);
                                progress(1);
                                Debug.WriteLine($"Deleted library cache: {path}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Failed to delete library cache {path}: {ex.Message}");
                            }
                        }
                    }
                }, cancellationToken);
            }
        }

        private async Task CleanEditorCache(Action<int> progress, CancellationToken cancellationToken)
        {
            var editorPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity", "Editor");
            if (Directory.Exists(editorPath))
            {
                await Task.Run(() =>
                {
                    foreach (var file in Directory.GetFiles(editorPath, "*", SearchOption.AllDirectories))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            File.Delete(file);
                            progress(1);
                            Debug.WriteLine($"Deleted editor cache file: {file}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to delete editor cache file {file}: {ex.Message}");
                        }
                    }
                }, cancellationToken);
            }
        }

        private int CountFilesInDirectory(string path)
        {
            if (!Directory.Exists(path)) return 0;
            
            try
            {
                return Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length;
            }
            catch (Exception ex)
            {
                LogError(string.Format("Error counting files in {0}: {1}", path, ex.Message));
                return 0;
            }
        }

        private void CleanNetcodeCache()
        {
            LogMessage("Cleaning Netcode for GameObjects cache...");

            // Kill Unity Burst Compiler process
            KillProcess("Unity.Burst.Compiler");

            // Clean package cache
            string packageCachePath = Path.Combine(projectPath, "Library", "PackageCache");
            if (Directory.Exists(packageCachePath))
            {
                LogMessage("Cleaning package cache...");
                try
                {
                    Directory.Delete(packageCachePath, true);
                    LogSuccess(string.Format("Package cache cleaned successfully"));
                }
                catch (Exception ex)
                {
                    LogError(string.Format("Error cleaning package cache: {0}", ex.Message));
                }
            }

            // Clean Asset Store cache
            string assetStorePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Unity", "Asset Store-5.x");
            if (Directory.Exists(assetStorePath))
            {
                LogMessage("Cleaning Asset Store cache...");
                try
                {
                    Directory.Delete(assetStorePath, true);
                    LogSuccess(string.Format("Asset Store cache cleaned successfully"));
                }
                catch (Exception ex)
                {
                    LogError(string.Format("Error cleaning Asset Store cache: {0}", ex.Message));
                }
            }

            // Clean Assembly-CSharp files
            string[] assemblyPatterns = new[] {
                "Assembly-CSharp*.csproj",
                "Assembly-CSharp*.dll"
            };

            foreach (string pattern in assemblyPatterns)
            {
                string[] files = Directory.GetFiles(projectPath, pattern, SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        LogSuccess(string.Format("Deleted {0}", Path.GetFileName(file)));
                    }
                    catch (Exception ex)
                    {
                        LogError(string.Format("Error deleting {0}: {1}", Path.GetFileName(file), ex.Message));
                    }
                }
            }

            LogMessage("Netcode cache cleanup completed");
        }

        private void RegenerateAssemblyDefinitions()
        {
            LogMessage("Attempting to regenerate assembly definitions...");
            
            // Clean assembly-related files
            string[] asmdefFiles = Directory.GetFiles(projectPath, "*.asmdef", SearchOption.AllDirectories);
            foreach (string asmdef in asmdefFiles)
            {
                try
                {
                    string backupPath = asmdef + ".backup";
                    File.Copy(asmdef, backupPath, true);
                    LogSuccess(string.Format("Backed up {0}", Path.GetFileName(asmdef)));
                }
                catch (Exception ex)
                {
                    LogError(string.Format("Error backing up {0}: {1}", Path.GetFileName(asmdef), ex.Message));
                }
            }

            LogMessage("Assembly definition files backed up. Please regenerate them in Unity Editor.");
        }

        private void LogMessage(string message)
        {
            if (logTextBox.InvokeRequired || debugOutputBox.InvokeRequired)
            {
                this.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = string.Format("[{0}] {1}{2}", timestamp, message, Environment.NewLine);
            
            // Add to regular log
            logTextBox.AppendText(logEntry);
            logTextBox.ScrollToCaret();

            // Add to debug output with more details
            string debugEntry = string.Format("[{0}] [Thread:{1}] {2}", timestamp, System.Threading.Thread.CurrentThread.ManagedThreadId, message);
            debugOutputBox.AppendText(debugEntry + Environment.NewLine);
            
            // Add stack trace for errors
            if (message.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                string stackTrace = Environment.StackTrace;
                debugOutputBox.AppendText(string.Format("Stack Trace:{0}{1}{0}", Environment.NewLine, stackTrace));
            }
            
            debugOutputBox.ScrollToCaret();
            
            // Also write to debug output
            Debug.WriteLine(message);
            
            // Update status label
            if (statusLabel != null)
            {
                if (statusLabel.InvokeRequired)
                {
                    statusLabel.Invoke(new Action(() => statusLabel.Text = message));
                }
                else
                {
                    statusLabel.Text = message;
                }
            }
        }

        private void UpdateDebugState()
        {
            if (debugOutputBox.InvokeRequired)
            {
                this.Invoke(new Action(UpdateDebugState));
                return;
            }

            StringBuilder debug = new StringBuilder();
            debug.AppendLine("=== Unity Cache Cleaner Debug State ===");
            debug.AppendLine(string.Format("Timestamp: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
            debug.AppendLine(string.Format("Project Path: {0}", projectPath));
            debug.AppendLine(string.Format("Unity Editor Running: {0}", Process.GetProcessesByName("Unity").Length > 0 || Process.GetProcessesByName("UnityEditor").Length > 0));
            debug.AppendLine("\nSelected Options:");
            debug.AppendLine(string.Format("- Clean Library Cache: {0}", cleanLibraryCache.Checked));
            debug.AppendLine(string.Format("- Clean Temp Cache: {0}", cleanTempCache.Checked));
            debug.AppendLine(string.Format("- Clean Editor Cache: {0}", cleanEditorCache.Checked));
            debug.AppendLine("\nProtected Paths:");
            foreach (var path in protectedPaths)
            {
                debug.AppendLine(string.Format("- {0}", path));
            }
            debug.AppendLine("\nSafe Cache Paths:");
            foreach (var path in safeTempPaths)
            {
                debug.AppendLine(string.Format("- {0}", path));
            }
            debug.AppendLine("\nSystem Information:");
            debug.AppendLine(string.Format("OS Version: {0}", Environment.OSVersion));
            debug.AppendLine(string.Format("Machine Name: {0}", Environment.MachineName));
            debug.AppendLine(string.Format("Processor Count: {0}", Environment.ProcessorCount));
            debug.AppendLine(string.Format("Working Set: {0} MB", Environment.WorkingSet / 1024 / 1024));
            debug.AppendLine(string.Format(".NET Runtime: {0}", Environment.Version));
            debug.AppendLine("\n=== End Debug State ===");

            debugOutputBox.Text = debug.ToString() + Environment.NewLine + debugOutputBox.Text;
        }

        private void KillProcess(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes)
            {
                process.Kill();
            }
        }

        private void LogError(string message)
        {
            Debug.WriteLine($"ERROR: {message}");
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => LogError(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] ERROR: {message}{Environment.NewLine}";
            logTextBox.AppendText(logEntry);
            logTextBox.ScrollToCaret();
        }

        private void LogSuccess(string message)
        {
            Debug.WriteLine($"SUCCESS: {message}");
            
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => LogSuccess(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] SUCCESS: {message}{Environment.NewLine}";
            logTextBox.AppendText(logEntry);
            logTextBox.ScrollToCaret();
        }

        private void Log(string message)
        {
            Debug.WriteLine(message);
            
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => Log(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
            logTextBox.AppendText(logEntry);
            logTextBox.ScrollToCaret();
        }

        private void BrowseForProjectDirectory()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Unity Project Directory";
                dialog.ShowNewFolderButton = false;
                
                if (Directory.Exists(projectPath))
                {
                    dialog.SelectedPath = projectPath;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    projectPath = dialog.SelectedPath;
                    projectDirComboBox.Text = projectPath;
                    AddToRecentProjects(projectPath);
                    LogMessage(string.Format("Selected project directory: {0}", projectPath));
                    Debug.WriteLine(string.Format("Selected project directory: {0}", projectPath));
                }
            }
        }

        private void LoadRecentProjects()
        {
            try
            {
                if (File.Exists(RECENT_PROJECTS_FILE))
                {
                    var lines = File.ReadAllLines(RECENT_PROJECTS_FILE);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && Directory.Exists(line))
                        {
                            recentProjects.Add(line);
                            Debug.WriteLine($"Loaded recent project: {line}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recent projects: {ex.Message}");
            }
        }

        private void AddToRecentProjects(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                // Remove existing entry if present
                recentProjects.Remove(path);

                // Add to beginning of list
                recentProjects.Insert(0, path);

                // Trim list to max size
                while (recentProjects.Count > MAX_RECENT_PROJECTS)
                {
                    recentProjects.RemoveAt(recentProjects.Count - 1);
                }

                // Save to file
                File.WriteAllLines(RECENT_PROJECTS_FILE, recentProjects);
                Debug.WriteLine($"Added {path} to recent projects");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding to recent projects: {ex.Message}");
            }
        }

        private bool IsValidUnityProject(string? path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return false;
            }

            // Check for required Unity project folders
            string[] requiredFolders = { "Assets", "ProjectSettings", "Packages" };
            foreach (string folder in requiredFolders)
            {
                if (!Directory.Exists(Path.Combine(path, folder)))
                {
                    return false;
                }
            }

            return true;
        }

        private void PopulateRecentProjects()
        {
            try
            {
                // Clear existing items
                projectDirComboBox.Items.Clear();

                // Add recent projects
                foreach (var project in recentProjects)
                {
                    if (Directory.Exists(project))
                    {
                        projectDirComboBox.Items.Add(project);
                        Debug.WriteLine($"Added recent project to combo box: {project}");
                    }
                }

                // Search for additional Unity projects
                var registryProjects = GetProjectsFromRegistry();
                foreach (var project in registryProjects)
                {
                    if (!recentProjects.Contains(project) && Directory.Exists(project))
                    {
                        projectDirComboBox.Items.Add(project);
                        Debug.WriteLine($"Added registry project to combo box: {project}");
                    }
                }

                var commonProjects = SearchCommonProjectDirectories();
                foreach (var project in commonProjects)
                {
                    if (!recentProjects.Contains(project) && !registryProjects.Contains(project) && Directory.Exists(project))
                    {
                        projectDirComboBox.Items.Add(project);
                        Debug.WriteLine($"Added common project to combo box: {project}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error populating recent projects: {ex.Message}");
            }
        }

        private List<string> GetProjectsFromRegistry()
        {
            var projects = new List<string>();
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Unity Editor 5.x");
                if (key != null)
                {
                    var recentProjects = key.GetValue("RecentlyUsedProjectPaths") as string[];
                    if (recentProjects != null)
                    {
                        foreach (var project in recentProjects)
                        {
                            if (!string.IsNullOrEmpty(project) && Directory.Exists(project))
                            {
                                projects.Add(project);
                                Debug.WriteLine($"Found Unity project in registry: {project}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: Failed to search registry: {ex.Message}");
                MessageBox.Show($"Failed to search registry: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return projects;
        }

        private List<string> SearchCommonProjectDirectories()
        {
            var projects = new List<string>();
            var commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Unity Projects"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "Unity Projects"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos")
            };

            foreach (var basePath in commonPaths)
            {
                try
                {
                    if (Directory.Exists(basePath))
                    {
                        var directories = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories);
                        foreach (var dir in directories)
                        {
                            if (IsValidUnityProject(dir))
                            {
                                projects.Add(dir);
                                Debug.WriteLine($"Found Unity project in common directory: {dir}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR: Failed to search directory {basePath}: {ex.Message}");
                    MessageBox.Show($"Failed to search directory {basePath}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            return projects;
        }

        private DateTime? GetLastAccessTime(string path)
        {
            try
            {
                string assetsPath = Path.Combine(path, "Assets");
                if (Directory.Exists(assetsPath))
                {
                    return Directory.GetLastAccessTime(assetsPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: Failed to get project access time: {ex.Message}");
                return null;
            }
            return null;
        }

        private string ValidateProjectStructure()
        {
            StringBuilder issues = new StringBuilder();
            
            // Check for required folders
            foreach (string folder in expectedFolders)
            {
                string path = Path.Combine(projectPath, folder);
                if (!Directory.Exists(path))
                {
                    issues.AppendLine($"❌ Missing required folder: {folder}");
                }
            }

            // Check recommended structure
            foreach (string folder in recommendedAssetFolders)
            {
                string path = Path.Combine(projectPath, folder);
                if (!Directory.Exists(path))
                {
                    issues.AppendLine($"⚠️ Recommended folder missing: {folder}");
                }
            }

            // Check for common issues
            foreach (var check in commonIssueChecks)
            {
                string path = Path.Combine(projectPath, check.Path);
                if (File.Exists(path) || Directory.Exists(path))
                {
                    // Additional specific checks based on path
                    if (check.Path == "Library/PackageCache" && IsPackageCacheCorrupted())
                    {
                        issues.AppendLine($"⚠️ Potential issue in {check.Path}: {check.Issue}");
                        issues.AppendLine($"   Recommendation: {check.Recommendation}");
                    }
                    else if (check.Path == "ProjectSettings/ProjectVersion.txt" && HasUnityVersionMismatch())
                    {
                        issues.AppendLine($"⚠️ {check.Issue}");
                        issues.AppendLine($"   Recommendation: {check.Recommendation}");
                    }
                }
            }

            return issues.ToString();
        }

        private bool IsPackageCacheCorrupted()
        {
            try
            {
                string packageCachePath = Path.Combine(projectPath, "Library/PackageCache");
                if (!Directory.Exists(packageCachePath)) return false;

                // Check for incomplete package folders
                var packageFolders = Directory.GetDirectories(packageCachePath);
                foreach (var folder in packageFolders)
                {
                    if (!File.Exists(Path.Combine(folder, "package.json")))
                    {
                        return true; // Corrupted if package.json is missing
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return true; // Consider corrupted if we can't access it
            }
        }

        private bool HasUnityVersionMismatch()
        {
            try
            {
                string versionPath = Path.Combine(projectPath, "ProjectSettings/ProjectVersion.txt");
                if (!File.Exists(versionPath)) return false;

                string[] lines = File.ReadAllLines(versionPath);
                string projectVersion = lines.FirstOrDefault(l => l.StartsWith("m_EditorVersion:"))?.Split(':')[1].Trim();
                
                // Check if project version matches any installed Unity version
                string unityHubPath = @"C:\Program Files\Unity Hub\Unity Hub.exe";
                if (File.Exists(unityHubPath) && !string.IsNullOrEmpty(projectVersion))
                {
                    string editorPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity", projectVersion);
                    return !Directory.Exists(editorPath);
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GetProjectRecommendations()
        {
            StringBuilder recommendations = new StringBuilder();
            recommendations.AppendLine("Project Recommendations:");

            // Check Assets folder organization
            string assetsPath = Path.Combine(projectPath, "Assets");
            if (Directory.Exists(assetsPath))
            {
                var rootFiles = Directory.GetFiles(assetsPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => !f.EndsWith(".meta"));
                
                if (rootFiles.Any())
                {
                    recommendations.AppendLine("⚠️ Found files in Assets root folder. Consider organizing them into appropriate subfolders.");
                }
            }

            // Check for large files
            const long largeFileThreshold = 100 * 1024 * 1024; // 100MB
            var largeFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("Library") && new FileInfo(f).Length > largeFileThreshold)
                .Take(5);

            if (largeFiles.Any())
            {
                recommendations.AppendLine("\n⚠️ Large files detected (>100MB):");
                foreach (var file in largeFiles)
                {
                    recommendations.AppendLine($"   - {Path.GetFileName(file)} ({new FileInfo(file).Length / 1024 / 1024}MB)");
                }
                recommendations.AppendLine("   Consider using Asset Bundles or splitting large assets.");
            }

            // Check for empty folders
            var emptyFolders = Directory.GetDirectories(projectPath, "*", SearchOption.AllDirectories)
                .Where(d => !d.Contains("Library") && !Directory.EnumerateFileSystemEntries(d).Any())
                .Take(5);

            if (emptyFolders.Any())
            {
                recommendations.AppendLine("\n⚠️ Empty folders detected:");
                foreach (var folder in emptyFolders)
                {
                    recommendations.AppendLine($"   - {folder.Replace(projectPath, "")}");
                }
                recommendations.AppendLine("   Consider removing empty folders or adding .keep files.");
            }

            return recommendations.ToString();
        }

        private void DonateButton_Click(object sender, EventArgs e)
        {
            OpenDonationLink();
        }

        private void DonateLink_Click(object sender, EventArgs e)
        {
            OpenDonationLink();
        }

        private void OpenDonationLink()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.paypal.com/paypalme/scottvectradesigns",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open PayPal link. Please send donations manually to scott@vectradesigns.com\n\nError: {ex.Message}", 
                    "Donation Link Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
            }
        }

        private string? GetVersionFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);
                    if (lines.Length > 0)
                    {
                        return lines[0].Trim();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: Failed to read version from file: {ex.Message}");
                return null;
            }
        }

        private void AboutItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Unity Cache Cleaner\n\n" +
                "A tool to help clean Unity project caches and improve build times.\n\n" +
                "Created by Scott Vectra\n" +
                " 2024 Vectra Designs",
                "About Unity Cache Cleaner",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void VersionItem_Click(object? sender, EventArgs e)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            MessageBox.Show(
                $"Unity Cache Cleaner v{version}\n\n" +
                "Build Information:\n" +
                $"- .NET Runtime: {Environment.Version}\n" +
                $"- OS: {Environment.OSVersion}",
                "Version Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void DocumentationItem_Click(object? sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/serovectra/UnityCacheCleaner/wiki",
                UseShellExecute = true
            });
        }

        private async void BuildAndRunButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectPath) || !IsValidUnityProject(projectPath))
            {
                MessageBox.Show("Please select a valid Unity project directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                buildAndRunButton.Enabled = false;
                statusLabel.Text = "Building and running...";
                Debug.WriteLine($"Starting build and run for project: {projectPath}");

                var buildManager = new BuildManager(projectPath);
                await buildManager.BuildAndRun();

                LogSuccess("Build and run completed successfully!");
            }
            catch (Exception ex)
            {
                LogError($"Error during build and run: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                buildAndRunButton.Enabled = true;
                statusLabel.Text = "Ready";
            }
        }

        private void ProjectDirComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedPath = projectDirComboBox.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedPath))
            {
                projectPath = selectedPath;
                UpdateDebugState();
                Debug.WriteLine($"Selected project path: {projectPath}");
            }
        }

        private void CheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateDebugState();
            if (sender is CheckBox checkBox)
            {
                Debug.WriteLine($"Checkbox {checkBox.Name} changed to {checkBox.Checked}");
            }
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            Debug.WriteLine("Cancel button clicked");
            cancellationTokenSource?.Cancel();
            LogMessage("Operation cancelled by user");
        }
    }
}
