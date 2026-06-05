using FFXIVClientStructs.FFXIV.Client.Game;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;

public abstract class InCombatMemory {
	private void ResetPointers() { }
	private bool HasValidPointers() => true;
	public bool IsValid() => true;
	public void ScanPointers() {
	}

	public unsafe bool GetInCombat() => Conditions.Instance()->InCombat;
}