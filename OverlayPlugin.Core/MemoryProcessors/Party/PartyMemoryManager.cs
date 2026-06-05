using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Party;

public interface IPartyMemory : IVersionedMemory {
	PartyListsStruct GetPartyLists();
}

internal class PartyMemoryManager : IPartyMemory {
	private readonly TinyIoCContainer container;
	private readonly FFXIVRepository repository;
	private IPartyMemory memory;

	public PartyMemoryManager(TinyIoCContainer container) {
		this.container = container;
		container.Register<IPartyMemory72, PartyMemory72>();
		repository = container.Resolve<FFXIVRepository>();

		var memory = container.Resolve<FFXIVMemory>();
		memory.RegisterOnProcessChangeHandler(FindMemory);
	}

	private void FindMemory(object sender, Process p) {
		memory = null;
		ScanPointers();
	}

	public void ScanPointers() {
		var candidates = new List<IPartyMemory> {
			container.Resolve<IPartyMemory72>()
		};
		memory = FFXIVMemory.FindCandidate(candidates, repository.GetMachinaRegion());
	}

	public bool IsValid() => true;

	public Version GetVersion() {
		return memory.GetVersion();
	}

	public PartyListsStruct GetPartyLists() {
		if (!IsValid()) {
			return new PartyListsStruct();
		}
		return memory.GetPartyLists();
	}
}