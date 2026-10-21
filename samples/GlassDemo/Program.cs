using Glasspane;

namespace GlassDemo;

static class Program
{
    internal static bool OpenSettings;

    [STAThread]
    static int Main(string[] args)
    {
        OpenSettings = args.Contains("--settings");
        UiThread.Error += (source, error) => Console.Error.WriteLine($"[{source}] {error}");
        UiThread.Initialize();
        using var window = new DemoWindow();
        return UiThread.Run();
    }
}
