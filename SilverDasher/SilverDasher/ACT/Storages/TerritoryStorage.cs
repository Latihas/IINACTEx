using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class TerritoryStorage : BaseStorage<Territory> {
    internal static TerritoryStorage Instance;

    private Dictionary<int, Territory> TerritoryById = new();

    internal override string ResourceFileName => "territories.json";

    internal TerritoryStorage(Keeper kp) : base(kp) {
        Instance = this;
    }

    internal override bool Contains(int id) => TerritoryById.ContainsKey(id);

    internal bool Contains(uint id) => TerritoryById.ContainsKey((int)id);

    internal override Territory Get(int id) => TerritoryById[id];

    // internal IEnumerable<int> Keys() => TerritoryById.Keys;

    internal override void Load() => LoadData(ResourceFileName, out TerritoryById);
}