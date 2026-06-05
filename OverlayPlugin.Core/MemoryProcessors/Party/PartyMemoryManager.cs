using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Party;

public interface IPartyMemory : IVersionedMemory {
	PartyListsStruct GetPartyLists();
}

internal class PartyMemoryManager : IPartyMemory {
	private readonly IPartyMemory memory;

	public PartyMemoryManager(TinyIoCContainer container) {
		container.Register<IPartyMemory72, PartyMemory72>();
		memory = container.Resolve<IPartyMemory72>();
		memory.ScanPointers();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => true;

	public Version GetVersion() => memory.GetVersion();

	public PartyListsStruct GetPartyLists() => memory.GetPartyLists();
}