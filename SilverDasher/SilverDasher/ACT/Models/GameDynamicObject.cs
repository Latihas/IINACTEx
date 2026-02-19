using Newtonsoft.Json;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT.Models;

public abstract class GameDynamicObject {
    private static readonly Territory DefaultMultiTerritory = new() {
        Content = 0,
        IsDataCenterMap = false,
        Name = "",
        Region = ""
    };

    [JsonIgnore] internal BaseStorage Storage;

    [JsonIgnore] internal int Id;

    [JsonProperty("name")] public LocalizedEntry Name;

    [JsonProperty("level")] public int Level;

    [JsonProperty("patch")] public int Patch;

    [JsonProperty("territory")] public uint TerritoryID;

    [JsonProperty("coords")] internal Coordinate Coordinate;

    [JsonIgnore] public int Progress;

    [JsonIgnore] public uint Instance;

    internal string TypeName;

    [JsonIgnore] public Territory Territory
    {
        get
        {
            if (TerritoryID == 0) return DefaultMultiTerritory;
            Storage.Keeper.Territories.TryGet((int)TerritoryID, out var item);
            return item;
        }
    }
}