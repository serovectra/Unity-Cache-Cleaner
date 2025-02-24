using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace UnityCacheCleaner
{
    public static class ControlExtensions
    {
        public static Task InvokeAsync(this Control control, Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            if (control.InvokeRequired)
            {
                control.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        action();
                        tcs.SetResult(null!);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }));
            }
            else
            {
                try
                {
                    action();
                    tcs.SetResult(null!);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }
            return tcs.Task;
        }
    }

    public partial class MainForm : Form
    {
        // UI Controls
        private Button cleanButton = new();
        private RichTextBox logTextBox = new();
        private Label statusLabel = new();
        private Button browseButton = new();
        private ComboBox projectPathComboBox = new();
        private CheckBox cleanLibraryCache = new();
        private CheckBox cleanTempCache = new();
        private CheckBox cleanEditorCache = new();
        private CheckBox signOutUnity = new();
        private ProgressBar progressBar = new();
        private Label percentLabel = new();
        private Button cancelButton = new();
        private MenuStrip menuStrip;
        private ToolStripMenuItem helpMenuItem;
        private ToolStripMenuItem versionMenuItem;
        private ToolStripMenuItem donateMenuItem;
        private ToolStripMenuItem paypalMenuItem;
        private ToolStripMenuItem cashappMenuItem;
        private ToolStripMenuItem venmoMenuItem;
        private ToolStripMenuItem schemeMenuItem;
        private ColorScheme currentScheme = ColorScheme.Dark;

        private enum ColorScheme
        {
            Dark,
            Light,
            Blue,
            Green,
            Custom
        }

        private class SchemeColors
        {
            public Color Background { get; set; }
            public Color Foreground { get; set; }
            public Color Accent { get; set; }
            public Color MenuBackground { get; set; }
            public Color MenuForeground { get; set; }
            public Color ButtonBackground { get; set; }
            public Color ButtonForeground { get; set; }
        }

        private readonly Dictionary<ColorScheme, SchemeColors> colorSchemes = new()
        {
            [ColorScheme.Light] = new SchemeColors
            {
                Background = Color.FromArgb(248, 249, 250),
                Foreground = Color.FromArgb(33, 37, 41),
                Accent = Color.FromArgb(0, 123, 255),
                MenuBackground = Color.FromArgb(255, 255, 255),
                MenuForeground = Color.FromArgb(33, 37, 41),
                ButtonBackground = Color.FromArgb(0, 123, 255),
                ButtonForeground = Color.White
            },
            [ColorScheme.Dark] = new SchemeColors
            {
                Background = Color.FromArgb(33, 37, 41),
                Foreground = Color.FromArgb(248, 249, 250),
                Accent = Color.FromArgb(0, 123, 255),
                MenuBackground = Color.FromArgb(52, 58, 64),
                MenuForeground = Color.FromArgb(248, 249, 250),
                ButtonBackground = Color.FromArgb(0, 123, 255),
                ButtonForeground = Color.White
            },
            [ColorScheme.Blue] = new SchemeColors
            {
                Background = Color.FromArgb(236, 242, 248),
                Foreground = Color.FromArgb(27, 46, 75),
                Accent = Color.FromArgb(0, 98, 204),
                MenuBackground = Color.FromArgb(245, 248, 251),
                MenuForeground = Color.FromArgb(27, 46, 75),
                ButtonBackground = Color.FromArgb(0, 98, 204),
                ButtonForeground = Color.White
            },
            [ColorScheme.Green] = new SchemeColors
            {
                Background = Color.FromArgb(236, 248, 242),
                Foreground = Color.FromArgb(27, 75, 46),
                Accent = Color.FromArgb(0, 204, 98),
                MenuBackground = Color.FromArgb(245, 251, 248),
                MenuForeground = Color.FromArgb(27, 75, 46),
                ButtonBackground = Color.FromArgb(0, 204, 98),
                ButtonForeground = Color.White
            },
            [ColorScheme.Custom] = new SchemeColors
            {
                Background = Color.FromArgb(248, 249, 250),
                Foreground = Color.FromArgb(33, 37, 41),
                Accent = Color.FromArgb(0, 123, 255),
                MenuBackground = Color.FromArgb(255, 255, 255),
                MenuForeground = Color.FromArgb(33, 37, 41),
                ButtonBackground = Color.FromArgb(0, 123, 255),
                ButtonForeground = Color.White
            }
        };

        // Constants and settings
        private const string RECENT_PROJECTS_FILE = "recent_projects.txt";
        private const int MAX_RECENT_PROJECTS = 10;
        private const string UNITY_PROCESS_NAME = "Unity";
        private const string UNITY_HUB_PROCESS_NAME = "Unity Hub";
        private const string UNITY_CRASH_HANDLER = "UnityCrashHandler";
        private List<string> unityProcessNames = new() { UNITY_PROCESS_NAME, UNITY_HUB_PROCESS_NAME, UNITY_CRASH_HANDLER };

        // State
        private string projectPath = string.Empty;
        private readonly List<string> recentProjects = new();
        private CancellationTokenSource? cancellationTokenSource;
        private bool isCleaningInProgress;

        // Safe directories to clean within Library
        private readonly string[] safeTempPaths = new[]
        {
            "Temp",
            "Library/ShaderCache",
            "Library/TempArtifacts",
            "Library/BuildCache",
            "Library/ArtifactDB",
            "Library/SourceAssetDB",
            "Library/APIUpdater",
            "Library/BurstCache",
            "Library/PackageCache"
        };

        // Protected paths that should not be deleted
        private readonly string[] protectedPaths = new[]
        {
            "ProjectSettings",
            "Assets",
            "Packages",
            "Library/LastSceneManagerSetup.txt",
            "Library/EditorUserBuildSettings.asset",
            "Library/BuildPlayer.prefs",
            "Library/assetservercachev3",
            "Library/unity default resources",
            "Library/unity editor resources",
            "Library/ScriptMapper"
        };

        // Common Unity project structure and validation
        private readonly string[] expectedFolders = new[]
        {
            "Assets",
            "Packages",
            "ProjectSettings",
            "Library"
        };

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

        private readonly (string Path, string Issue, string Recommendation)[] commonIssueChecks = new[]
        {
            ("Library/PackageCache", "Package cache might be corrupted", "Try deleting the PackageCache folder and let Unity rebuild it"),
            ("Library/ScriptAssemblies", "Script assembly issues", "Check for script compilation errors in the console"),
            ("Library/ShaderCache", "Shader compilation issues", "Clear the ShaderCache if you're experiencing material issues"),
            ("ProjectSettings/ProjectVersion.txt", "Unity version mismatch", "Ensure all team members use the same Unity version"),
            ("Assets/Plugins", "Plugin conflicts", "Check for duplicate or conflicting plugin versions"),
            ("Packages/manifest.json", "Package dependency issues", "Verify package versions in manifest.json")
        };

        private const string APPLICATION_NAME = "Unity Cache Cleaner";
        private const string VERSION = "1.3.0-beta";
        private const string COPYRIGHT = " 2025 Unity Cache Cleaner";

        public MainForm()
        {
            SetupMenuStrip();
            SetupMainLayout();
            LoadRecentProjects();

            Debug.WriteLine("MainForm initialized");
        }

        private void SetupMenuStrip()
        {
            // Initialize MenuStrip
            menuStrip = new MenuStrip();

            // Initialize menu items
            donateMenuItem = new ToolStripMenuItem();
            paypalMenuItem = new ToolStripMenuItem();
            cashappMenuItem = new ToolStripMenuItem();
            venmoMenuItem = new ToolStripMenuItem();
            helpMenuItem = new ToolStripMenuItem("Help");
            versionMenuItem = new ToolStripMenuItem($"Version {VERSION}");
            schemeMenuItem = new ToolStripMenuItem("Color Scheme");

            // Configure MenuStrip
            menuStrip.Items.AddRange(new ToolStripItem[] { helpMenuItem, schemeMenuItem, donateMenuItem });
            menuStrip.Dock = DockStyle.Bottom;
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(6);
            menuStrip.BackColor = colorSchemes[currentScheme].MenuBackground;
            menuStrip.ForeColor = colorSchemes[currentScheme].MenuForeground;
            menuStrip.RenderMode = ToolStripRenderMode.Professional;

            // Configure Help Menu
            helpMenuItem.DropDownItems.Add(versionMenuItem);
            helpMenuItem.ForeColor = colorSchemes[currentScheme].MenuForeground;

            // Configure Color Scheme Menu
            foreach (ColorScheme scheme in Enum.GetValues(typeof(ColorScheme)))
            {
                var schemeItem = new ToolStripMenuItem(scheme.ToString())
                {
                    Tag = scheme,
                    Checked = scheme == currentScheme
                };
                schemeItem.Click += SchemeItem_Click;
                schemeMenuItem.DropDownItems.Add(schemeItem);
            }

            // Add custom colors option
            var customizeItem = new ToolStripMenuItem("Customize Colors...");
            customizeItem.Click += CustomizeColors_Click;
            schemeMenuItem.DropDownItems.Add(new ToolStripSeparator());
            schemeMenuItem.DropDownItems.Add(customizeItem);

            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;

            ApplyColorScheme();
        }

        private void SchemeItem_Click(object? sender, EventArgs e)
        {
            try
            {
                if (sender is not ToolStripMenuItem menuItem)
                {
                    Debug.WriteLine("SchemeItem_Click: sender is not a ToolStripMenuItem");
                    return;
                }

                if (menuItem.Tag is not ColorScheme scheme)
                {
                    Debug.WriteLine("SchemeItem_Click: Tag is not a ColorScheme");
                    return;
                }

                Debug.WriteLine($"Changing color scheme to: {scheme}");
                currentScheme = scheme;

                // Update checkmarks
                foreach (ToolStripItem item in schemeMenuItem.DropDownItems)
                {
                    // Skip separators and items without tags
                    if (item is not ToolStripMenuItem menuItemToUpdate || item.Tag is not ColorScheme)
                    {
                        continue;
                    }

                    menuItemToUpdate.Checked = (ColorScheme)item.Tag == currentScheme;
                }

                ApplyColorScheme();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SchemeItem_Click: {ex.Message}");
                LogError($"Failed to change color scheme: {ex.Message}");
            }
        }

        private void CustomizeColors_Click(object? sender, EventArgs e)
        {
            try
            {
                var colors = colorSchemes[ColorScheme.Custom];
                var customizeForm = new Form
                {
                    Text = "Customize Colors",
                    Size = new Size(400, 500),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10),
                    RowCount = 8,
                    ColumnCount = 2
                };

                var colorButtons = new Dictionary<string, (Button button, Color initialColor, Action<Color> setter)>
                {
                    ["Background"] = (new Button(), colors.Background, c => colors.Background = c),
                    ["Text Color"] = (new Button(), colors.Foreground, c => colors.Foreground = c),
                    ["Accent"] = (new Button(), colors.Accent, c => colors.Accent = c),
                    ["Menu Background"] = (new Button(), colors.MenuBackground, c => colors.MenuBackground = c),
                    ["Menu Text"] = (new Button(), colors.MenuForeground, c => colors.MenuForeground = c),
                    ["Button Background"] = (new Button(), colors.ButtonBackground, c => colors.ButtonBackground = c),
                    ["Button Text"] = (new Button(), colors.ButtonForeground, c => colors.ButtonForeground = c)
                };

                int row = 0;
                foreach (var item in colorButtons)
                {
                    var label = new Label
                    {
                        Text = item.Key,
                        AutoSize = true,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    var button = item.Value.button;
                    button.BackColor = item.Value.initialColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.Size = new Size(100, 30);
                    button.Click += (s, e) =>
                    {
                        using var dialog = new ColorDialog
                        {
                            Color = button.BackColor,
                            FullOpen = true
                        };

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            button.BackColor = dialog.Color;
                            item.Value.setter(dialog.Color);
                        }
                    };

                    layout.Controls.Add(label, 0, row);
                    layout.Controls.Add(button, 1, row);
                    row++;
                }

                var previewButton = new Button
                {
                    Text = "Preview",
                    Dock = DockStyle.Bottom,
                    Height = 30,
                    Margin = new Padding(0, 10, 0, 0)
                };
                previewButton.Click += (s, e) =>
                {
                    currentScheme = ColorScheme.Custom;
                    ApplyColorScheme();
                };

                var saveButton = new Button
                {
                    Text = "Save",
                    Dock = DockStyle.Bottom,
                    Height = 30,
                    Margin = new Padding(0, 10, 0, 0)
                };
                saveButton.Click += (s, e) =>
                {
                    currentScheme = ColorScheme.Custom;
                    ApplyColorScheme();
                    customizeForm.Close();
                };

                customizeForm.Controls.Add(layout);
                customizeForm.Controls.Add(previewButton);
                customizeForm.Controls.Add(saveButton);

                Debug.WriteLine("Opening color customization dialog");
                customizeForm.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CustomizeColors_Click: {ex.Message}");
                LogError($"Failed to open color customization dialog: {ex.Message}");
            }
        }

        private void ApplyColorScheme()
        {
            try
            {
                var colors = colorSchemes[currentScheme];

                // Apply to form
                BackColor = colors.Background;
                ForeColor = colors.Foreground;

                // Apply to menu
                menuStrip.BackColor = colors.MenuBackground;
                menuStrip.ForeColor = colors.MenuForeground;
                foreach (ToolStripItem item in menuStrip.Items)
                {
                    item.ForeColor = colors.MenuForeground;
                }

                // Apply to controls
                cleanButton.BackColor = colors.ButtonBackground;
                cleanButton.ForeColor = colors.ButtonForeground;
                browseButton.BackColor = colors.ButtonBackground;
                browseButton.ForeColor = colors.ButtonForeground;
                cancelButton.BackColor = colors.ButtonBackground;
                cancelButton.ForeColor = colors.ButtonForeground;

                // Apply to other controls
                logTextBox.BackColor = colors.Background;
                logTextBox.ForeColor = colors.Foreground;
                projectPathComboBox.BackColor = colors.Background;
                projectPathComboBox.ForeColor = colors.Foreground;

                Debug.WriteLine($"Applied color scheme: {currentScheme}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying color scheme: {ex.Message}");
                LogError("Failed to apply color scheme");
            }
        }

        private void SetupMainLayout()
        {
            // Form properties
            Text = $"{APPLICATION_NAME} v{VERSION}";
            MinimumSize = new Size(600, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = colorSchemes[currentScheme].Background;
            ForeColor = colorSchemes[currentScheme].Foreground;

            // Main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 5,
                ColumnCount = 1,
                BackColor = colorSchemes[currentScheme].Background
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Project selection panel
            var projectPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                Margin = new Padding(0, 0, 0, 10)
            };
            projectPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            projectPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            projectPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var projectLabel = new Label
            {
                Text = "Project:",
                AutoSize = true,
                Margin = new Padding(0, 5, 5, 0),
                ForeColor = colorSchemes[currentScheme].Foreground
            };

            projectPathComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = colorSchemes[currentScheme].Background,
                ForeColor = colorSchemes[currentScheme].Foreground,
                FlatStyle = FlatStyle.Flat
            };

            browseButton = new Button
            {
                Text = "Browse",
                AutoSize = true,
                Margin = new Padding(5, 0, 0, 0),
                BackColor = colorSchemes[currentScheme].ButtonBackground,
                ForeColor = colorSchemes[currentScheme].ButtonForeground,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(10, 5, 10, 5)
            };

            projectPanel.Controls.Add(projectLabel, 0, 0);
            projectPanel.Controls.Add(projectPathComboBox, 1, 0);
            projectPanel.Controls.Add(browseButton, 2, 0);

            // Options panel
            var optionsGroup = new GroupBox
            {
                Text = "Cleaning Options",
                Dock = DockStyle.Fill,
                AutoSize = true,
                ForeColor = colorSchemes[currentScheme].Foreground,
                Padding = new Padding(10),
                Height = 180
            };

            var optionsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                AutoSize = true
            };

            cleanTempCache = CreateOptionCheckBox("Clean Temporary Files",
                "cleanTempCache", true);
            cleanLibraryCache = CreateOptionCheckBox("Clean Library Cache",
                "cleanLibraryCache", true);
            cleanEditorCache = CreateOptionCheckBox("Clean Editor Cache",
                "cleanEditorCache", true);
            signOutUnity = CreateOptionCheckBox("Sign Out of Unity",
                "signOutUnity", false);

            signOutUnity.CheckedChanged += SignOutUnity_CheckedChanged;

            optionsLayout.Controls.Add(cleanTempCache);
            optionsLayout.Controls.Add(cleanLibraryCache);
            optionsLayout.Controls.Add(cleanEditorCache);
            optionsLayout.Controls.Add(signOutUnity);

            optionsGroup.Controls.Add(optionsLayout);

            // Log panel
            var logPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 10)
            };

            logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = colorSchemes[currentScheme].Background,
                ForeColor = colorSchemes[currentScheme].Foreground,
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.None
            };

            logPanel.Controls.Add(logTextBox);

            // Bottom panel
            var bottomPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 4,
                Margin = new Padding(0)
            };
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Progress bar
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Percent label
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Clean button
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Cancel button

            progressBar = new ProgressBar
            {
                Width = 200,
                Height = 23,
                Style = ProgressBarStyle.Continuous,
                MarqueeAnimationSpeed = 30
            };

            percentLabel = new Label
            {
                Text = "0%",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(5, 0, 10, 0),
                ForeColor = colorSchemes[currentScheme].Foreground
            };

            cleanButton = new Button
            {
                Text = "Clean",
                AutoSize = true,
                Margin = new Padding(0),
                BackColor = colorSchemes[currentScheme].ButtonBackground,
                ForeColor = colorSchemes[currentScheme].ButtonForeground,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(20, 5, 20, 5)
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(5, 0, 0, 0),
                BackColor = colorSchemes[currentScheme].ButtonBackground,
                ForeColor = colorSchemes[currentScheme].ButtonForeground,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(10, 5, 10, 5)
            };

            bottomPanel.Controls.Add(progressBar, 0, 0);
            bottomPanel.Controls.Add(percentLabel, 1, 0);
            bottomPanel.Controls.Add(cleanButton, 2, 0);
            bottomPanel.Controls.Add(cancelButton, 3, 0);

            // Add all panels to main layout
            mainLayout.Controls.Add(projectPanel, 0, 0);
            mainLayout.Controls.Add(optionsGroup, 0, 1);
            mainLayout.Controls.Add(logPanel, 0, 2);
            mainLayout.Controls.Add(bottomPanel, 0, 3);

            Controls.Add(mainLayout);

            // Set up event handlers
            SetupEventHandlers();

            // Load recent projects
            LoadRecentProjects();
            PopulateRecentProjects();

            // Initialize state
            UpdateCleanButtonState();
        }

        private void SetupEventHandlers()
        {
            cleanButton.Click += CleanButton_Click;
            browseButton.Click += BrowseButton_Click;
            cleanTempCache.CheckedChanged += CheckBox_CheckedChanged;
            cleanLibraryCache.CheckedChanged += CheckBox_CheckedChanged;
            cleanEditorCache.CheckedChanged += CheckBox_CheckedChanged;
            signOutUnity.CheckedChanged += CheckBox_CheckedChanged;
            projectPathComboBox.SelectedIndexChanged += ProjectPathComboBox_SelectedIndexChanged;
            cancelButton.Click += CancelButton_Click;
        }

        private async void BrowseButton_Click(object sender, EventArgs e)
        {
            if (isCleaningInProgress)
            {
                LogError("Cannot browse while cleaning is in progress");
                return;
            }

            try
            {
                browseButton.Enabled = false;
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select Unity Project Directory";
                    folderDialog.UseDescriptionForTitle = true;
                    folderDialog.ShowNewFolderButton = false;

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        string selectedPath = folderDialog.SelectedPath;
                        Debug.WriteLine($"Selected directory: {selectedPath}");

                        if (string.IsNullOrEmpty(selectedPath))
                        {
                            LogError("Selected path is empty");
                            return;
                        }

                        if (!Directory.Exists(selectedPath))
                        {
                            LogError("Selected directory does not exist");
                            return;
                        }

                        try
                        {
                            await ValidateProjectAsync(selectedPath);
                            projectPathComboBox.Text = selectedPath;
                            PopulateRecentProjects();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error validating project: {ex.Message}");
                            LogError($"Error validating project: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in browse button click: {ex.Message}");
                LogError($"Error selecting directory: {ex.Message}");
            }
            finally
            {
                browseButton.Enabled = true;
            }
        }

        private async Task ValidateProjectAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Project path cannot be empty");
            }

            if (!Directory.Exists(path))
            {
                throw new ArgumentException("Directory does not exist");
            }

            try
            {
                Debug.WriteLine($"Validating project path: {path}");

                bool isValid = await Task.Run(() =>
                {
                    try
                    {
                        return IsValidUnityProjectDirectory(path);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error checking Unity project: {ex.Message}");
                        return false;
                    }
                });

                if (!isValid)
                {
                    throw new InvalidOperationException("Selected directory is not a valid Unity project");
                }

                projectPath = path;
                await Task.Run(() => AddToRecentProjects(path));
                LogSuccess($"Valid Unity project found at: {path}");
                UpdateCleanButtonState();
                await UpdateDebugStateAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error validating project: {ex.Message}");
                throw; // Rethrow to be handled by caller
            }
        }

        private bool IsValidUnityProjectDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    return false;
                }

                // Check for essential Unity project folders
                bool hasAssetsFolder = Directory.Exists(Path.Combine(path, "Assets"));
                bool hasProjectSettingsFolder = Directory.Exists(Path.Combine(path, "ProjectSettings"));
                bool hasPackagesFolder = Directory.Exists(Path.Combine(path, "Packages"));

                // Check for Unity project file
                bool hasProjectFile = File.Exists(Path.Combine(path, "ProjectSettings", "ProjectVersion.txt"));

                Debug.WriteLine($"Unity project validation - Assets: {hasAssetsFolder}, ProjectSettings: {hasProjectSettingsFolder}, " +
                              $"Packages: {hasPackagesFolder}, ProjectFile: {hasProjectFile}");

                return hasAssetsFolder && hasProjectSettingsFolder && hasPackagesFolder && hasProjectFile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error validating Unity project directory: {ex.Message}");
                return false;
            }
        }

        private async void CleanButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateSelectedPath())
                {
                    return;
                }

                // Check for running Unity processes
                var unityProcesses = Process.GetProcessesByName("Unity");
                var unityHubProcesses = Process.GetProcessesByName("Unity Hub");
                
                if (unityProcesses.Length > 0 || unityHubProcesses.Length > 0)
                {
                    var message = "Unity or Unity Hub is currently running. These applications must be closed before cleaning the cache.\n\n" +
                                "Would you like to close them automatically?";
                    
                    var result = MessageBox.Show(
                        message,
                        "Unity Processes Running",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.Cancel)
                    {
                        Debug.WriteLine("Cache cleaning cancelled - user chose not to close Unity");
                        return;
                    }

                    if (result == DialogResult.Yes)
                    {
                        Debug.WriteLine("Attempting to close Unity processes");
                        foreach (var process in unityProcesses.Concat(unityHubProcesses))
                        {
                            try
                            {
                                process.CloseMainWindow();
                                if (!process.WaitForExit(5000)) // Wait up to 5 seconds
                                {
                                    process.Kill(); // Force close if it doesn't respond
                                }
                                Debug.WriteLine($"Closed process: {process.ProcessName}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Failed to close process {process.ProcessName}: {ex.Message}");
                            }
                        }

                        // Double check if processes are closed
                        unityProcesses = Process.GetProcessesByName("Unity");
                        unityHubProcesses = Process.GetProcessesByName("Unity Hub");
                        if (unityProcesses.Length > 0 || unityHubProcesses.Length > 0)
                        {
                            MessageBox.Show(
                                "Unable to close all Unity processes. Please close them manually before proceeding.",
                                "Warning",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                            );
                            return;
                        }
                    }
                }

                // Confirm cleaning operation
                var confirmResult = MessageBox.Show(
                    "Are you sure you want to clean the Unity cache?\n\n" +
                    "This will delete:\n" +
                    "- Temporary files\n" +
                    "- Library cache\n" +
                    "- Editor cache\n\n" +
                    "This operation cannot be undone.",
                    "Confirm Cache Cleaning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirmResult != DialogResult.Yes)
                {
                    Debug.WriteLine("Cache cleaning cancelled by user");
                    return;
                }

                StartCleaning();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CleanButton_Click: {ex.Message}");
                LogError($"Failed to start cleaning: {ex.Message}");
                MessageBox.Show(
                    $"An error occurred while preparing to clean: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void SaveRecentProjects()
        {
            try
            {
                Debug.WriteLine("Saving recent projects");
                var projects = projectPathComboBox.Items.Cast<string>().ToList();
                File.WriteAllLines(RECENT_PROJECTS_FILE, projects);
                Debug.WriteLine($"Saved {projects.Count} recent projects");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving recent projects: {ex.Message}");
                LogError($"Failed to save recent projects: {ex.Message}");
            }
        }

        private void LoadRecentProjects()
        {
            try
            {
                Debug.WriteLine("Loading recent projects");
                if (File.Exists(RECENT_PROJECTS_FILE))
                {
                    var projects = File.ReadAllLines(RECENT_PROJECTS_FILE);
                    projectPathComboBox.Items.Clear();
                    foreach (var project in projects)
                    {
                        if (!string.IsNullOrEmpty(project) && Directory.Exists(project) && IsValidUnityProjectDirectory(project))
                        {
                            Debug.WriteLine($"Loading recent project: {project}");
                            if (!projectPathComboBox.Items.Contains(project))
                            {
                                projectPathComboBox.Items.Add(project);
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Skipping invalid project: {project}");
                        }
                    }
                    Debug.WriteLine($"Loaded {projectPathComboBox.Items.Count} recent projects");
                }
                else
                {
                    Debug.WriteLine("No recent projects file found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recent projects: {ex.Message}");
                LogError($"Failed to load recent projects: {ex.Message}");
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

        private void UpdateProgress(int value, int total)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int, int>(UpdateProgress), value, total);
                return;
            }

            try
            {
                progressBar.Value = Math.Min(100, Math.Max(0, (int)((float)value / total * 100)));
                percentLabel.Text = $"{progressBar.Value}%";
                Debug.WriteLine($"Progress updated: {progressBar.Value}%");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating progress: {ex.Message}");
            }
        }

        private void PopulateRecentProjects()
        {
            try
            {
                // Clear existing items
                projectPathComboBox.Items.Clear();

                // Add recent projects
                foreach (var project in recentProjects)
                {
                    if (Directory.Exists(project))
                    {
                        projectPathComboBox.Items.Add(project);
                        Debug.WriteLine($"Added recent project to combo box: {project}");
                    }
                }

                // Search for additional Unity projects
                var unityDirs = FindUnityDirectories();
                
                foreach (var dir in unityDirs)
                {
                    if (!recentProjects.Contains(dir) && Directory.Exists(dir))
                    {
                        projectPathComboBox.Items.Add(dir);
                        Debug.WriteLine($"Added Unity directory to combo box: {dir}");
                    }
                }

                if (projectPathComboBox.Items.Count > 0)
                {
                    projectPathComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error populating recent projects: {ex.Message}");
            }
        }

        private List<string> FindUnityDirectories()
        {
            var unityDirs = new List<string>();
            try
            {
                // Common Unity project locations
                var searchPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Unity Projects"),
                    @"C:\Unity Projects",
                    @"D:\Unity Projects"
                };

                foreach (var searchPath in searchPaths)
                {
                    if (!Directory.Exists(searchPath))
                    {
                        continue;
                    }

                    try
                    {
                        var dirs = Directory.GetDirectories(searchPath, "*", SearchOption.AllDirectories)
                            .Where(dir => Directory.Exists(Path.Combine(dir, "Library")) ||
                                        Directory.Exists(Path.Combine(dir, "ProjectSettings")))
                            .ToList();

                        unityDirs.AddRange(dirs);
                        Debug.WriteLine($"Found {dirs.Count} Unity directories in {searchPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error searching in {searchPath}: {ex.Message}");
                    }
                }

                // Load recent projects
                if (File.Exists(RECENT_PROJECTS_FILE))
                {
                    var recentProjects = File.ReadAllLines(RECENT_PROJECTS_FILE)
                        .Where(dir => Directory.Exists(dir))
                        .ToList();
                    unityDirs.AddRange(recentProjects);
                    Debug.WriteLine($"Loaded {recentProjects.Count} recent projects");
                }

                return unityDirs.Distinct().ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in FindUnityDirectories: {ex.Message}");
                LogError("Failed to find Unity directories");
                return unityDirs;
            }
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

        private void OpenPayPalDonation(object? sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("Opening PayPal donation link (@scottjedelman)");
                LogMessage("Opening PayPal donation link...");

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://paypal.me/scottjedelman",
                    UseShellExecute = true
                };
                using var process = System.Diagnostics.Process.Start(psi);
                Debug.WriteLine("Successfully opened PayPal donation link");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Debug.WriteLine($"Win32 error opening PayPal donation link: {ex.Message}");
                LogError($"Failed to open PayPal donation link: {ex.Message}");
                MessageBox.Show($"Error opening PayPal donation link: {ex.Message}\nPlease send to: @scottjedelman",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"Invalid operation opening PayPal donation link: {ex.Message}");
                LogError($"Failed to open PayPal donation link: {ex.Message}");
                MessageBox.Show($"Error opening PayPal donation link: {ex.Message}\nPlease send to: @scottjedelman",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening PayPal donation link: {ex.Message}");
                LogError($"Failed to open PayPal donation link: {ex.Message}");
                MessageBox.Show("Error opening PayPal donation link. Please send to: @scottjedelman",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenCashAppDonation(object? sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("Opening CashApp donation link ($SeroVectrA)");
                LogMessage("Opening CashApp donation link...");

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://cash.app/$SeroVectrA",
                    UseShellExecute = true
                };
                using var process = System.Diagnostics.Process.Start(psi);
                Debug.WriteLine("Successfully opened CashApp donation link");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Debug.WriteLine($"Win32 error opening CashApp donation link: {ex.Message}");
                LogError($"Failed to open CashApp donation link: {ex.Message}");
                MessageBox.Show($"Error opening CashApp donation link: {ex.Message}\nPlease send to: $SeroVectrA",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"Invalid operation opening CashApp donation link: {ex.Message}");
                LogError($"Failed to open CashApp donation link: {ex.Message}");
                MessageBox.Show($"Error opening CashApp donation link: {ex.Message}\nPlease send to: $SeroVectrA",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening CashApp donation link: {ex.Message}");
                LogError($"Failed to open CashApp donation link: {ex.Message}");
                MessageBox.Show("Error opening CashApp donation link. Please send to: $SeroVectrA",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenVenmoDonation(object? sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("Opening Venmo donation link (@SeroVectrA)");
                LogMessage("Opening Venmo donation link...");

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://venmo.com/SeroVectrA",
                    UseShellExecute = true
                };
                using var process = System.Diagnostics.Process.Start(psi);
                Debug.WriteLine("Successfully opened Venmo donation link");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Debug.WriteLine($"Win32 error opening Venmo donation link: {ex.Message}");
                LogError($"Failed to open Venmo donation link: {ex.Message}");
                MessageBox.Show($"Error opening Venmo donation link: {ex.Message}\nPlease send to: @SeroVectrA",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"Invalid operation opening Venmo donation link: {ex.Message}");
                LogError($"Failed to open Venmo donation link: {ex.Message}");
                MessageBox.Show($"Error opening Venmo donation link: {ex.Message}\nPlease send to: @SeroVectrA",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening Venmo donation link: {ex.Message}");
                LogError($"Failed to open Venmo donation link: {ex.Message}");
                MessageBox.Show("Error opening Venmo donation link. Please send to: @SeroVectrA",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AboutItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                $"{APPLICATION_NAME}\n\n" +
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
                $"{APPLICATION_NAME} v{version}\n\n" +
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

        private void ProjectPathComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string selectedPath = projectPathComboBox.SelectedItem?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _ = ValidateProjectAsync(selectedPath); // Fire and forget async call
            }
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCleanButtonState();
            _ = UpdateDebugStateAsync(); // Fire and forget async call
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            if (isCleaningInProgress && cancellationTokenSource != null)
            {
                Debug.WriteLine("Cancelling cleaning operation...");
                cancellationTokenSource.Cancel();
                LogMessage("Cancelling operation...");
            }
        }

        private void LogMessage(string message)
        {
            try
            {
                if (logTextBox.InvokeRequired)
                {
                    logTextBox.Invoke(new Action(() => LogMessage(message)));
                    return;
                }

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                logTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
                logTextBox.SelectionStart = logTextBox.Text.Length;
                logTextBox.ScrollToCaret();
                Debug.WriteLine($"Log: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging message: {ex.Message}");
            }
        }

        private void LogError(string message)
        {
            try
            {
                if (logTextBox.InvokeRequired)
                {
                    logTextBox.Invoke(new Action(() => LogError(message)));
                    return;
                }

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                logTextBox.AppendText($"[{timestamp}] ERROR: {message}{Environment.NewLine}");
                logTextBox.SelectionStart = logTextBox.Text.Length;
                logTextBox.ScrollToCaret();
                Debug.WriteLine($"Error: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging error message: {ex.Message}");
            }
        }

        private void LogSuccess(string message)
        {
            try
            {
                if (logTextBox.InvokeRequired)
                {
                    logTextBox.Invoke(new Action(() => LogSuccess(message)));
                    return;
                }

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                logTextBox.AppendText($"[{timestamp}] SUCCESS: {message}{Environment.NewLine}");
                logTextBox.SelectionStart = logTextBox.Text.Length;
                logTextBox.ScrollToCaret();
                Debug.WriteLine($"Success: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging success message: {ex.Message}");
            }
        }

        private void AddToRecentProjects(string path)
        {
            try
            {
                Debug.WriteLine($"Adding to recent projects: {path}");

                if (!projectPathComboBox.Items.Contains(path))
                {
                    projectPathComboBox.Items.Add(path);
                    SaveRecentProjects();
                    Debug.WriteLine("Project added to recent list");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding to recent projects: {ex.Message}");
                LogError($"Failed to add project to recent list: {ex.Message}");
            }
        }

        private async Task CloseUnityProcesses()
        {
            try
            {
                Debug.WriteLine("Attempting to close Unity processes...");
                await Task.Run(() =>
                {
                    foreach (var processName in unityProcessNames)
                    {
                        try
                        {
                            var processes = Process.GetProcessesByName(processName);
                            foreach (var process in processes)
                            {
                                Debug.WriteLine($"Attempting to close {processName} process...");
                                process.Kill();
                                process.WaitForExit(5000); // Wait up to 5 seconds
                                Debug.WriteLine($"Successfully closed {processName} process");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error closing {processName} process: {ex.Message}");
                        }
                    }
                });
                Debug.WriteLine("Finished closing Unity processes");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CloseUnityProcesses: {ex.Message}");
                throw;
            }
        }

        private async Task<int> CountFilesToProcess()
        {
            return await Task.Run(() =>
            {
                int count = 0;

                try
                {
                    if (cleanTempCache.Checked)
                    {
                        string tempPath = Path.Combine(projectPath, "Temp");
                        if (Directory.Exists(tempPath))
                        {
                            count += Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories)
                                .Count(file => !protectedPaths.Contains(file));
                        }
                    }

                    if (cleanLibraryCache.Checked)
                    {
                        string libraryPath = Path.Combine(projectPath, "Library");
                        if (Directory.Exists(libraryPath))
                        {
                            count += Directory.GetFiles(libraryPath, "*.*", SearchOption.AllDirectories)
                                .Count(file => !protectedPaths.Contains(file));
                        }
                    }

                    if (cleanEditorCache.Checked)
                    {
                        string editorPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor");
                        if (Directory.Exists(editorPath))
                        {
                            count += Directory.GetFiles(editorPath, "*.*", SearchOption.AllDirectories)
                                .Count(file => !protectedPaths.Contains(file));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error counting files: {ex.Message}");
                }

                return count;
            });
        }

        private async Task StartCleaningAsync(CancellationToken cancellationToken)
        {
            try
            {
                isCleaningInProgress = true;
                UpdateCleanButtonState();
                await UpdateDebugStateAsync();

                cancellationTokenSource = new CancellationTokenSource();
                var token = cancellationTokenSource.Token;

                await CloseUnityProcesses();

                var totalFiles = await CountFilesToProcess();
                if (totalFiles == 0 && !signOutUnity.Checked)
                {
                    LogMessage("No files to clean.");
                    return;
                }

                var processedFiles = 0;
                var progress = new Progress<int>(value =>
                {
                    processedFiles = value;
                    UpdateProgress(processedFiles, totalFiles);
                });

                if (cleanTempCache.Checked)
                {
                    await CleanTempDirectory(value => ((IProgress<int>)progress).Report(value), token);
                }

                if (cleanLibraryCache.Checked)
                {
                    await CleanLibraryCache(value => ((IProgress<int>)progress).Report(value), token);
                }

                if (cleanEditorCache.Checked)
                {
                    await CleanEditorCache(value => ((IProgress<int>)progress).Report(value), token);
                }

                if (signOutUnity.Checked)
                {
                    await SignOutFromUnity();
                }

                LogSuccess("All operations completed successfully!");

                var summary = new StringBuilder();
                summary.AppendLine("Operation Summary:");
                if (cleanTempCache.Checked) summary.AppendLine("✓ Temporary Cache Cleaned");
                if (cleanLibraryCache.Checked) summary.AppendLine("✓ Library Cache Cleaned");
                if (cleanEditorCache.Checked) summary.AppendLine("✓ Editor Cache Cleaned");
                if (signOutUnity.Checked) summary.AppendLine("✓ Unity Sign-out Completed");
                summary.AppendLine("\nTotal files processed: " + processedFiles);
                LogMessage(summary.ToString());

                // Show completion dialog with exit option
                var result = MessageBox.Show(
                    $"All operations completed successfully!\n\n{summary}\nWould you like to exit the application?",
                    "Operations Complete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    Application.Exit();
                }
                else
                {
                    LogMessage("You can safely close the application when ready.");
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("Operation cancelled by user.");
            }
            catch (Exception ex)
            {
                LogError($"Error during operations: {ex.Message}");
            }
            finally
            {
                isCleaningInProgress = false;
                UpdateCleanButtonState();
                await UpdateDebugStateAsync();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        private void StartCleaning()
        {
            try
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    LogError("Please select a Unity project directory first.");
                    return;
                }

                if (!cleanTempCache.Checked && !cleanLibraryCache.Checked && !cleanEditorCache.Checked && !signOutUnity.Checked)
                {
                    LogError("Please select at least one cache type to clean.");
                    return;
                }

                StartCleaningAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in StartCleaning: {ex.Message}");
                LogError($"Error starting cleaning process: {ex.Message}");
            }
        }

        private async Task CleanTempDirectory(Action<int> progress, CancellationToken cancellationToken)
        {
            try
            {
                string tempPath = Path.Combine(projectPath, "Temp");
                if (!Directory.Exists(tempPath))
                {
                    LogMessage("Temp directory not found. Skipping...");
                    return;
                }

                await Task.Run(() =>
                {
                    var files = Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => !protectedPaths.Contains(file));

                    int count = 0;
                    foreach (var file in files)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        try
                        {
                            File.Delete(file);
                            count++;
                            progress(count);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error deleting temp file {file}: {ex.Message}");
                        }
                    }
                }, cancellationToken);

                LogSuccess("Temp directory cleaned successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning temp directory: {ex.Message}");
                LogError($"Error cleaning temp directory: {ex.Message}");
            }
        }

        private async Task CleanLibraryCache(Action<int> progress, CancellationToken cancellationToken)
        {
            try
            {
                string libraryPath = Path.Combine(projectPath, "Library");
                if (!Directory.Exists(libraryPath))
                {
                    LogMessage("Library directory not found. Skipping...");
                    return;
                }

                await Task.Run(() =>
                {
                    var files = Directory.GetFiles(libraryPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => !protectedPaths.Contains(file));

                    int count = 0;
                    foreach (var file in files)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        try
                        {
                            File.Delete(file);
                            count++;
                            progress(count);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error deleting library file {file}: {ex.Message}");
                        }
                    }
                }, cancellationToken);

                LogSuccess("Library cache cleaned successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning library cache: {ex.Message}");
                LogError($"Error cleaning library cache: {ex.Message}");
            }
        }

        private async Task CleanEditorCache(Action<int> progress, CancellationToken cancellationToken)
        {
            try
            {
                string editorPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor");
                if (!Directory.Exists(editorPath))
                {
                    LogMessage("Editor cache directory not found. Skipping...");
                    return;
                }

                await Task.Run(() =>
                {
                    var files = Directory.GetFiles(editorPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => !protectedPaths.Contains(file));

                    int count = 0;
                    foreach (var file in files)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        try
                        {
                            File.Delete(file);
                            count++;
                            progress(count);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error deleting editor cache file {file}: {ex.Message}");
                        }
                    }
                }, cancellationToken);

                LogSuccess("Editor cache cleaned successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning editor cache: {ex.Message}");
                LogError($"Error cleaning editor cache: {ex.Message}");
            }
        }

        private async Task SignOutFromUnity()
        {
            try
            {
                Debug.WriteLine("Starting Unity sign-out process");
                LogMessage("Signing out of Unity...");

                // Unity credential locations
                var locations = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UnityHub"),
                };

                foreach (var location in locations)
                {
                    if (Directory.Exists(location))
                    {
                        try
                        {
                            // Clear Unity credentials
                            var credentialFiles = Directory.GetFiles(location, "*", SearchOption.AllDirectories)
                                .Where(f => f.Contains("Unity.sso", StringComparison.OrdinalIgnoreCase) ||
                                          f.Contains("accessToken", StringComparison.OrdinalIgnoreCase) ||
                                          f.Contains("refreshToken", StringComparison.OrdinalIgnoreCase) ||
                                          f.Contains("credentials", StringComparison.OrdinalIgnoreCase));

                            foreach (var file in credentialFiles)
                            {
                                File.Delete(file);
                                Debug.WriteLine($"Deleted credential file: {file}");
                            }

                            LogMessage($"Cleared Unity credentials from {location}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error clearing credentials from {location}: {ex.Message}");
                            LogError($"Failed to clear some Unity credentials: {ex.Message}");
                        }
                    }
                }

                LogSuccess("Unity sign-out completed");
                MessageBox.Show(
                    "You have been signed out of Unity.\n\n" +
                    "Next time you open Unity or Unity Hub:\n" +
                    "1. You will need to sign in with your Unity ID\n" +
                    "2. Reactivate your Unity license\n" +
                    "3. Reconfigure Unity Hub settings\n\n" +
                    "Make sure you have your credentials ready.",
                    "Unity Sign-Out Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during Unity sign-out: {ex.Message}");
                LogError($"Failed to complete Unity sign-out: {ex.Message}");
            }
        }

        private void ShowVersionInfo()
        {
            try
            {
                string message = $"{APPLICATION_NAME} v{VERSION}\n" +
                               $"Build Date: {DateTime.Now}\n\n" +
                               $" 2025 Serovectra";

                Debug.WriteLine($"Showing version info: {VERSION}");
                MessageBox.Show(message, "Version Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing version info: {ex.Message}");
                LogError("Failed to display version information");
            }
        }

        private CheckBox CreateOptionCheckBox(string text, string name, bool defaultChecked = true)
        {
            var checkBox = new CheckBox
            {
                Text = text,
                Name = name,
                Checked = defaultChecked,
                AutoSize = true,
                ForeColor = colorSchemes[currentScheme].Foreground,
                BackColor = colorSchemes[currentScheme].Background
            };
            checkBox.CheckedChanged += CheckBox_CheckedChanged;
            Debug.WriteLine($"Created checkbox: {name}, Default checked: {defaultChecked}");
            return checkBox;
        }

        private void UpdateCleanButtonState()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(UpdateCleanButtonState));
                    return;
                }

                bool hasValidPath = !string.IsNullOrEmpty(projectPath) && IsValidUnityProjectDirectory(projectPath);
                bool hasOptionSelected = cleanTempCache.Checked || cleanLibraryCache.Checked || cleanEditorCache.Checked || signOutUnity.Checked;
                cleanButton.Enabled = hasValidPath && hasOptionSelected && !isCleaningInProgress;
                cancelButton.Enabled = isCleaningInProgress;
                Debug.WriteLine($"Clean button state updated - Enabled: {cleanButton.Enabled}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating clean button state: {ex.Message}");
            }
        }

        private async Task UpdateDebugStateAsync()
        {
            try
            {
                if (logTextBox.InvokeRequired)
                {
                    await Task.Run(() => this.Invoke(new Action(async () => await UpdateDebugStateAsync())));
                    return;
                }

                StringBuilder debug = new StringBuilder();
                debug.AppendLine("=== Unity Cache Cleaner Debug State ===");
                debug.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                debug.AppendLine($"Project Path: {projectPath}");

                bool unityRunning = await Task.Run(() =>
                    Process.GetProcessesByName("Unity").Length > 0 ||
                    Process.GetProcessesByName("UnityEditor").Length > 0);
                debug.AppendLine($"Unity Editor Running: {unityRunning}");

                debug.AppendLine("\nSelected Options:");
                debug.AppendLine($"- Clean Library Cache: {cleanLibraryCache.Checked}");
                debug.AppendLine($"- Clean Temp Cache: {cleanTempCache.Checked}");
                debug.AppendLine($"- Clean Editor Cache: {cleanEditorCache.Checked}");
                debug.AppendLine($"- Sign Out of Unity: {signOutUnity.Checked}");

                debug.AppendLine("\nProtected Paths:");
                foreach (var path in protectedPaths)
                {
                    debug.AppendLine($"- {path}");
                }

                debug.AppendLine("\nSafe Cache Paths:");
                foreach (var path in safeTempPaths)
                {
                    debug.AppendLine($"- {path}");
                }

                debug.AppendLine("\nSystem Information:");
                await Task.Run(() =>
                {
                    debug.AppendLine($"OS Version: {Environment.OSVersion}");
                    debug.AppendLine($"Machine Name: {Environment.MachineName}");
                    debug.AppendLine($"Processor Count: {Environment.ProcessorCount}");
                    debug.AppendLine($"Working Set: {Environment.WorkingSet / 1024 / 1024} MB");
                    debug.AppendLine($".NET Runtime: {Environment.Version}");
                });
                debug.AppendLine("\n=== End Debug State ===");

                logTextBox.Text = debug.ToString() + Environment.NewLine + logTextBox.Text;
                Debug.WriteLine("Debug state updated successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating debug state: {ex.Message}");
                LogError("Failed to update debug state");
            }
        }

        private string GetCleaningSummary()
        {
            var summary = new StringBuilder();
            summary.AppendLine("Cleaning Summary:");
            summary.AppendLine("----------------");

            if (cleanTempCache.Checked)
                summary.AppendLine("✓ Temporary Files");
            if (cleanLibraryCache.Checked)
                summary.AppendLine("✓ Library Cache");
            if (cleanEditorCache.Checked)
                summary.AppendLine("✓ Editor Cache");
            if (signOutUnity.Checked)
                summary.AppendLine("✓ Sign Out of Unity");

            return summary.ToString();
        }

        private bool ValidateSelectedPath()
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                LogError("Please select a Unity project directory first.");
                return false;
            }

            if (!Directory.Exists(projectPath))
            {
                LogError("Selected directory does not exist.");
                return false;
            }

            if (!IsValidUnityProjectDirectory(projectPath))
            {
                LogError("Selected directory is not a valid Unity project.");
                return false;
            }

            return true;
        }

        private void SignOutUnity_CheckedChanged(object sender, EventArgs e)
        {
            if (signOutUnity.Checked)
            {
                var result = MessageBox.Show(
                    "Warning: Signing out of Unity will:\n\n" +
                    "1. Remove all Unity credentials\n" +
                    "2. Require you to sign in again with your Unity ID\n" +
                    "3. Need to reactivate Unity license\n" +
                    "4. Reset Unity Hub preferences\n\n" +
                    "Make sure you have your Unity ID and password ready before proceeding.\n\n" +
                    "Do you want to proceed with Unity sign-out?",
                    "Unity Sign-Out Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.No)
                {
                    signOutUnity.Checked = false;
                }
                else
                {
                    Debug.WriteLine("User confirmed Unity sign-out option");
                }
            }
        }
    }
}
