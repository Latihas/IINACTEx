using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Aggro;

public interface IAggroMemory : IVersionedMemory {
	List<AggroEntry> GetAggroList(List<Combatant.Combatant> combatantList);
}

public class AggroMemoryManager : IAggroMemory {
	private readonly IAggroMemory memory;

	public AggroMemoryManager(TinyIoCContainer container) {
		container.Register<IAggroMemory60, AggroMemory60>();
		memory = container.Resolve<IAggroMemory60>();
		memory.ScanPointers();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => true;

	public Version GetVersion() => memory.GetVersion();

	public List<AggroEntry> GetAggroList(List<Combatant.Combatant> combatantList) => memory.GetAggroList(combatantList);
}