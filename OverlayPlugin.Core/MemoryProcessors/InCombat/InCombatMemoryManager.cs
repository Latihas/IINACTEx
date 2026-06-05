using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;

public interface IInCombatMemory : IVersionedMemory {
	bool GetInCombat();
}

internal class InCombatMemoryManager : IInCombatMemory {
	private readonly TinyIoCContainer container;
	private readonly FFXIVRepository repository;
	private IInCombatMemory memory;

	public InCombatMemoryManager(TinyIoCContainer container) {
		this.container = container;
		container.Register<IInCombatMemory73, InCombatMemory73>();
		repository = container.Resolve<FFXIVRepository>();
		var memory = container.Resolve<FFXIVMemory>();
		memory.RegisterOnProcessChangeHandler(FindMemory);
	}

	private void FindMemory(object sender, Process p) {
		memory = null;
		ScanPointers();
	}

	public void ScanPointers() {
		var candidates = new List<IInCombatMemory> {
			container.Resolve<IInCombatMemory73>()
		};
		memory = FFXIVMemory.FindCandidate(candidates, repository.GetMachinaRegion());
	}

	public bool IsValid() => memory != null && memory.IsValid();

	public Version GetVersion() => !IsValid() ? null : memory.GetVersion();

	public bool GetInCombat() => IsValid() && memory.GetInCombat();
}