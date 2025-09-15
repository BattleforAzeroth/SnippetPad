namespace SnippetPad.Models;

public class SnippetGroup
{
    public string Name { get; set; } = "";
    public List<Snippet> Snippets { get; set; } = new();
}

public class RootConfig
{
    public List<SnippetGroup> Groups { get; set; } = new();
}