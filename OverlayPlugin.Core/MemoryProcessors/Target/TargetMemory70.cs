using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target;

internal interface ITargetMemory70 : ITargetMemory;

internal class TargetMemory70(TinyIoCContainer container) : TargetMemory(container), ITargetMemory70 {
	public override Version GetVersion() => new(7, 0);
}