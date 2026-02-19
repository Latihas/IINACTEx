using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SilverDasher.ACT.Views;

public class CheckTreeNode(string name, string id) : UIBinded {
    internal static bool BUILDING;

    private CheckTreeNode Parent;

    internal readonly List<CheckTreeNode> Related = [];

    private bool changing;

    public string Name { get; } = name;

    public string ID { get; } = id;

    public ObservableCollection<CheckTreeNode> Nodes { get; set; } = [];

    public bool? ViewChecked
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            foreach (var item in Related) {
                item.ViewChecked = value;
            }
            NotifyPropertyChanged("ViewChecked");
            if (Nodes is { Count: > 0 } && field.HasValue) {
                changing = true;
                foreach (var node in Nodes) {
                    node.ViewChecked = field;
                }
                changing = false;
            }
            if (Parent is { changing: false }) Parent.ValidateStatus();
        }
    } = false;

    internal void Add(CheckTreeNode node) {
        if (Nodes.Contains(node) || Parent == node) return;
        node.Parent = this;
        Nodes.Add(node);
        if (!BUILDING && Parent != null) Parent.ValidateStatus();
        NotifyPropertyChanged("Nodes");
    }

    // internal void Add(string name, string id, bool c) {
    //     var node = new CheckTreeNode(name, id) {
    //         isChecked = c
    //     };
    //     Add(node);
    // }

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