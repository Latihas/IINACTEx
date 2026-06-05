using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;

public interface IEnmityMemory : IVersionedMemory {
	List<EnmityEntry> GetEnmityEntryList(List<Combatant.Combatant> combatantList);
}

public class EnmityMemoryManager : IEnmityMemory {
	private readonly TinyIoCContainer container;
	private readonly FFXIVRepository repository;
	private IEnmityMemory memory;

	public EnmityMemoryManager(TinyIoCContainer container) {
		this.container = container;
		container.Register<IEnmityMemory60, EnmityMemory60>();
		repository = container.Resolve<FFXIVRepository>();

		var memory = container.Resolve<FFXIVMemory>();
		memory.RegisterOnProcessChangeHandler(FindMemory);
	}

	private void FindMemory(object sender, Process p) {
		memory = null;
		ScanPointers();
	}

	public void ScanPointers() {
		var candidates = new List<IEnmityMemory> { container.Resolve<IEnmityMemory60>() };
		memory = FFXIVMemory.FindCandidate(candidates, repository.GetMachinaRegion());
	}

	public bool IsValid() => true;

	public Version GetVersion() {
		if (!IsValid())
			return null;
		return memory.GetVersion();
	}

	public List<EnmityEntry> GetEnmityEntryList(List<Combatant.Combatant> combatantList) {
		if (!IsValid()) {
			return null;
		}

		return memory.GetEnmityEntryList(combatantList);
	}
}