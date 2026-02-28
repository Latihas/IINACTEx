using System;
using System.Runtime.InteropServices;
using Advanced_Combat_Tracker;
using Dalamud.Interface.ImGuiNotification;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SilverDasher.ACT.Models;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT.Doppelgangers;

internal partial class Notifier(SilverDasher self) : Doppelganger(self) {
    private readonly JsonSerializerSettings unpackSettings = new() {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = StrNullToEmptyContractResolver.DefaultInstance
    };

    internal override void Init() {
    }

    internal override void Deinit() {
    }

    internal void Unpack(string topic, string message) {
        dynamic val = JsonConvert.DeserializeObject<JObject>(message, unpackSettings);
        var array = topic.Split('/');
        var text = array[1];
        if (!Keeper.Worlds.TryGetByLabel(text, out var w)) {
            Log($"Invalid world {text} for topic {topic}.");
            return;
        }
        var text2 = array[2];
        var s = array[3];
        switch (text2) {
            case "hunt": {
                var num = int.Parse(s);
                var map = (int)val.m.Value;
                var num2 = 0;
                var hp = 100;
                Coordinate coords = null;
                try {
                    num2 = (int)val.i.Value;
                    if (val.c != null) {
                        var coordinate = new Coordinate {
                            x = (int)val.c.x.Value,
                            y = (int)val.c.y.Value
                        };
                        coords = coordinate;
                    }
                    if (val.hp != null) {
                        hp = (int)val.hp.Value;
                    }
                }
                catch (Exception) {
                    //
                }
                Keeper.ReceivedMobUpdate(w, map, num2, num, hp, coords);
                if (!Keeper.Config.ExtendedReport) return;
                var s2 = $"{w.Name} - {val.m.Value}{num2} - {num}";
                Log(s2);
                break;
            }
            case "fate": {
                var num3 = int.Parse(s);
                // var map2 = (int)val.m.Value;
                var num4 = 0;
                var hp2 = 0;
                Coordinate coords2 = null;
                try {
                    num4 = (int)val.i.Value;
                    if (val.c != null) {
                        var coordinate = new Coordinate {
                            x = (int)val.c.x.Value,
                            y = (int)val.c.y.Value
                        };
                        coords2 = coordinate;
                    }
                    if (val.hp != null) {
                        hp2 = 100 - (int)val.hp.Value;
                    }
                }
                catch (Exception) {
                    //
                }
                Keeper.ReceivedFateUpdate(w, num4, num3, hp2, coords2);
                if (!Keeper.Config.ExtendedReport) return;
                var s3 = $"{w.Name} - {val.m.Value}{num4} - {num3}";
                Log(s3);
                break;
            }
            default:
                Log($"Unknown packet type {text2}.");
                break;
        }
    }

    public void NotifyMobStatusChanged(string worldId, int instance, HuntMob mob) {
        Notify(worldId, instance, mob, mob.State);
    }

    public void NotifyFateStatusChanged(string worldId, int instance, Fate fate) {
        Notify(worldId, instance, fate, fate.State);
    }

    private void Notify(string worldId, int instance, GameDynamicObject gobj, HuntState status) {
        if (Keeper.InDuty() && Keeper.Config.PauseInDuty) {
            return;
        }
        var byLabel = Keeper.Worlds.GetByLabel(worldId);
        var text = "";
        if (!Keeper.Territories.TryGet((int)gobj.TerritoryID, out var item)) {
            Log($"处理信息时收到未能识别的地图编码。{gobj.TerritoryID}");
            return;
        }
        if (instance > 0) {
            text = $"[{instance}]";
        }
        var text2 = !item.IsDataCenterMap ? $"{byLabel.Name} - {item}{text} - {gobj.Name}" : $"{byLabel.DataCenter} - {item}{text} - {gobj.Name}";
        var coord = "";
        if (gobj.Coordinate != null)
            coord = $"({gobj.Coordinate.displayX}, {gobj.Coordinate.displayY}) ";
        SendToast(text2, status, coord);
        SendTTS(text2, status, coord);
        Log($"已尝试推送 {text2}, {MobStorage.GetStateName(status)}");
    }

    public void WriteLogline(World world, int instance, GameDynamicObject gobj) {
        if (!Keeper.Config.WriteACTLog) return;
        var logLine = "";
        var territory = Keeper.Territories.Get(gobj.TerritoryID);
        logLine = gobj switch {
            HuntMob huntMob =>
                $"00|{DateTime.Now:O}|0|SilverDasher|{huntMob.TypeName}|{huntMob.Id}|{huntMob.Name}|{world.Id}|{world.Name}|{huntMob.TerritoryID}|{territory}|{instance}|{huntMob.RankRaw}|{huntMob.Coordinate?.displayX}|{huntMob.Coordinate?.displayY}|{huntMob.Progress}",
            Fate fate => $"00|{DateTime.Now:O}|0|SilverDasher|{fate.TypeName}|{fate.Id}|{fate.Name}|{world.Id}|{world.Name}|{fate.TerritoryID}|{territory}|{instance}|{fate.Special}|{fate.Coordinate?.displayX}|{fate.Coordinate?.displayY}|{fate.Progress}",
            _ => logLine
        };
        // ActGlobals.oFormActMain.ParseRawLogLine(isImport: false, DateTime.Now, logLine);
        ActGlobals.oFormActMain.ParseRawLogLine(logLine);
    }

    private static void SendTTS(string message, HuntState status, string coord) {
        if (!Keeper.Config.TTS || !Keeper.Config.StatusPushable("TTS", status)) return;
        if (Keeper.Config.TTSExtend) message += coord;
        ActGlobals.oFormActMain.TTS(message + MobStorage.GetStateName(status));
    }
#pragma warning disable SYSLIB1054,CA2101
    [DllImport("winmm.dll")]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);
#pragma warning restore SYSLIB1054,CA2101

    private void SendToast(string message, HuntState status, string coord = "", bool checkPermit = true) {
        lock (this) {
            if (checkPermit && (!Keeper.Config.SystemToast || !Keeper.Config.StatusPushable("Toast", status))) return;
            SilverDasher.NotificationManager.AddNotification(new Notification {
                Content = message,
                Title = coord + MobStorage.GetStateName(status)
            });
            try {
                PlaySound("Notification.Default", IntPtr.Zero, 0x10000 | 0x0001 | 0x0002);
            }
            catch {
                //
            }
        }
    }

    public static void TestTTS() => ActGlobals.oFormActMain.TTS("亚以太利斯 - 艾欧泽亚 - 光之战士 (1.0，1.0) 健康");
    public void TestToast() => SendToast("亚以太利斯 - 艾欧泽亚 - 光之战士", HuntState.Healthy, "(1.0，1.0)", false);
}