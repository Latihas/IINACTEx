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
		FFXIVACTPlugin = ActGlobals.oFormActMain.FfxivPlugin;
	}

	internal override void Init() {
		ffevents = FFXIVACTPlugin.DataSubscription;
		ffevents.NetworkReceived += Overseer.OnNetworkReceive;
		ffevents.LogLine += OnInGameLoglineRead;
	}

	internal override void Deinit() {
		var obj = FFXIVACTPlugin.DataSubscription;
		obj.NetworkReceived -= Overseer.OnNetworkReceive;
		obj.LogLine -= OnInGameLoglineRead; //++
	}

	internal void ScanMobs() {
		ReadOnlyCollection<Combatant> readOnlyCollection = FFXIVACTPlugin.DataRepository.GetCombatantList();
		HashSet<string> hashSet = [];
		var currentTerritoryID2 = SilverDasher.ClientState.TerritoryType;
		var currentInstance2 = Primal.GetCurrentInstance();
		if (Keeper.CurrentMapID == 255 || Keeper.CurrentInstance == 255) {
			Keeper.CurrentFates.Clear();
			Keeper.CurrentMobs.Clear();
			Log("Map or instance changed while detecting mobs, Messages will be disposed.");
			return;
		}
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
	}
}