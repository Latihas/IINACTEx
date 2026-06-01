using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target;

public interface ITargetMemory : IVersionedMemory {
	Combatant.Combatant GetTargetCombatant();

	Combatant.Combatant GetFocusCombatant();

	Combatant.Combatant GetHoverCombatant();
}

internal class TargetMemoryManager : ITargetMemory {
	private readonly TinyIoCContainer container;
	private readonly FFXIVRepository repository;
	private ITargetMemory? memory;

	public TargetMemoryManager(TinyIoCContainer container) {
		this.container = container;
		container.Register<ITargetMemory70, TargetMemory70>();
		repository = container.Resolve<FFXIVRepository>();
		container.Resolve<FFXIVMemory>().RegisterOnProcessChangeHandler(FindMemory);
	}

	private void FindMemory(object? sender, Process p) {
		memory = null;
		ScanPointers();
	}

	public void ScanPointers() {
		List<ITargetMemory> candidates = [
			container.Resolve<ITargetMemory70>()
		];
		memory = FFXIVMemory.FindCandidate(candidates, repository.GetMachinaRegion());
	}

	public bool IsValid() => memory != null && memory.IsValid();

	public Version? GetVersion() => !IsValid() ? null : memory?.GetVersion();

	public Combatant.Combatant? GetTargetCombatant() => !IsValid() ? null : memory?.GetTargetCombatant();

	public Combatant.Combatant? GetFocusCombatant() => !IsValid() ? null : memory?.GetFocusCombatant();

	public Combatant.Combatant? GetHoverCombatant() => !IsValid() ? null : memory?.GetHoverCombatant();
}