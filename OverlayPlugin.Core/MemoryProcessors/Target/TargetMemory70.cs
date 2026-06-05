using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target;

internal interface ITargetMemory70 : ITargetMemory;

internal class TargetMemory70(TinyIoCContainer container) : TargetMemory(container, targetTargetOffset, focusTargetOffset, hoverTargetOffset), ITargetMemory70 {

	// Offsets from the targetAddress to find the correct target type.
	private const int targetTargetOffset = 176;
	private const int focusTargetOffset = 248;
	private const int hoverTargetOffset = 208;
	public override Version GetVersion() => new(7, 0);
}