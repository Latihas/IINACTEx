using System;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target;

public abstract class TargetMemory(TinyIoCContainer container) : ITargetMemory {
	private readonly ICombatantMemory combatantMemory = container.Resolve<ICombatantMemory>();

	public bool IsValid() => true;

	public void ScanPointers() {
	}

	public abstract Version GetVersion();

	public unsafe Combatant.Combatant? GetTargetCombatant() {
		var t = TargetSystem.Instance()->GetTargetObject();
		return t == null ? null : combatantMemory.GetCombatantFromAddress(t, 0);
	}

	public unsafe Combatant.Combatant? GetFocusCombatant() {
		var t = TargetSystem.Instance()->FocusTarget;
		return t == null ? null : combatantMemory.GetCombatantFromAddress(t, 0);
	}

	public unsafe Combatant.Combatant? GetHoverCombatant() {
		var t = TargetSystem.Instance()->MouseOverTarget;
		return t == null ? null : combatantMemory.GetCombatantFromAddress(t, 0);
	}
}