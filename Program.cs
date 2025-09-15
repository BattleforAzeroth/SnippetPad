using SnippetPad.Services;
using SnippetPad.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // .NET 6+ WinForms 初始化（.NET 9 同样适用）
        ApplicationConfiguration.Initialize();

        // 依赖注入/服务实例化
        var configService = new ConfigService();     // 默认读取 EXE 同目录的 snippets.json
        var clipboardService = new ClipboardService();

        // 启动主窗体
        Application.Run(new MainForm(configService, clipboardService));
    }
}