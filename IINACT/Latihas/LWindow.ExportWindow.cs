// using Dalamud.Bindings.ImGui;
// using Dalamud.Interface.Windowing;
//
// namespace IINACT.Latihas;
//
// public static partial class LWindow {
// 	public class ExportWindow() : Window($"{WindowPrefix}ExportWindow") {
// 		internal static string exportstr = "";
//
// 		public override void Draw() {
// 			ImGui.InputTextMultiline(
// 				"##ReadOnlyTextArea",
// 				ref exportstr,
// 				exportstr.Length + 8,
// 				ImGui.GetContentRegionAvail(),
// 				ImGuiInputTextFlags.ReadOnly
// 			);
// 		}
//
// 		public void Open(string str) {
// 			exportstr = str;
// 			Plugin.Instance.ExportWindow.IsOpen = true;
// 		}
// 	}
// }

