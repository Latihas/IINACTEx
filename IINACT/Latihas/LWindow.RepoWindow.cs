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
        }

        public void Open(Repository repository) {
            Repository = repository;
            Plugin.Instance.RepoWindow.IsOpen = true;
        }
    }
}