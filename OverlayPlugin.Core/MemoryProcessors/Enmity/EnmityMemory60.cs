using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;

internal interface IEnmityMemory60 : IEnmityMemory {
}

internal class EnmityMemory60 : EnmityMemory, IEnmityMemory60 {
	public const string enmitySignature = "83f9ff7412448b048e8bd3488d0d";
	private const int enmitySignatureOffset = -2608;

	public EnmityMemory60(TinyIoCContainer container)
		: base(container, enmitySignature, enmitySignatureOffset) {
	}

	public override Version GetVersion() => new(6, 0);
}