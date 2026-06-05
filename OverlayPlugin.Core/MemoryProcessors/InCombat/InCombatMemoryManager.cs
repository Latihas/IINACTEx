using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;

public interface IInCombatMemory : IVersionedMemory {
	bool GetInCombat();
}

internal class InCombatMemoryManager : IInCombatMemory {
	private readonly IInCombatMemory memory;

	public InCombatMemoryManager(TinyIoCContainer container) {
		container.Register<IInCombatMemory73, InCombatMemory73>();
		memory = container.Resolve<IInCombatMemory73>();
		memory.ScanPointers();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => true;

	public Version GetVersion() => memory.GetVersion();

	public bool GetInCombat() => memory.GetInCombat();
}