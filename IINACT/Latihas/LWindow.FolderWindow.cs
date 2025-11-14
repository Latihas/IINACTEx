using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Triggernometry;

namespace IINACT.Latihas;

public static partial class LWindow
{
    public class FolderWindow() : Window($"{WindowPrefix}FolderWindow")
    {
        private static Folder? Folder;

        public override void Draw()
        {if(Folder==null)return;
            ImGui.Text("Id: ");
            ImGui.SameLine();
            var id = Folder.Id.ToString();
            if (ImGui.Button(id)) ImGui.SetClipboardText(id);
            var Name = Folder.Name;
            if (ImGui.InputText("名称", ref Name))
                Folder.Name = Name;
            var ZoneFilterEnabled = Folder.ZoneFilterEnabled != null && bool.Parse(Folder.ZoneFilterEnabled);
            if (ImGui.Checkbox("限制区域名(正则)", ref ZoneFilterEnabled))
                Folder.ZoneFilterEnabled = ZoneFilterEnabled.ToString();
            var ZoneFilterRegularExpression = Folder.ZoneFilterRegularExpression;
            if (ImGui.InputText("区域名称", ref ZoneFilterRegularExpression))
                Folder.ZoneFilterRegularExpression = ZoneFilterRegularExpression;

            var FFXIVZoneFilterEnabled = Folder.FFXIVZoneFilterEnabled != null && bool.Parse(Folder.FFXIVZoneFilterEnabled);
            if (ImGui.Checkbox("限制区域ID(正则)", ref FFXIVZoneFilterEnabled))
                Folder.FFXIVZoneFilterEnabled = FFXIVZoneFilterEnabled.ToString();
            var FfxivZoneFilterRegularExpression = Folder.FfxivZoneFilterRegularExpression;
            if (ImGui.InputText("区域ID", ref FfxivZoneFilterRegularExpression))
                Folder.FfxivZoneFilterRegularExpression = FfxivZoneFilterRegularExpression;

            var EventFilterEnabled = Folder.EventFilterEnabled != null && bool.Parse(Folder.EventFilterEnabled);
            if (ImGui.Checkbox("限制文本匹配(正则)", ref EventFilterEnabled))
                Folder.EventFilterEnabled = EventFilterEnabled.ToString();
            var EventFilterRegularExpression = Folder.EventFilterRegularExpression;
            if (ImGui.InputText("文本", ref EventFilterRegularExpression))
                Folder.EventFilterRegularExpression = EventFilterRegularExpression;
            
            // var FFXIVJobFilterEnabled = Folder.FFXIVJobFilterEnabled != null && bool.Parse(Folder.FFXIVJobFilterEnabled);
            // if (ImGui.Checkbox("限制职业", ref FFXIVJobFilterEnabled))
            //     Folder.FFXIVJobFilterEnabled = FFXIVJobFilterEnabled.ToString();
            ImGui.SetNextItemWidth(-1);
            var RawEnvironmentVariables = Folder.RawEnvironmentVariables;
            if (ImGui.InputTextMultiline("环境", ref RawEnvironmentVariables))
                Folder.RawEnvironmentVariables = RawEnvironmentVariables;
        }

        public void Open(Folder folder)
        {
            Folder = folder;
            Plugin.Instance.FolderWindow.IsOpen = true;
        }
    }
}
