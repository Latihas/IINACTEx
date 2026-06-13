using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

public interface ICombatantMemory : IVersionedMemory {
	Combatant? GetSelfCombatant();
	unsafe Combatant? GetCombatantFromAddress(GameObject* address, uint selfCharID);
	List<Combatant> GetCombatantList();
	void ReturnCombatant();
}

public class CombatantMemoryManager : ICombatantMemory {
	public readonly ICombatantMemory memory;

	public CombatantMemoryManager(TinyIoCContainer container) {
		container.Register<ICombatantMemory75, CombatantMemory75>();
		memory = container.Resolve<ICombatantMemory75>();
		memory.ScanPointers();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => true;

	public Version GetVersion() => memory.GetVersion();

	public unsafe Combatant? GetCombatantFromAddress(GameObject* address, uint selfCharID) => memory.GetCombatantFromAddress(address, selfCharID);

	public List<Combatant> GetCombatantList() => IsValid() ? memory.GetCombatantList() : [];

	public Combatant? GetSelfCombatant() => IsValid() ? memory.GetSelfCombatant() : null;

	public void ReturnCombatant() {
		if (IsValid()) memory.ReturnCombatant();
	}
}