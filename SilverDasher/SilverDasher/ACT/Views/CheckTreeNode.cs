using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dalamud.Bindings.ImGui;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Views;

public class CheckTreeNode(string name, string id) : UIBinded {
    internal static bool BUILDING;

    private CheckTreeNode Parent;

    internal readonly List<CheckTreeNode> Related = [];

    private bool changing;

    private string Name { get; } = name;

    private string ID { get; } = id;

    public ObservableCollection<CheckTreeNode> Nodes { get; init; } = [];

    public bool? ViewChecked { get; set; } = false;

    public void DrawImGui() {
        var isChecked = ViewChecked ?? false;
        var isPartial = ViewChecked is null;
        ImGui.PushID(ID);
        if (Nodes.Count > 0) {
            bool checkboxChanged;
            if (isPartial) {
                ImGui.PushStyleColor(ImGuiCol.CheckMark, 0xFF888888);
                checkboxChanged = ImGui.Checkbox($"##chk_{ID}", ref isChecked);
                ImGui.PopStyleColor();
            }
            else
                checkboxChanged = ImGui.Checkbox($"##chk_{ID}", ref isChecked);

            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            var nodeOpen = ImGui.TreeNodeEx(Name);
            if (checkboxChanged && !BUILDING) {
                ViewChecked = isChecked;
                OnCheckedChanged(isChecked);
            }

            if (nodeOpen) {
                foreach (var node in Nodes)
                    node.DrawImGui();
                ImGui.TreePop();
            }
        }
        else {
            if (ImGui.Checkbox(Name, ref isChecked) && !BUILDING) {
                ViewChecked = isChecked;
                OnCheckedChanged(isChecked);
            }
        }
        ImGui.PopID();
    }

    private void OnCheckedChanged(bool isChecked) {
        ViewChecked = isChecked;
        foreach (var item in Related) {
            item.ViewChecked = isChecked;
        }
        NotifyPropertyChanged("ViewChecked");
        if (Nodes is { Count: > 0 }) {
            changing = true;
            foreach (var node in Nodes) {
                node.ViewChecked = isChecked;
            }
            changing = false;
        }
        try {
            var paramz = ID.Split('-');
            var id = int.Parse(paramz[2]);
            if (paramz[0] == "hunt")
                if (isChecked) Keeper.Config.HuntSubscriptions.Add(id);
                else Keeper.Config.HuntSubscriptions.RemoveAll(i => i == id);
            if (paramz[0] == "fate")
                if (isChecked) Keeper.Config.FateSubscriptions.Add(id);
                else Keeper.Config.FateSubscriptions.RemoveAll(i => i == id);
            Config.Save();
        }
        catch {
            //
        }
        if (Parent is { changing: false }) Parent.ValidateStatus();
    }

    internal void Add(CheckTreeNode node) {
        if (Nodes.Contains(node) || Parent == node) return;
        node.Parent = this;
        Nodes.Add(node);
        if (!BUILDING && Parent != null) Parent.ValidateStatus();
        NotifyPropertyChanged("Nodes");
    }

    internal void ValidateStatus() {
        var num = 0;
        var num2 = 0;
        foreach (var node in Nodes) {
            switch (node.ViewChecked) {
                case true:
                    num++;
                    break;
                case false:
                    num2++;
                    break;
            }
        }
        if (num == Nodes.Count) ViewChecked = true;
        else if (num2 == Nodes.Count) ViewChecked = false;
        else ViewChecked = null;
    }
}