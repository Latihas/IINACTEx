using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

public interface ICombatantMemory : IVersionedMemory {
	Combatant? GetSelfCombatant();
	unsafe Combatant? GetCombatantFromAddress(BattleChara* address, uint selfCharID);
	List<Combatant> GetCombatantList();
	void ReturnCombatant();
}

public class CombatantMemoryManager : ICombatantMemory {
	public readonly ICombatantMemory memory;

	public CombatantMemoryManager(TinyIoCContainer container) {
		container.Register<ICombatantMemory75, CombatantMemory75>();
		memory = container.Resolve<ICombatantMemory75>();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => true;

	public Version GetVersion() => memory.GetVersion();

	public unsafe Combatant? GetCombatantFromAddress(BattleChara* address, uint selfCharID) => memory.GetCombatantFromAddress(address, selfCharID);

	public List<Combatant> GetCombatantList() => memory.GetCombatantList();

	public Combatant? GetSelfCombatant() => memory.GetSelfCombatant();

	public void ReturnCombatant() { }
}