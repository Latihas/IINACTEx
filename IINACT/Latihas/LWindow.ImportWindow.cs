using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Triggernometry.UI.CustomControls;

namespace IINACT.Latihas;

public static partial class LWindow
{
    public class ImportWindow() : Window($"{WindowPrefix}ImportWindow")
    {
        internal static object Tag;
        internal static string str;

        public override void Draw()
        {
            ImGui.InputTextMultiline(
                "##ImportWindowText",
                ref str,
                1145141,
                new Vector2(400, 300)
            );
            if (ImGui.Button("导入")) Import(Tag,str);
        }

        public void Import(object tag,string s)
        {
            str = s;
            Tag = tag;
            var content = "";
            if (s.StartsWith("<?xml")) content = s;
            UserInterface.ImportResultsFromForm(Tag, content);
            UserInterface.BuildTriggerTreeFromConfiguration(null, null);
        }

        public void Open(object obj)
        {
            Tag = obj;
            Plugin.Instance.ImportWindow.IsOpen = true;
        }
    }
}
