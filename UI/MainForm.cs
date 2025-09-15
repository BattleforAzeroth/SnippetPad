using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32; // 监听分辨率/显示设置变化
using SnippetPad.Models;
using SnippetPad.Services;
// 为 WinForms 的 Timer 建立别名，避免和 System.Threading.Timer 冲突
using WinFormsTimer = System.Windows.Forms.Timer;

namespace SnippetPad.UI;

public partial class MainForm : Form
{
    private readonly ConfigService _config;
    private readonly ClipboardService _clipboard;
    private RootConfig _data = new();

    // 紧凑视觉参数（基准值，后续按 DPI 缩放）
    private readonly Font _uiFont = new("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    private const int ButtonHeight = 28;          // 基准按钮高度
    private const int ButtonHPad = 16;            // 文本左右总内边距（基准）
    private const int GroupOuterMargin = 6;       // 组外边距（基准）
    private const int GroupInnerPad = 6;          // 组内边距（基准）
    private readonly WinFormsTimer _statusTimer = new() { Interval = 2000 };

    public MainForm(ConfigService config, ClipboardService clipboard)
    {
        _config = config;
        _clipboard = clipboard;
        InitializeComponent();

        // 使用 DPI 自适应（让控件基础尺寸随 DPI 缩放）
        this.AutoScaleMode = AutoScaleMode.Dpi;

        TopMost = true;
        _statusTimer.Tick += (_, _) => { lblStatus.Text = ""; _statusTimer.Stop(); };

        // 初始定位（确保可见）
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new Point(100, 70);
        EnsureWindowVisible();

        // 首次构建
        LoadDataAndBuild();

        // 窗口大小变化时重建布局，让组宽随窗口宽变化，内部自动换行
        this.SizeChanged += (_, _) => RebuildUI();

        // 当分辨率/显示设置变化时，重新布局并保证窗口在可见区域内
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        // 当 DPI（每显示器缩放比）变化时，重建布局并调整位置
        this.DpiChanged += (_, __) =>
        {
            RebuildUI();
            EnsureWindowVisible();
        };
    }

    private void LoadDataAndBuild()
    {
        _data = _config.LoadOrCreate();
        RebuildUI();

        int groupCount = _data.Groups?.Count ?? 0;
        int btnCount = _data.Groups?.Sum(g => g.Snippets?.Count ?? 0) ?? 0;
        ShowStatus($"已加载 {groupCount} 组 / {btnCount} 个按钮");
    }

    private void ReloadConfig()
    {
        LoadDataAndBuild();
        ShowStatus("配置已刷新");
    }

    private void RebuildUI()
    {
        // 当前 DPI 缩放因子（96 为标准 DPI）
        float dpiScale = this.DeviceDpi / 96f;
        int S(int v) => (int)System.Math.Round(v * dpiScale);

        // 按 DPI 计算的尺寸
        int btnHeight = S(ButtonHeight);
        int btnHPad   = S(ButtonHPad);
        int outerM    = S(GroupOuterMargin);
        int innerP    = S(GroupInnerPad);
        int fudge     = S(8); // 原额外内边距

        flowGroups.SuspendLayout();
        flowGroups.Controls.Clear();

        // flowGroups 可视宽度（随 DPI/窗口宽动态变化）
        int viewportWidth = flowGroups.ClientSize.Width;
        if (viewportWidth <= 0) viewportWidth = this.ClientSize.Width - S(20);

        foreach (var group in _data.Groups)
        {
            // 计算每个 Group 的目标宽度：占满可视区域（减去外边距）
            int groupWidth = System.Math.Max(S(200), viewportWidth - outerM * 2 - 25);

            // Group 容器：固定宽度，防止横向超出
            var container = new Panel
            {
                AutoSize = false,
                Width = groupWidth,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(outerM),
                Padding = new Padding(innerP)
            };

            // 标题
            var lbl = new Label
            {
                Text = string.IsNullOrWhiteSpace(group.Name) ? "(未命名组)" : group.Name,
                AutoSize = true,
                Font = new Font(_uiFont, FontStyle.Bold),
                Location = new Point(4, 4),
                Margin = new Padding(0)
            };
            container.Controls.Add(lbl);

            // 组内内容有效宽度（用于按钮面板）
            int innerWidth = groupWidth - container.Padding.Horizontal - fudge;
            if (innerWidth < S(100)) innerWidth = S(100);

            // 按钮面板：固定宽度 + WrapContents=true，高度自适应 => 按钮自动换行
            var panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,                    // 高度根据内容增长
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new Point(4, lbl.Bottom + 4),
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            // 固定宽度（通过 Min/Max 限制 AutoSize 的横向变化）
            panel.MinimumSize = new Size(innerWidth, 0);
            panel.MaximumSize = new Size(innerWidth, int.MaxValue);
            panel.Width = innerWidth;
            container.Controls.Add(panel);

            // 添加按钮：宽度=文本测量+内边距，且不超过 panel 宽度
            var snippets = group.Snippets ?? new();
            foreach (var s in snippets)
            {
                var title = (s?.Title ?? "").Trim();
                var content = s?.Content ?? "";

                if (string.IsNullOrEmpty(title))
                {
                    title = string.IsNullOrEmpty(content)
                        ? "(未命名)"
                        : (content.Length > 12 ? content[..12] + "..." : content);
                }

                var textSize = TextRenderer.MeasureText(
                    title, _uiFont, new Size(int.MaxValue, btnHeight),
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

                int preferredWidth = textSize.Width + btnHPad;
                // 设一个最小宽度，避免在高 DPI 下文本完全被挤没
                int minBtnWidth = S(50);
                int btnWidth = System.Math.Max(minBtnWidth, System.Math.Min(preferredWidth, innerWidth));

                var btn = new Button
                {
                    Text = title,
                    AutoSize = false,
                    Width = btnWidth,
                    Height = btnHeight,
                    Font = _uiFont,
                    Tag = s,
                    TextAlign = ContentAlignment.MiddleLeft,
                    FlatStyle = FlatStyle.Standard,
                    UseCompatibleTextRendering = false, // 与 TextRenderer 一致，避免 DPI 下渲染不一致
                    AutoEllipsis = true,
                    UseMnemonic = false,
                    Margin = new Padding(S(3))
                };

                // 悬停预览
                if (!string.IsNullOrEmpty(content))
                {
                    var tt = new ToolTip();
                    var preview = content.Length > 150 ? content[..150] + "..." : content;
                    tt.SetToolTip(btn, preview);
                }

                btn.Click += (_, _) => CopySnippet((Snippet)btn.Tag!);

                panel.Controls.Add(btn);
            }

            // 让 panel 完成布局后，再计算容器高度
            panel.PerformLayout();
            container.Height = panel.Bottom + innerP + 4;

            flowGroups.Controls.Add(container);
        }

        flowGroups.ResumeLayout();

        // 布局完成后再次确保窗口可见（在极端缩放下可能有偏移）
        EnsureWindowVisible();
    }

    private void CopySnippet(Snippet s)
    {
        try
        {
            _clipboard.SetText(s.Content ?? "");
            ShowStatus($"已复制：{(string.IsNullOrWhiteSpace(s.Title) ? "无标题" : s.Title)}");
        }
        catch (Exception ex)
        {
            ShowStatus("复制失败：" + ex.Message, Color.IndianRed);
        }
    }

    private void ShowStatus(string text, Color? color = null)
    {
        lblStatus.ForeColor = color ?? Color.DimGray;
        lblStatus.Text = text;
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    // 确保窗口在当前工作区内（分辨率/DPI 改变后可能在屏幕外）
    private void EnsureWindowVisible()
    {
        try
        {
            var screen = Screen.FromControl(this);
            var wa = screen.WorkingArea;

            int x = this.Left;
            int y = this.Top;

            if (this.Right > wa.Right) x = wa.Right - this.Width;
            if (this.Bottom > wa.Bottom) y = wa.Bottom - this.Height;
            if (x < wa.Left) x = wa.Left;
            if (y < wa.Top) y = wa.Top;

            this.Location = new Point(x, y);
        }
        catch
        {
            // 忽略异常，保持当前位置
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (IsDisposed) return;

        if (InvokeRequired)
        {
            try { BeginInvoke(new MethodInvoker(() => { EnsureWindowVisible(); RebuildUI(); })); }
            catch { /* ignore */ }
        }
        else
        {
            EnsureWindowVisible();
            RebuildUI();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // 取消订阅静态事件，避免内存泄漏
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        base.OnFormClosing(e);
    }
}