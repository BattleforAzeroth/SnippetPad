# SnippetPad（JSON 驱动，无热键）

Windows 端 WinForms 小工具：根据 `snippets.json` 动态生成分组与按钮；点击按钮复制预设文本到剪贴板；支持“置顶”和手动刷新配置。

## 快速使用

release下载exe程序和snippets.json，置于同目录，按需修改json文件

## 说明

本项目由GPT生成，不建议二次开发（不如重头开发）

## 运行

```bash
dotnet run
```

或发布（单文件）：

```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false
```

发布产物在：
```
bin/Release/net9.0-windows/win-x64/publish/
```

## 配置文件（snippets.json）

结构：
```json
{
  "Groups": [
    {
      "Name": "组名",
      "Snippets": [
        { "Title": "按钮标题", "Content": "复制内容文本" }
      ]
    }
  ]
}
```

- 可直接修改 `snippets.json`，在程序中点击“刷新”按钮即可重载。

## 常见问题

- 复制失败？
  - 少数情况下剪贴板被其他程序暂时占用，已内置重试。如仍报错，稍后重试或关闭占用剪贴板的软件。
