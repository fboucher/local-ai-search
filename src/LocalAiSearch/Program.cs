using Uno.UI.Runtime.Skia.Gtk;

namespace LocalAiSearch;

class Program
{
    static void Main(string[] args)
    {
        new GtkHost(() => new App()).Run();
    }
}
