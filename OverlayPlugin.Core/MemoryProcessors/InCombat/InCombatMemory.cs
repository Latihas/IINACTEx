using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;

public abstract class InCombatMemory(TinyIoCContainer container, string inCombatSignature, int inCombatSignatureOffset, int inCombatRipOffset) {
	protected readonly FFXIVMemory memory = container.Resolve<FFXIVMemory>();
	protected readonly ILogger logger = container.Resolve<ILogger>();

	protected IntPtr inCombatAddress = IntPtr.Zero;

	private void ResetPointers() => inCombatAddress = IntPtr.Zero;

	private bool HasValidPointers() => inCombatAddress != IntPtr.Zero;

	public bool IsValid() => memory.IsValid() && HasValidPointers();

	public void ScanPointers() {
		ResetPointers();
		if (!memory.IsValid())
			return;

		var fail = new List<string>();

		var list = memory.SigScan(inCombatSignature, inCombatSignatureOffset, true, inCombatRipOffset);

		if (list is { Count: > 0 }) {
			inCombatAddress = list[0];
		} else {
			inCombatAddress = IntPtr.Zero;
			fail.Add(nameof(inCombatAddress));
		}


		logger.Log(LogLevel.Debug, "inCombatAddress: 0x{0:X}", inCombatAddress.ToInt64());

		if (fail.Count == 0) {
			logger.Log(LogLevel.Info, $"Found in combat memory via {GetType().Name}.");
			return;
		}

		logger.Log(LogLevel.Error,
			$"Failed to find in combat memory via {GetType().Name}: {string.Join(", ", fail)}.");
	}

	public abstract Version GetVersion();

	public bool GetInCombat() {
		if (!IsValid())
			return false;
		var bytes = memory.Read8(inCombatAddress, 1);
		return bytes[0] != 0;
	}
}