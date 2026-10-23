using System;
using System.Collections.Generic;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors;

internal class LineFateControl : LineBaseSubMachina<LineFateControl.FateControlPacket> {
	public static readonly Server_ActorControlCategory[] FateActorControlCategories = [
		Server_ActorControlCategory.FateAdd,
		Server_ActorControlCategory.FateRemove,
		Server_ActorControlCategory.FateUpdate
	];

	internal class FateControlPacket : MachinaPacketWrapper {
		public override string? ToString(long epoch, uint ActorID) {
			var category = Get<Server_ActorControlCategory>("category");

			if (!FateActorControlCategories.Contains(category)) return null;

			var padding = Get<ushort>("padding");
			var fateID = Get<uint>("param1");
			var progress = Get<uint>("param2");
			var param3 = Get<uint>("param3");
			var param4 = Get<uint>("param4");
			var param5 = Get<uint>("param5");
			var param6 = Get<uint>("param6");
			var padding1 = Get<uint>("padding1");

			var categoryStr = category.ToString().Replace("Fate", "");

			switch (category) {
				// Do some basic filtering on fate data to avoid spamming the log needlessly.
				case Server_ActorControlCategory.FateAdd when !fates.TryAdd(fateID, 0):
				case Server_ActorControlCategory.FateRemove when !fates.Remove(fateID): return null;
				case Server_ActorControlCategory.FateUpdate: {
					if (fates.TryGetValue(fateID, out var oldProgress)) {
						if (progress == oldProgress) {
							return null;
						}
					}
					fates[fateID] = progress;
					break;
				}
			}

			return $"{categoryStr}|" +
			       $"{padding:X4}|" +
			       $"{fateID:X8}|" +
			       $"{progress:X8}|" +
			       $"{param3:X8}|" +
			       $"{param4:X8}|" +
			       $"{param5:X8}|" +
			       $"{param6:X8}|" +
			       $"{padding1:X8}";
		}
	}

	private static readonly Dictionary<uint, uint> fates = new();

	public const uint LogFileLineID = 258;

	public const string LogLineName = "FateDirector";
	public const string MachinaPacketName = "ActorControlSelf";

	public LineFateControl(TinyIoCContainer container)
		: base(container, LogFileLineID, LogLineName, MachinaPacketName) {
		ffxiv.RegisterZoneChangeDelegate((_, _) => fates.Clear());
	}
}