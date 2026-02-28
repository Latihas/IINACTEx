using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SilverDasher.ACT.Enums;
using SilverDasher.ACT.Models;
using SilverDasher.ACT.Models.Messages;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT.Doppelgangers;

internal class Keeper : Doppelganger {
    internal static string PlayerName => SilverDasher.ObjectTable.LocalPlayer == null ? "" : SilverDasher.ObjectTable.LocalPlayer.Name.ToString();

    internal static string PlayerWorld => SilverDasher.ObjectTable.LocalPlayer == null ? "" : SilverDasher.ObjectTable.LocalPlayer.HomeWorld.Value.Name.ToString();

    internal static uint PlayerWorldID => SilverDasher.ObjectTable.LocalPlayer == null ? 0 : SilverDasher.ObjectTable.LocalPlayer.HomeWorld.RowId;

    // internal string CurrentWorld;

    private static uint NetworkMapID => CurrentWorldID;

    internal static uint CurrentInstance => SilverDasher.ClientState.Instance;
    internal static uint CurrentWorldID => SilverDasher.ObjectTable.LocalPlayer == null ? 0 : SilverDasher.ObjectTable.LocalPlayer.CurrentWorld.RowId;

    internal static uint CurrentMapID => SilverDasher.ClientState.TerritoryType;
    // internal uint NetworkInstance = 255u;

    // internal uint ChatLogInstance = 255u;

    internal bool RUNNING = true;

    internal readonly Dictionary<int, Fate> CurrentFates = new();

    internal readonly ConcurrentDictionary<string, HuntMob> CurrentMobs = new();

    private readonly Dictionary<(string, int, int), HuntMob> ReceivedMobs = new();

    private readonly Dictionary<(string, int, int), Fate> ReceivedFates = new();

    internal readonly FateStorage Fates;

    internal readonly SpFateStorage SpFates;

    internal readonly SpHuntStorage SpHunts;

    internal readonly OpcodeStorage Opcodes;

    internal readonly MobStorage Mobs;

    internal readonly WorldStorage Worlds;

    internal readonly TerritoryStorage Territories;

    internal readonly PatchStorage Patches;

    internal static Config Config;


    private Territory CurrentTerritory
    {
        get
        {
            Territories.TryGet((int)CurrentMapID, out var item);
            return item;
        }
    }

    public Keeper(SilverDasher self) : base(self) {
        Fates = new FateStorage(this);
        SpFates = new SpFateStorage(this);
        Opcodes = new OpcodeStorage(this);
        Mobs = new MobStorage(this);
        SpHunts = new SpHuntStorage(this);
        Worlds = new WorldStorage(this);
        Territories = new TerritoryStorage(this);
        Patches = new PatchStorage(this);
    }

    internal override void Init() {
        Fates.Load();
        SpFates.Load();
        Opcodes.Load();
        Mobs.Load();
        SpHunts.Load();
        Worlds.Load();
        Territories.Load();
        Patches.Load();
        Config = Config.Load();
    }

    internal override void Deinit() => Config.Save(Config);

    // internal void ConfigPropertyChanged(object sender, PropertyChangedEventArgs e) {
    //     var config = sender as Config;
    //     switch (e.PropertyName) {
    //         case "UseExtraServer":
    //             Utils.ShowMessageBox("暂不开放。");
    //             break;
    //         case "ServerSelection":
    //             if (config.ServerSelection <= 0 && DataStorage.SelectedNest != config.ServerSelection) {
    //                 DataStorage.SelectedNest = config.ServerSelection;
    //                 Self.RestartLoop();
    //             }
    //             break;
    //     }
    // }

    internal World GetCurrentWorld() => Worlds.Get(CurrentWorldID);

    // internal World GetPlayerWorld() => Worlds.Get(PlayerWorldID);

    internal void FateUpdate(int fateId, int progress = -1, Coordinate coords = null, uint startTime = 0u, uint duration = 0u) {
        if (!CurrentFates.TryGetValue(fateId, out var value)) {
            try {
                if (!Fates.Contains(fateId)) return;
                value = Fates.Get(fateId);
                CurrentFates.Add(fateId, value);
            }
            catch {
                //
            }
        }
        if (value == null) return;
        if (value.Coordinate == null && coords != null) value.Coordinate = coords;
        switch (progress) {
            case 100:
                CurrentFates.Remove(fateId);
                ReportFateEnd(fateId);
                return;
            default:
                value.Progress = progress;
                break;
            case -1:
                break;
        }
        if (startTime != 0 && value.StartTime != (int)startTime) value.StartTime = (int)startTime;
        if (duration != 0 && value.Duration != (int)duration) value.Duration = (int)duration;
    }

    internal void MobUpdate(int mobId, int hp, Coordinate coords, uint territory, uint instance) {
        var key = $"{territory}-{mobId}-{instance}";
        if (!CurrentMobs.TryGetValue(key, out var value)) {
            if (!Mobs.TryGet(mobId, out value)) {
                Log($"Illegal mob id {mobId} received from ACT. Dropped.");
                return;
            }
            value.Coordinate = coords;
            value.Instance = instance;
            if (value.TerritoryID == 0 && value.Rank is Rank.SS or Rank.SSMinion) {
                if (NetworkMapID == 0 || NetworkMapID != CurrentMapID) return;
                value.TerritoryID = territory;
            }
            CurrentMobs.TryAdd(key, value);
        }
        if (hp != 0) {
            value.Health = hp;
            return;
        }
        ReportMobDied(value);
        CurrentMobs.TryRemove(key, out _);
    }

    internal void ReceivedMobUpdate(World world, int map, int instance, int mobId, int hp, Coordinate coords) {
        (string, int, int) key = (world.Label, instance, mobId);
        if (!ReceivedMobs.TryGetValue(key, out var value)) {
            if (!Mobs.TryGet(mobId, out value)) {
                Log($"Mob not found. {mobId}. Maybe you should restart your ACT to update data.");
                return;
            }
            value = value.Clone();
            value.Coordinate = coords;
            value.Health = hp;
            value.TerritoryID = (uint)map;
            ReceivedMobs.Add(key, value);
            Notifier.NotifyMobStatusChanged(world.Label, instance, value);
            Notifier.WriteLogline(world, instance, value);
        }
        else {
            value.Coordinate = coords;
            if (value.State != MobStorage.GetState(hp)) {
                value.Health = hp;
                Notifier.NotifyMobStatusChanged(world.Label, instance, value);
            }
            if (hp != 0) {
                value.Health = hp;
            }
            Notifier.WriteLogline(world, instance, value);
        }
    }

    internal void ReceivedFateUpdate(World world, int instance, int fateId, int hp, Coordinate coords) {
        (string, int, int) key = (world.Label, instance, fateId);
        if (!ReceivedFates.TryGetValue(key, out var value)) {
            if (!Fates.TryGet(fateId, out value)) {
                Log($"Fate not found. {fateId}. Maybe you should restart your ACT to update data.");
                return;
            }
            value = value.Clone();
            value.Coordinate = coords;
            value.Progress = hp;
            ReceivedFates.Add(key, value);
            Notifier.NotifyFateStatusChanged(world.Label, instance, value);
            Notifier.WriteLogline(world, instance, value);
            return;
        }
        value.Coordinate = coords;
        if (value.State != FateStorage.GetState(hp)) {
            value.Progress = hp;
            Notifier.NotifyFateStatusChanged(world.Label, instance, value);
        }
        if (hp != 100) {
            value.Progress = hp;
        }
        Notifier.WriteLogline(world, instance, value);
    }

    private void ReportFateEnd(int fateId) {
        Messager.QueueMessage(new FateMessage {
            id = fateId,
            Progress = 100,
            instance = CurrentInstance,
            world = CurrentWorldID
        });
    }

    private void ReportMobDied(HuntMob mob) {
        Messager.QueueMessage(new HuntMessage {
            id = mob.Id,
            health = 0,
            coordinate = mob.Coordinate,
            map = mob.TerritoryID,
            instance = mob.Instance,
            world = CurrentWorldID
        });
    }

    internal void ReportFateStatus() {
        foreach (var key in CurrentFates.Keys) {
            if (CurrentFates.TryGetValue(key, out var fate))
                Messager.QueueMessage(new FateMessage {
                    id = key,
                    Progress = fate.Progress,
                    coordinate = fate.Coordinate,
                    map = fate.TerritoryID,
                    instance = CurrentInstance,
                    world = CurrentWorldID
                });
        }
    }

    internal void ReportMobStatus() {
        foreach (var key in CurrentMobs.Keys) {
            if (CurrentMobs.TryGetValue(key, out var huntMob))
                Messager.QueueMessage(new HuntMessage {
                    id = huntMob.Id,
                    health = huntMob.Health,
                    coordinate = huntMob.Coordinate,
                    map = huntMob.TerritoryID,
                    instance = huntMob.Instance,
                    world = CurrentWorldID
                });
        }
    }

    internal bool InDuty() {
        try {
            if (CurrentTerritory is { IsDataCenterMap: true }) return false;
            return CurrentTerritory is not { Content: 0 };
        }
        catch {
            return false;
        }
    }

    internal List<BaseStorage> GetStorages() => [Worlds, Territories, Patches, Opcodes, Mobs, SpHunts, Fates, SpFates];
}