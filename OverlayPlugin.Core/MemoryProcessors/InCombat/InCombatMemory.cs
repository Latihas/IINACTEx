using FFXIVClientStructs.FFXIV.Client.Game;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;

public abstract class InCombatMemory {
	public bool IsValid() => true;
	public void ScanPointers() {
	}

	public unsafe bool GetInCombat() => Conditions.Instance()->InCombat;
}