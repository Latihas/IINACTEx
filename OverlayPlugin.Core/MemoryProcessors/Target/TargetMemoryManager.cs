using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target;

public interface ITargetMemory : IVersionedMemory {
	Combatant.Combatant? GetTargetCombatant();

	Combatant.Combatant? GetFocusCombatant();

	Combatant.Combatant? GetHoverCombatant();
}

internal class TargetMemoryManager : ITargetMemory {
	private readonly ITargetMemory memory;

	public TargetMemoryManager(TinyIoCContainer container) {
		container.Register<ITargetMemory70, TargetMemory70>();
		memory = container.Resolve<ITargetMemory70>();
		memory.ScanPointers();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => true;

	public Version GetVersion() => memory.GetVersion();

	public Combatant.Combatant? GetTargetCombatant() => memory.GetTargetCombatant();

	public Combatant.Combatant? GetFocusCombatant() => memory.GetFocusCombatant();

	public Combatant.Combatant? GetHoverCombatant() => memory.GetHoverCombatant();
}