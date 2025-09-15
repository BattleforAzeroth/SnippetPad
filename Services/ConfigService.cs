using System.Text.Json;
using System.Text.Json.Serialization;
using SnippetPad.Models;

namespace SnippetPad.Services;

public class ConfigService
{
    public string ConfigPath { get; }
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigService(string? path = null)
    {
        // 程序运行时会从输出目录读取，例如 bin/Debug/.../
        ConfigPath = path ?? Path.Combine(AppContext.BaseDirectory, "snippets.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true // 允许 title/content 小写等变体
        };
    }

    public RootConfig LoadOrCreate()
    {
        if (!File.Exists(ConfigPath))
        {
            var demo = CreateDefaultFromSpec();
            Save(demo);
            return demo;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<RootConfig>(json, _jsonOptions) ?? new RootConfig();
        }
        catch
        {
            return new RootConfig();
        }
    }

    public void Save(RootConfig cfg)
    {
        var json = JsonSerializer.Serialize(cfg, _jsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    // 省略 CreateDefaultFromSpec，与之前相同
    private RootConfig CreateDefaultFromSpec() => new()
    {
        // ... 你的默认分组（与之前相同）
        Groups = new()
        {
            // 示例分组略
        }
    };
}