using System.Threading;

namespace SnippetPad.Services;

public class ClipboardService
{
    public void SetText(string text, int retry = 3)
    {
        for (int i = 0; i < retry; i++)
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(text);
                return;
            }
            catch
            {
                Thread.Sleep(40);
            }
        }
        throw new Exception("无法访问剪贴板，请重试。");
    }
}