using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface.Windowing;

namespace IINACT.Latihas;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class LWindow
{
    private static void Start(string cmd) => Process.Start(new ProcessStartInfo(cmd)
    {
        UseShellExecute = true
    });
    
    public class NewFolderWindow() : Window($"{WindowPrefix}NewFolderWindow")
    {
        public override void Draw()
        {
            //TODO
        }
    }
}
