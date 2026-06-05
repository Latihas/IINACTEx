using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;

public abstract class EnmityMemory(TinyIoCContainer container) : IEnmityMemory {
	private readonly ICombatantMemory combatantMemory = container.Resolve<ICombatantMemory>();

	public bool IsValid() => true;

	public void ScanPointers() {
	}

	public abstract Version GetVersion();

	private static unsafe Hate ReadEnmityList() => UIState.Instance()->Hate;

	public List<EnmityEntry> GetEnmityEntryList(List<Combatant.Combatant> combatantList) {
		var mychar = combatantMemory.GetSelfCombatant();
		var topEnmity = 0;
		var result = new List<EnmityEntry>();
		foreach (var e in ReadEnmityList().HateInfo) {
			topEnmity = Math.Max(topEnmity, e.Enmity);

			Combatant.Combatant? c = null;
			if (e.EntityId > 0) {
				var e1 = e;
				c = combatantList.Find(x => x.ID == e1.EntityId);
			}
			var entry = new EnmityEntry {
				ID = e.EntityId,
				Enmity = (uint)e.Enmity,
				isMe = e.EntityId == mychar.ID,
				Name = c == null ? "Unknown" : c.Name,
				OwnerID = c?.OwnerID ?? 0,
				HateRate = (int)(e.Enmity / (double)topEnmity * 100),
				Job = c?.Job ?? 0
			};
			result.Add(entry);
		}
		combatantMemory.ReturnCombatant(mychar);
		return result;
	}
}