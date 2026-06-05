using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;

internal interface IInCombatMemory73 : IInCombatMemory;

internal class InCombatMemory73 : InCombatMemory, IInCombatMemory73 {
	public virtual Version GetVersion() => new(7, 3);
}