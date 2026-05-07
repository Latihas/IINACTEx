using System.Collections.Generic;
using Newtonsoft.Json;

namespace IINACT.Latihas.Overlay;

public class CombatDataWrapper {
	[JsonProperty] public string type { get; set; }

	[JsonProperty] public string msgtype { get; set; }

	[JsonProperty] public CombatData msg { get; set; }
}

public class CombatData {
	[JsonProperty] public string type { get; set; }

	[JsonProperty] public Encounter Encounter { get; set; }

	[JsonProperty] public Dictionary<string, Combatant> Combatant { get; set; }

	[JsonProperty] public string isActive { get; set; }
}

public class Encounter {
	[JsonProperty] public string n { get; set; }

	[JsonProperty] public string t { get; set; }

	[JsonProperty] public string title { get; set; }

	[JsonProperty] public string duration { get; set; }

	[JsonProperty] public string DURATION { get; set; }

	[JsonProperty] public string damage { get; set; }

	[JsonProperty("damage-m")] public string damage_m { get; set; }

	[JsonProperty("damage-*")] public string damage_star { get; set; }

	[JsonProperty("DAMAGE-k")] public string DAMAGE_k { get; set; }

	[JsonProperty("DAMAGE-m")] public string DAMAGE_m { get; set; }

	[JsonProperty("DAMAGE-b")] public string DAMAGE_b { get; set; }

	[JsonProperty("DAMAGE-*")] public string DAMAGE_star { get; set; }

	[JsonProperty("dps")] public string dps { get; set; }

	[JsonProperty("dps-*")] public string dps_star { get; set; }

	[JsonProperty] public string DPS { get; set; }

	[JsonProperty("DPS-k")] public string DPS_k { get; set; }

	[JsonProperty("DPS-m")] public string DPS_m { get; set; }

	[JsonProperty("DPS-*")] public string DPS_star { get; set; }

	[JsonProperty] public string encdps { get; set; }

	[JsonProperty("encdps-*")] public string encdps_star { get; set; }

	[JsonProperty] public string ENCDPS { get; set; }

	[JsonProperty("ENCDPS-k")] public string ENCDPS_k { get; set; }

	[JsonProperty("ENCDPS-m")] public string ENCDPS_m { get; set; }

	[JsonProperty("ENCDPS-*")] public string ENCDPS_star { get; set; }

	[JsonProperty] public string hits { get; set; }

	[JsonProperty] public string crithits { get; set; }

	[JsonProperty("crithit%")] public string crithit_percent { get; set; }

	[JsonProperty] public string misses { get; set; }

	[JsonProperty] public string hitfailed { get; set; }

	[JsonProperty] public string swings { get; set; }

	[JsonProperty] public string tohit { get; set; }

	[JsonProperty] public string TOHIT { get; set; }

	[JsonProperty] public string maxhit { get; set; }

	[JsonProperty] public string MAXHIT { get; set; }

	[JsonProperty("maxhit-*")] public string maxhit_star { get; set; }

	[JsonProperty("MAXHIT-*")] public string MAXHIT_star { get; set; }

	[JsonProperty] public string healed { get; set; }

	[JsonProperty] public string enchps { get; set; }

	[JsonProperty("enchps-*")] public string enchps_star { get; set; }

	[JsonProperty] public string ENCHPS { get; set; }

	[JsonProperty("ENCHPS-k")] public string ENCHPS_k { get; set; }

	[JsonProperty("ENCHPS-m")] public string ENCHPS_m { get; set; }

	[JsonProperty("ENCHPS-*")] public string ENCHPS_star { get; set; }

	[JsonProperty] public string heals { get; set; }

	[JsonProperty] public string critheals { get; set; }

	[JsonProperty("critheal%")] public string critheal_percent { get; set; }

	[JsonProperty] public string cures { get; set; }

	[JsonProperty] public string maxheal { get; set; }

	[JsonProperty] public string MAXHEAL { get; set; }

	[JsonProperty] public string maxhealward { get; set; }

	[JsonProperty] public string MAXHEALWARD { get; set; }

	[JsonProperty("maxheal-*")] public string maxheal_star { get; set; }

	[JsonProperty("MAXHEAL-*")] public string MAXHEAL_star { get; set; }

	[JsonProperty("maxhealward-*")] public string maxhealward_star { get; set; }

	[JsonProperty("MAXHEALWARD-*")] public string MAXHEALWARD_star { get; set; }

	[JsonProperty] public string damagetaken { get; set; }

	[JsonProperty("damagetaken-*")] public string damagetaken_star { get; set; }

	[JsonProperty] public string healstaken { get; set; }

	[JsonProperty("healstaken-*")] public string healstaken_star { get; set; }

	[JsonProperty] public string powerdrain { get; set; }

	[JsonProperty("powerdrain-*")] public string powerdrain_star { get; set; }

	[JsonProperty] public string powerheal { get; set; }

	[JsonProperty("powerheal-*")] public string powerheal_star { get; set; }

	[JsonProperty] public string kills { get; set; }

	[JsonProperty] public string deaths { get; set; }

	[JsonProperty] public string CurrentZoneName { get; set; }

	[JsonProperty] public string Last10DPS { get; set; }

	[JsonProperty] public string Last30DPS { get; set; }

	[JsonProperty] public string Last60DPS { get; set; }
}

public class Combatant {
	[JsonProperty] public string n { get; set; }

	[JsonProperty] public string t { get; set; }

	[JsonProperty] public string name { get; set; }

	[JsonProperty] public string duration { get; set; }

	[JsonProperty] public string DURATION { get; set; }

	[JsonProperty] public string damage { get; set; }

	[JsonProperty("damage-m")] public string damage_m { get; set; }

	[JsonProperty("damage-b")] public string damage_b { get; set; }

	[JsonProperty("damage-*")] public string damage_star { get; set; }

	[JsonProperty("DAMAGE-k")] public string DAMAGE_k { get; set; }

	[JsonProperty("DAMAGE-m")] public string DAMAGE_m { get; set; }

	[JsonProperty("DAMAGE-b")] public string DAMAGE_b { get; set; }

	[JsonProperty("DAMAGE-*")] public string DAMAGE_star { get; set; }

	[JsonProperty("damage%")] public string damage_percent { get; set; }

	[JsonProperty] public string dps { get; set; }

	[JsonProperty("dps-*")] public string dps_star { get; set; }

	[JsonProperty] public string DPS { get; set; }

	[JsonProperty("DPS-k")] public string DPS_k { get; set; }

	[JsonProperty("DPS-m")] public string DPS_m { get; set; }

	[JsonProperty("DPS-*")] public string DPS_star { get; set; }

	[JsonProperty] public string encdps { get; set; }

	[JsonProperty("encdps-*")] public string encdps_star { get; set; }

	[JsonProperty] public string ENCDPS { get; set; }

	[JsonProperty("ENCDPS-k")] public string ENCDPS_k { get; set; }

	[JsonProperty("ENCDPS-m")] public string ENCDPS_m { get; set; }

	[JsonProperty("ENCDPS-*")] public string ENCDPS_star { get; set; }

	[JsonProperty] public string hits { get; set; }

	[JsonProperty] public string crithits { get; set; }

	[JsonProperty("crithit%")] public string crithit_percent { get; set; }

	[JsonProperty] public string crittypes { get; set; }

	[JsonProperty] public string misses { get; set; }

	[JsonProperty] public string hitfailed { get; set; }

	[JsonProperty] public string swings { get; set; }

	[JsonProperty] public string tohit { get; set; }

	[JsonProperty] public string TOHIT { get; set; }

	[JsonProperty] public string maxhit { get; set; }

	[JsonProperty] public string MAXHIT { get; set; }

	[JsonProperty("maxhit-*")] public string maxhit_star { get; set; }

	[JsonProperty("MAXHIT-*")] public string MAXHIT_star { get; set; }

	[JsonProperty] public string healed { get; set; }

	[JsonProperty("healed%")] public string healed_percent { get; set; }

	[JsonProperty] public string enchps { get; set; }

	[JsonProperty("enchps-*")] public string enchps_star { get; set; }

	[JsonProperty] public string ENCHPS { get; set; }

	[JsonProperty("ENCHPS-k")] public string ENCHPS_k { get; set; }

	[JsonProperty("ENCHPS-m")] public string ENCHPS_m { get; set; }

	[JsonProperty("ENCHPS-*")] public string ENCHPS_star { get; set; }

	[JsonProperty] public string critheals { get; set; }

	[JsonProperty("critheal%")] public string critheal_percent { get; set; }

	[JsonProperty] public string heals { get; set; }

	[JsonProperty] public string cures { get; set; }

	[JsonProperty] public string maxheal { get; set; }

	[JsonProperty] public string MAXHEAL { get; set; }

	[JsonProperty] public string maxhealward { get; set; }

	[JsonProperty] public string MAXHEALWARD { get; set; }

	[JsonProperty("maxheal-*")] public string maxheal_star { get; set; }

	[JsonProperty("MAXHEAL-*")] public string MAXHEAL_star { get; set; }

	[JsonProperty("maxhealward-*")] public string maxhealward_star { get; set; }

	[JsonProperty("MAXHEALWARD-*")] public string MAXHEALWARD_star { get; set; }

	[JsonProperty] public string damagetaken { get; set; }

	[JsonProperty("damagetaken-*")] public string damagetaken_star { get; set; }

	[JsonProperty] public string healstaken { get; set; }

	[JsonProperty("healstaken-*")] public string healstaken_star { get; set; }

	[JsonProperty] public string powerdrain { get; set; }

	[JsonProperty("powerdrain-*")] public string powerdrain_star { get; set; }

	[JsonProperty] public string powerheal { get; set; }

	[JsonProperty("powerheal-*")] public string powerheal_star { get; set; }

	[JsonProperty] public string kills { get; set; }

	[JsonProperty] public string deaths { get; set; }

	[JsonProperty] public string threatstr { get; set; }

	[JsonProperty] public string threatdelta { get; set; }

	[JsonProperty] public string Last10DPS { get; set; }

	[JsonProperty] public string Last30DPS { get; set; }

	[JsonProperty] public string Last60DPS { get; set; }

	[JsonProperty] public string Job { get; set; }

	[JsonProperty] public string ParryPct { get; set; }

	[JsonProperty] public string BlockPct { get; set; }

	[JsonProperty] public string IncToHit { get; set; }

	[JsonProperty] public string OverHealPct { get; set; }

	[JsonProperty] public string DirectHitPct { get; set; }

	[JsonProperty] public string DirectHitCount { get; set; }

	[JsonProperty] public string CritDirectHitCount { get; set; }

	[JsonProperty] public string CritDirectHitPct { get; set; }

	[JsonProperty] public string overHeal { get; set; }

	[JsonProperty] public string damageShield { get; set; }

	[JsonProperty] public string absorbHeal { get; set; }
}