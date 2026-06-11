using System;
using System.Linq;
using Advanced_Combat_Tracker;

namespace RainbowMage.OverlayPlugin;

internal class FFXIVExportVariables {
	private static readonly string outH = CombatantData.DamageTypeDataOutgoingHealing;

	public static void Init() {
		// TODO: Profile and optimize if necessary.

		// The code below was taken under MIT license from https://github.com/ZCube/ACTWebSocket/blob/master/ACTWebSocket.Core/Functions/OverlayACTWork.cs.
		// Copyright (c) 2016 ZCube

		if (!CombatantData.ExportVariables.ContainsKey("overHeal")) {
			CombatantData.ExportVariables.Add("overHeal",
				new CombatantData.TextExportFormatter(
					"overHeal", "Overheal", "Amount of healing that made flood over 100% of health.",
					(Data, _) => {
						if (!Data.Items[outH].Items.TryGetValue("All", out var attack)) return "0";
						long sum = 0;
						var swings = attack.Items;
						foreach (var t in swings) {
							if (t.Tags.TryGetValue("overheal", out var value)) {
								sum += Convert.ToInt64(value);
							}
						}
						return sum.ToString();
					}
				)
			);
		}

		if (!CombatantData.ExportVariables.ContainsKey("damageShield")) {
			CombatantData.ExportVariables.Add("damageShield",
				new CombatantData.TextExportFormatter(
					"damageShield", "Damage Shield", "Damage blocked by Shield skills of healer.",
					(Data, _) => {
						if (!Data.Items[outH].Items.TryGetValue("All", out var attack)) return "0";

						var swings = attack.Items;
						var sum = swings.Where(t => t.Special == "DamageShield").Aggregate<MasterSwing, long>(0, (current, t) => (long)(current + t.Damage));

						return sum.ToString();
					}
				)
			);
		}

		if (!CombatantData.ExportVariables.ContainsKey("absorbHeal")) {
			CombatantData.ExportVariables.Add("absorbHeal",
				new CombatantData.TextExportFormatter(
					"absorbHeal", "Healed by Absorbing", "Amount of heal, done by absorbing.",
					(Data, _) => {
						if (!Data.Items[outH].Items.TryGetValue("All", out var attack)) return "0";

						var swings = attack.Items;
						var sum = swings.Where(t => t.DamageType == "Absorb").Aggregate<MasterSwing, long>(0, (current, t) => (long)(current + t.Damage));

						return sum.ToString();
					}
				)
			);
		}
	}
}