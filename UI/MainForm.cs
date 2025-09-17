using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SnippetPad.Models;
using SnippetPad.Services;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace SnippetPad.UI
{
    public partial class MainForm : Form
    {
        private readonly ConfigService _config;
        private readonly ClipboardService _clipboard;
        private RootConfig _data = new();

        // ====== 方案A：放大按钮尺寸 & Padding，并设置较大的最小宽度 ======
        private readonly Font _uiFont = new("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point); // 字体稍增大
        private const int ButtonHeight = 32;        // 原 28 → 32
        private const int ButtonHPad = 20;          // 原 16 → 20  (左右文本总补偿)
        private const int MinButtonWidth = 60;     // 新增：统一最小按钮宽度
        private const int GroupOuterMargin = 8;     // 原 6 → 8
        private const int GroupInnerPad = 8;        // 原 6 → 8

        // 轻微缩小 Group 宽度：在原算法基础上再减去一个常量
        private const int GroupWidthShrink = 22;    // “稍微减小”可调；如觉得太窄调小为 20

        private readonly WinFormsTimer _statusTimer = new() { Interval = 2000 };

        // 仅首次构建一次布局
        private bool _layoutBuilt = false;

        // 前台窗口跟踪（用于粘贴）
        private IntPtr _lastExternalForeground = IntPtr.Zero;
        private IntPtr _winEventHook = IntPtr.Zero;
        private WinEventDelegate? _winEventDelegate;

        public MainForm(ConfigService config, ClipboardService clipboard)
        {
            _config = config;
            _clipboard = clipboard;
            InitializeComponent();

            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(100, 70);

            _statusTimer.Tick += (_, _) =>
            {
                lblStatus.Text = "";
                _statusTimer.Stop();
            };

            // 加载配置并构建初始 UI
            LoadConfigAndMaybeRebuild(fullRebuild: true);

            // 安装前台窗口监听（用于粘贴到外部活动窗口的输入框）
            InstallForegroundHook();
        }

        #region 配置刷新与构建

        private void ReloadConfig()
        {
            LoadConfigAndMaybeRebuild(fullRebuild: false);
            ShowStatus("配置已刷新");
        }

        private void LoadConfigAndMaybeRebuild(bool fullRebuild)
        {
            var newData = _config.LoadOrCreate();

            if (!_layoutBuilt)
            {
                _data = newData;
                BuildFullLayout();
                _layoutBuilt = true;
                ReportLoadStats();
                return;
            }

            var oldNames = (_data.Groups ?? new()).Select(g => g.Name ?? "").ToList();
            var newNames = (newData.Groups ?? new()).Select(g => g.Name ?? "").ToList();
            bool structureChanged = fullRebuild ||
                                    oldNames.Count != newNames.Count ||
                                    !oldNames.SequenceEqual(newNames);

            _data = newData;

            if (structureChanged)
            {
                BuildFullLayout();
            }
            else
            {
                RebuildButtonsOnly(); // 只重建按钮
            }

            ReportLoadStats();
        }

        private void ReportLoadStats()
        {
            int groupCount = _data.Groups?.Count ?? 0;
            int btnCount = _data.Groups?.Sum(g => g.Snippets?.Count ?? 0) ?? 0;
            ShowStatus($"已加载 {groupCount} 组 / {btnCount} 个按钮");
        }

        #endregion

        #region 全量布局（仅首次或结构变化）
        private void BuildFullLayout()
        {
            flowGroups.SuspendLayout();
            flowGroups.Controls.Clear();

            int outerM = GroupOuterMargin;
            int innerP = GroupInnerPad;
            int fudge = 8;

            // 基准宽度（不再随窗口变化自动调整）
            int viewportWidth = flowGroups.ClientSize.Width;
            if (viewportWidth <= 0)
                viewportWidth = ClientSize.Width - 20;

            foreach (var group in _data.Groups ?? new List<SnippetGroup>())
            {
                // “稍微缩窄”在原公式基础上再减去 GroupWidthShrink
                int groupWidth = Math.Max(200, viewportWidth - outerM * 2 - GroupWidthShrink);

                var container = new Panel
                {
                    AutoSize = false,
                    Width = groupWidth,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(outerM),
                    Padding = new Padding(innerP),
                    Tag = group
                };

                var lbl = new Label
                {
                    Text = string.IsNullOrWhiteSpace(group.Name) ? "(未命名组)" : group.Name,
                    AutoSize = true,
                    Font = new Font(_uiFont, FontStyle.Bold),
                    Location = new Point(4, 4),
                    Margin = new Padding(0)
                };
                container.Controls.Add(lbl);

                int innerWidth = groupWidth - container.Padding.Horizontal - fudge;
                if (innerWidth < 120) innerWidth = 120;

                var panel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Location = new Point(4, lbl.Bottom + 4),
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    Tag = "ButtonsPanel"
                };
                panel.MinimumSize = new Size(innerWidth, 0);
                panel.MaximumSize = new Size(innerWidth, int.MaxValue);
                panel.Width = innerWidth;
                container.Controls.Add(panel);

                AddButtonsToPanel(panel, group, innerWidth);

                panel.PerformLayout();
                container.Height = panel.Bottom + innerP + 4;

                flowGroups.Controls.Add(container);
            }

            flowGroups.ResumeLayout();
        }

        #endregion

        #region 仅按钮重建

        private void RebuildButtonsOnly()
        {
            if (_data.Groups == null) return;

            flowGroups.SuspendLayout();

            var panels = flowGroups.Controls.OfType<Panel>().ToList();
            for (int i = 0; i < _data.Groups.Count && i < panels.Count; i++)
            {
                var group = _data.Groups[i];
                var container = panels[i];

                var lbl = container.Controls.OfType<Label>().FirstOrDefault();
                if (lbl != null)
                {
                    lbl.Text = string.IsNullOrWhiteSpace(group.Name) ? "(未命名组)" : group.Name;
                }

                var btnPanel = container.Controls
                    .OfType<FlowLayoutPanel>()
                    .FirstOrDefault(p => Equals(p.Tag, "ButtonsPanel"));
                if (btnPanel == null) continue;

                int innerWidth = btnPanel.Width;

                // 清理旧按钮
                var oldButtons = btnPanel.Controls.OfType<Button>().ToList();
                foreach (var b in oldButtons)
                {
                    b.Click -= OnSnippetButtonClick;
                    b.Dispose();
                }
                btnPanel.Controls.Clear();

                AddButtonsToPanel(btnPanel, group, innerWidth);

                btnPanel.PerformLayout();
                container.Height = btnPanel.Bottom + GroupInnerPad + 4;
            }

            flowGroups.ResumeLayout();
        }

        private void AddButtonsToPanel(FlowLayoutPanel panel, SnippetGroup group, int innerWidth)
        {
            var snippets = group.Snippets ?? new List<Snippet>();
            foreach (var s in snippets)
            {
                string title = (s.Title ?? "").Trim();
                string content = s.Content ?? "";
                if (string.IsNullOrEmpty(title))
                {
                    title = string.IsNullOrEmpty(content)
                        ? "(未命名)"
                        : (content.Length > 12 ? content[..12] + "..." : content);
                }

                var textSize = TextRenderer.MeasureText(
                    title, _uiFont, new Size(int.MaxValue, ButtonHeight),
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

                int preferredWidth = textSize.Width + ButtonHPad;
                int btnWidth = Math.Max(MinButtonWidth, Math.Min(preferredWidth, innerWidth));

                var btn = new Button
                {
                    Text = title,
                    AutoSize = false,
                    Width = btnWidth,
                    Height = ButtonHeight,
                    Font = _uiFont,
                    Tag = s,
                    TextAlign = ContentAlignment.MiddleLeft,
                    FlatStyle = FlatStyle.Standard,
                    UseCompatibleTextRendering = false,
                    AutoEllipsis = true,
                    UseMnemonic = false,
                    Margin = new Padding(3)
                };

                if (!string.IsNullOrEmpty(content))
                {
                    var tt = new ToolTip();
                    var preview = content.Length > 150 ? content[..150] + "..." : content;
                    tt.SetToolTip(btn, preview);
                }

                btn.Click += OnSnippetButtonClick;
                panel.Controls.Add(btn);
            }
        }

        #endregion

        #region 按钮点击与自动粘贴（任意当前激活输入框）

        private void OnSnippetButtonClick(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is Snippet s)
            {
                CopySnippet(s);
            }
        }

        private void CopySnippet(Snippet s)
        {
            try
            {
                string text = s.Content ?? "";
                _clipboard.SetText(text);
                ShowStatus($"已复制：{(string.IsNullOrWhiteSpace(s.Title) ? "无标题" : s.Title)}");
                PasteIntoLastExternal(text);
            }
            catch (Exception ex)
            {
                ShowStatus("复制失败：" + ex.Message, Color.IndianRed);
            }
        }

        // 将文本粘贴到最近一次处于前台、且不属于本进程的窗口
        private void PasteIntoLastExternal(string text)
        {
            if (_lastExternalForeground == IntPtr.Zero)
                return;

            if (NativeMethods.IsWindow(_lastExternalForeground))
            {
                NativeMethods.SetForegroundWindow(_lastExternalForeground);
                System.Threading.Thread.Sleep(100);
                SendKeys.SendWait("^v");
            }
        }

        #endregion

        #region 前台窗口跟踪 (WinEvent Hook)

        private void InstallForegroundHook()
        {
            _winEventDelegate = WinEventCallback;
            _winEventHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _winEventDelegate,
                0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

            var fg = NativeMethods.GetForegroundWindow();
            if (fg != IntPtr.Zero && !IsOwnWindow(fg))
                _lastExternalForeground = fg;
        }

        private void UninstallForegroundHook()
        {
            if (_winEventHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
            }
        }

        private void WinEventCallback(
            IntPtr hWinEventHook,
            uint @event,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            if (@event == NativeMethods.EVENT_SYSTEM_FOREGROUND)
            {
                if (hwnd != IntPtr.Zero && !IsOwnWindow(hwnd))
                {
                    _lastExternalForeground = hwnd;
                }
            }
        }

        private bool IsOwnWindow(IntPtr hwnd)
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            return pid == (uint)Process.GetCurrentProcess().Id;
        }

        #endregion

        #region 状态显示与清理

        private void ShowStatus(string text, Color? color = null)
        {
            lblStatus.ForeColor = color ?? Color.DimGray;
            lblStatus.Text = text;
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UninstallForegroundHook();
            base.OnFormClosing(e);
        }

        #endregion

        #region Win32

        // 方案A：delegate 提升为 public 以匹配 public P/Invoke
        public delegate void WinEventDelegate(
            IntPtr hWinEventHook,
            uint @event,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime);

        internal static class NativeMethods
        {
            public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
            public const uint WINEVENT_OUTOFCONTEXT = 0;
            public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

            [DllImport("user32.dll")]
            public static extern IntPtr SetWinEventHook(
                uint eventMin,
                uint eventMax,
                IntPtr hmodWinEventProc,
                WinEventDelegate lpfnWinEventProc,
                uint idProcess,
                uint idThread,
                uint dwFlags);

            [DllImport("user32.dll")]
            public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            [DllImport("user32.dll")]
            public static extern bool IsWindow(IntPtr hWnd);
        }

        #endregion
    }
}