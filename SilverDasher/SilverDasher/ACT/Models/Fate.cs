using System.Collections.Generic;
using Newtonsoft.Json;

namespace SilverDasher.ACT.Models;

public class Fate : GameDynamicObject {
    [JsonIgnore] public bool Special;

    [JsonIgnore] public int StartTime;

    [JsonIgnore] public int Duration;

    [JsonIgnore] private List<FateGroup> Groups = [];

    [JsonIgnore] internal HuntState State
    {
        get
        {
            return Progress switch {
                0 => HuntState.Healthy,
                < 20 => HuntState.Taunted,
                < 100 => HuntState.Dying,
                _ => HuntState.Died
            };
        }
    }

    private Fate() {
        TypeName = "fate";
    }

    public Fate Clone() =>
        new() {
            Name = Name,
            Level = Level,
            Patch = Patch,
            TerritoryID = TerritoryID,
            Special = Special,
            Groups = Groups
        };
}