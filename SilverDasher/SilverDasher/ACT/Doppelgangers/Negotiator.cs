using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Common.Models;
using SilverDasher.ACT.Models;
using SilverDasher.ACT.Models.Messages;

namespace SilverDasher.ACT.Doppelgangers;

internal class Negotiator : Doppelganger
{
	internal IActPluginV1 FFXIVACTPlugin;

	internal IDataSubscription ffevents;

	internal IDataRepository ffdata;

	internal Dictionary<char, int> InstanceMap = new Dictionary<char, int>
	{
		{ '\ue0b1', 1 },
		{ '\ue0b2', 2 },
		{ '\ue0b3', 3 },
		{ '\ue0b4', 4 },
		{ '\ue0b5', 5 },
		{ '\ue0b6', 6 },
		{ '\ue0b7', 7 },
		{ '\ue0b8', 8 },
		{ '\ue0b9', 9 }
	};

	internal Negotiator(SilverDasher self)
		: base(self)
	{
		foreach (ActPluginData actPlugin in ActGlobals.oFormActMain.ActPlugins)
		{
			if (actPlugin.pluginObj != null && actPlugin.pluginFile.Name == "FFXIV_ACT_Plugin.dll")
			{
				FFXIVACTPlugin = actPlugin.pluginObj;
			}
		}
		if (FFXIVACTPlugin == null)
		{
			throw new NotSupportedException("你还未传送至艾欧泽亚。（未加载FF解析插件，或修改了解析插件的名称导致无法识别，请检查后重新加载插件。）");
		}
	}

	internal override void Init()
	{
		dynamic fFXIVACTPlugin = FFXIVACTPlugin;
		ffevents = fFXIVACTPlugin.DataSubscription as IDataSubscription;
		ffevents.ZoneChanged += GetPlayerInfo;
		ffevents.NetworkReceived += base.Overseer.OnNetworkReceive;
		ffevents.ProcessChanged += ProcessChanged;
		ffevents.LogLine += OnInGameLoglineRead;
		ffdata = fFXIVACTPlugin.DataRepository;
	}

	internal override void Deinit()
	{
		dynamic fFXIVACTPlugin = FFXIVACTPlugin;
		IDataSubscription obj = fFXIVACTPlugin.DataSubscription as IDataSubscription;
		obj.ZoneChanged -= GetPlayerInfo;
		obj.NetworkReceived -= base.Overseer.OnNetworkReceive;
		obj.ProcessChanged -= ProcessChanged;
	}

	internal void GetPlayerInfo(uint ZoneID, string ZoneName)
	{
		dynamic fFXIVACTPlugin = FFXIVACTPlugin;
		IDataRepository dataRepository = fFXIVACTPlugin.DataRepository;
		uint currentPlayerID = dataRepository.GetCurrentPlayerID();
		ReadOnlyCollection<Combatant> combatantList = dataRepository.GetCombatantList();
		Combatant combatant = null;
		foreach (Combatant item in combatantList)
		{
			if (item.ID == currentPlayerID)
			{
				combatant = item;
				break;
			}
		}
		if (combatant != null && combatant.WorldID != 0)
		{
			base.Keeper.PlayerName = combatant.Name;
			base.Keeper.PlayerWorld = combatant.WorldName;
			base.Keeper.PlayerWorldID = combatant.WorldID;
			base.Keeper.CurrentWorldID = combatant.CurrentWorldID;
			if (ZoneID != 0)
			{
				base.Keeper.CurrentMapID = ZoneID;
			}
			else
			{
				base.Keeper.CurrentMapID = dataRepository.GetCurrentTerritoryID();
			}
			if (base.Keeper.Worlds.TryGetByLabel(base.Keeper.PlayerWorld, out var w))
			{
				base.Keeper.PlayerWorld = w.Name;
			}
		}
	}

	internal void ProcessChanged(Process process)
	{
		base.Keeper.CurrentMobs.Clear();
		base.Keeper.CurrentFates.Clear();
		Self.RestartLoop();
		base.Primal.ChangeProcess(process);
	}

	internal List<Message> ScanMobs()
	{
		dynamic fFXIVACTPlugin = FFXIVACTPlugin;
		uint currentTerritoryID = ffdata.GetCurrentTerritoryID();
		uint currentInstance = base.Primal.GetCurrentInstance();
		ReadOnlyCollection<Combatant> readOnlyCollection = fFXIVACTPlugin.DataRepository.GetCombatantList();
		List<Message> result = new List<Message>();
		HashSet<string> hashSet = new HashSet<string>();
		uint currentTerritoryID2 = ffdata.GetCurrentTerritoryID();
		uint currentInstance2 = base.Primal.GetCurrentInstance();
		base.Keeper.CurrentMapID = currentTerritoryID2;
		base.Keeper.CurrentInstance = currentInstance2;
		base.Logger.Debug($"Scanned Territory {currentTerritoryID2}");
		if (currentTerritoryID != currentTerritoryID2 || currentInstance != currentInstance2 || base.Keeper.CurrentMapID == 255 || base.Keeper.CurrentInstance == 255)
		{
			base.Keeper.CurrentFates.Clear();
			base.Keeper.CurrentMobs.Clear();
			Log("Map or instance changed while detecting mobs, Messages will be disposed.");
			return result;
		}
		if (readOnlyCollection.Count > 0)
		{
			base.Keeper.CurrentWorldID = readOnlyCollection[0].CurrentWorldID;
		}
		foreach (Combatant item2 in readOnlyCollection)
		{
			string item = $"{currentTerritoryID2}-{(int)item2.BNpcNameID}-{currentInstance2}";
			if (!hashSet.Contains(item) && base.Keeper.Mobs.Contains((int)item2.BNpcNameID))
			{
				base.Keeper.MobUpdate((int)item2.BNpcNameID, (int)Math.Ceiling((double)item2.CurrentHP / (double)item2.MaxHP * 100.0), new Coordinate
				{
					x = (int)(((double)item2.PosX * 0.02 + 21.5) * 100.0),
					y = (int)(((double)item2.PosY * 0.02 + 21.5) * 100.0)
				}, currentTerritoryID2, currentInstance2);
				hashSet.Add(item);
			}
		}
		List<string> list = new List<string>();
		foreach (string key in base.Keeper.CurrentMobs.Keys)
		{
			if (!hashSet.Contains(key))
			{
				list.Add(key);
			}
		}
		foreach (string item3 in list)
		{
			base.Keeper.CurrentMobs.Remove(item3);
		}
		return result;
	}

	internal void OnInGameLoglineRead(uint EventType, uint Seconds, string logline)
	{
		if (logline.StartsWith("当前所在副本区为"))
		{
			Debug(logline);
			base.Keeper.CurrentMobs.Clear();
			base.Keeper.CurrentFates.Clear();
			string text = logline.Substring(logline.IndexOf("“"), logline.IndexOf("”"));
			text.Substring(0, text.Length - 1);
			char key = text[text.Length - 1];
			int chatLogInstance = InstanceMap[key];
			base.Keeper.ChatLogInstance = (uint)chatLogInstance;
		}
	}

	internal void OnParsedLoglineRead(uint sequence, int messagetype, string message)
	{
		string[] array = message.Split('|');
		if (array.Length > 2 && array[0] == "00" && array[1] == "0039")
		{
			Debug(message);
		}
	}
}
