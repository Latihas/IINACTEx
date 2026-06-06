using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

public interface ICombatantMemory : IVersionedMemory {
	Combatant? GetSelfCombatant();
	unsafe Combatant GetCombatantFromAddress(GameObject* address, uint selfCharID);
	List<Combatant> GetCombatantList();
	void ReturnCombatant(Combatant combatant);
}

public class CombatantMemoryManager : ICombatantMemory {
	public readonly ICombatantMemory memory;

	public CombatantMemoryManager(TinyIoCContainer container) {
		container.Register<ICombatantMemory74, CombatantMemory74>();
		container.Resolve<FFXIVRepository>();
		memory = container.Resolve<ICombatantMemory74>();
		memory.ScanPointers();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => memory != null && memory.IsValid();

	public Version GetVersion() => IsValid() ? memory.GetVersion() : null;

	public unsafe Combatant GetCombatantFromAddress(GameObject* address, uint selfCharID) => IsValid() ? memory.GetCombatantFromAddress(address, selfCharID) : null;

	public List<Combatant> GetCombatantList() => IsValid() ? memory.GetCombatantList() : [];

	public Combatant? GetSelfCombatant() => IsValid() ? memory.GetSelfCombatant() : null;

	public void ReturnCombatant(Combatant combatant) {
		if (IsValid()) memory.ReturnCombatant(combatant);
	}
}