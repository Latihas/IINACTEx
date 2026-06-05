using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;

internal interface IInCombatMemory70 : IInCombatMemory;

internal class InCombatMemory70(TinyIoCContainer container) : InCombatMemory(container, inCombatSignature, inCombatSignatureOffset, inCombatRIPOffset), IInCombatMemory70 {
	private const string inCombatSignature = "803D??????????74??488B03488BCBFF50";
	private const int inCombatSignatureOffset = -15;
	private const int inCombatRIPOffset = 1;

	public override Version GetVersion() => new(7, 0);
}