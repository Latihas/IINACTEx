using System.Collections.Generic;
using Newtonsoft.Json;

namespace SilverDasher.ACT.Models;

public class ItemGroup {
    [JsonProperty("group")] public string Group;

    [JsonProperty("name")] public string Name;

    [JsonProperty("items")] public List<int> Items = [];

    [JsonProperty("subGroups")] public List<ItemGroup> SubGroups = [];

    internal HashSet<ItemGroup> TraverseSubGroups() {
        HashSet<ItemGroup> hashSet = [];
        if (SubGroups.Count <= 0) return hashSet;
        foreach (var subGroup in SubGroups) {
            hashSet.Add(subGroup);
            hashSet.UnionWith(subGroup.TraverseSubGroups());
        }
        return hashSet;
    }

    internal HashSet<int> TraverseGetItems() {
        HashSet<int> hashSet = [];
        hashSet.UnionWith(Items);
        foreach (var item in TraverseSubGroups()) hashSet.UnionWith(item.Items);
        return hashSet;
    }
}