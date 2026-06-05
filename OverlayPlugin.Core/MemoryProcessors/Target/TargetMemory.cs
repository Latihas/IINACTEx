using System;
using System.Collections.Generic;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target;

public abstract class TargetMemory(
	TinyIoCContainer container, string targetSignature, int targetTargetOffset, int focusTargetOffset,
	int hoverTargetOffset)
	: ITargetMemory {
	private readonly FFXIVMemory memory = container.Resolve<FFXIVMemory>();
	private readonly ILogger logger = container.Resolve<ILogger>();
	private readonly ICombatantMemory combatantMemory = container.Resolve<ICombatantMemory>();

	private IntPtr targetAddress = IntPtr.Zero;

	// Offsets from the targetAddress to find the correct target type.

	private void ResetPointers() {
		targetAddress = IntPtr.Zero;
	}

	private bool HasValidPointers() {
		if (targetAddress == IntPtr.Zero)
			return false;
		return true;
	}

	public bool IsValid() {
		if (!memory.IsValid())
			return false;

		if (!HasValidPointers())
			return false;

		return true;
	}

	public void ScanPointers() {
		ResetPointers();
		if (!memory.IsValid())
			return;

		var fail = new List<string>();

		var list = memory.SigScan(targetSignature, 0, true);
		if (list != null && list.Count > 0) {
			targetAddress = list[0];
		} else {
			targetAddress = IntPtr.Zero;
			fail.Add(nameof(targetAddress));
		}

		logger.Log(LogLevel.Debug, "targetAddress: 0x{0:X}", targetAddress.ToInt64());

		if (fail.Count == 0) {
			logger.Log(LogLevel.Info, $"Found target memory via {GetType().Name}.");
			return;
		}

		logger.Log(LogLevel.Error,
			$"Failed to find target memory via {GetType().Name}: {string.Join(", ", fail)}.");
	}

	public abstract Version GetVersion();

	private Combatant.Combatant GetTargetRelativeCombatant(int offset) {
		var address = memory.ReadIntPtr(IntPtr.Add(targetAddress, offset));
		if (address == IntPtr.Zero)
			return null;

		return combatantMemory.GetCombatantFromAddress(address, 0);
	}

	public Combatant.Combatant GetTargetCombatant() => GetTargetRelativeCombatant(targetTargetOffset);

	public Combatant.Combatant GetFocusCombatant() => GetTargetRelativeCombatant(focusTargetOffset);

	public Combatant.Combatant GetHoverCombatant() => GetTargetRelativeCombatant(hoverTargetOffset);
}