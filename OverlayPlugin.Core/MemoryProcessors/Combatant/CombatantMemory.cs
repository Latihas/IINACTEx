using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Microsoft.Extensions.ObjectPool;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

public abstract class CombatantMemory : ICombatantMemory {
	public readonly FFXIVMemory memory;
	public unsafe IntPtr charmapAddress {
		get {
			var gobs = CharacterManager.Instance()->BattleCharas;
			return gobs.Length == 0 ? IntPtr.Zero : (IntPtr)gobs[0].Value;
		}
	}
	public readonly int numMemoryCombatants;
	public readonly int combatantSize;

	protected readonly ObjectPool<Combatant> combatantPool;

	// Constants.
	protected const uint emptyID = 0xE0000000;

	public CombatantMemory(
		TinyIoCContainer container, int combatantSize, int effectSize,
		int numMemoryCombatants = 421) {
		this.combatantSize = combatantSize;
		this.numMemoryCombatants = numMemoryCombatants;
		container.Resolve<ILogger>();
		memory = container.Resolve<FFXIVMemory>();
		var policy = new DefaultPooledObjectPolicy<Combatant>();
		combatantPool = new DefaultObjectPool<Combatant>(policy, 2 * numMemoryCombatants);
	}

	public bool IsValid() => true;

	public void ScanPointers() {
	}

	public abstract Version GetVersion();

	public unsafe Combatant? GetSelfCombatant() {
		var gobs = CharacterManager.Instance()->BattleCharas;
		return gobs.Length == 0 ? null : GetCombatantFromByteArray(gobs[0].Value, 0, true, true);
	}

	public unsafe Combatant GetCombatantFromAddress(BattleChara* address, uint selfCharID = 0) => GetCombatantFromByteArray(address, selfCharID, false);

	public unsafe List<Combatant> GetCombatantList() {
		var result = new List<Combatant>();
		var seen = new HashSet<uint>();
		var mychar = GetSelfCombatant();

		foreach (var p in CharacterManager.Instance()->BattleCharas) {
			var combatant = GetMobFromByteArray(p.Value, mychar?.ID ?? 0);
			if (combatant == null) continue;
			if (seen.Contains(combatant.ID)) {
				ReturnCombatant(combatant);
				continue;
			}

			// TODO: should this just be a dictionary? there are a lot of id lookups.
			result.Add(combatant);
			seen.Add(combatant.ID);
		}

		ReturnCombatant(mychar);
		return result;
	}

	public void ReturnCombatant(Combatant? combatant) {
		if (combatant == null) return;
		combatantPool.Return(combatant);
	}

	// Returns a combatant if the combatant is a mob or a PC.
	public abstract unsafe Combatant? GetMobFromByteArray(BattleChara* gameObject, uint mycharID);

	// Will return any kind of combatant, even if not a mob.
	// This function always returns a combatant object, even if empty.
	protected abstract unsafe Combatant GetCombatantFromByteArray(
		BattleChara* character, uint mycharID, bool isPlayer, bool exceptEffects = false);

	protected unsafe List<EffectEntry> GetEffectEntries(byte* source, ObjectType type, uint mycharID) {
		var result = new List<EffectEntry>();
		var maxEffects = type == ObjectType.PC ? 30 : 60;
		var size = EffectMemory.Size * maxEffects;

		var bytes = ArrayPool<byte>.Shared.Rent(size);
		Marshal.Copy((IntPtr)source, bytes, 0, size);

		for (var i = 0; i < maxEffects; i++) {
			var effect = GetEffectEntryFromByteArray(bytes, i);

			if (effect.BuffID > 0 &&
			    effect.Stack >= 0 &&
			    effect.Timer >= 0.0f &&
			    effect.ActorID > 0) {
				effect.isOwner = effect.ActorID == mycharID;

				result.Add(effect);
			}
		}

		ArrayPool<byte>.Shared.Return(bytes);
		return result;
	}

	protected unsafe EffectEntry GetEffectEntryFromByteArray(byte[] source, int num = 0) {
		fixed (byte* p = source) {
			var mem = *(EffectMemory*)&p[num * EffectMemory.Size];

			var effectEntry = new EffectEntry {
				BuffID = mem.BuffID,
				Stack = mem.Stack,
				Timer = mem.Timer,
				ActorID = mem.ActorID,
				isOwner = false
			};

			return effectEntry;
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = Size)]
	public struct EffectMemory {
		public const int Size = 12;

		[FieldOffset(0)] public ushort BuffID;

		[FieldOffset(2)] public ushort Stack;

		[FieldOffset(4)] public float Timer;

		[FieldOffset(8)] public uint ActorID;
	}
}