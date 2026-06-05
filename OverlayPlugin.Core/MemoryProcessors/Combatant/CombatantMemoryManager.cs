using System;
using System.Collections.Generic;
using System.Diagnostics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

public interface ICombatantMemory : IVersionedMemory {
	Combatant? GetSelfCombatant();
	unsafe Combatant GetCombatantFromAddress(BattleChara* address, uint selfCharID);
	List<Combatant> GetCombatantList();
	void ReturnCombatant(Combatant combatant);
}

public class CombatantMemoryManager : ICombatantMemory {
	private readonly TinyIoCContainer container;
	private readonly FFXIVRepository repository;
	public ICombatantMemory? memory;

	public CombatantMemoryManager(TinyIoCContainer container) {
		this.container = container;
		container.Register<ICombatantMemory74, CombatantMemory74>();
		repository = container.Resolve<FFXIVRepository>();

		var memory = container.Resolve<FFXIVMemory>();
		memory.RegisterOnProcessChangeHandler(FindMemory);
	}

	private void FindMemory(object? sender, Process p) {
		memory = null;
		ScanPointers();
	}

	public void ScanPointers() {
		var candidates = new List<ICombatantMemory> {
			container.Resolve<ICombatantMemory74>()
		};
		memory = FFXIVMemory.FindCandidate(candidates, repository.GetMachinaRegion());
	}

	public bool IsValid() => memory != null && memory.IsValid();

	public Version GetVersion() => IsValid() ? memory.GetVersion() : null;

	public unsafe Combatant GetCombatantFromAddress(BattleChara* address, uint selfCharID) => IsValid() ? memory.GetCombatantFromAddress(address, selfCharID) : null;

	public List<Combatant> GetCombatantList() => IsValid() ? memory.GetCombatantList() : [];

	public Combatant? GetSelfCombatant() => IsValid() ? memory.GetSelfCombatant() : null;

	public void ReturnCombatant(Combatant combatant) {
		if (IsValid()) memory.ReturnCombatant(combatant);
	}
}