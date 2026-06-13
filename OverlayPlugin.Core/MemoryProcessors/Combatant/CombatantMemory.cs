using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.Interop;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

public abstract class CombatantMemory : ICombatantMemory {
	protected const uint emptyID = 0xE0000000;
	public unsafe GameObject* charmapAddress {
		get {
			var gobs = CharacterManager.Instance()->BattleCharas.ToArray()
				.Select(entry => (Pointer<GameObject>)(GameObject*)entry.Value)
				.Where(entry => entry.Value != null).ToArray();
			return gobs.Length == 0 ? null : gobs[0].Value;
		}
	}

	public bool IsValid() => true;

	public void ScanPointers() {
	}

	public abstract Version GetVersion();

	public unsafe Combatant? GetSelfCombatant() {
		var gobs = CharacterManager.Instance()->BattleCharas.ToArray()
			.Select(entry => (Pointer<GameObject>)(GameObject*)entry.Value)
			.Where(entry => entry.Value != null).ToArray();
		return gobs.Length == 0 ? null : GetCombatantFromByteArray(gobs[0].Value, 0, true, true);
	}

	public unsafe Combatant? GetCombatantFromAddress(GameObject* address, uint selfCharID = 0) => GetCombatantFromByteArray(address, selfCharID, false);

	public unsafe List<Combatant> GetCombatantList() {
		var result = new List<Combatant>();
		var seen = new HashSet<uint>();
		var mychar = GetSelfCombatant();

		foreach (var p in CharacterManager.Instance()->BattleCharas.ToArray()
			         .Select(entry => (Pointer<GameObject>)(GameObject*)entry.Value)
			         .Where(entry => entry.Value != null)) {
			var combatant = GetMobFromByteArray(p.Value, mychar?.ID ?? 0);
			if (combatant == null) continue;
			if (seen.Contains(combatant.ID)) {
				ReturnCombatant();
				continue;
			}

			// TODO: should this just be a dictionary? there are a lot of id lookups.
			result.Add(combatant);
			seen.Add(combatant.ID);
		}

		ReturnCombatant();
		return result;
	}

	public void ReturnCombatant() {
	}

	// Returns a combatant if the combatant is a mob or a PC.
	public abstract unsafe Combatant? GetMobFromByteArray(GameObject* gameObject, uint mycharID);

	// Will return any kind of combatant, even if not a mob.
	// This function always returns a combatant object, even if empty.
	protected abstract unsafe Combatant? GetCombatantFromByteArray(
		GameObject* character, uint mycharID, bool isPlayer, bool exceptEffects = false);

	protected static List<EffectEntry> GetEffectEntries(StatusManager statusManager, ObjectType type, uint mycharID) {
		var result = new List<EffectEntry>();
		var maxEffects = type == ObjectType.PC ? 30 : 60;
		var status = statusManager.Status;
		for (var i = 0; i < maxEffects; i++) {
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