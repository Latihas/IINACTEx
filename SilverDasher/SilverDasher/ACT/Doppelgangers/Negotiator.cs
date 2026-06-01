using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Common.Models;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Doppelgangers;

internal class Negotiator : Doppelganger {
	private readonly FFXIV_ACT_Plugin.FFXIV_ACT_Plugin FFXIVACTPlugin;
	private IDataSubscription ffevents;

	internal Negotiator(SilverDasher self) : base(self) {
		foreach (var actPlugin in ActGlobals.oFormActMain.ActPlugins.Where(actPlugin => actPlugin is { pluginFile.Name: "FFXIV_ACT_Plugin.dll" })) {
			FFXIVACTPlugin = (actPlugin.pluginObj as FFXIV_ACT_Plugin.FFXIV_ACT_Plugin)!;
		}
		if (FFXIVACTPlugin == null) {
			throw new NotSupportedException("你还未传送至艾欧泽亚。（未加载FF解析插件，或修改了解析插件的名称导致无法识别，请检查后重新加载插件。）");
		}
	}

	internal override void Init() {
		ffevents = (IDataSubscription)FFXIVACTPlugin.DataSubscription;
		// ffevents.ZoneChanged += GetPlayerInfo;
		ffevents.NetworkReceived += Overseer.OnNetworkReceive;
		// ffevents.ProcessChanged += ProcessChanged;
		ffevents.LogLine += OnInGameLoglineRead;
	}

	internal override void Deinit() {
		var obj = (IDataSubscription)FFXIVACTPlugin.DataSubscription;
		// obj.ZoneChanged -= GetPlayerInfo;
		obj.NetworkReceived -= Overseer.OnNetworkReceive;
		// obj.ProcessChanged -= ProcessChanged;
		obj.LogLine -= OnInGameLoglineRead; //++
	}

	// internal void GetPlayerInfo(uint ZoneID, string ZoneName) {
	//     IDataRepository dataRepository = FFXIVACTPlugin.DataRepository;
	//     var currentPlayerID = dataRepository.GetCurrentPlayerID();
	//     var combatantList = dataRepository.GetCombatantList();
	//     var combatant = combatantList.FirstOrDefault(item => item.ID == currentPlayerID);
	//     if (combatant == null || combatant.WorldID == 0) return;
	//     Keeper.PlayerName = combatant.Name;
	//     Keeper.PlayerWorld = combatant.WorldName;
	//     Keeper.PlayerWorldID = combatant.WorldID;
	//     Keeper.CurrentWorldID = combatant.CurrentWorldID;
	//     Keeper.CurrentMapID = ZoneID != 0 ? ZoneID : SilverDasher.ClientState.TerritoryType;
	//     if (Keeper.Worlds.TryGetByLabel(Keeper.PlayerWorld, out var w))
	//         Keeper.PlayerWorld = w.Name;
	// }

	// internal void ProcessChanged(Process process) {
	//     Keeper.CurrentMobs.Clear();
	//     Keeper.CurrentFates.Clear();
	//     Self.RestartLoop();
	//     Primal.ChangeProcess(process);
	// }

	internal void ScanMobs() {
		var currentTerritoryID = SilverDasher.ClientState.TerritoryType;
		var currentInstance = Primal.GetCurrentInstance();
		ReadOnlyCollection<Combatant> readOnlyCollection = FFXIVACTPlugin.DataRepository.GetCombatantList();
		HashSet<string> hashSet = [];
		var currentTerritoryID2 = SilverDasher.ClientState.TerritoryType;
		var currentInstance2 = Primal.GetCurrentInstance();
		// Keeper.CurrentMapID = currentTerritoryID2;
		// Keeper.CurrentInstance = currentInstance2;
		// Logger.Debug($"Scanned Territory {currentTerritoryID2}");
		if (currentTerritoryID != currentTerritoryID2 || currentInstance != currentInstance2 || Keeper.CurrentMapID == 255 || Keeper.CurrentInstance == 255) {
			Keeper.CurrentFates.Clear();
			Keeper.CurrentMobs.Clear();
			Log("Map or instance changed while detecting mobs, Messages will be disposed.");
			return;
		}
		// if (readOnlyCollection.Count > 0) Keeper.CurrentWorldID = readOnlyCollection[0].CurrentWorldID;
		foreach (var item2 in readOnlyCollection) {
			var item = $"{currentTerritoryID2}-{(int)item2.BNpcNameID}-{currentInstance2}";
			if (hashSet.Contains(item) || !Keeper.Mobs.Contains((int)item2.BNpcNameID)) continue;
			Keeper.MobUpdate((int)item2.BNpcNameID, (int)Math.Ceiling(item2.CurrentHP / (double)item2.MaxHP * 100.0), new Coordinate {
				x = (int)((item2.PosX * 0.02 + 21.5) * 100.0),
				y = (int)((item2.PosY * 0.02 + 21.5) * 100.0)
			}, currentTerritoryID2, currentInstance2);
			hashSet.Add(item);
		}
		List<string> list = [];
		list.AddRange(Keeper.CurrentMobs.Keys.Where(key => !hashSet.Contains(key)));
		foreach (var item3 in list)
			Keeper.CurrentMobs.Remove(item3, out _);
	}

	private void OnInGameLoglineRead(uint EventType, uint Seconds, string logline) {
		if (!logline.StartsWith("当前所在副本区为")) return;
		Debug(logline);
		Keeper.CurrentMobs.Clear();
		Keeper.CurrentFates.Clear();
		// var text = logline.Substring(logline.IndexOf("“"), logline.IndexOf("”"));
		// var key = text[^1];
		// var chatLogInstance = InstanceMap[key];
		// Keeper.ChatLogInstance = (uint)chatLogInstance;
	}

	// internal void OnParsedLoglineRead(uint sequence, int messagetype, string message) {
	//     var array = message.Split('|');
	//     if (array.Length > 2 && array[0] == "00" && array[1] == "0039") {
	//         Debug(message);
	//     }
	// }
}