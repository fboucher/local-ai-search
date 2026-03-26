// Skia desktop entry point for net10.0 (Mac + Linux via GTK).
// Windows uses the WinUI-generated entry point; this file is excluded there.
#if !WINDOWS
using Uno.UI.Runtime.Skia.Gtk;

namespace LocalAiSearch;

class Program
{
    static void Main(string[] args)
    {
        new GtkHost(() => new App()).Run();
    }
}
#endif
