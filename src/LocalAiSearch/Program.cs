// Skia desktop entry point for net10.0 (Mac + Linux).
// Windows uses the WinUI-generated entry point; this file is excluded there.
#if !WINDOWS
using Uno.UI.Runtime.Skia;

namespace LocalAiSearch;

class Program
{
    static void Main(string[] args)
    {
        var host = SkiaHostBuilder.Create()
            .App(() => new App())
            .UseDesktop()
            .Build();

        host.Run();
    }
}
#endif
