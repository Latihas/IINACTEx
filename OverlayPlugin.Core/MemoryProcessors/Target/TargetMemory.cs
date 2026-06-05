using System;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target;

public abstract class TargetMemory(
	TinyIoCContainer container, int targetTargetOffset, int focusTargetOffset,
	int hoverTargetOffset) : ITargetMemory {
	private readonly ICombatantMemory combatantMemory = container.Resolve<ICombatantMemory>();
	
	public bool IsValid() => true;

	public void ScanPointers() {
	}

	public abstract Version GetVersion();

	private unsafe Combatant.Combatant? GetTargetRelativeCombatant(int offset) {
		var address = (GameObject*)((IntPtr)TargetSystem.Instance() + offset);
		return address == null || !address->IsCharacter() ? null : combatantMemory.GetCombatantFromAddress((BattleChara*)address, 0);
	}

	public Combatant.Combatant GetTargetCombatant() => GetTargetRelativeCombatant(targetTargetOffset);

	public Combatant.Combatant GetFocusCombatant() => GetTargetRelativeCombatant(focusTargetOffset);

	public Combatant.Combatant GetHoverCombatant() => GetTargetRelativeCombatant(hoverTargetOffset);
}