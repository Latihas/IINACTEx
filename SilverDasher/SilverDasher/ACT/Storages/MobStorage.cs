using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Enums;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class MobStorage(Keeper kp) : BaseStorage<HuntMob>(kp) {
    internal Dictionary<int, HuntMob> HuntByBnpcNameID = new();

    private readonly Dictionary<(int, Rank), List<int>> HuntsByPatchRank = new();

    internal override string ResourceFileName => "hunts.json";

    internal override HuntMob Get(int id) => HuntByBnpcNameID[id];

    // internal IEnumerable<int> Keys() => HuntByBnpcNameID.Keys;

    // internal List<int> GetHuntsByPatchRank(int patch, Rank rank) {
    //     HuntsByPatchRank.TryGetValue((patch, rank), out var value);
    //     return value;
    // }

    internal override bool Contains(int id) => HuntByBnpcNameID.ContainsKey(id);

    internal List<string> Query(int patch = 0, Rank rank = Rank.Unknown, int map = 0) {
        List<string> list = [];
        foreach (var key in HuntByBnpcNameID.Keys) {
            var huntMob = HuntByBnpcNameID[key];
            var flag = true;
            if (patch != 0) flag = huntMob.Patch == patch;
            if (rank != Rank.Unknown) flag = huntMob.Rank == rank;
            if (map != 0) flag = huntMob.TerritoryID == map;
            if (flag) list.Add(key.ToString());
        }
        return list;
    }

    internal static HuntState GetState(int health) => health switch {
        100 => HuntState.Healthy,
        > 95 => HuntState.Taunted,
        > 0 => HuntState.Dying,
        _ => HuntState.Died
    };


    internal static string GetStateName(HuntState state) => state switch {
        HuntState.Healthy => "健康",
        HuntState.Taunted => "已开怪",
        HuntState.Dying => "被暴打中",
        HuntState.Died => "挂了",
        HuntState.Unknown => "",
        _ => "不见了"
    };


    internal override void Load() {
        LoadData(ResourceFileName, out HuntByBnpcNameID);
        foreach (var key in HuntByBnpcNameID.Keys) {
            var huntMob = HuntByBnpcNameID[key];
            huntMob.Id = key;
            huntMob.Storage = this;
            if (!HuntsByPatchRank.TryGetValue((huntMob.Patch, huntMob.Rank), out var value)) {
                value = [];
                HuntsByPatchRank[(huntMob.Patch, huntMob.Rank)] = value;
            }
            value.Add(key);
        }
    }
}