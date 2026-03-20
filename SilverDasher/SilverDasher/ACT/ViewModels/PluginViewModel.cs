using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dalamud.Bindings.ImGui;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;
using SilverDasher.ACT.Storages;
using SilverDasher.ACT.Views;

namespace SilverDasher.ACT.ViewModels;

public class PluginViewModel {
	private Dictionary<int, Patch> PatchNames;

	private readonly CheckTreeNode huntRoot;

	private readonly CheckTreeNode fateRoot;

	private readonly Dictionary<string, CheckTreeNode> HuntNodeIndex = new();

	private readonly Dictionary<int, List<CheckTreeNode>> HuntNodesById = new();

	private readonly Dictionary<string, CheckTreeNode> FateNodeIndex = new();

	private readonly Dictionary<int, List<CheckTreeNode>> FateNodesById = new();

	private ObservableCollection<CheckTreeNode> HuntRoot
	{
		get => huntRoot.Nodes;
		init
		{
			huntRoot = new CheckTreeNode("全部狩猎", "hunt-all") {
				Nodes = value
			};
			// NotifyPropertyChanged("HuntRoot");
		}
	}

	private ObservableCollection<CheckTreeNode> FateRoot
	{
		get => fateRoot.Nodes;
		init
		{
			fateRoot = new CheckTreeNode("全部Fate", "fate-all") {
				Nodes = value
			};
			// NotifyPropertyChanged("FateRoot");
		}
	}

	public void SetPatchNames(Dictionary<int, Patch> pin) {
		PatchNames = pin;
	}

	public PluginViewModel() {
		HuntRoot = [];
		FateRoot = [];
	}

	public void ClearAllNodes() {
		HuntRoot.Clear();
		FateRoot.Clear();
		FateNodesById.Clear();
		HuntNodesById.Clear();
		HuntNodeIndex.Clear();
		FateNodeIndex.Clear();
	}

	private CheckTreeNode BuildGroupNode<T>(Dictionary<int, T> itemEntries, ItemGroup itemGroup, string type, Dictionary<int, List<CheckTreeNode>> nodeIndex) where T : GameDynamicObject {
		var checkTreeNode = new CheckTreeNode(itemGroup.Name, $"{type}-group-{itemGroup.Group}");
		foreach (var subGroup in itemGroup.SubGroups)
			checkTreeNode.Add(BuildGroupNode(itemEntries, subGroup, type, nodeIndex));
		foreach (var item in itemGroup.Items) {
			if (!itemEntries.TryGetValue(item, out var value)) continue;
			var checkTreeNode2 = new CheckTreeNode($"{value.Territory.Name}-{value.Name}", $"{type}-gid-{itemGroup.Group}-{value.Id}");
			if (type == "hunt" && Keeper.Config.HuntSubscriptions.Contains(value.Id)) {
				checkTreeNode2.ViewChecked = true;
				checkTreeNode2.ValidateStatus();
			}
			if (type == "fate" && Keeper.Config.FateSubscriptions.Contains(value.Id)) {
				checkTreeNode2.ViewChecked = true;
				checkTreeNode2.ValidateStatus();
			}
			checkTreeNode.Add(checkTreeNode2);
			if (!nodeIndex.TryGetValue(item, out var value2)) continue;
			foreach (var item2 in value2) {
				item2.Related.Add(checkTreeNode2);
				checkTreeNode2.Related.Add(item2);
			}
		}
		checkTreeNode.ValidateStatus();
		return checkTreeNode;
	}

	private CheckTreeNode BuildHuntGroupNode(Dictionary<int, HuntMob> huntEntries, ItemGroup huntGroup) => BuildGroupNode(huntEntries, huntGroup, "hunt", HuntNodesById);

	public void SetupHuntMobs(Dictionary<int, HuntMob> huntEntries, List<ItemGroup> huntGroupsTree, List<int> subs) {
		CheckTreeNode.BUILDING = true;
		List<CheckTreeNode> list = [];
		if (!HuntNodeIndex.TryGetValue("hunt-all-0", out var value))
			HuntRoot.Add(HuntNodeIndex["hunt-all-0"] = value = new CheckTreeNode("全部狩猎", "hunt-all-0"));
		if (!FateNodeIndex.TryGetValue("hunt-allsp", out var value2))
			HuntRoot.Add(HuntNodeIndex["hunt-allsp-0"] = value2 = new CheckTreeNode("狩猎分组", "hunt-allsp-0"));
		foreach (var (key, value3) in huntEntries) {
			if (!HuntNodeIndex.TryGetValue($"hunt-patch-{value3.Patch}", out var value4)) {
				value4 = new CheckTreeNode(PatchNames.TryGetValue(value3.Patch, out var value1) ? value1.Name.ToString() : "未知版本（Unknown）", $"hunt-patch-{value3.Patch}");
				HuntNodeIndex[$"hunt-patch-{value3.Patch}"] = value4;
				value.Add(value4);
				list.Add(value4);
			}
			var checkTreeNode = new CheckTreeNode($"{value3.Territory.Name}-{value3.Rank}-{value3.Name}", $"hunt-id-{key}");
			value4.Add(checkTreeNode);
			HuntNodeIndex[$"hunt-id-{key}"] = checkTreeNode;
			HuntNodesById.TryGetValue(key, out var value5);
			value5 ??= [];
			value5.Add(checkTreeNode);
			HuntNodesById[key] = value5;
		}
		foreach (var item in huntGroupsTree) {
			value2.Add(BuildHuntGroupNode(huntEntries, item));
		}
		CheckTreeNode.BUILDING = false;
		foreach (var sub in subs)
			if (HuntNodeIndex.TryGetValue($"hunt-id-{sub}", out var value6))
				value6.ViewChecked = true;
		foreach (var item2 in list)
			item2.ValidateStatus();
		foreach (var node in value2.Nodes)
			node.ValidateStatus();
	}

	private CheckTreeNode BuildFateGroupNode(Dictionary<int, Fate> fateEntries, ItemGroup fateGroup) => BuildGroupNode(fateEntries, fateGroup, "fate", FateNodesById);

	public void SetupFates(Dictionary<int, Fate> fateEntries, List<ItemGroup> fateGroupsTree, List<int> subs) {
		CheckTreeNode.BUILDING = true;
		List<CheckTreeNode> list = [];
		List<CheckTreeNode> list2 = [];
		if (!FateNodeIndex.TryGetValue("fate-all", out var value))
			FateRoot.Add(FateNodeIndex["fate-all-0"] = value = new CheckTreeNode("全部Fate", "fate-all-0"));
		if (!FateNodeIndex.TryGetValue("fate-allsp", out var value2))
			FateRoot.Add(FateNodeIndex["fate-allsp-0"] = value2 = new CheckTreeNode("全部特殊Fate", "fate-allsp-0"));
		foreach (var (key, value3) in fateEntries) {
			if (!FateNodeIndex.TryGetValue($"fate-patch-{value3.Patch}", out var value4)) {
				value4 = new CheckTreeNode(PatchNames.TryGetValue(value3.Patch, out var value1) ? value1.Name.ToString() : "未知版本（Unknown）", $"fate-patch-{value3.Patch}");
				FateNodeIndex[$"fate-patch-{value3.Patch}"] = value4;
				value.Add(value4);
				list.Add(value4);
			}
			if (!FateNodeIndex.TryGetValue($"fate-map-{value3.TerritoryID}", out var value5)) {
				value5 = new CheckTreeNode(
					!TerritoryStorage.Instance.Contains(value3.TerritoryID) ? "未知地图（Unknown）" : TerritoryStorage.Instance.Get(value3.TerritoryID).Name.Length > 0 ? TerritoryStorage.Instance.Get(value3.TerritoryID).Name : "未知地图（Unknown）",
					$"fate-map-{value3.TerritoryID}");
				FateNodeIndex[$"fate-map-{value3.TerritoryID}"] = value5;
				value4.Add(value5);
				list2.Add(value5);
			}
			var checkTreeNode = new CheckTreeNode($"{value3.Name}", $"fate-id-{key}");
			value5.Add(checkTreeNode);
			FateNodeIndex[$"fate-id-{key}"] = checkTreeNode;
			FateNodesById.TryGetValue(key, out var value6);
			value6 ??= [];
			value6.Add(checkTreeNode);
			FateNodesById[key] = value6;
		}
		foreach (var item in fateGroupsTree)
			value2.Add(BuildFateGroupNode(fateEntries, item));
		CheckTreeNode.BUILDING = false;
		foreach (var sub in subs)
			if (FateNodeIndex.TryGetValue($"fate-id-{sub}", out var value7))
				value7.ViewChecked = true;
		foreach (var item2 in list2) item2.ValidateStatus();
		foreach (var item3 in list) item3.ValidateStatus();
		foreach (var node in value2.Nodes) node.ValidateStatus();
	}

	public void DrawImGui() {
		if (ImGui.CollapsingHeader("狩猎订阅"))
			foreach (var node in HuntRoot)
				node.DrawImGui();
		if (ImGui.CollapsingHeader("Fate订阅"))
			foreach (var node in FateRoot)
				node.DrawImGui();
	}
}