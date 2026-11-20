using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlbionGatherBot
{
    public partial class Form1 : Form
    {
        // ===== Windows API =====
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint KEYEVENTF_KEYDOWN = 0x0000;
        const uint KEYEVENTF_KEYUP = 0x0002;

        // ===== Цветовая схема (геймерская тёмная) =====
        private static readonly Color BG_DARK = Color.FromArgb(30, 30, 46);
        private static readonly Color BG_PANEL = Color.FromArgb(49, 50, 68);
        private static readonly Color ACCENT_GREEN = Color.FromArgb(166, 227, 161);
        private static readonly Color ACCENT_RED = Color.FromArgb(243, 139, 168);
        private static readonly Color ACCENT_BLUE = Color.FromArgb(137, 180, 250);
        private static readonly Color ACCENT_YELLOW = Color.FromArgb(249, 226, 175);
        private static readonly Color TEXT_MAIN = Color.FromArgb(205, 214, 244);
        private static readonly Color TEXT_DIM = Color.FromArgb(108, 112, 134);

        // ===== Элементы формы =====
        private Button btnStart;
        private Button btnStop;
        private Button btnCalibrate;
        private NumericUpDown numDelay;
        private CheckBox chkRandom;
        private CheckBox chkLookAround;
        private CheckBox chkMoveForward;
        private Label lblStatus;
        private Label lblFound;
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblColorInfo;
        private ProgressBar progressBar;
        private Panel panelControl;
        private Panel panelSettings;
        private Panel panelStatus;

        // ===== Настройки бота =====
        private Color targetColor = Color.FromArgb(139, 90, 43);
        private int colorTolerance = 40;
        private bool isRunning = false;
        private CancellationTokenSource? cts;
        private Random rnd = new Random();
        private int resourcesFound = 0;
        private int searchAttempts = 0;

        public Form1()
        {
            InitializeComponent();
            CreateBeautifulUI();
        }

        // ===== СОЗДАНИЕ КРАСИВОГО ИНТЕРФЕЙСА =====
        private void CreateBeautifulUI()
        {
            // Настройки окна
            this.Text = "Albion Gather Bot v1.0";
            this.Size = new Size(520, 620);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BG_DARK;
            this.Padding = new Padding(15);

            // ===== ЗАГОЛОВОК =====
            lblTitle = new Label();
            lblTitle.Text = "⛏️ ALBION GATHER BOT";
            lblTitle.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblTitle.ForeColor = ACCENT_GREEN;
            lblTitle.Location = new Point(20, 20);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            lblSubtitle = new Label();
            lblSubtitle.Text = "Автоматический сбор ресурсов с распознаванием экрана";
            lblSubtitle.Font = new Font("Segoe UI", 10);
            lblSubtitle.ForeColor = TEXT_DIM;
            lblSubtitle.Location = new Point(20, 55);
            lblSubtitle.AutoSize = true;
            this.Controls.Add(lblSubtitle);

            // ===== ПАНЕЛЬ УПРАВЛЕНИЯ =====
            panelControl = CreatePanel(20, 90, 470, 130, "УПРАВЛЕНИЕ");
            this.Controls.Add(panelControl);

            btnStart = CreateButton("▶  ЗАПУСТИТЬ", 15, 40, 140, 50, ACCENT_GREEN, BG_DARK);
            btnStart.Click += btnStart_Click;
            panelControl.Controls.Add(btnStart);

            btnStop = CreateButton("⏹  ОСТАНОВИТЬ", 170, 40, 140, 50, ACCENT_RED, BG_DARK);
            btnStop.Enabled = false;
            btnStop.Click += btnStop_Click;
            panelControl.Controls.Add(btnStop);

            btnCalibrate = CreateButton("🎯  КАЛИБРОВАТЬ", 320, 40, 135, 50, ACCENT_BLUE, BG_DARK);
            btnCalibrate.Click += btnCalibrate_Click;
            panelControl.Controls.Add(btnCalibrate);

            // Информация о цвете
            lblColorInfo = new Label();
            lblColorInfo.Text = $"🎨 Цвет: RGB({targetColor.R}, {targetColor.G}, {targetColor.B})";
            lblColorInfo.Font = new Font("Segoe UI", 9);
            lblColorInfo.ForeColor = TEXT_DIM;
            lblColorInfo.Location = new Point(15, 100);
            lblColorInfo.AutoSize = true;
            panelControl.Controls.Add(lblColorInfo);

            // ===== ПАНЕЛЬ НАСТРОЕК =====
            panelSettings = CreatePanel(20, 235, 470, 200, "НАСТРОЙКИ");
            this.Controls.Add(panelSettings);

            // Задержка
            Label lblDelay = new Label();
            lblDelay.Text = "⏱ Задержка между сканами:";
            lblDelay.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblDelay.ForeColor = TEXT_MAIN;
            lblDelay.Location = new Point(15, 30);
            lblDelay.AutoSize = true;
            panelSettings.Controls.Add(lblDelay);

            numDelay = new NumericUpDown();
            numDelay.Location = new Point(230, 28);
            numDelay.Size = new Size(100, 25);
            numDelay.Minimum = 500;
            numDelay.Maximum = 10000;
            numDelay.Value = 2000;
            numDelay.Increment = 100;
            numDelay.BackColor = BG_PANEL;
            numDelay.ForeColor = TEXT_MAIN;
            numDelay.Font = new Font("Segoe UI", 10);
            panelSettings.Controls.Add(numDelay);

            Label lblMs = new Label();
            lblMs.Text = "мс";
            lblMs.Font = new Font("Segoe UI", 10);
            lblMs.ForeColor = TEXT_DIM;
            lblMs.Location = new Point(340, 30);
            lblMs.AutoSize = true;
            panelSettings.Controls.Add(lblMs);

            // Флажки
            chkRandom = CreateCheckBox("🎲 Рандомная задержка (анти-детект)", 15, 70, true);
            panelSettings.Controls.Add(chkRandom);

            chkLookAround = CreateCheckBox("🔄 Автоматический поворот камеры", 15, 100, true);
            panelSettings.Controls.Add(chkLookAround);

            chkMoveForward = CreateCheckBox("🚶 Двигаться вперёд при отсутствии ресурсов", 15, 130, false);
            panelSettings.Controls.Add(chkMoveForward);

            // Допуск по цвету
            Label lblTolerance = new Label();
            lblTolerance.Text = "🎨 Допуск по цвету:";
            lblTolerance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblTolerance.ForeColor = TEXT_MAIN;
            lblTolerance.Location = new Point(15, 165);
            lblTolerance.AutoSize = true;
            panelSettings.Controls.Add(lblTolerance);

            NumericUpDown numTolerance = new NumericUpDown();
            numTolerance.Location = new Point(170, 163);
            numTolerance.Size = new Size(80, 25);
            numTolerance.Minimum = 10;
            numTolerance.Maximum = 100;
            numTolerance.Value = 40;
            numTolerance.BackColor = BG_PANEL;
            numTolerance.ForeColor = TEXT_MAIN;
            numTolerance.Font = new Font("Segoe UI", 10);
            numTolerance.ValueChanged += (s, e) => colorTolerance = (int)numTolerance.Value;
            panelSettings.Controls.Add(numTolerance);

            // ===== ПАНЕЛЬ СТАТУСА =====
            panelStatus = CreatePanel(20, 450, 470, 120, "СТАТУС");
            this.Controls.Add(panelStatus);

            lblStatus = new Label();
            lblStatus.Text = "⏸ Ожидание запуска...";
            lblStatus.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblStatus.ForeColor = ACCENT_YELLOW;
            lblStatus.Location = new Point(15, 30);
            lblStatus.AutoSize = true;
            panelStatus.Controls.Add(lblStatus);

            lblFound = new Label();
            lblFound.Text = "📦 Собрано ресурсов: 0";
            lblFound.Font = new Font("Segoe UI", 11);
            lblFound.ForeColor = TEXT_MAIN;
            lblFound.Location = new Point(15, 60);
            lblFound.AutoSize = true;
            panelStatus.Controls.Add(lblFound);

            // Прогресс-бар (визуализация работы)
            progressBar = new ProgressBar();
            progressBar.Location = new Point(15, 90);
            progressBar.Size = new Size(440, 20);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 30;
            progressBar.Visible = false;
            panelStatus.Controls.Add(progressBar);
        }

        // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ UI =====
        private Panel CreatePanel(int x, int y, int width, int height, string title)
        {
            Panel panel = new Panel();
            panel.Location = new Point(x, y);
            panel.Size = new Size(width, height);
            panel.BackColor = BG_PANEL;
            panel.Padding = new Padding(15, 30, 15, 15);

            // Заголовок панели
            Label lbl = new Label();
            lbl.Text = title;
            lbl.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lbl.ForeColor = ACCENT_BLUE;
            lbl.Location = new Point(15, 8);
            lbl.AutoSize = true;
            panel.Controls.Add(lbl);

            // Линия-разделитель под заголовком
            Label line = new Label();
            line.BackColor = Color.FromArgb(69, 71, 90);
            line.Location = new Point(15, 28);
            line.Size = new Size(width - 30, 1);
            panel.Controls.Add(line);

            return panel;
        }

        private Button CreateButton(string text, int x, int y, int width, int height, Color bgColor, Color textColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(width, height);
            btn.BackColor = bgColor;
            btn.ForeColor = textColor;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;

            // Эффект при наведении
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(bgColor, 0.2f);
            btn.MouseLeave += (s, e) => btn.BackColor = bgColor;

            return btn;
        }

        private CheckBox CreateCheckBox(string text, int x, int y, bool isChecked)
        {
            CheckBox chk = new CheckBox();
            chk.Text = text;
            chk.Location = new Point(x, y);
            chk.AutoSize = true;
            chk.Checked = isChecked;
            chk.ForeColor = TEXT_MAIN;
            chk.Font = new Font("Segoe UI", 10);
            chk.BackColor = Color.Transparent;
            chk.FlatStyle = FlatStyle.Flat;
            return chk;
        }

        // ===== ОБРАБОТЧИКИ СОБЫТИЙ =====
        private async void btnStart_Click(object? sender, EventArgs e)
        {
            if (isRunning) return;
            isRunning = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            progressBar.Visible = true;
            lblStatus.Text = "🟢 Бот активен — сканирование экрана...";
            lblStatus.ForeColor = ACCENT_GREEN;

            cts = new CancellationTokenSource();
            await Task.Run(() => GatherLoop(cts.Token));
        }

        private void btnStop_Click(object? sender, EventArgs e)
        {
            isRunning = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            progressBar.Visible = false;
            lblStatus.Text = "⏹ Бот остановлен";
            lblStatus.ForeColor = ACCENT_RED;
            cts?.Cancel();
        }

        private void btnCalibrate_Click(object? sender, EventArgs e)
        {
            lblStatus.Text = "⚠️ Наведи мышь на ресурс в игре и нажми любую клавишу...";
            lblStatus.ForeColor = ACCENT_YELLOW;
            this.KeyPreview = true;
            this.KeyPress += Form1_KeyPress;
        }

        private void Form1_KeyPress(object? sender, KeyPressEventArgs e)
        {
            var cursorPos = Cursor.Position;
            using (Bitmap bmp = new Bitmap(1, 1))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(cursorPos, Point.Empty, new Size(1, 1));
                }
                targetColor = bmp.GetPixel(0, 0);
            }
            lblColorInfo.Text = $"🎨 Цвет: RGB({targetColor.R}, {targetColor.G}, {targetColor.B})";
            lblStatus.Text = "✅ Цвет сохранён! Можешь запускать бота.";
            lblStatus.ForeColor = ACCENT_GREEN;
            this.KeyPress -= Form1_KeyPress;
        }

        // ===== ГЛАВНЫЙ ЦИКЛ СБОРА =====
        private void GatherLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && isRunning)
            {
                try
                {
                    var screenshot = CaptureScreen();
                    var resourcePoint = FindResourceByColor(screenshot, targetColor, colorTolerance);

                    if (resourcePoint.HasValue)
                    {
                        ClickAt(resourcePoint.Value.X, resourcePoint.Value.Y);
                        resourcesFound++;
                        searchAttempts = 0;
                        Log($"✅ Собрано: {resourcesFound} | Координаты: ({resourcePoint.Value.X}, {resourcePoint.Value.Y})");
                        Thread.Sleep(rnd.Next(2000, 3500));
                        PressKey((byte)Keys.E);
                        Thread.Sleep(rnd.Next(500, 1000));
                    }
                    else
                    {
                        searchAttempts++;
                        Log($"🔍 Поиск... (попытка {searchAttempts})");

                        if (searchAttempts >= 3 && chkLookAround.Checked)
                        {
                            Log("🔄 Поворот камеры...");
                            LookAround();
                            searchAttempts = 0;
                        }
                        else if (searchAttempts >= 6 && chkMoveForward.Checked)
                        {
                            Log("🚶 Движение вперёд...");
                            MoveForward();
                            searchAttempts = 0;
                        }
                    }

                    int delay = (int)numDelay.Value;
                    if (chkRandom.Checked)
                        delay += rnd.Next(-delay / 5, delay / 5);
                    Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    Log($"❌ Ошибка: {ex.Message}");
                }
            }
        }

        // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ БОТА =====
        private Bitmap CaptureScreen()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }
            return bitmap;
        }

        private Point? FindResourceByColor(Bitmap screenshot, Color target, int tolerance)
        {
            for (int y = 0; y < screenshot.Height; y += 10)
            {
                for (int x = 0; x < screenshot.Width; x += 10)
                {
                    var pixel = screenshot.GetPixel(x, y);
                    if (Math.Abs(pixel.R - target.R) <= tolerance &&
                        Math.Abs(pixel.G - target.G) <= tolerance &&
                        Math.Abs(pixel.B - target.B) <= tolerance)
                    {
                        return new Point(x, y);
                    }
                }
            }
            return null;
        }

        private void LookAround()
        {
            var originalPos = Cursor.Position;
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_MOVE, 300, 0, 0, UIntPtr.Zero);
            Thread.Sleep(800);
            mouse_event(MOUSEEVENTF_MOVE, -600, 0, 0, UIntPtr.Zero);
            Thread.Sleep(800);
            mouse_event(MOUSEEVENTF_MOVE, 300, 0, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(300);
            SetCursorPos(originalPos.X, originalPos.Y);
        }

        private void MoveForward()
        {
            keybd_event((byte)Keys.W, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(2000);
            keybd_event((byte)Keys.W, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(500);
        }

        private void ClickAt(int x, int y)
        {
            SetCursorPos(x, y);
            Thread.Sleep(30);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(20);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        private void PressKey(byte key)
        {
            keybd_event(key, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(30);
            keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    lblStatus.Text = $"Статус: {message}";
                    lblFound.Text = $"📦 Собрано ресурсов: {resourcesFound}";
                }));
                return;
            }
            lblStatus.Text = $"Статус: {message}";
            lblFound.Text = $"📦 Собрано ресурсов: {resourcesFound}";
        }
    }
}