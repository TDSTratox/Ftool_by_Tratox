using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using GDI = System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using D2D1 = SharpDX.Direct2D1;

namespace FToolByTratox
{
    public partial class MainForm : Form
    {
        // API Windows pour l'envoi de touches
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private D2D1.Factory d2dFactory;
        private WindowRenderTarget renderTarget;
        private Dictionary<string, D2D1.SolidColorBrush> brushCache = new Dictionary<string, D2D1.SolidColorBrush>();

        const uint WM_KEYDOWN = 0x0100;
        const uint WM_KEYUP = 0x0101;
        const uint WM_CHAR = 0x0102;
        const int WM_HOTKEY = 0x0312;
        const uint MOD_ALT = 0x0001;
        const uint MOD_CONTROL = 0x0002;
        const uint MOD_SHIFT = 0x0004;
        const uint MOD_WIN = 0x0008;
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCAPTION = 2;

        private static readonly Dictionary<string, uint> VirtualKeys = new Dictionary<string, uint>
        {
            {"F1", 112}, {"F2", 113}, {"F3", 114}, {"F4", 115}, {"F5", 116},
            {"F6", 117}, {"F7", 118}, {"F8", 119}, {"F9", 120},
            {"1", 49}, {"2", 50}, {"3", 51}, {"4", 52}, {"5", 53},
            {"6", 54}, {"7", 55}, {"8", 56}, {"9", 57},

            {"Num0", 96}, {"Num1", 97}, {"Num2", 98}, {"Num3", 99}, {"Num4", 100},
            {"Num5", 101}, {"Num6", 102}, {"Num7", 103}, {"Num8", 104}, {"Num9", 105},
            {"NumPlus", 107}, {"NumMinus", 109}, {"NumMultiply", 106}, {"NumDivide", 111},
            {"NumDecimal", 110}, {"NumEnter", 13},
            
            {"Space", 32}, {"Enter", 13}, {"Tab", 9}, {"Escape", 27},
            {"Backspace", 8}, {"Delete", 46}, {"Insert", 45},
            {"Home", 36}, {"End", 35}, {"PageUp", 33}, {"PageDown", 34},

            {"A", 65}, {"B", 66}, {"C", 67}, {"D", 68}, {"E", 69}, {"F", 70},
            {"G", 71}, {"H", 72}, {"I", 73}, {"J", 74}, {"K", 75}, {"L", 76},
            {"M", 77}, {"N", 78}, {"O", 79}, {"P", 80}, {"Q", 81}, {"R", 82},
            {"S", 83}, {"T", 84}, {"U", 85}, {"V", 86}, {"W", 87}, {"X", 88},
            {"Y", 89}, {"Z", 90}

        };
        private static readonly Dictionary<Keys, string> KeyNames = new Dictionary<Keys, string>();

        public class SpammerData
        {
            public Button StartButton { get; set; }
            public ComboBox WindowCombo { get; set; }
            public TextBox IntervalText { get; set; }
            public ComboBox FKeyCombo { get; set; }
            public ComboBox SkillCombo { get; set; }
            public IntPtr TargetWindowHandle { get; set; }
            public string WindowTitle { get; set; } = "";
            public Panel Container { get; set; }
            public Panel StatusIndicator { get; set; }
            public Label StatusLabel { get; set; }
            public Label ActionsLabel { get; set; }
            public bool IsRunning { get; set; }
            public System.Windows.Forms.Timer SpamTimer { get; set; }
            public DateTime LastActionTime { get; set; }
            public int ActionCount { get; set; }
            public string HotKey { get; set; } = "";
            public int HotKeyId { get; set; } = -1;
        }

        public class SettingsData
        {
            public Dictionary<int, string> SpammerHotKeys { get; set; } = new Dictionary<int, string>();
            public string MasterStartHotKey { get; set; } = "";
            public string MasterStopHotKey { get; set; } = "";
            public bool EnableGlobalHotKeys { get; set; } = true;
        }

        private List<SpammerData> spammers = new List<SpammerData>();
        private TabControl tabControl;
        private System.Windows.Forms.Timer windowCheckTimer;
        private System.Windows.Forms.Timer statusUpdateTimer;
        private string settingsFile = "FToolByTratox_Settings.ini";
        private Button masterStartStopButton;
        private Label connectionStatusLabel;
        private Panel titleBar;
        private Button minimizeBtn;
        private Button closeBtn;
        private SettingsData settings = new SettingsData();
        private Dictionary<int, Keys> registeredHotKeys = new Dictionary<int, Keys>();
        private Point mouseLocation;
        private bool isDragging = false;
        private readonly Color PrimaryBackground = GDI.Color.FromArgb(255, 26, 26, 35);
        private readonly Color SecondaryBackground = GDI.Color.FromArgb(255, 35, 35, 50);
        private readonly Color CardBackground = GDI.Color.FromArgb(255, 45, 45, 65);
        private readonly Color AccentRed = GDI.Color.FromArgb(255, 255, 66, 77);
        private readonly Color AccentBlue = GDI.Color.FromArgb(255, 66, 165, 245);
        private readonly Color AccentGreen = GDI.Color.FromArgb(255, 76, 175, 80);
        private readonly Color AccentPurple = GDI.Color.FromArgb(255, 171, 71, 188);
        private readonly Color TextPrimary = GDI.Color.FromArgb(255, 255, 255, 255);
        private readonly Color TextSecondary = GDI.Color.FromArgb(255, 190, 190, 190);
        private readonly Color TextMuted = GDI.Color.FromArgb(255, 140, 140, 140);
        private readonly Color BorderColor = GDI.Color.FromArgb(255, 65, 65, 85);
        private readonly Color HoverColor = GDI.Color.FromArgb(255, 55, 55, 75);
        private readonly Color WarningColor = GDI.Color.FromArgb(255, 255, 193, 7);
        private readonly Color DangerColor = GDI.Color.FromArgb(255, 244, 67, 54);

        public MainForm()
        {
            InitializeDirect2D(); // <-- AJOUTER ICI
            InitializeKeyNames();
            InitializeComponent();
        }

        private void InitializeDirect2D()
        {
            try
            {
                d2dFactory = new D2D1.Factory(FactoryType.MultiThreaded);
                Debug.WriteLine("✅ DirectX 11 activé");
            }
            catch { }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED - Active le double buffering pour TOUT
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCHITTEST:
                    base.WndProc(ref m);
                    Point pos = new Point(m.LParam.ToInt32());
                    pos = this.PointToClient(pos);
                    int resizeBorder = 8;
                    if (pos.X <= resizeBorder && pos.Y <= resizeBorder)
                        m.Result = new IntPtr(HTTOPLEFT);
                    else if (pos.X >= this.Width - resizeBorder && pos.Y <= resizeBorder)
                        m.Result = new IntPtr(HTTOPRIGHT);
                    else if (pos.X <= resizeBorder && pos.Y >= this.Height - resizeBorder)
                        m.Result = new IntPtr(HTBOTTOMLEFT);
                    else if (pos.X >= this.Width - resizeBorder && pos.Y >= this.Height - resizeBorder)
                        m.Result = new IntPtr(HTBOTTOMRIGHT);
                    else if (pos.X <= resizeBorder)
                        m.Result = new IntPtr(HTLEFT);
                    else if (pos.X >= this.Width - resizeBorder)
                        m.Result = new IntPtr(HTRIGHT);
                    else if (pos.Y <= resizeBorder)
                        m.Result = new IntPtr(HTTOP);
                    else if (pos.Y >= this.Height - resizeBorder)
                        m.Result = new IntPtr(HTBOTTOM);
                    // Barre de titre pour déplacement (exclut les boutons de contrôle)
                    // Cette partie est maintenant gérée par les événements MouseDown/Move/Up sur la titleBar
                    // else if (pos.Y <= 35 && pos.X < this.Width - 80)
                    //     m.Result = new IntPtr(HTCAPTION);
                    break;

                case WM_HOTKEY:
                    int hotkeyId = m.WParam.ToInt32();
                    HandleHotkey(hotkeyId);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void InitializeKeyNames()
        {
            KeyNames[Keys.A] = "A"; KeyNames[Keys.B] = "B"; KeyNames[Keys.C] = "C"; KeyNames[Keys.D] = "D";
            KeyNames[Keys.E] = "E"; KeyNames[Keys.F] = "F"; KeyNames[Keys.G] = "G"; KeyNames[Keys.H] = "H";
            KeyNames[Keys.I] = "I"; KeyNames[Keys.J] = "J"; KeyNames[Keys.K] = "K"; KeyNames[Keys.L] = "L";
            KeyNames[Keys.M] = "M"; KeyNames[Keys.N] = "N"; KeyNames[Keys.O] = "O"; KeyNames[Keys.P] = "P";
            KeyNames[Keys.Q] = "Q"; KeyNames[Keys.R] = "R"; KeyNames[Keys.S] = "S"; KeyNames[Keys.T] = "T";
            KeyNames[Keys.U] = "U"; KeyNames[Keys.V] = "V"; KeyNames[Keys.W] = "W"; KeyNames[Keys.X] = "X";
            KeyNames[Keys.Y] = "Y"; KeyNames[Keys.Z] = "Z";

            KeyNames[Keys.D0] = "0"; KeyNames[Keys.D1] = "1"; KeyNames[Keys.D2] = "2"; KeyNames[Keys.D3] = "3";
            KeyNames[Keys.D4] = "4"; KeyNames[Keys.D5] = "5"; KeyNames[Keys.D6] = "6"; KeyNames[Keys.D7] = "7";
            KeyNames[Keys.D8] = "8"; KeyNames[Keys.D9] = "9";

            KeyNames[Keys.F1] = "F1"; KeyNames[Keys.F2] = "F2"; KeyNames[Keys.F3] = "F3"; KeyNames[Keys.F4] = "F4";
            KeyNames[Keys.F5] = "F5"; KeyNames[Keys.F6] = "F6"; KeyNames[Keys.F7] = "F7"; KeyNames[Keys.F8] = "F8";
            KeyNames[Keys.F9] = "F9"; KeyNames[Keys.F10] = "F10"; KeyNames[Keys.F11] = "F11"; KeyNames[Keys.F12] = "F12";

            KeyNames[Keys.Space] = "Space"; KeyNames[Keys.Enter] = "Enter"; KeyNames[Keys.Tab] = "Tab";
            KeyNames[Keys.Back] = "Backspace"; KeyNames[Keys.Delete] = "Delete"; KeyNames[Keys.Insert] = "Insert";
            KeyNames[Keys.Home] = "Home"; KeyNames[Keys.End] = "End"; KeyNames[Keys.PageUp] = "PageUp";
            KeyNames[Keys.PageDown] = "PageDown"; KeyNames[Keys.Escape] = "Escape";

            KeyNames[Keys.NumPad0] = "Num0"; KeyNames[Keys.NumPad1] = "Num1"; KeyNames[Keys.NumPad2] = "Num2";
            KeyNames[Keys.NumPad3] = "Num3"; KeyNames[Keys.NumPad4] = "Num4"; KeyNames[Keys.NumPad5] = "Num5";
            KeyNames[Keys.NumPad6] = "Num6"; KeyNames[Keys.NumPad7] = "Num7"; KeyNames[Keys.NumPad8] = "Num8";
            KeyNames[Keys.NumPad9] = "Num9";
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // AJOUT - Optimisations de performance maximales
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true
            );
            this.DoubleBuffered = true;
            this.UpdateStyles();

            this.Text = "FTool by Tratox v1.2";
            this.Size = new Size(520, 730);
            this.MinimumSize = new Size(520, 650);
            this.BackColor = PrimaryBackground;
            this.ForeColor = TextPrimary;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            this.Icon = CreateModernIcon();
            this.KeyPreview = true;

            CreateCustomTitleBar();
            CreateMainInterface();
            windowCheckTimer = new System.Windows.Forms.Timer { Interval = 2000, Enabled = true };
            windowCheckTimer.Tick += CheckWindowsExist;
            statusUpdateTimer = new System.Windows.Forms.Timer { Interval = 500, Enabled = true };
            statusUpdateTimer.Tick += UpdateStatus;
            LoadSettings();
            this.FormClosing += MainForm_FormClosing;
            this.Resize += MainForm_Resize;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void CreateCustomTitleBar()
        {
            titleBar = new GradientPanel
            {
                Location = new Point(0, 0),
                Size = new Size(this.Width, 35),
                StartColor = SecondaryBackground,
                EndColor = CardBackground,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            titleBar.MouseDown += TitleBar_MouseDown;
            titleBar.MouseMove += TitleBar_MouseMove;
            titleBar.MouseUp += TitleBar_MouseUp;
            Label logoLabel = new Label
            {
                Text = "⚡",
                Font = new Font("Segoe UI Emoji", 16, FontStyle.Bold),
                ForeColor = AccentRed,
                Location = new Point(10, 5),
                Size = new Size(25, 25),
                BackColor = Color.Transparent
            };
            logoLabel.MouseDown += TitleBar_MouseDown;
            logoLabel.MouseMove += TitleBar_MouseMove;
            logoLabel.MouseUp += TitleBar_MouseUp;


            Label titleLabel = new Label
            {
                Text = "FTool by Tratox",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextPrimary,
                Location = new Point(40, 8),
                Size = new Size(150, 20),
                BackColor = Color.Transparent
            };
            titleLabel.MouseDown += TitleBar_MouseDown;
            titleLabel.MouseMove += TitleBar_MouseMove;
            titleLabel.MouseUp += TitleBar_MouseUp;

            Label versionLabel = new Label
            {
                Text = "v1.0",
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = AccentBlue,
                Location = new Point(190, 10),
                Size = new Size(30, 15),
                BackColor = Color.Transparent
            };
            versionLabel.MouseDown += TitleBar_MouseDown;
            versionLabel.MouseMove += TitleBar_MouseMove;
            versionLabel.MouseUp += TitleBar_MouseUp;
            minimizeBtn = new ModernControlButton
            {
                Text = "─",
                Size = new Size(30, 25),
                BackColor = Color.Transparent,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            minimizeBtn.Click += MinimizeBtn_Click;

            closeBtn = new ModernControlButton
            {
                Text = "✕",
                Size = new Size(30, 25),
                BackColor = Color.Transparent,
                ForeColor = AccentRed,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            closeBtn.Click += CloseBtn_Click;

            PositionControlButtons();

            titleBar.Controls.AddRange(new Control[] {
                logoLabel, titleLabel, versionLabel, minimizeBtn, closeBtn
            });

            closeBtn.BringToFront();
            minimizeBtn.BringToFront();

            this.Controls.Add(titleBar);
        }

        private void PositionControlButtons()
        {
            if (titleBar != null)
            {
                closeBtn.Location = new Point(titleBar.Width - 35, 5);
                minimizeBtn.Location = new Point(titleBar.Width - 70, 5);
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (minimizeBtn != null && closeBtn != null && titleBar != null)
            {
                titleBar.Width = this.Width;
                PositionControlButtons();

                closeBtn.BringToFront();
                minimizeBtn.BringToFront();
                titleBar.Invalidate();
            }
        }

        private void MinimizeBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void CloseBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                mouseLocation = e.Location;
            }
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                this.Location = new Point(
                    this.Location.X + (e.X - mouseLocation.X),
                    this.Location.Y + (e.Y - mouseLocation.Y));
            }
        }

        private void TitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void CreateMainInterface()
        {
            Panel headerPanel = new GradientPanel
            {
                Location = new Point(0, 35),
                Size = new Size(this.Width, 90),
                StartColor = SecondaryBackground,
                EndColor = PrimaryBackground,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            connectionStatusLabel = new AnimatedLabel
            {
                Text = "🔍 Scanning for Flyff processes...",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = WarningColor,
                Location = new Point(25, 15),
                Size = new Size(300, 20),
                BackColor = Color.Transparent
            };

            masterStartStopButton = new NeonButton
            {
                Text = "🚀 START ALL",
                Location = new Point(this.Width - 180, 15),
                Size = new Size(150, 35),
                BackColor = AccentGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                GlowColor = AccentGreen,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            masterStartStopButton.Click += MasterStartStop_Click;

            Label statsLabel = new Label
            {
                Text = "📊 Real-time Statistics",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = TextSecondary,
                Location = new Point(25, 50),
                Size = new Size(150, 15),
                BackColor = Color.Transparent
            };

            headerPanel.Controls.AddRange(new Control[] {
                connectionStatusLabel, masterStartStopButton, statsLabel
            });
            this.Controls.Add(headerPanel);
            tabControl = new GamingTabControl
            {
                Location = new Point(15, 140),
                Size = new Size(590, 630), 
                BackColor = PrimaryBackground,
                ForeColor = TextPrimary,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            this.Controls.Add(tabControl);

            for (int i = 1; i <= 4; i++)
            {
                TabPage tab = new TabPage($"💀 Spammer {i}")
                {
                    BackColor = PrimaryBackground,
                    ForeColor = TextPrimary,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    UseVisualStyleBackColor = false
                };

                GamingScrollablePanel scrollPanel = new GamingScrollablePanel
                {
                    Location = new Point(0, 0),
                    Size = new Size(583, 515),
                    BackColor = PrimaryBackground,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                    AutoScroll = true,
                    AutoScrollMinSize = new Size(0, 600),
                    ScrollbarColor = AccentRed,
                    ScrollbarBackColor = SecondaryBackground
                };

                tab.Controls.Add(scrollPanel);

                for (int j = 0; j < 5; j++)
                {
                    int globalIndex = (i - 1) * 5 + j;
                    CreateGamingSpammerCard(scrollPanel, globalIndex);
                }

                tabControl.TabPages.Add(tab);
            }
            CreateSettingsTab();
        }

        private void CreateSettingsTab()
        {
            TabPage settingsTab = new TabPage("⚙️ Settings")
            {
                BackColor = PrimaryBackground,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };

            GamingScrollablePanel settingsPanel = new GamingScrollablePanel
            {
                Location = new Point(0, 0),
                Size = new Size(483, 515),
                BackColor = PrimaryBackground,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                AutoScrollMinSize = new Size(0, 800),
                ScrollbarColor = AccentPurple,
                ScrollbarBackColor = SecondaryBackground
            };

            Label mainTitle = new Label
            {
                Text = "🎮 Global Hotkeys Configuration",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = AccentPurple,
                Location = new Point(20, 20),
                Size = new Size(400, 30),
                BackColor = Color.Transparent
            };
            settingsPanel.Controls.Add(mainTitle);

            Panel masterControlsPanel = new GlassCard
            {
                Location = new Point(10, 60),
                Size = new Size(450, 120),
                BackColor = CardBackground
            };

            Label masterTitle = new Label
            {
                Text = "🚀 Master Controls",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = TextPrimary,
                Location = new Point(15, 10),
                Size = new Size(200, 25),
                BackColor = Color.Transparent
            };

            Label masterStartLabel = new Label
            {
                Text = "Start All Spammers:",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = TextSecondary,
                Location = new Point(15, 45),
                Size = new Size(150, 20),
                BackColor = Color.Transparent
            };

            Button masterStartHotkeyBtn = new HotkeyButton
            {
                Text = string.IsNullOrEmpty(settings.MasterStartHotKey) ? "Click to set hotkey" : settings.MasterStartHotKey,
                Location = new Point(170, 42),
                Size = new Size(150, 25),
                Tag = "MasterStart"
            };
            masterStartHotkeyBtn.Click += HotkeyButton_Click;

            Button masterStartClearBtn = new ModernMiniButton
            {
                Text = "✕",
                Location = new Point(330, 42),
                Size = new Size(25, 25),
                BackColor = AccentRed,
                Tag = "MasterStart"
            };
            masterStartClearBtn.Click += ClearHotkey_Click;

            Label masterStopLabel = new Label
            {
                Text = "Stop All Spammers:",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = TextSecondary,
                Location = new Point(15, 80),
                Size = new Size(150, 20),
                BackColor = Color.Transparent
            };

            Button masterStopHotkeyBtn = new HotkeyButton
            {
                Text = string.IsNullOrEmpty(settings.MasterStopHotKey) ? "Click to set hotkey" : settings.MasterStopHotKey,
                Location = new Point(170, 77),
                Size = new Size(150, 25),
                Tag = "MasterStop"
            };
            masterStopHotkeyBtn.Click += HotkeyButton_Click;

            Button masterStopClearBtn = new ModernMiniButton
            {
                Text = "✕",
                Location = new Point(330, 77),
                Size = new Size(25, 25),
                BackColor = AccentRed,
                Tag = "MasterStop"
            };
            masterStopClearBtn.Click += ClearHotkey_Click;

            masterControlsPanel.Controls.AddRange(new Control[] {
            masterTitle, masterStartLabel, masterStartHotkeyBtn, masterStartClearBtn,
            masterStopLabel, masterStopHotkeyBtn, masterStopClearBtn
            });
            settingsPanel.Controls.Add(masterControlsPanel);

            Label spammersTitle = new Label
            {
                Text = "⚔️ Individual Spammer Hotkeys",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = AccentRed,
                Location = new Point(20, 200),
                Size = new Size(300, 25),
                BackColor = Color.Transparent
            };
            settingsPanel.Controls.Add(spammersTitle);

            for (int i = 0; i < 20; i++)
            {
                int row = i / 2;
                int col = i % 2;
                int yPos = 240 + (row * 90);
                int xPos = 10 + (col * 225);

                Panel spammerPanel = new GlassCard
                {
                    Location = new Point(xPos, yPos),
                    Size = new Size(215, 70),
                    BackColor = CardBackground
                };

                Label spammerLabel = new Label
                {
                    Text = $"Spammer #{i + 1:00}",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = TextPrimary,
                    Location = new Point(10, 5),
                    Size = new Size(100, 20),
                    BackColor = Color.Transparent
                };

                string currentHotkey = settings.SpammerHotKeys.ContainsKey(i) ? settings.SpammerHotKeys[i] : "";
                Button spammerHotkeyBtn = new HotkeyButton
                {
                    Text = string.IsNullOrEmpty(currentHotkey) ? "No hotkey" : currentHotkey,
                    Location = new Point(10, 30),
                    Size = new Size(120, 25),
                    Font = new Font("Segoe UI", 8, FontStyle.Regular),
                    Tag = $"Spammer_{i}"
                };
                spammerHotkeyBtn.Click += HotkeyButton_Click;

                Button spammerClearBtn = new ModernMiniButton
                {
                    Text = "✕",
                    Location = new Point(140, 30),
                    Size = new Size(25, 25),
                    BackColor = AccentRed,
                    Tag = $"Spammer_{i}"
                };
                spammerClearBtn.Click += ClearHotkey_Click;

                spammerPanel.Controls.AddRange(new Control[] {
                spammerLabel, spammerHotkeyBtn, spammerClearBtn
                });
                settingsPanel.Controls.Add(spammerPanel);
            }

            Panel optionsPanel = new GlassCard
            {
                Location = new Point(10, 1525),
                Size = new Size(450, 100),
                BackColor = CardBackground
            };

            Label optionsTitle = new Label
            {
                Text = "🔧 Options",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = AccentBlue,
                Location = new Point(15, 10),
                Size = new Size(200, 25),
                BackColor = Color.Transparent
            };

            CheckBox enableHotkeysCheckBox = new ModernCheckBox
            {
                Text = "Enable Global Hotkeys",
                Checked = settings.EnableGlobalHotKeys,
                Location = new Point(15, 45),
                Size = new Size(200, 20),
                ForeColor = TextPrimary
            };
            enableHotkeysCheckBox.CheckedChanged += (s, e) => {
                settings.EnableGlobalHotKeys = enableHotkeysCheckBox.Checked;
                if (settings.EnableGlobalHotKeys)
                    RegisterAllHotKeys();
                else
                    UnregisterAllHotKeys();
                SaveSettings();
            };

            optionsPanel.Controls.AddRange(new Control[] {
            optionsTitle, enableHotkeysCheckBox
            });
            settingsPanel.Controls.Add(optionsPanel);

            settingsPanel.AutoScrollMinSize = new Size(0, 1200);

            settingsTab.Controls.Add(settingsPanel);
            tabControl.TabPages.Add(settingsTab);
        }
        private void HotkeyButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            string tag = btn.Tag.ToString();

            HotkeyCapture captureForm = new HotkeyCapture();
            if (captureForm.ShowDialog() == DialogResult.OK)
            {
                string hotkeyString = captureForm.CapturedHotkey;
                btn.Text = hotkeyString;
                if (tag == "MasterStart")
                {
                    settings.MasterStartHotKey = hotkeyString;
                }
                else if (tag == "MasterStop")
                {
                    settings.MasterStopHotKey = hotkeyString;
                }
                else if (tag.StartsWith("Spammer_"))
                {
                    int spammerIndex = int.Parse(tag.Substring(8));
                    settings.SpammerHotKeys[spammerIndex] = hotkeyString;
                    if (spammerIndex < spammers.Count)
                    {
                        spammers[spammerIndex].HotKey = hotkeyString;
                    }
                }

                RegisterAllHotKeys();
                SaveSettings();
            }
            captureForm.Dispose();
        }

        private void ClearHotkey_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            string tag = btn.Tag.ToString();

            if (tag == "MasterStart")
            {
                settings.MasterStartHotKey = "";
                UpdateHotkeyButtonText("MasterStart", "Click to set hotkey");
            }
            else if (tag == "MasterStop")
            {
                settings.MasterStopHotKey = "";
                UpdateHotkeyButtonText("MasterStop", "Click to set hotkey");
            }
            else if (tag.StartsWith("Spammer_"))
            {
                int spammerIndex = int.Parse(tag.Substring(8));

                if (settings.SpammerHotKeys.ContainsKey(spammerIndex))
                {
                    settings.SpammerHotKeys.Remove(spammerIndex);
                }

                if (spammerIndex < spammers.Count)
                {
                    spammers[spammerIndex].HotKey = "";
                }

                UpdateHotkeyButtonText(tag, "No hotkey");
            }

            RegisterAllHotKeys();
            SaveSettings();
        }
        private void UpdateHotkeyButtonText(string targetTag, string newText)
        {
            foreach (Control control in GetAllControls(tabControl))
            {
                if (control is Button hotkeyBtn && hotkeyBtn.Tag?.ToString() == targetTag)
                {
                    hotkeyBtn.Text = newText;
                    break;
                }
            }
        }

        private IEnumerable<Control> GetAllControls(Control container)
        {
            var controls = new List<Control>();
            foreach (Control control in container.Controls)
            {
                controls.Add(control);
                controls.AddRange(GetAllControls(control));
            }
            return controls;
        }

        private void RegisterAllHotKeys()
        {
            UnregisterAllHotKeys();

            if (!settings.EnableGlobalHotKeys) return;

            int hotkeyId = 1;
            if (!string.IsNullOrEmpty(settings.MasterStartHotKey))
            {
                var (modifiers, key) = ParseHotkey(settings.MasterStartHotKey);
                if (key != Keys.None)
                {
                    RegisterHotKey(this.Handle, hotkeyId, modifiers, (uint)key);
                    registeredHotKeys[hotkeyId] = key;
                    hotkeyId++;
                }
            }
            if (!string.IsNullOrEmpty(settings.MasterStopHotKey))
            {
                var (modifiers, key) = ParseHotkey(settings.MasterStopHotKey);
                if (key != Keys.None)
                {
                    RegisterHotKey(this.Handle, hotkeyId, modifiers, (uint)key);
                    registeredHotKeys[hotkeyId] = key;
                    hotkeyId++;
                }
            }
            for (int i = 0; i < spammers.Count; i++)
            {
                string hotkey = settings.SpammerHotKeys.ContainsKey(i) ? settings.SpammerHotKeys[i] : "";
                if (!string.IsNullOrEmpty(hotkey))
                {
                    var (modifiers, key) = ParseHotkey(hotkey);
                    if (key != Keys.None)
                    {
                        RegisterHotKey(this.Handle, hotkeyId, modifiers, (uint)key);
                        registeredHotKeys[hotkeyId] = key;
                        spammers[i].HotKeyId = hotkeyId;
                        hotkeyId++;
                    }
                }
            }
        }
        private void UnregisterAllHotKeys()
        {
            foreach (var kvp in registeredHotKeys)
            {
                UnregisterHotKey(this.Handle, kvp.Key);
            }
            registeredHotKeys.Clear();
            foreach (var spammer in spammers)
            {
                spammer.HotKeyId = -1;
            }
        }
        private (uint modifiers, Keys key) ParseHotkey(string hotkeyString)
        {
            if (string.IsNullOrEmpty(hotkeyString)) return (0, Keys.None);

            uint modifiers = 0;
            Keys key = Keys.None;

            string[] parts = hotkeyString.Split('+');

            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                switch (trimmedPart.ToUpper())
                {
                    case "CTRL":
                        modifiers |= MOD_CONTROL;
                        break;
                    case "ALT":
                        modifiers |= MOD_ALT;
                        break;
                    case "SHIFT":
                        modifiers |= MOD_SHIFT;
                        break;
                    case "WIN":
                        modifiers |= MOD_WIN;
                        break;
                    default:
                        Keys parsedKey;
                        if (Enum.TryParse(trimmedPart, true, out parsedKey))
                        {
                            key = parsedKey;
                        }
                        break;
                }
            }

            return (modifiers, key);
        }
        private void HandleHotkey(int hotkeyId)
        {
            if (!registeredHotKeys.ContainsKey(hotkeyId)) return;

            if (hotkeyId == 1 && !string.IsNullOrEmpty(settings.MasterStartHotKey))
            {
                MasterStartStop_Click(null, null);
                return;
            }

            if (hotkeyId == 2 && !string.IsNullOrEmpty(settings.MasterStopHotKey))
            {
                MasterStartStop_Click(null, null);
                return;
            }

            for (int i = 0; i < spammers.Count; i++)
            {
                if (spammers[i].HotKeyId == hotkeyId)
                {
                    StartButton_Click(spammers[i].StartButton, null);
                    break;
                }
            }
        }
        private void CreateGamingSpammerCard(Panel parent, int index)
        {
            int localIndex = index % 5;
            int yPos = localIndex * 110 + 10;

            SpammerData spammer = new SpammerData();

            Panel card = new GlassCard
            {
                Location = new Point(5, yPos),
                Size = new Size(450, 100),
                BackColor = CardBackground
            };
            spammer.Container = card;
            parent.Controls.Add(card);

            Panel cardHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(450, 25),
                BackColor = GDI.Color.FromArgb(50, AccentRed.R, AccentRed.G, AccentRed.B)
            };

            Label cardTitle = new Label
            {
                Text = $"⚔️ Spammer #{index + 1:00}",
                Location = new Point(10, 4),
                Size = new Size(120, 16),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            Panel statusIndicator = new PulsingIndicator
            {
                Location = new Point(420, 6),
                Size = new Size(10, 10),
                BackColor = BorderColor
            };
            spammer.StatusIndicator = statusIndicator;

            Label statusLabel = new Label
            {
                Text = "IDLE",
                Location = new Point(360, 5),
                Size = new Size(55, 14),
                ForeColor = TextMuted,
                Font = new Font("Consolas", 7, FontStyle.Bold),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };
            spammer.StatusLabel = statusLabel;

            cardHeader.Controls.AddRange(new Control[] { cardTitle, statusLabel, statusIndicator });
            card.Controls.Add(cardHeader);

            Label actionsLabel = new Label
            {
                Text = "Actions: 0",
                Location = new Point(10, 80),
                Size = new Size(80, 12),
                ForeColor = AccentBlue,
                Font = new Font("Consolas", 7, FontStyle.Regular),
                BackColor = Color.Transparent
            };
            spammer.ActionsLabel = actionsLabel;
            card.Controls.Add(actionsLabel);

            Label hotkeyLabel = new Label
            {
                Text = "No hotkey",
                Location = new Point(100, 80),
                Size = new Size(80, 12),
                ForeColor = AccentPurple,
                Font = new Font("Consolas", 7, FontStyle.Regular),
                BackColor = Color.Transparent
            };
            card.Controls.Add(hotkeyLabel);

            Button startButton = new GamingButton
            {
                Text = "▶️ START",
                Location = new Point(10, 35),
                Size = new Size(75, 40),
                BackColor = AccentGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Tag = index
            };
            startButton.Click += StartButton_Click;
            spammer.StartButton = startButton;
            card.Controls.Add(startButton);

            CreateCompactLabel(card, "WINDOW", 95, 26, new Font("Segoe UI", 10, FontStyle.Bold));
            ComboBox windowCombo = new GamingComboBox
            {
                Location = new Point(95, 47),
                Size = new Size(130, 25),
                BackColor = SecondaryBackground,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8)
            };
            windowCombo.Items.Add("Select Window");
            windowCombo.SelectedIndex = 0;
            windowCombo.DropDown += WindowCombo_DropDown;
            windowCombo.SelectedIndexChanged += (s, e) => SaveSettings();
            spammer.WindowCombo = windowCombo;
            card.Controls.Add(windowCombo);

            CreateCompactLabel(card, "Sec", 235, 26, new Font("Segoe UI", 10, FontStyle.Bold));
            TextBox intervalText = new GamingTextBox
            {
                Text = "0",
                Location = new Point(235, 47),
                Size = new Size(45, 25),
                BackColor = SecondaryBackground,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8)
            };
            intervalText.KeyPress += IntervalText_KeyPress;
            intervalText.Leave += IntervalText_Leave;
            intervalText.TextChanged += (s, e) => SaveSettings();
            spammer.IntervalText = intervalText;
            card.Controls.Add(intervalText);

            CreateCompactLabel(card, "F-KEY", 290, 26, new Font("Segoe UI", 9, FontStyle.Bold));
            ComboBox fkeyCombo = new GamingComboBox
            {
                Location = new Point(290, 45),
                Size = new Size(45, 22),
                BackColor = SecondaryBackground,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8)
            };
            string[] fkeys = { " ", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9" };
            fkeyCombo.Items.AddRange(fkeys);
            fkeyCombo.SelectedIndex = 0;
            fkeyCombo.SelectedIndexChanged += (s, e) => SaveSettings();
            spammer.FKeyCombo = fkeyCombo;
            card.Controls.Add(fkeyCombo);

            CreateCompactLabel(card, "SKILL", 355, 26, new Font("Segoe UI", 9, FontStyle.Bold));
            ComboBox skillCombo = new GamingComboBox
            {
                Location = new Point(355, 45),
                Size = new Size(55, 22),
                BackColor = SecondaryBackground,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8)
            };
            string[] skills = { " ", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            skillCombo.Items.AddRange(skills);
            skillCombo.SelectedIndex = 0;
            skillCombo.SelectedIndexChanged += (s, e) => SaveSettings();
            spammer.SkillCombo = skillCombo;
            card.Controls.Add(skillCombo);

            Button resetButton = new ModernMiniButton
            {
                Text = "🔄",
                Location = new Point(420, 45),
                Size = new Size(25, 22),
                BackColor = AccentPurple,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Tag = index
            };
            resetButton.Click += (s, e) => ResetSpammer((int)((Button)s).Tag);
            card.Controls.Add(resetButton);

            spammers.Add(spammer);
            UpdateSpammerHotkeyDisplay(index, hotkeyLabel);
        }

        private void UpdateSpammerHotkeyDisplay(int index, Label hotkeyLabel)
        {
            if (settings.SpammerHotKeys.ContainsKey(index))
            {
                hotkeyLabel.Text = settings.SpammerHotKeys[index];
                hotkeyLabel.ForeColor = AccentGreen;
            }
            else
            {
                hotkeyLabel.Text = "No hotkey";
                hotkeyLabel.ForeColor = TextMuted;
            }
        }

        private void CreateCompactLabel(Panel parent, string text, int x, int y, Font customFont = null)
        {
            Label label = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = TextSecondary,
                Font = customFont ?? new Font("Segoe UI", 6, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            parent.Controls.Add(label);
        }

        private void IntervalText_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void IntervalText_Leave(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (!int.TryParse(textBox.Text, out int value) || value < 0)
            {
                textBox.Text = "0";
            }
            SaveSettings();
        }
        private void ResetSpammer(int index)
        {
            if (spammers[index].IsRunning)
                StopSpammer(index);

            spammers[index].WindowCombo.SelectedIndex = 0;
            spammers[index].IntervalText.Text = "0";
            spammers[index].FKeyCombo.SelectedIndex = 0;
            spammers[index].SkillCombo.SelectedIndex = 0;
            spammers[index].ActionCount = 0;
            spammers[index].ActionsLabel.Text = "Actions: 0";
            SaveSettings();
        }
        private void MasterStartStop_Click(object sender, EventArgs e)
        {
            bool anyRunning = spammers.Any(s => s.IsRunning);

            if (anyRunning)
            {
                for (int i = 0; i < spammers.Count; i++)
                {
                    if (spammers[i].IsRunning)
                        StopSpammer(i);
                }
                masterStartStopButton.Text = "🚀 START ALL";
                masterStartStopButton.BackColor = AccentGreen;
            }
            else
            {
                int started = 0;
                for (int i = 0; i < spammers.Count; i++)
                {
                    if (!spammers[i].IsRunning && IsValidConfiguration(i))
                    {
                        StartSpammer(i);
                        started++;
                    }
                }

                if (started > 0)
                {
                    masterStartStopButton.Text = "⏹️ STOP ALL";
                    masterStartStopButton.BackColor = AccentRed;
                }
                else
                {
                    MessageBox.Show("No spammer configured correctly.", "FTool by Tratox",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private bool IsValidConfiguration(int index)
        {
            var spammer = spammers[index];
            string window = spammer.WindowCombo.SelectedItem?.ToString();
            string fkey = spammer.FKeyCombo.SelectedItem?.ToString();
            string skill = spammer.SkillCombo.SelectedItem?.ToString();

            return window != "Select Window" && !string.IsNullOrEmpty(window) &&
                   ((fkey != " " && !string.IsNullOrEmpty(fkey)) || (skill != " " && !string.IsNullOrEmpty(skill)));
        }
        private void UpdateStatus(object sender, EventArgs e)
        {
            var processes = Process.GetProcessesByName("Neuz");
            if (processes.Length > 0)
            {
                connectionStatusLabel.Text = $"✅ {processes.Length} Flyff process(es) detected";
                connectionStatusLabel.ForeColor = AccentGreen;
            }
            else
            {
                connectionStatusLabel.Text = "❌ No Flyff process found";
                connectionStatusLabel.ForeColor = AccentRed;
            }

            for (int i = 0; i < spammers.Count; i++)
            {
                var spammer = spammers[i];
                if (spammer.IsRunning)
                {
                    spammer.StatusLabel.Text = "ACTIVE";
                    spammer.StatusLabel.ForeColor = AccentGreen;
                    spammer.ActionsLabel.Text = $"Actions: {spammer.ActionCount}";
                }
                else
                {
                    spammer.StatusLabel.Text = "IDLE";
                    spammer.StatusLabel.ForeColor = TextMuted;
                }
            }

            bool anyRunning = spammers.Any(s => s.IsRunning);
            masterStartStopButton.Text = anyRunning ? "⏹️ STOP ALL" : "🚀 START ALL";
            masterStartStopButton.BackColor = anyRunning ? AccentRed : AccentGreen;
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            int index = (int)button.Tag;
            SpammerData spammer = spammers[index];

            if (!spammer.IsRunning)
            {
                StartSpammer(index);
            }
            else
            {
                StopSpammer(index);
            }
        }
        private void StartSpammer(int index)
        {
            SpammerData spammer = spammers[index];

            string window = spammer.WindowCombo.SelectedItem?.ToString();
            string intervalText = spammer.IntervalText.Text;
            string fkey = spammer.FKeyCombo.SelectedItem?.ToString();
            string skill = spammer.SkillCombo.SelectedItem?.ToString();

            if (window == "Select Window" || string.IsNullOrEmpty(window))
            {
                MessageBox.Show("Select a Flyff window.", "FTool by Tratox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var processes = Process.GetProcessesByName("Neuz");
            IntPtr windowHandle = IntPtr.Zero;
            bool windowFound = false;

            foreach (var proc in processes)
            {
                try
                {
                    if (proc.MainWindowTitle == window)
                    {
                        windowHandle = proc.MainWindowHandle;
                        windowFound = true;
                        break;
                    }
                }
                catch {}
            }

            if (!windowFound)
            {
                MessageBox.Show("Selected Flyff window doesn't exist.", window, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if ((fkey == " " || string.IsNullOrEmpty(fkey)) && (skill == " " || string.IsNullOrEmpty(skill)))
            {
                MessageBox.Show("Select at least one F-Key or Skill Bar.", "FTool by Tratox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int intervalValue = 0;
            if (!string.IsNullOrEmpty(intervalText))
            {
                if (!int.TryParse(intervalText, out intervalValue) || intervalValue < 0)
                {
                    intervalValue = 0;
                    spammer.IntervalText.Text = "0";
                }
            }

            try
            {
                spammer.TargetWindowHandle = windowHandle;
                spammer.WindowTitle = window;
                spammer.IsRunning = true;
                spammer.LastActionTime = DateTime.Now;
                spammer.ActionCount = 0;

                SendSpamToFlyff(spammer.TargetWindowHandle, fkey, skill);
                spammer.ActionCount++;

                int timerInterval = intervalValue == 0 ? 100 : intervalValue * 1000;

                spammer.SpamTimer = new System.Windows.Forms.Timer();
                spammer.SpamTimer.Interval = timerInterval;
                spammer.SpamTimer.Tag = new { Index = index, FKey = fkey, Skill = skill };
                spammer.SpamTimer.Tick += SpamTimer_Tick;
                spammer.SpamTimer.Start();

                spammer.StartButton.Text = "⏹️ STOP";
                spammer.StartButton.BackColor = AccentRed;
                spammer.StatusIndicator.BackColor = AccentGreen;
                spammer.WindowCombo.Enabled = false;
                spammer.IntervalText.Enabled = false;
                spammer.FKeyCombo.Enabled = false;
                spammer.SkillCombo.Enabled = false;

                Debug.WriteLine($"Spammer {index} started - Window: {window}, FKey: {fkey}, Skill: {skill}, Interval: {intervalValue}s");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting spammer: {ex.Message}", "FTool by Tratox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                spammer.IsRunning = false;
            }
        }

        private void SpamTimer_Tick(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer timer = sender as System.Windows.Forms.Timer;
            dynamic data = timer.Tag;
            int index = data.Index;
            string fkey = data.FKey;
            string skill = data.Skill;

            SpammerData spammer = spammers[index];

            try
            {
                if (!IsWindow(spammer.TargetWindowHandle))
                {
                    Debug.WriteLine($"Window closed for spammer {index}");
                    StopSpammer(index);
                    return;
                }

                SendSpamToFlyff(spammer.TargetWindowHandle, fkey, skill);
                spammer.ActionCount++;
                spammer.LastActionTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR in spammer {index} timer: {ex.Message}");
                StopSpammer(index);
            }
        }

        private void SendSpamToFlyff(IntPtr windowHandle, string fkey, string skill)
        {
            bool hasFKey = (!string.IsNullOrEmpty(fkey) && fkey != " ");
            bool hasSkill = (!string.IsNullOrEmpty(skill) && skill != " ");

            try
            {
                if (!hasFKey && !hasSkill) return;

                if (hasSkill && VirtualKeys.TryGetValue(skill, out uint skillKeyCode))
                {
                    SendKeyToFlyff(windowHandle, skillKeyCode);
                    Debug.WriteLine($"Skill sent: {skill} (VK: {skillKeyCode})");

                    if (hasFKey)
                    {
                        Thread.Sleep(150);
                    }
                }

                if (hasFKey && VirtualKeys.TryGetValue(fkey, out uint fkeyCode))
                {
                    SendKeyToFlyff(windowHandle, fkeyCode);
                    Debug.WriteLine($"F-Key sent: {fkey} (VK: {fkeyCode})");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending spam: {ex.Message}");
            }
        }

        private void SendKeyToFlyff(IntPtr windowHandle, uint virtualKeyCode)
        {
            try
            {
                bool success = PostMessage(windowHandle, WM_KEYDOWN, new IntPtr(virtualKeyCode), IntPtr.Zero);
                if (!success)
                {
                    SendMessage(windowHandle, WM_KEYDOWN, new IntPtr(virtualKeyCode), IntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending key to Flyff: {ex.Message}");
            }
        }

        private void StopSpammer(int index)
        {
            SpammerData spammer = spammers[index];

            try
            {
                spammer.SpamTimer?.Stop();
                spammer.SpamTimer?.Dispose();
                spammer.SpamTimer = null;
                spammer.IsRunning = false;
            }
            catch {}

            spammer.TargetWindowHandle = IntPtr.Zero;
            spammer.WindowTitle = "";

            spammer.StartButton.Text = "▶️ START";
            spammer.StartButton.BackColor = AccentGreen;
            spammer.StatusIndicator.BackColor = BorderColor;
            spammer.WindowCombo.Enabled = true;
            spammer.IntervalText.Enabled = true;
            spammer.FKeyCombo.Enabled = true;
            spammer.SkillCombo.Enabled = true;

            Debug.WriteLine($"Spammer {index} stopped - Actions performed: {spammer.ActionCount}");
        }

        private void WindowCombo_DropDown(object sender, EventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            GetNeuzWindows(combo);
        }

        private void GetNeuzWindows(ComboBox combo)
        {
            string currentSelection = combo.Text;
            combo.Items.Clear();
            combo.Items.Add("Select Window");

            var processes = Process.GetProcessesByName("Neuz");
            var windowTitles = new HashSet<string>();

            foreach (var proc in processes)
            {
                try
                {
                    if (!string.IsNullOrEmpty(proc.MainWindowTitle) &&
                        !windowTitles.Contains(proc.MainWindowTitle))
                    {
                        combo.Items.Add(proc.MainWindowTitle);
                        windowTitles.Add(proc.MainWindowTitle);
                    }
                }
                catch {}
            }
            int selectedIndex = combo.FindStringExact(currentSelection);
            if (selectedIndex != -1)
            {
                combo.SelectedIndex = selectedIndex;
            }
            else
            {
                combo.SelectedIndex = 0;
            }
        }

        private void CheckWindowsExist(object sender, EventArgs e)
        {
            var processes = Process.GetProcessesByName("Neuz");
            var activeWindows = new HashSet<string>(processes
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                .Select(p => p.MainWindowTitle));

            for (int i = 0; i < spammers.Count; i++)
            {
                SpammerData spammer = spammers[i];
                if (!string.IsNullOrEmpty(spammer.WindowTitle) && spammer.IsRunning)
                {
                    if (!activeWindows.Contains(spammer.WindowTitle))
                    {
                        Debug.WriteLine($"Window {spammer.WindowTitle} no longer exists, stopping spammer {i}");
                        StopSpammer(i);
                    }
                }
            }
        }
        private void LoadSettings()
        {
            if (!File.Exists(settingsFile)) return;

            try
            {
                var lines = File.ReadAllLines(settingsFile);
                var settingsDict = new Dictionary<string, Dictionary<string, string>>();

                string currentSection = "";
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";")) continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        settingsDict[currentSection] = new Dictionary<string, string>();
                    }
                    else if (trimmedLine.Contains("=") && !string.IsNullOrEmpty(currentSection))
                    {
                        var parts = trimmedLine.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            settingsDict[currentSection][parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }

                if (settingsDict.ContainsKey("General"))
                {
                    var generalSettings = settingsDict["General"];

                    if (generalSettings.ContainsKey("MasterStartHotKey"))
                        settings.MasterStartHotKey = generalSettings["MasterStartHotKey"];

                    if (generalSettings.ContainsKey("MasterStopHotKey"))
                        settings.MasterStopHotKey = generalSettings["MasterStopHotKey"];

                    if (generalSettings.ContainsKey("EnableGlobalHotKeys"))
                    {
                        bool enableGlobalHotKeys;
                        if (bool.TryParse(generalSettings["EnableGlobalHotKeys"], out enableGlobalHotKeys))
                            settings.EnableGlobalHotKeys = enableGlobalHotKeys;
                    }
                }

                if (settingsDict.ContainsKey("HotKeys"))
                {
                    var hotkeySettings = settingsDict["HotKeys"];
                    foreach (var kvp in hotkeySettings)
                    {
                        if (kvp.Key.StartsWith("Spammer") && int.TryParse(kvp.Key.Substring(7), out int spammerIndex))
                        {
                            if (!string.IsNullOrEmpty(kvp.Value))
                            {
                                settings.SpammerHotKeys[spammerIndex - 1] = kvp.Value; // -1 car les index commencent à 0
                            }
                        }
                    }
                }

                for (int i = 0; i < spammers.Count; i++)
                {
                    string section = $"Spammer{i + 1}";
                    if (settingsDict.ContainsKey(section))
                    {
                        var spammer = spammers[i];
                        var sectionData = settingsDict[section];

                        if (sectionData.ContainsKey("WindowTitle"))
                        {
                            string windowTitle = sectionData["WindowTitle"];
                            if (!string.IsNullOrEmpty(windowTitle))
                            {
                                if (!spammer.WindowCombo.Items.Contains(windowTitle))
                                {
                                    spammer.WindowCombo.Items.Add(windowTitle);
                                }
                                spammer.WindowCombo.Text = windowTitle;
                            }
                            else
                            {
                                spammer.WindowCombo.SelectedIndex = 0;
                            }
                        }

                        if (sectionData.ContainsKey("Interval"))
                        {
                            string interval = sectionData["Interval"];
                            spammer.IntervalText.Text = string.IsNullOrEmpty(interval) ? "0" : interval;
                        }

                        if (sectionData.ContainsKey("FKey"))
                        {
                            string fkey = sectionData["FKey"];
                            if (!string.IsNullOrEmpty(fkey) && spammer.FKeyCombo.Items.Contains(fkey))
                            {
                                spammer.FKeyCombo.SelectedItem = fkey;
                            }
                            else
                            {
                                spammer.FKeyCombo.SelectedIndex = 0;
                            }
                        }

                        if (sectionData.ContainsKey("Skill"))
                        {
                            string skill = sectionData["Skill"];
                            if (!string.IsNullOrEmpty(skill) && spammer.SkillCombo.Items.Contains(skill))
                            {
                                spammer.SkillCombo.SelectedItem = skill;
                            }
                            else
                            {
                                spammer.SkillCombo.SelectedIndex = 0;
                            }
                        }
                        if (settings.SpammerHotKeys.ContainsKey(i))
                        {
                            spammer.HotKey = settings.SpammerHotKeys[i];
                        }
                    }
                }
                if (tabControl != null && tabControl.TabPages.Count > 0)
                {
                    foreach (Control control in GetAllControls(tabControl))
                    {
                        if (control is Button hotkeyBtn && hotkeyBtn.Tag != null)
                        {
                            string tag = hotkeyBtn.Tag.ToString();
                            if (hotkeyBtn is ModernMiniButton) continue;

                            if (tag == "MasterStart")
                            {
                                hotkeyBtn.Text = string.IsNullOrEmpty(settings.MasterStartHotKey) ?
                                    "Click to set hotkey" : settings.MasterStartHotKey;
                            }
                            else if (tag == "MasterStop")
                            {
                                hotkeyBtn.Text = string.IsNullOrEmpty(settings.MasterStopHotKey) ?
                                    "Click to set hotkey" : settings.MasterStopHotKey;
                            }
                            else if (tag.StartsWith("Spammer_"))
                            {
                                int spammerIndex = int.Parse(tag.Substring(8));
                                string hotkey = settings.SpammerHotKeys.ContainsKey(spammerIndex) ?
                                    settings.SpammerHotKeys[spammerIndex] : "";
                                hotkeyBtn.Text = string.IsNullOrEmpty(hotkey) ? "No hotkey" : hotkey;
                            }
                        }
                    }
                }
                if (settings.EnableGlobalHotKeys)
                {
                    RegisterAllHotKeys();
                }

                Debug.WriteLine("Settings loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(settingsFile))
                {
                    writer.WriteLine("; FTool by Tratox - Opera GX Edition");
                    writer.WriteLine("; Auto-generated on " + DateTime.Now);
                    writer.WriteLine();
                    writer.WriteLine("[General]");
                    writer.WriteLine($"MasterStartHotKey={settings.MasterStartHotKey}");
                    writer.WriteLine($"MasterStopHotKey={settings.MasterStopHotKey}");
                    writer.WriteLine($"EnableGlobalHotKeys={settings.EnableGlobalHotKeys}");
                    writer.WriteLine();
                    writer.WriteLine("[HotKeys]");
                    foreach (var kvp in settings.SpammerHotKeys)
                    {
                        writer.WriteLine($"Spammer{kvp.Key + 1}={kvp.Value}");
                    }
                    writer.WriteLine();
                    for (int i = 0; i < spammers.Count; i++)
                    {
                        var spammer = spammers[i];
                        writer.WriteLine($"[Spammer{i + 1}]");

                        string windowTitle = spammer.WindowCombo.Text;
                        if (windowTitle == "Select Window") windowTitle = "";
                        writer.WriteLine($"WindowTitle={windowTitle}");

                        writer.WriteLine($"Interval={spammer.IntervalText.Text}");

                        string fkey = spammer.FKeyCombo.Text;
                        if (fkey == " ") fkey = "";
                        writer.WriteLine($"FKey={fkey}");

                        string skill = spammer.SkillCombo.Text;
                        if (skill == " ") skill = "";
                        writer.WriteLine($"Skill={skill}");

                        writer.WriteLine();
                    }
                }

                Debug.WriteLine("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < spammers.Count; i++)
            {
                if (spammers[i].IsRunning)
                {
                    StopSpammer(i);
                }
            }
            UnregisterAllHotKeys();
            SaveSettings();
            windowCheckTimer?.Stop();
            windowCheckTimer?.Dispose();
            statusUpdateTimer?.Stop();
            statusUpdateTimer?.Dispose();

            Debug.WriteLine("FTool by Tratox closed properly");

            // Cleanup DirectX
            renderTarget?.Dispose();
            d2dFactory?.Dispose();
            foreach (var brush in brushCache.Values) brush?.Dispose();
        }

        #region Modern Gaming UI Controls

        public class GradientPanel : Panel
        {
            public Color StartColor { get; set; }
            public Color EndColor { get; set; }

            protected override void OnPaint(PaintEventArgs e)
            {
                using (System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    this.ClientRectangle, StartColor, EndColor, LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }
            }
        }

        public class GamingScrollablePanel : Panel
        {
            public Color ScrollbarColor { get; set; } = Color.Red;
            public Color ScrollbarBackColor { get; set; } = Color.Gray;
            private bool showCustomScrollbar = false;

            public GamingScrollablePanel()
            {
                this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                    true
                 );
                this.DoubleBuffered = true;
                this.AutoScroll = true;
                this.SetStyle(ControlStyles.UserPaint, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.SetStyle(ControlStyles.DoubleBuffer, true);
                this.SetStyle(ControlStyles.ResizeRedraw, true);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                if (this.AutoScrollMinSize.Height > this.ClientSize.Height || this.VerticalScroll.Visible)
                {
                    showCustomScrollbar = true;
                    DrawCustomScrollbar(e.Graphics);
                }
            }
            private void DrawCustomScrollbar(Graphics g)
            {
                if (!showCustomScrollbar) return;

                Rectangle scrollRect = new Rectangle(this.Width - 15, 0, 15, this.Height);

                using (System.Drawing.Drawing2D.LinearGradientBrush bgBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    scrollRect, GDI.Color.FromArgb(100, ScrollbarBackColor), ScrollbarBackColor, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bgBrush, scrollRect);
                }

                using (Pen borderPen = new Pen(GDI.Color.FromArgb(150, ScrollbarBackColor), 1))
                {
                    g.DrawRectangle(borderPen, scrollRect);
                }

                if (this.VerticalScroll.Maximum > 0)
                {
                    int contentHeight = this.AutoScrollMinSize.Height;
                    int visibleHeight = this.ClientSize.Height;
                    int scrollMax = Math.Max(1, this.VerticalScroll.Maximum);
                    int thumbHeight = Math.Max(20, (visibleHeight * visibleHeight) / Math.Max(contentHeight, visibleHeight));
                    int scrollRange = this.Height - thumbHeight - 4;
                    int thumbTop = 2 + (int)((float)this.VerticalScroll.Value / scrollMax * scrollRange);

                    Rectangle thumbRect = new Rectangle(this.Width - 13, thumbTop, 11, thumbHeight);

                    using (System.Drawing.Drawing2D.LinearGradientBrush thumbBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        thumbRect, ScrollbarColor, GDI.Color.FromArgb(180, ScrollbarColor.R, ScrollbarColor.G, ScrollbarColor.B), LinearGradientMode.Vertical))
                    {
                        g.FillRoundedRectangle(thumbBrush, thumbRect, 5);
                    }

                    using (Pen glowPen = new Pen(GDI.Color.FromArgb(120, ScrollbarColor.R, ScrollbarColor.G, ScrollbarColor.B), 2))
                    {
                        Rectangle glowRect = new Rectangle(thumbRect.X - 1, thumbRect.Y - 1, thumbRect.Width + 2, thumbRect.Height + 2);
                        g.DrawRoundedRectangle(glowPen, glowRect, 6);
                    }

                    using (Pen highlightPen = new Pen(GDI.Color.FromArgb(200, 255, 255, 255), 1))
                    {
                        Rectangle highlightRect = new Rectangle(thumbRect.X + 2, thumbRect.Y + 2, thumbRect.Width - 4, thumbRect.Height - 4);
                        g.DrawRoundedRectangle(highlightPen, highlightRect, 3);
                    }
                }
            }
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg == 0x20A || m.Msg == 0x115)
                {
                    this.Invalidate();
                }
            }
        }
        public class GlassCard : Panel
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (SolidBrush glassBrush = new SolidBrush(GDI.Color.FromArgb(60, 255, 255, 255)))
                {
                    GraphicsPath glassPath = GetRoundedRectPath(this.ClientRectangle, 12);
                    e.Graphics.FillPath(glassBrush, glassPath);
                    glassPath.Dispose();
                }

                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    GraphicsPath path = GetRoundedRectPath(
                        new Rectangle(1, 1, Width - 2, Height - 2), 12);
                    e.Graphics.FillPath(brush, path);
                    path.Dispose();
                }

                using (Pen neonPen = new Pen(GDI.Color.FromArgb(100, 255, 66, 77), 2))
                {
                    GraphicsPath borderPath = GetRoundedRectPath(
                        new Rectangle(1, 1, Width - 3, Height - 3), 12);
                    e.Graphics.DrawPath(neonPen, borderPath);
                    borderPath.Dispose();
                }
            }
            private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseAllFigures();
                return path;
            }
        }
        public class GamingButton : Button
        {
            private bool isHovered = false;

            public GamingButton()
            {
                this.FlatStyle = FlatStyle.Flat;
                this.FlatAppearance.BorderSize = 0;
                this.SetStyle(ControlStyles.UserPaint, true);
                this.MouseEnter += (s, e) => { isHovered = true; this.Invalidate(); };
                this.MouseLeave += (s, e) => { isHovered = false; this.Invalidate(); };
            }
            protected override void OnPaint(PaintEventArgs pevent)
            {
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                Color bgColor = isHovered ?
                    GDI.Color.FromArgb(Math.Min(255, this.BackColor.R + 20),
                                   Math.Min(255, this.BackColor.G + 20),
                                   Math.Min(255, this.BackColor.B + 20)) :
                    this.BackColor;

                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    GraphicsPath path = GetRoundedRectPath(rect, 8);
                    pevent.Graphics.FillPath(brush, path);
                    path.Dispose();
                }

                if (isHovered)
                {
                    using (Pen glowPen = new Pen(GDI.Color.FromArgb(100, this.BackColor.R, this.BackColor.G, this.BackColor.B), 3))
                    {
                        GraphicsPath glowPath = GetRoundedRectPath(
                            new Rectangle(-1, -1, Width + 1, Height + 1), 8);
                        pevent.Graphics.DrawPath(glowPen, glowPath);
                        glowPath.Dispose();
                    }
                }
                TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font,
                    this.ClientRectangle, this.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseAllFigures();
                return path;
            }
        }
        public class NeonButton : Button
        {
            public Color GlowColor { get; set; } = Color.Red;
            private bool isHovered = false;

            public NeonButton()
            {
                this.FlatStyle = FlatStyle.Flat;
                this.FlatAppearance.BorderSize = 0;
                this.UseVisualStyleBackColor = false;
                this.SetStyle(ControlStyles.UserPaint, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.SetStyle(ControlStyles.SupportsTransparentBackColor, false);
                this.MouseEnter += (s, e) => { isHovered = true; this.Invalidate(); };
                this.MouseLeave += (s, e) => { isHovered = false; this.Invalidate(); };
            }
            protected override void OnPaint(PaintEventArgs pevent)
            {
                pevent.Graphics.Clear(this.Parent.BackColor);
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(2, 2, Width - 4, Height - 4);

                if (isHovered)
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        using (Pen glowPen = new Pen(GDI.Color.FromArgb(50 - i * 15, GlowColor.R, GlowColor.G, GlowColor.B), i * 2))
                        {
                            GraphicsPath glowPath = GetRoundedRectPath(
                                new Rectangle(rect.X - i, rect.Y - i,
                                rect.Width + i * 2, rect.Height + i * 2), 10);
                            pevent.Graphics.DrawPath(glowPen, glowPath);
                            glowPath.Dispose();
                        }
                    }
                }

                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    GraphicsPath path = GetRoundedRectPath(rect, 8);
                    pevent.Graphics.FillPath(brush, path);
                    path.Dispose();
                }

                TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font,
                    this.ClientRectangle, this.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseAllFigures();
                return path;
            }
        }
        public class HotkeyButton : Button
        {
            private bool isHovered = false;

            public HotkeyButton()
            {
                this.FlatStyle = FlatStyle.Flat;
                this.FlatAppearance.BorderSize = 1;
                this.FlatAppearance.BorderColor = GDI.Color.FromArgb(255, 65, 65, 85);
                this.BackColor = GDI.Color.FromArgb(255, 35, 35, 50);
                this.ForeColor = GDI.Color.FromArgb(255, 190, 190, 190);
                this.Font = new Font("Segoe UI", 8, FontStyle.Regular);
                this.SetStyle(ControlStyles.UserPaint, true);
                this.MouseEnter += (s, e) => { isHovered = true; this.Invalidate(); };
                this.MouseLeave += (s, e) => { isHovered = false; this.Invalidate(); };
            }
            protected override void OnPaint(PaintEventArgs pevent)
            {
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                Color bgColor = isHovered ? GDI.Color.FromArgb(255, 45, 45, 65) : this.BackColor;
                Color borderColor = isHovered ? GDI.Color.FromArgb(255, 171, 71, 188) : GDI.Color.FromArgb(255, 65, 65, 85);

                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    pevent.Graphics.FillRectangle(brush, rect);
                }

                using (Pen borderPen = new Pen(borderColor, 1))
                {
                    pevent.Graphics.DrawRectangle(borderPen, rect);
                }

                Color textColor = isHovered ? Color.White : this.ForeColor;
                TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font,
                    this.ClientRectangle, textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
        public class ModernControlButton : Button
        {
            private bool isHovered = false;

            public ModernControlButton()
            {
                this.FlatStyle = FlatStyle.Flat;
                this.FlatAppearance.BorderSize = 0;
                this.SetStyle(ControlStyles.UserPaint, true);
                this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                this.BackColor = Color.Transparent;
                this.MouseEnter += (s, e) => { isHovered = true; this.Invalidate(); };
                this.MouseLeave += (s, e) => { isHovered = false; this.Invalidate(); };
            }

            protected override void OnPaint(PaintEventArgs pevent)
            {
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (SolidBrush clearBrush = new SolidBrush(GDI.Color.FromArgb(255, 26, 26, 35)))
                {
                    pevent.Graphics.FillRectangle(clearBrush, this.ClientRectangle);
                }

                if (isHovered)
                {
                    using (SolidBrush brush = new SolidBrush(GDI.Color.FromArgb(80, 255, 255, 255)))
                    {
                        pevent.Graphics.FillEllipse(brush, 2, 2, Width - 4, Height - 4);
                    }
                }

                TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font,
                    this.ClientRectangle, this.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        public class ModernMiniButton : Button
        {
            private bool isHovered = false;

            public ModernMiniButton()
            {
                this.FlatStyle = FlatStyle.Flat;
                this.FlatAppearance.BorderSize = 0;
                this.SetStyle(ControlStyles.UserPaint, true);
                this.MouseEnter += (s, e) => { isHovered = true; this.Invalidate(); };
                this.MouseLeave += (s, e) => { isHovered = false; this.Invalidate(); };
            }

            protected override void OnPaint(PaintEventArgs pevent)
            {
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                Color bgColor = isHovered ?
                    GDI.Color.FromArgb(Math.Min(255, this.BackColor.R + 30),
                                   Math.Min(255, this.BackColor.G + 30),
                                   Math.Min(255, this.BackColor.B + 30)) :
                    this.BackColor;

                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    pevent.Graphics.FillRectangle(brush, rect);
                }

                TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font,
                    this.ClientRectangle, this.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        public class ModernCheckBox : CheckBox
        {
            public ModernCheckBox()
            {
                this.SetStyle(ControlStyles.UserPaint, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            }

            protected override void OnPaint(PaintEventArgs pevent)
            {
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle checkBoxRect = new Rectangle(0, 2, 16, 16);
                Rectangle textRect = new Rectangle(22, 0, Width - 22, Height);
                Color boxColor = this.Checked ? GDI.Color.FromArgb(255, 171, 71, 188) : GDI.Color.FromArgb(255, 65, 65, 85);
                using (SolidBrush brush = new SolidBrush(boxColor))
                {
                    pevent.Graphics.FillRectangle(brush, checkBoxRect);
                }
                using (Pen borderPen = new Pen(GDI.Color.FromArgb(255, 90, 90, 110), 1))
                {
                    pevent.Graphics.DrawRectangle(borderPen, checkBoxRect);
                }
                if (this.Checked)
                {
                    using (Pen checkPen = new Pen(Color.White, 2))
                    {
                        pevent.Graphics.DrawLines(checkPen, new Point[]
                        {
                            new Point(4, 8),
                            new Point(7, 11),
                            new Point(12, 5)
                        });
                    }
                }
                TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font,
                    textRect, this.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        public class PulsingIndicator : Panel
        {
            private System.Windows.Forms.Timer pulseTimer;
            private float pulseAlpha = 0.5f;
            private bool pulseIncreasing = true;

            public PulsingIndicator()
            {
                pulseTimer = new System.Windows.Forms.Timer { Interval = 100 };
                pulseTimer.Tick += (s, e) =>
                {
                    if (pulseIncreasing)
                        pulseAlpha += 0.05f;
                    else
                        pulseAlpha -= 0.05f;

                    if (pulseAlpha >= 1.0f)
                    {
                        pulseAlpha = 1.0f;
                        pulseIncreasing = false;
                    }
                    else if (pulseAlpha <= 0.3f)
                    {
                        pulseAlpha = 0.3f;
                        pulseIncreasing = true;
                    }

                    this.Invalidate();
                };
                pulseTimer.Start();
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                int alpha = (int)(255 * Math.Max(0.3f, Math.Min(1.0f, pulseAlpha)));

                using (SolidBrush brush = new SolidBrush(
                    GDI.Color.FromArgb(alpha, this.BackColor.R, this.BackColor.G, this.BackColor.B)))
                {
                    e.Graphics.FillEllipse(brush, 1, 1, Width - 2, Height - 2);
                }

                using (SolidBrush glowBrush = new SolidBrush(
                    GDI.Color.FromArgb(alpha / 3, this.BackColor.R, this.BackColor.G, this.BackColor.B)))
                {
                    e.Graphics.FillEllipse(glowBrush, 0, 0, Width, Height);
                }
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    pulseTimer?.Stop();
                    pulseTimer?.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        public class AnimatedLabel : Label
        {
            private System.Windows.Forms.Timer animationTimer;
            private int animationFrame = 0;

            public AnimatedLabel()
            {
                animationTimer = new System.Windows.Forms.Timer { Interval = 500 };
                animationTimer.Tick += (s, e) =>
                {
                    animationFrame = (animationFrame + 1) % 4;
                    if (this.Text.Contains("Scanning"))
                    {
                        string dots = new string('.', animationFrame);
                        this.Text = "🔍 Scanning for Flyff processes" + dots;
                    }
                };
                animationTimer.Start();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    animationTimer?.Stop();
                    animationTimer?.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        public class GamingComboBox : ComboBox
        {
            public GamingComboBox()
            {
                this.DropDownStyle = ComboBoxStyle.DropDownList;
                this.FlatStyle = FlatStyle.Flat;
                this.DrawMode = DrawMode.OwnerDrawFixed;
            }

            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                e.DrawBackground();

                if (e.Index >= 0)
                {
                    Color textColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected ?
                        Color.White : this.ForeColor;

                    using (SolidBrush brush = new SolidBrush(textColor))
                    {
                        e.Graphics.DrawString(this.Items[e.Index].ToString(), this.Font, brush, e.Bounds);
                    }
                }

                e.DrawFocusRectangle();
            }
        }

        public class GamingTextBox : TextBox
        {
            private bool isFocused = false;

            public GamingTextBox()
            {
                this.BorderStyle = BorderStyle.None;
                this.SetStyle(ControlStyles.UserPaint, true);
                this.Multiline = true;
                this.GotFocus += (s, e) => { isFocused = true; this.Invalidate(); };
                this.LostFocus += (s, e) => { isFocused = false; this.Invalidate(); };
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }

                Color borderColor = isFocused ? GDI.Color.FromArgb(255, 255, 66, 77) : GDI.Color.FromArgb(255, 65, 65, 85);
                using (Pen pen = new Pen(borderColor, isFocused ? 2 : 1))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }

                TextRenderer.DrawText(e.Graphics, this.Text, this.Font,
                    new Rectangle(4, 2, Width - 8, Height - 4), this.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        public class GamingTabControl : TabControl
        {
            public GamingTabControl()
            {
                this.SetStyle(ControlStyles.UserPaint, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.SetStyle(ControlStyles.DoubleBuffer, true);
                this.SetStyle(ControlStyles.ResizeRedraw, true);
                this.SizeMode = TabSizeMode.FillToRight;
                this.ItemSize = new Size(0, 35);
                this.DrawMode = TabDrawMode.OwnerDrawFixed;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(this.BackColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush bgBrush = new SolidBrush(GDI.Color.FromArgb(255, 26, 26, 35)))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, this.Width, this.ItemSize.Height);
                }

                for (int i = 0; i < this.TabCount; i++)
                {
                    Rectangle tabRect = this.GetTabRect(i);
                    bool isSelected = (i == this.SelectedIndex);

                    Color startColor = isSelected ? GDI.Color.FromArgb(255, 255, 66, 77) : GDI.Color.FromArgb(255, 45, 45, 65);
                    Color endColor = isSelected ? GDI.Color.FromArgb(255, 200, 50, 60) : GDI.Color.FromArgb(255, 35, 35, 50);

                    using (System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(tabRect, startColor, endColor, LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillRectangle(brush, tabRect);
                    }

                    if (isSelected)
                    {
                        using (Pen neonPen = new Pen(GDI.Color.FromArgb(150, 255, 66, 77), 2))
                        {
                            e.Graphics.DrawRectangle(neonPen, tabRect.X, tabRect.Y, tabRect.Width - 1, tabRect.Height - 1);
                        }
                    }

                    Color textColor = isSelected ? Color.White : GDI.Color.FromArgb(255, 190, 190, 190);
                    TextRenderer.DrawText(e.Graphics, this.TabPages[i].Text,
                        new Font("Segoe UI", 9, FontStyle.Bold), tabRect, textColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                int totalTabsWidth = 0;
                for (int i = 0; i < this.TabCount; i++)
                {
                    totalTabsWidth += this.GetTabRect(i).Width;
                }

                if (totalTabsWidth < this.Width)
                {
                    Rectangle remainingRect = new Rectangle(totalTabsWidth, 0, this.Width - totalTabsWidth, this.ItemSize.Height);
                    using (SolidBrush remainingBrush = new SolidBrush(GDI.Color.FromArgb(255, 26, 26, 35)))
                    {
                        e.Graphics.FillRectangle(remainingBrush, remainingRect);
                    }
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x1328)
                {
                    base.WndProc(ref m);
                    RECT rc = (RECT)m.GetLParam(typeof(RECT));
                    rc.Left -= 2;
                    rc.Right += 2;
                    rc.Top -= 2;
                    rc.Bottom += 2;
                    Marshal.StructureToPtr(rc, m.LParam, true);
                }
                else
                {
                    base.WndProc(ref m);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        #endregion

        #region Hotkey Capture Form

        public partial class HotkeyCapture : Form
        {
            public string CapturedHotkey { get; private set; } = "";
            private Keys currentKey = Keys.None;
            private bool ctrlPressed = false;
            private bool altPressed = false;
            private bool shiftPressed = false;
            private bool winPressed = false;
            private Label instructionLabel;
            private Label previewLabel;
            private Button confirmButton;
            private Button cancelButton;

            public HotkeyCapture()
            {
                InitializeHotkeyCapture();
            }
            private void InitializeHotkeyCapture()
            {
                this.Text = "Capture Hotkey";
                this.Size = new Size(400, 200);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = GDI.Color.FromArgb(255, 26, 26, 35);
                this.ForeColor = Color.White;
                this.KeyPreview = true;

                instructionLabel = new Label
                {
                    Text = "Press any key combination (Ctrl, Alt, Shift + Key)",
                    Location = new Point(20, 20),
                    Size = new Size(360, 40),
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    ForeColor = GDI.Color.FromArgb(255, 190, 190, 190),
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                previewLabel = new Label
                {
                    Text = "Press keys...",
                    Location = new Point(20, 80),
                    Size = new Size(360, 30),
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = GDI.Color.FromArgb(255, 171, 71, 188),
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle
                };

                confirmButton = new Button
                {
                    Text = "Confirm",
                    Location = new Point(220, 130),
                    Size = new Size(80, 30),
                    BackColor = GDI.Color.FromArgb(255, 76, 175, 80),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Enabled = false
                };
                confirmButton.FlatAppearance.BorderSize = 0;
                confirmButton.Click += ConfirmButton_Click;

                cancelButton = new Button
                {
                    Text = "Cancel",
                    Location = new Point(310, 130),
                    Size = new Size(80, 30),
                    BackColor = GDI.Color.FromArgb(255, 244, 67, 54),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                };
                cancelButton.FlatAppearance.BorderSize = 0;
                cancelButton.Click += CancelButton_Click;

                this.Controls.AddRange(new Control[] {
                    instructionLabel, previewLabel, confirmButton, cancelButton
                });

                this.KeyDown += HotkeyCapture_KeyDown;
                this.KeyUp += HotkeyCapture_KeyUp;
            }

            private void HotkeyCapture_KeyDown(object sender, KeyEventArgs e)
            {
                ctrlPressed = e.Control;
                altPressed = e.Alt;
                shiftPressed = e.Shift;
                winPressed = (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin);

                if (e.KeyCode != Keys.ControlKey && e.KeyCode != Keys.Menu &&
                    e.KeyCode != Keys.ShiftKey && e.KeyCode != Keys.LWin && e.KeyCode != Keys.RWin)
                {
                    currentKey = e.KeyCode;
                }

                UpdatePreview();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }

            private void HotkeyCapture_KeyUp(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.ControlKey) ctrlPressed = false;
                if (e.KeyCode == Keys.Menu) altPressed = false;
                if (e.KeyCode == Keys.ShiftKey) shiftPressed = false;
                if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin) winPressed = false;

                UpdatePreview();
                e.Handled = true;
            }

            private void UpdatePreview()
            {
                List<string> parts = new List<string>();

                if (ctrlPressed) parts.Add("Ctrl");
                if (altPressed) parts.Add("Alt");
                if (shiftPressed) parts.Add("Shift");
                if (winPressed) parts.Add("Win");

                if (currentKey != Keys.None)
                {
                    string keyName = GetKeyName(currentKey);
                    if (!string.IsNullOrEmpty(keyName))
                    {
                        parts.Add(keyName);
                    }
                }

                if (parts.Count > 0)
                {
                    CapturedHotkey = string.Join(" + ", parts);
                    previewLabel.Text = CapturedHotkey;
                    confirmButton.Enabled = (parts.Count > 1 || (parts.Count == 1 && currentKey != Keys.None));
                }
                else
                {
                    previewLabel.Text = "Press keys...";
                    confirmButton.Enabled = false;
                    CapturedHotkey = "";
                }
            }
            private string GetKeyName(Keys key)
            {
                if (KeyNames.ContainsKey(key))
                {
                    return KeyNames[key];
                }
                return key.ToString();
            }

            private void ConfirmButton_Click(object sender, EventArgs e)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }

            private void CancelButton_Click(object sender, EventArgs e)
            {
                CapturedHotkey = "";
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }
        #endregion
        private Icon CreateModernIcon()
        {
            GDI.Bitmap bitmap = new GDI.Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (System.Drawing.Drawing2D.LinearGradientBrush gradBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, 32, 32), AccentRed, AccentPurple, LinearGradientMode.Horizontal))
                {
                    g.FillEllipse(gradBrush, 2, 2, 28, 28);
                }

                g.DrawString("F", new Font("Arial", 18, FontStyle.Bold),
                    Brushes.White, new PointF(8, 4));
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, GDI.Brush brush, Rectangle rect, int radius)
        {
            using (GraphicsPath path = GetRoundedRectPath(rect, radius))
            {
                graphics.FillPath(brush, path);
            }
        }
        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle rect, int radius)
        {
            using (GraphicsPath path = GetRoundedRectPath(rect, radius))
            {
                graphics.DrawPath(pen, path);
            }
        }
        private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            return path;
        }
    }
}