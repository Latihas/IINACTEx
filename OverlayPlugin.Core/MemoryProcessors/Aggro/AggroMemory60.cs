using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Aggro;

internal interface IAggroMemory60 : IAggroMemory;

internal class AggroMemory60(TinyIoCContainer container) : AggroMemory(container), IAggroMemory60 {
	public override Version GetVersion() => new(6, 0);
}