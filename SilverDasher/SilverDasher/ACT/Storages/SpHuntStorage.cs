using System.Collections.Generic;
using System.Linq;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class SpHuntStorage(Keeper kp) : BaseStorage(kp) {
    private readonly HashSet<int> SpHunts = [];

    internal List<ItemGroup> HuntGroupsTree = [];

    private readonly HashSet<ItemGroup> HuntGroupsAll = [];

    internal Dictionary<string, ItemGroup> HuntGroupById = new();

    internal override string ResourceFileName => "sphunts.json";

    internal override void Load() {
        SpHunts.Clear();
        HuntGroupsAll.Clear();
        LoadData(ResourceFileName, out HuntGroupsTree);
        HuntGroupsAll.UnionWith(HuntGroupsTree);
        foreach (var item in HuntGroupsTree) {
            HuntGroupsAll.UnionWith(item.TraverseSubGroups());
            SpHunts.UnionWith(item.TraverseGetItems());
        }
        // foreach (var spHunt in SpHunts) {
        //     Keeper.Mobs.Get(spHunt);
        // }
        HuntGroupById = HuntGroupsAll.ToDictionary(group => group.Group);
    }
}