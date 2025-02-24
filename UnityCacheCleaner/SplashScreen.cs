using System.Diagnostics;

namespace UnityCacheCleaner
{
    public class SplashScreen : Form
    {
        private const int SPLASH_WIDTH = 400;
        private const int SPLASH_HEIGHT = 300;
        private readonly string[] broomFrames = new[]
        {
            @"    .-'''''-.    ",
            @"    |       |    ",
            @"    |  [_]  |    ",
            @"    |       |    ",
            @"    |       |    ",
            @"    |       |    ",
            @"    `-.....-'    ",
            @"        |        ",
            @"     ===|===     ",
            @"        |        ",
            @"        |        ",
            @"     .--'        ",
            @"    (           *",
            @"     `--.__   * ",
            @"           `--* "
        };

        private int currentFrame = 0;
        private Label? broomLabel;
        private System.Windows.Forms.Timer? animationTimer;

        public SplashScreen()
        {
            InitializeUI();
            InitializeAnimation();
        }

        private void InitializeAnimation()
        {
            try
            {
                // Initialize broom animation
                broomLabel = new Label
                {
                    Text = string.Join(Environment.NewLine, broomFrames),
                    Font = new Font("Consolas", 10, FontStyle.Regular),
                    ForeColor = Color.FromArgb(200, 200, 200),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true
                };

                // Center the broom in the form
                broomLabel.Location = new Point(
                    (SPLASH_WIDTH - broomLabel.Width) / 2,
                    (SPLASH_HEIGHT - broomLabel.Height) / 2 + 20
                );
                Controls.Add(broomLabel);

                // Setup animation timer
                animationTimer = new System.Windows.Forms.Timer
                {
                    Interval = 100 // Update every 100ms
                };
                animationTimer.Tick += AnimationTimer_Tick;
                animationTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation initialization error: {ex.Message}");
            }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (IsDisposed || broomLabel == null) return;

                // Simple left-right animation
                currentFrame = (currentFrame + 1) % 4;
                int offset = currentFrame - 2; // Range: -2 to 1

                broomLabel.Left = (SPLASH_WIDTH - broomLabel.Width) / 2 + (offset * 2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation error: {ex.Message}");
                animationTimer?.Stop();
            }
        }

        private void InitializeUI()
        {
            try
            {
                // Form settings
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.CenterScreen;
                Size = new Size(SPLASH_WIDTH, SPLASH_HEIGHT);
                BackColor = Color.FromArgb(32, 32, 32);
                TopMost = true;
                ShowInTaskbar = false;

                // Create title label
                var titleLabel = new Label
                {
                    Text = $"Unity Cache Cleaner v{Program.Version}",
                    Font = new Font("Segoe UI", 16, FontStyle.Bold),
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.None,
                    AutoSize = false,
                    Size = new Size(SPLASH_WIDTH, 40),
                    Location = new Point(0, 30)
                };

                // Create subtitle label
                var subtitleLabel = new Label
                {
                    Text = "Sweeping away the cache...",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.FromArgb(200, 200, 200),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.None,
                    AutoSize = false,
                    Size = new Size(SPLASH_WIDTH, 30),
                    Location = new Point(0, 70)
                };

                Controls.Add(titleLabel);
                Controls.Add(subtitleLabel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UI initialization error: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    animationTimer?.Stop();
                    animationTimer?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Dispose error: {ex.Message}");
                }
            }
            base.Dispose(disposing);
        }
    }
}
