using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class WorldStorage(Keeper kp) : BaseStorage<World>(kp) {
    private Dictionary<int, World> WorldByID = new();

    private readonly Dictionary<string, World> WorldByLabel = new();

    internal override string ResourceFileName => "worlds.json";

    internal override World Get(int id) => WorldByID[id];

    internal World GetByLabel(string label) => WorldByLabel[label];

    internal bool TryGetByLabel(string label, out World w) => WorldByLabel.TryGetValue(label, out w);

    private Dictionary<int, World>.KeyCollection Keys() => WorldByID.Keys;

    internal override bool Contains(int id) => WorldByID.ContainsKey(id);

    internal override void Load() {
        LoadData(ResourceFileName, out WorldByID);
        foreach (var item in Keys()) {
            var world = Get(item);
            world.Id = item;
            WorldByLabel[world.Label] = world;
        }
    }
}