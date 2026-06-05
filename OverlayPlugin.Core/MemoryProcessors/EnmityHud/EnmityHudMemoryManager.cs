using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.EnmityHud;

public interface IEnmityHudMemory : IVersionedMemory {
	List<EnmityHudEntry> GetEnmityHudEntries();
}

public class EnmityHudMemoryManager : IEnmityHudMemory {
	public readonly IEnmityHudMemory memory;

	public EnmityHudMemoryManager(TinyIoCContainer container) {
		container.Register<IEnmityHudMemory73, EnmityHudMemory73>();
		memory = container.Resolve<IEnmityHudMemory73>();
		memory.ScanPointers();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => true;

	public Version GetVersion() => memory.GetVersion();

	public List<EnmityHudEntry> GetEnmityHudEntries() => memory.GetEnmityHudEntries();
}