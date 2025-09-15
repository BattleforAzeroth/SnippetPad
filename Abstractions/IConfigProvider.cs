using SnippetPad.Models;

namespace SnippetPad.Abstractions;

public interface IConfigProvider
{
    RootConfig Load();
    void Save(RootConfig cfg);
    string Path { get; }
}