using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

public abstract class CombatantMemory(TinyIoCContainer container) : ICombatantMemory {
	private readonly IObjectTable ObjectTable = container.Resolve<IObjectTable>();
	// protected const uint emptyID = 0xE0000000;
	public unsafe GameObject* charmapAddress {
		get {
			var p = ObjectTable.LocalPlayer;
			return p == null ? null : (GameObject*)p.Address;
		}
	}

	public bool IsValid() => true;

	public void ScanPointers() {
	}

	public abstract Version GetVersion();

	public unsafe Combatant? GetSelfCombatant() {
		var p = ObjectTable.LocalPlayer;
		return p == null ? null : GetCombatantFromByteArray((BattleChara*)p.Address, 0, true, true);
	}

	public unsafe Combatant? GetCombatantFromAddress(BattleChara* address, uint selfCharID = 0) => GetCombatantFromByteArray(address, selfCharID, false);

	public unsafe List<Combatant> GetCombatantList() {
		var temp = new Dictionary<uint, Combatant>();
		var mychar = GetSelfCombatant();

		foreach (var p in CharacterManager.Instance()->BattleCharas) {
			if(p==null)continue;
			var combatant = GetMobFromByteArray(p.Value, mychar?.ID ?? 0);
			if (combatant == null) continue;
			temp.TryAdd(combatant.ID, combatant);
		}
		return [.. temp.Values];
	}

	public void ReturnCombatant() { }

	// Returns a combatant if the combatant is a mob or a PC.
	public abstract unsafe Combatant? GetMobFromByteArray(BattleChara* gameObject, uint mycharID);

	// Will return any kind of combatant, even if not a mob.
	// This function always returns a combatant object, even if empty.
	protected abstract unsafe Combatant? GetCombatantFromByteArray(
		BattleChara* character, uint mycharID, bool isPlayer, bool exceptEffects = false);

	protected static List<EffectEntry> GetEffectEntries(StatusManager statusManager, ObjectType type, uint mycharID) {
		var result = new List<EffectEntry>();
		var status = statusManager.Status;
		for (var i = 0; i < (type == ObjectType.PC ? 30 : 60); i++) {
			var effect = GetEffectEntryFromByteArray(status[i]);
			if (effect is { BuffID: > 0, Stack: >= 0, Timer: >= 0.0f, ActorID: > 0 }) {
				effect.isOwner = effect.ActorID == mycharID;
				result.Add(effect);
			}
		}
		return result;
	}

	protected static EffectEntry GetEffectEntryFromByteArray(Status source) {
		var effectEntry = new EffectEntry {
			BuffID = source.StatusId,
			Stack = source.Param,
			Timer = source.RemainingTime,
			ActorID = (uint)source.SourceObject,
			isOwner = false
		};
		return effectEntry;
	}
}