using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Dalamud.Plugin.Services;
using FFXIV_ACT_Plugin.Common;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using RainbowMage.OverlayPlugin.MemoryProcessors.JobGauge;
using RainbowMage.OverlayPlugin.MemoryProcessors.Party;
using RainbowMage.OverlayPlugin.NetworkProcessors;
// For some reason this using is required by the github build?
using PluginCombatant = FFXIV_ACT_Plugin.Common.Models.Combatant;

namespace RainbowMage.OverlayPlugin.EventSources;

internal class FFXIVRequiredEventSource : EventSourceBase {
	public enum PartyType {
		Solo,
		Party,
		AllianceA,
		AllianceB,
		AllianceC,
		AllianceD,
		AllianceE,
		AllianceF
	}

	private const string OnlineStatusChangedEvent = "OnlineStatusChanged";
	private const string PartyChangedEvent = "PartyChanged";
	private const string JobGaugeChangedEvent = "JobGaugeChanged";

	// In milliseconds
	private const int PollingRate = 50;

	private static readonly Dictionary<uint, string> StatusMap = new() {
		[0] = "Online",
		[12] = "Busy",
		[15] = "InCutscene",
		[17] = "AFK",
		[21] = "LookingToMeld",
		[22] = "RP",
		[23] = "LookingForParty"
	};

	// private readonly CancellationTokenSource? cancellationToken;
	private readonly ICombatantMemory combatantMemory;
	private readonly IJobGaugeMemory jobGaugeMemory;
	private readonly IPartyMemory partyMemory;

	private readonly FFXIVRepository repository;
	private PartyListsStruct cachedPartyList = new();
	private readonly IFramework framework;

	public FFXIVRequiredEventSource(TinyIoCContainer container) : base(container) {
		Name = "FFXIVRequired";
		framework = container.Resolve<IFramework>();
		var haveRepository = container.TryResolve(out repository);
		var haveCombatantMemory = container.TryResolve(out combatantMemory);

		if (haveRepository && haveCombatantMemory) {
			if (!container.TryResolve(out partyMemory)) {
				Log(LogLevel.Error, "Could not construct FFXIVRequiredEventSource: Missing partyMemory");
			}

			if (!container.TryResolve(out jobGaugeMemory)) {
				Log(LogLevel.Error, "Could not construct FFXIVRequiredEventSource: Missing jobGaugeMemory");
			}

			// These events need to deliver cached values to new subscribers.
			RegisterCachedEventTypes([
				OnlineStatusChangedEvent,
				PartyChangedEvent,
				JobGaugeChangedEvent
			]);

			RegisterEventHandler("getLanguage", _ => {
				var lang = repository.GetLanguage();
				var region = repository.GetMachinaRegion();
				return JObject.FromObject(new {
					language = lang.ToString("g"),
					languageId = lang.ToString("d"),
					region = region.ToString("g"),
					regionId = region.ToString("d")
				});
			});

			RegisterEventHandler("getVersion", _ => JObject.FromObject(new {
				version = repository.GetOverlayPluginVersion().ToString()
			}));

			RegisterEventHandler("getCombatants", msg => {
				var combatants = GetCombatants(
					msg["ids"] is JArray msgids ? [.. msgids.Select(id => id.ToObject<uint>())] : [],
					msg["names"] is JArray msgnames ? [.. msgnames.Select(name => name.ToString())] : [],
					msg["props"] is JArray msgprops ? [.. msgprops.Select(prop => prop.ToString())] : []);
				return JObject.FromObject(new {
					combatants
				});
			});

			container.Resolve<NetworkParser>().OnOnlineStatusChanged += (_, e) => {
				var obj = new JObject {
					["type"] = OnlineStatusChangedEvent,
					["target"] = e.Target,
					["rawStatus"] = e.Status,
					["status"] = StatusMap.GetValueOrDefault(e.Status, "Unknown")
				};

				DispatchAndCacheEvent(obj);
			};

			// cancellationToken = new CancellationTokenSource();
			framework.Update += PollJobGauge;
			// Task.Run(PollJobGauge, cancellationToken.Token);
		}
	}

	// Event Source

	public BuiltinEventConfig Config { get; set; }

	[MethodImpl(MethodImplOptions.NoInlining)]
	private List<Dictionary<string, object?>> GetCombatants(List<uint> ids, List<string> names, List<string> props) {
		var filteredCombatants = new List<Dictionary<string, object?>>();
		var pluginCombatants = repository.GetCombatants();
		var pluginCombatantsById = new Dictionary<uint, PluginCombatant>(pluginCombatants.Count);
		foreach (var combatant in pluginCombatants) pluginCombatantsById.TryAdd(combatant.ID, combatant);
		var idSet = ids.Count > 0 ? new HashSet<uint>(ids) : null;
		var nameSet = names.Count > 0
			? new HashSet<string>(names, StringComparer.InvariantCultureIgnoreCase)
			: null;
		var propSet = props.Count > 0 ? new HashSet<string>(props) : null;
		var memCombatants = combatantMemory.GetCombatantList();
		foreach (var combatant in memCombatants) {
			if (combatant.ID == 0) continue;
			var include = idSet == null && nameSet == null;
			if (!include && idSet != null && idSet.Contains(combatant.ID)) include = true;
			if (!include && nameSet != null && combatant.Name != null && nameSet.Contains(combatant.Name)) include = true;
			if (!include) continue;

			var jObjCombatant = JObject.FromObject(combatant);
			var result = new Dictionary<string, object?>(jObjCombatant.Count);
			foreach (var property in jObjCombatant.Properties()) {
				result[property.Name] = property.Value.ToObject<object>();
			}

			var pluginCombatant = pluginCombatantsById.GetValueOrDefault(combatant.ID);
			if (pluginCombatant != null) {
				result["PartyType"] = GetPartyType(pluginCombatant);
			}

			// Handle 0xFFFE (outofrange1) and 0xFFFF (outofrange2) values for WorldID
			var WorldID = Convert.ToUInt32(result["WorldID"]);
			string? WorldName = null;
			if (WorldID < 0xFFFE) WorldName = GetWorldName(WorldID);

			result["WorldName"] = WorldName;

			// If the request is filtering properties, remove them here
			if (propSet != null) {
				foreach (var key in result.Keys.ToList().Where(key => !propSet.Contains(key))) result.Remove(key);
			}

			filteredCombatants.Add(result);
		}
		return filteredCombatants;
	}

	private int GetPartyType(PluginCombatant combatant) =>
		// The PartyTypeEnum was renamed in 2.6.0.0 to work around that, we use reflection and cast the result to int.
		(int)combatant.PartyType;

	private string? GetWorldName(uint WorldID) => repository.GetResourceDictionary(ResourceType.WorldList_EN).TryGetValue(WorldID, out var WorldName) ? WorldName : null;

	private void DispatchPartyChangeEvent() {
		// TODO: We know how to detect alliance A/B/C. Need to verify alliance D/E/F
		var result = new List<PartyMember>(24);

		var remainingAlliances = new List<PartyType> {
			PartyType.AllianceA,
			PartyType.AllianceB,
			PartyType.AllianceC,
			PartyType.AllianceD,
			PartyType.AllianceE,
			PartyType.AllianceF
		};

		PartyType currentAlliance;
		if ((cachedPartyList.allianceFlags & 0x1) == 0)
			currentAlliance = cachedPartyList.memberCount <= 1 ? PartyType.Solo : PartyType.Party;
		else if ((cachedPartyList.currentPartyFlags & 0x100) == 0x100) {
			currentAlliance = remainingAlliances[0];
			remainingAlliances.RemoveAt(0);
		} else if ((cachedPartyList.currentPartyFlags & 0x1) == 0x1) {
			currentAlliance = remainingAlliances[1];
			remainingAlliances.RemoveAt(1);
		} else if ((cachedPartyList.currentPartyFlags & 0x10000) == 0x10000) {
			currentAlliance = remainingAlliances[2];
			remainingAlliances.RemoveAt(2);
		} else {
			currentAlliance = remainingAlliances[2];
			remainingAlliances.RemoveAt(2);
			Log(LogLevel.Warning, $"Could not detect player alliance, value {cachedPartyList.currentPartyFlags:X8}");
		}

		BuildPartyMemberResults(result, cachedPartyList.partyMembers, currentAlliance, true);
		BuildPartyMemberResults(result, cachedPartyList.alliance1Members, remainingAlliances[0], false);
		BuildPartyMemberResults(result, cachedPartyList.alliance2Members, remainingAlliances[1], false);
		BuildPartyMemberResults(result, cachedPartyList.alliance3Members, remainingAlliances[2], false);
		BuildPartyMemberResults(result, cachedPartyList.alliance4Members, remainingAlliances[3], false);
		BuildPartyMemberResults(result, cachedPartyList.alliance5Members, remainingAlliances[4], false);

		Log(LogLevel.Debug, "party list: {0}", JObject.FromObject(new {
			party = result
		}).ToString());

		DispatchAndCacheEvent(JObject.FromObject(new {
			type = PartyChangedEvent,
			party = result,
			partyInfo = new {
				cachedPartyList.allianceFlags,
				cachedPartyList.memberCount,
				cachedPartyList.partyId,
				cachedPartyList.partyId_2,
				cachedPartyList.partyLeaderIndex,

				cachedPartyList.currentPartyFlags
			}
			// #if DEBUG
// 			debugPartyStruct = cachedPartyList,
// #endif
		}));
	}

	private void BuildPartyMemberResults(List<PartyMember> result, PartyListEntry?[]? members, PartyType partyType, bool inParty) {
		if (members == null) return;
		result.AddRange(members
			.Where(member => member != null)
			.Cast<PartyListEntry>()
			.Where(member => (member.flags & 0x1) == 0x1)
			.Select(member => new PartyMember {
				id = $"{member.objectId:X}", name = member.name, worldId = member.homeWorld, job = member.classJob, level = member.level, inParty = inParty, contentId = member.contentId, flags = member.flags, objectId = member.objectId,
				territoryType = member.territoryType, partyType = partyType.ToString()
			}));
	}

	public override void LoadConfig(IPluginConfig config) {
		Config = container.Resolve<BuiltinEventConfig>();
		Config.UpdateIntervalChanged += (_, _) => Start();
	}

	public override void SaveConfig(IPluginConfig config) {
	}

	public override void Start() {
		// Use the config update interval, but clamp an upper limit at 5000ms
		var updateInterval = Math.Min(5000, Config.UpdateInterval * 1000);
		timer?.Change(0, updateInterval);
	}

	public override void Stop() {
		base.Stop();
		framework.Update -= PollJobGauge;
		// cancellationToken?.Cancel();
	}

	private IJobGauge? lastJobGauge;

	private void PollJobGauge(IFramework framework1) {
		if (!HasSubscriber(JobGaugeChangedEvent)) return;
		var jobGauge = jobGaugeMemory.GetJobGauge();
		if (jobGauge == null || jobGauge.Equals(lastJobGauge)) return;
		lastJobGauge = jobGauge;
		var obj = JObject.FromObject(jobGauge);
		obj["type"] = JobGaugeChangedEvent;
		DispatchAndCacheEvent(obj);
	}

	protected override void Update() {
		UpdateParty();
	}

	private void UpdateParty() {
		var newParty = partyMemory.GetPartyLists();

		// If we don't have anyone in the party, "correct" it by making sure the current player is in the party
		if (newParty.memberCount == 0) {
			var currentPlayer = combatantMemory.GetSelfCombatant();
			if (currentPlayer == null) {
				return;
			}
			newParty.memberCount = 1;
			newParty.partyMembers = [
				new PartyListEntry {
					x = currentPlayer.PosX,
					y = currentPlayer.PosY,
					z = currentPlayer.PosZ,
					objectId = currentPlayer.ID,
					currentHP = (uint)currentPlayer.CurrentHP,
					maxHP = (uint)currentPlayer.MaxHP,
					currentMP = (ushort)currentPlayer.CurrentMP,
					maxMP = (ushort)currentPlayer.MaxMP,
					homeWorld = currentPlayer.WorldID,
					name = currentPlayer.Name ?? "",
					classJob = currentPlayer.Job,
					level = currentPlayer.Level,
					flags = 0x13
				}
			];
			newParty.partyLeaderIndex = 0;
		}

		var dispatchEvent = false;
		// If the party member count has changed, dispatch the event
		if (newParty.memberCount != cachedPartyList.memberCount) {
			dispatchEvent = true;
		}
		// Check each of party and alliances 1-5
		if (!dispatchEvent) {
			dispatchEvent = HasPartyCompChanged(cachedPartyList.partyMembers, newParty.partyMembers);
		}
		if (!dispatchEvent) {
			dispatchEvent = HasPartyCompChanged(cachedPartyList.alliance1Members, newParty.alliance1Members);
		}
		if (!dispatchEvent) {
			dispatchEvent = HasPartyCompChanged(cachedPartyList.alliance2Members, newParty.alliance2Members);
		}
		if (!dispatchEvent) {
			dispatchEvent = HasPartyCompChanged(cachedPartyList.alliance3Members, newParty.alliance3Members);
		}
		if (!dispatchEvent) {
			dispatchEvent = HasPartyCompChanged(cachedPartyList.alliance4Members, newParty.alliance4Members);
		}
		if (!dispatchEvent) {
			dispatchEvent = HasPartyCompChanged(cachedPartyList.alliance5Members, newParty.alliance5Members);
		}
		cachedPartyList = newParty;
		if (dispatchEvent) {
			DispatchPartyChangeEvent();
		}
	}

	private bool HasPartyCompChanged(PartyListEntry?[]? oldList, PartyListEntry?[]? newList) {
		// If both lists are the same reference or both null, nothing changed
		if (ReferenceEquals(oldList, newList)) {
			return false;
		}
		if (oldList == null && newList == null) {
			return false;
		}
		// If one is null and the other isn't, changed
		if (oldList == null || newList == null) {
			return true;
		}
		// If lengths differ, changed
		if (oldList.Length != newList.Length) {
			return true;
		}

		for (var i = 0; i < newList.Length; ++i) {
			var newMember = newList[i];
			var oldMember = oldList[i];
			if (newMember == null || oldMember == null) {
				// If one of these is null and the other isn't, they've changed, dispatch the event
				if (!ReferenceEquals(newMember, oldMember)) {
					return true;
				}
				// Otherwise they're both null, we don't need to check them, continue
				continue;
			}
			// If the party list is in a different order, dispatch the event
			if (newMember.objectId != oldMember.objectId) {
				return true;
			}
			// If the party member's job or level changed, dispatch the event
			if (newMember.classJob != oldMember.classJob || newMember.level != oldMember.level) {
				return true;
			}
			// If the member's flags have changed, dispatch the event. This covers players jumping zones properly.
			if (newMember.flags != oldMember.flags) {
				return true;
			}
		}
		return false;
	}

	[SuppressMessage("ReSharper", "NotAccessedField.Local")]
	private struct PartyMember {
		// Player id in hex (for ease in matching logs).
		public string id;
		public string name;

		public uint worldId;

		// Raw job id.
		public int job;

		public int level;

		// @deprecated, please use partyType
		public bool inParty;
		public long contentId;
		// 0x1 = valid/present
		// 0x2 = unknown but set for some alliance members?
		// 0x4 = unknown but always set for current party and alliance members?
		// 0x8 = unknown but always set for current party?
		public byte flags;
		public uint objectId;
		public ushort territoryType;
		public string partyType;
	}
}