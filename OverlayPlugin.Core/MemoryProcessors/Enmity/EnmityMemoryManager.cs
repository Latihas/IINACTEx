using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;

public interface IEnmityMemory : IVersionedMemory {
	List<EnmityEntry> GetEnmityEntryList(List<Combatant.Combatant> combatantList);
}

public class EnmityMemoryManager : IEnmityMemory {
	private readonly IEnmityMemory memory;

	public EnmityMemoryManager(TinyIoCContainer container) {
		container.Register<IEnmityMemory60, EnmityMemory60>();
		memory = container.Resolve<IEnmityMemory60>();
		memory.ScanPointers();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => true;

	public Version GetVersion() => memory.GetVersion();

	public List<EnmityEntry> GetEnmityEntryList(List<Combatant.Combatant> combatantList) =>
		memory.GetEnmityEntryList(combatantList);
}