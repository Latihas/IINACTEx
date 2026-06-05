using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage;

public abstract class AtkStageMemory(TinyIoCContainer container) {
	protected readonly FFXIVMemory memory = container.Resolve<FFXIVMemory>();
	protected ILogger logger = container.Resolve<ILogger>();

	public bool IsValid() => memory.IsValid();

	public void ScanPointers() {
	}

	public abstract Version GetVersion();
}