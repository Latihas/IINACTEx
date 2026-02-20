using System;
using Newtonsoft.Json;
using SilverDasher.ACT.Enums;

namespace SilverDasher.ACT.Models;

public class World {
    [JsonIgnore] public int Id;

    [JsonProperty("name")] public string Name;

    [JsonProperty("name_label")] public string Label;

    [JsonProperty("dc")] public string DataCenter;

    [JsonProperty("dc_label")] public string DataCenterLabel;

    [JsonIgnore] public Region Region
    {
        get
        {
            return DataCenterLabel switch {
                "Mana" or "Elemental" or "Gaia" or "Light" or "Crystal" or "Chaos" or "Primal" or "Aether" or "Materia" => Region.Global,
                "LuXingNiao" or "MaoXiaoPang" or "MoGuLi" or "DouDouChai" => Region.China,
                _ => throw new NotImplementedException($"Unable to find Region for DataCenter {DataCenter}, {DataCenterLabel}. ")
            };
        }
    }

    public override string ToString() => Name;
}