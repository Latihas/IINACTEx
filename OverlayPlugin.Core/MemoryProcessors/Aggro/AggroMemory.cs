using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;
using RainbowMage.OverlayPlugin.MemoryProcessors.Target;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Aggro;

public abstract class AggroMemory(TinyIoCContainer container) : IAggroMemory {
	private readonly ICombatantMemory combatantMemory = container.Resolve<ICombatantMemory>();
	private readonly ITargetMemory targetMemory = container.Resolve<ITargetMemory>();

	public bool IsValid() => true;

	public void ScanPointers() {
	}

	public abstract Version GetVersion();

	public unsafe List<AggroEntry> GetAggroList(List<Combatant.Combatant> combatantList) {
		var mychar = combatantMemory.GetSelfCombatant();

		uint currentTargetID = 0;
		var targetCombatant = targetMemory.GetTargetCombatant();
		if (targetCombatant != null) {
			currentTargetID = targetCombatant.ID;
			combatantMemory.ReturnCombatant(targetCombatant);
		}

		var result = new List<AggroEntry>();

		var list = UIState.Instance()->Hater.Haters;
		foreach (var e in list) {
			if (e.EntityId <= 0)
				continue;
			var e1 = e;
			var c = combatantList.Find(x => x.ID == e1.EntityId);
			if (c == null)
				continue;

			var entry = new AggroEntry {
				ID = e.EntityId,
				// Rather than storing enmity, this is hate rate for the aggro list.
				// This is likely because we're reading the memory for the aggro sidebar.
				HateRate = e.Enmity,
				isCurrentTarget = e.EntityId == currentTargetID,
				IsTargetable = c.IsTargetable,
				Name = c.Name,
				MaxHP = c.MaxHP,
				CurrentHP = c.CurrentHP,
				Effects = c.Effects
			};

			// TODO: it seems like when your chocobo has aggro, this entry
			// is you, and not your chocobo.  It's not clear if there's
			// anything that can be done about it.
			if (c.TargetID > 0) {
				var t = combatantList.Find(x => x.ID == c.TargetID);
				if (t != null) {
					entry.Target = new EnmityEntry {
						ID = t.ID,
						Name = t.Name,
						OwnerID = t.OwnerID,
						isMe = mychar.ID == t.ID ? true : false,
						Enmity = 0,
						HateRate = 0,
						Job = t.Job
					};
				}
			}

			result.Add(entry);
		}

		combatantMemory.ReturnCombatant(mychar);

		return result;
	}
}