using System.Diagnostics;

namespace UnityCacheCleaner
{
    internal static class Program
    {
        private static Mutex? mutex;
        private const string AppGuid = "6A9C4D6F-1234-5678-ABCD-EF1234567890"; // Unique GUID for the app
        public const string Version = "1.2.6"; // Updated for menu fixes and UI improvements
        public const string BuildDate = "2025-02-24"; // Current build date

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                mutex = new Mutex(true, AppGuid, out bool createdNew);

                if (!createdNew)
                {
                    MessageBox.Show("Unity Cache Cleaner is already running.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Debug.WriteLine($"Starting Unity Cache Cleaner v{Version}");
                ApplicationConfiguration.Initialize();

                // Show splash screen
                using var splash = new SplashScreen();
                splash.Show();

                // Create main form while splash is showing
                var mainForm = new MainForm();

                // Start a timer to close the splash screen
                var timer = new System.Windows.Forms.Timer
                {
                    Interval = 5000 // 5 seconds total
                };

                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    splash.Close();
                    mainForm.Show();
                    timer.Dispose();
                };

                timer.Start();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fatal error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"A fatal error occurred: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mutex?.ReleaseMutex();
            }
        }
    }
}