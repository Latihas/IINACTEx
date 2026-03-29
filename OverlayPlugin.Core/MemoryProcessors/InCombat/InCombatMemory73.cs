using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;

internal interface IInCombatMemory73 : IInCombatMemory {
}

internal class InCombatMemory73 : InCombatMemory, IInCombatMemory73 {
	private const string inCombatSignature = "74??803D??????????74??488B03488BCBFF50";
	private const int inCombatSignatureOffset = -15;
	private const int inCombatRIPOffset = 1;

	public InCombatMemory73(TinyIoCContainer container) : base(container, inCombatSignature, inCombatSignatureOffset, inCombatRIPOffset) {
	}

	public override Version GetVersion() => new(7, 3);
}