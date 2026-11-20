namespace ConsoleApp1.Tests;

/// <summary>Шлях до тимчасового файлу, який видаляється після тесту.</summary>
public sealed class TempFile : IDisposable
{
    public string FilePath { get; } =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

    public void Dispose()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}
