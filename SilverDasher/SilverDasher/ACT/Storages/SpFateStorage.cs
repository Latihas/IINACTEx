using System.Collections.Generic;
using System.Linq;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class SpFateStorage(Keeper kp) : BaseStorage(kp) {
    internal readonly HashSet<int> Spfates = [];

    internal List<ItemGroup> FateGroupsTree = [];

    private readonly HashSet<ItemGroup> FateGroupsAll = [];

    internal Dictionary<string, ItemGroup> FateGroupById = new();

    internal override string ResourceFileName => "spfates.json";

    internal override void Load() {
        Spfates.Clear();
        FateGroupsAll.Clear();
        LoadData(ResourceFileName, out FateGroupsTree);
        FateGroupsAll.UnionWith(FateGroupsTree);
        foreach (var item2 in FateGroupsTree) {
            FateGroupsAll.UnionWith(item2.TraverseSubGroups());
            Spfates.UnionWith(item2.TraverseGetItems());
        }
        foreach (var spfate in Spfates)
            if (Keeper.Fates.TryGet(spfate, out var item))
                item.Special = true;
        FateGroupById = FateGroupsAll.ToDictionary(group => group.Group);
    }
}