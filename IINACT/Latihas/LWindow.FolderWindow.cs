using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Triggernometry.Core;

namespace IINACT.Latihas;

public static partial class LWindow {
	public class FolderWindow() : Window($"{WindowPrefix}FolderWindow") {
		private static Folder? Folder;

		public override void Draw() {
			if (Folder == null) return;
			ImGui.Text("Id: ");
			ImGui.SameLine();
			var id = Folder.Id.ToString();
			if (ImGui.Button(id)) ImGui.SetClipboardText(id);
			var Name = Folder.Name;
			if (ImGui.InputText("名称", ref Name))
				Folder.Name = Name;
			var ZoneFilterEnabled = Folder.ZoneFilterEnabled;
			if (ImGui.Checkbox("限制区域名(正则)", ref ZoneFilterEnabled))
				Folder.ZoneFilterEnabled = ZoneFilterEnabled;
			var ZoneRegex = Folder.ZoneRegex;
			if (ImGui.InputText("区域名称", ref ZoneRegex))
				Folder.ZoneRegex = ZoneRegex;
			var FFXIVZoneFilterEnabled = Folder.FFXIVZoneFilterEnabled;
			if (ImGui.Checkbox("限制区域ID(正则)", ref FFXIVZoneFilterEnabled))
				Folder.FFXIVZoneFilterEnabled = FFXIVZoneFilterEnabled;
			var FfxivZoneIdRegex = Folder.FfxivZoneIdRegex;
			if (ImGui.InputText("区域ID", ref FfxivZoneIdRegex))
				Folder.FfxivZoneIdRegex = FfxivZoneIdRegex;

			var EventFilterEnabled = Folder.EventFilterEnabled;
			if (ImGui.Checkbox("限制文本匹配(正则)", ref EventFilterEnabled))
				Folder.EventFilterEnabled = EventFilterEnabled;
			var EventRegex = Folder.EventRegex;
			if (ImGui.InputText("文本", ref EventRegex))
				Folder.EventRegex = EventRegex;

			// var FFXIVJobFilterEnabled = Folder.FFXIVJobFilterEnabled != null && bool.Parse(Folder.FFXIVJobFilterEnabled);
			// if (ImGui.Checkbox("限制职业", ref FFXIVJobFilterEnabled))
			//     Folder.FFXIVJobFilterEnabled = FFXIVJobFilterEnabled.ToString();
			ImGui.Text("环境");
			var RawEnvironmentVariables = Folder.RawEnvironmentVariables;
			if (ImGui.InputTextMultiline("## 环境", ref RawEnvironmentVariables, 1145141, new Vector2(-1, -1)))
				Folder.RawEnvironmentVariables = RawEnvironmentVariables;
		}

		public void Open(Folder folder) {
			Folder = folder;
			Plugin.Instance.FolderWindow.IsOpen = true;
		}
	}
}