using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;

internal interface IEnmityMemory60 : IEnmityMemory;

internal class EnmityMemory60(TinyIoCContainer container) : EnmityMemory(container), IEnmityMemory60 {
	public override Version GetVersion() => new(6, 0);
}