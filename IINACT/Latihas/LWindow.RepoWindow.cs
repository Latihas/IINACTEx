using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Triggernometry.Core;

namespace IINACT.Latihas;

public static partial class LWindow {
	public class RepoWindow() : Window($"{WindowPrefix}RepoWindow") {
		internal static Repository? Repository;

		public override void Draw() {
			if (Repository == null) return;
			ImGui.Text("Id: ");
			ImGui.SameLine();
			var id = Repository.Id.ToString();
			if (ImGui.Button(id)) ImGui.SetClipboardText(id);
			var Name = Repository.Name;
			if (ImGui.InputText("名称", ref Name))
				Repository.Name = Name;
			var Address = Repository.Address;
			if (ImGui.InputText("链接", ref Address))
				Repository.Address = Address;
			var AllowProcessLaunch = Repository.AllowProcessLaunch;
			if (ImGui.Checkbox("允许启动进程", ref AllowProcessLaunch))
				Repository.AllowProcessLaunch = AllowProcessLaunch;
			var AllowScriptExecution = Repository.AllowScriptExecution;
			if (ImGui.Checkbox("允许执行代码", ref AllowScriptExecution))
				Repository.AllowScriptExecution = AllowScriptExecution;
			var AllowObsControl = Repository.AllowObsControl;
			if (ImGui.Checkbox("允许控制OBS", ref AllowObsControl))
				Repository.AllowObsControl = AllowObsControl;
			var AllowDiskOperations = Repository.AllowDiskOperations;
			if (ImGui.Checkbox("允许操作文件", ref AllowDiskOperations))
				Repository.AllowDiskOperations = AllowDiskOperations;
			var AllowWindowMessages = Repository.AllowWindowMessages;
			if (ImGui.Checkbox("允许Windows Message", ref AllowWindowMessages))
				Repository.AllowWindowMessages = AllowWindowMessages;
		}

		public void Open(Repository repository) {
			Repository = repository;
			Plugin.Instance.RepoWindow.IsOpen = true;
		}
	}
}