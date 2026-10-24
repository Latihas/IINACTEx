using System.Collections.Generic;
using Newtonsoft.Json;

namespace IINACT.Latihas.Overlay;

public class CombatDataWrapper {
	[JsonProperty] public string type { get; set; } = null!;

	[JsonProperty] public string msgtype { get; set; } = null!;

	[JsonProperty] public CombatData msg { get; set; } = null!;
}

public class CombatData {
	[JsonProperty] public string type { get; set; } = null!;

	[JsonProperty] public Encounter Encounter { get; set; } = null!;

	[JsonProperty] public Dictionary<string, Combatant> Combatant { get; set; } = null!;

	[JsonProperty] public string isActive { get; set; } = null!;
}

public class Encounter {
	[JsonProperty] public string n { get; set; } = null!;

	[JsonProperty] public string t { get; set; } = null!;

	[JsonProperty] public string title { get; set; } = null!;

	[JsonProperty] public string duration { get; set; } = null!;

	[JsonProperty] public string DURATION { get; set; } = null!;

	[JsonProperty] public string damage { get; set; } = null!;

	[JsonProperty("damage-m")] public string damage_m { get; set; } = null!;

	[JsonProperty("damage-*")] public string damage_star { get; set; } = null!;

	[JsonProperty("DAMAGE-k")] public string DAMAGE_k { get; set; } = null!;

	[JsonProperty("DAMAGE-m")] public string DAMAGE_m { get; set; } = null!;

	[JsonProperty("DAMAGE-b")] public string DAMAGE_b { get; set; } = null!;

	[JsonProperty("DAMAGE-*")] public string DAMAGE_star { get; set; } = null!;

	[JsonProperty("dps")] public string dps { get; set; } = null!;

	[JsonProperty("dps-*")] public string dps_star { get; set; } = null!;

	[JsonProperty] public string DPS { get; set; } = null!;

	[JsonProperty("DPS-k")] public string DPS_k { get; set; } = null!;

	[JsonProperty("DPS-m")] public string DPS_m { get; set; } = null!;

	[JsonProperty("DPS-*")] public string DPS_star { get; set; } = null!;

	[JsonProperty] public string encdps { get; set; } = null!;

	[JsonProperty("encdps-*")] public string encdps_star { get; set; } = null!;

	[JsonProperty] public string ENCDPS { get; set; } = null!;

	[JsonProperty("ENCDPS-k")] public string ENCDPS_k { get; set; } = null!;

	[JsonProperty("ENCDPS-m")] public string ENCDPS_m { get; set; } = null!;

	[JsonProperty("ENCDPS-*")] public string ENCDPS_star { get; set; } = null!;

	[JsonProperty] public string hits { get; set; } = null!;

	[JsonProperty] public string crithits { get; set; } = null!;

	[JsonProperty("crithit%")] public string crithit_percent { get; set; } = null!;

	[JsonProperty] public string misses { get; set; } = null!;

	[JsonProperty] public string hitfailed { get; set; } = null!;

	[JsonProperty] public string swings { get; set; } = null!;

	[JsonProperty] public string tohit { get; set; } = null!;

	[JsonProperty] public string TOHIT { get; set; } = null!;

	[JsonProperty] public string maxhit { get; set; } = null!;

	[JsonProperty] public string MAXHIT { get; set; } = null!;

	[JsonProperty("maxhit-*")] public string maxhit_star { get; set; } = null!;

	[JsonProperty("MAXHIT-*")] public string MAXHIT_star { get; set; } = null!;

	[JsonProperty] public string healed { get; set; } = null!;

	[JsonProperty] public string enchps { get; set; } = null!;

	[JsonProperty("enchps-*")] public string enchps_star { get; set; } = null!;

	[JsonProperty] public string ENCHPS { get; set; } = null!;

	[JsonProperty("ENCHPS-k")] public string ENCHPS_k { get; set; } = null!;

	[JsonProperty("ENCHPS-m")] public string ENCHPS_m { get; set; } = null!;

	[JsonProperty("ENCHPS-*")] public string ENCHPS_star { get; set; } = null!;

	[JsonProperty] public string heals { get; set; } = null!;

	[JsonProperty] public string critheals { get; set; } = null!;

	[JsonProperty("critheal%")] public string critheal_percent { get; set; } = null!;

	[JsonProperty] public string cures { get; set; } = null!;

	[JsonProperty] public string maxheal { get; set; } = null!;

	[JsonProperty] public string MAXHEAL { get; set; } = null!;

	[JsonProperty] public string maxhealward { get; set; } = null!;

	[JsonProperty] public string MAXHEALWARD { get; set; } = null!;

	[JsonProperty("maxheal-*")] public string maxheal_star { get; set; } = null!;

	[JsonProperty("MAXHEAL-*")] public string MAXHEAL_star { get; set; } = null!;

	[JsonProperty("maxhealward-*")] public string maxhealward_star { get; set; } = null!;

	[JsonProperty("MAXHEALWARD-*")] public string MAXHEALWARD_star { get; set; } = null!;

	[JsonProperty] public string damagetaken { get; set; } = null!;

	[JsonProperty("damagetaken-*")] public string damagetaken_star { get; set; } = null!;

	[JsonProperty] public string healstaken { get; set; } = null!;

	[JsonProperty("healstaken-*")] public string healstaken_star { get; set; } = null!;

	[JsonProperty] public string powerdrain { get; set; } = null!;

	[JsonProperty("powerdrain-*")] public string powerdrain_star { get; set; } = null!;

	[JsonProperty] public string powerheal { get; set; } = null!;

	[JsonProperty("powerheal-*")] public string powerheal_star { get; set; } = null!;

	[JsonProperty] public string kills { get; set; } = null!;

	[JsonProperty] public string deaths { get; set; } = null!;

	[JsonProperty] public string CurrentZoneName { get; set; } = null!;

	[JsonProperty] public string Last10DPS { get; set; } = null!;

	[JsonProperty] public string Last30DPS { get; set; } = null!;

	[JsonProperty] public string Last60DPS { get; set; } = null!;
}

public class Combatant {
	[JsonProperty] public string n { get; set; } = null!;

	[JsonProperty] public string t { get; set; } = null!;

	[JsonProperty] public string name { get; set; } = null!;

	[JsonProperty] public string duration { get; set; } = null!;

	[JsonProperty] public string DURATION { get; set; } = null!;

	[JsonProperty] public string damage { get; set; } = null!;

	[JsonProperty("damage-m")] public string damage_m { get; set; } = null!;

	[JsonProperty("damage-b")] public string damage_b { get; set; } = null!;

	[JsonProperty("damage-*")] public string damage_star { get; set; } = null!;

	[JsonProperty("DAMAGE-k")] public string DAMAGE_k { get; set; } = null!;

	[JsonProperty("DAMAGE-m")] public string DAMAGE_m { get; set; } = null!;

	[JsonProperty("DAMAGE-b")] public string DAMAGE_b { get; set; } = null!;

	[JsonProperty("DAMAGE-*")] public string DAMAGE_star { get; set; } = null!;

	[JsonProperty("damage%")] public string damage_percent { get; set; } = null!;

	[JsonProperty] public string dps { get; set; } = null!;

	[JsonProperty("dps-*")] public string dps_star { get; set; } = null!;

	[JsonProperty] public string DPS { get; set; } = null!;

	[JsonProperty("DPS-k")] public string DPS_k { get; set; } = null!;

	[JsonProperty("DPS-m")] public string DPS_m { get; set; } = null!;

	[JsonProperty("DPS-*")] public string DPS_star { get; set; } = null!;

	[JsonProperty] public string encdps { get; set; } = null!;

	[JsonProperty("encdps-*")] public string encdps_star { get; set; } = null!;

	[JsonProperty] public string ENCDPS { get; set; } = null!;

	[JsonProperty("ENCDPS-k")] public string ENCDPS_k { get; set; } = null!;

	[JsonProperty("ENCDPS-m")] public string ENCDPS_m { get; set; } = null!;

	[JsonProperty("ENCDPS-*")] public string ENCDPS_star { get; set; } = null!;

	[JsonProperty] public string hits { get; set; } = null!;

	[JsonProperty] public string crithits { get; set; } = null!;

	[JsonProperty("crithit%")] public string crithit_percent { get; set; } = null!;

	[JsonProperty] public string crittypes { get; set; } = null!;

	[JsonProperty] public string misses { get; set; } = null!;

	[JsonProperty] public string hitfailed { get; set; } = null!;

	[JsonProperty] public string swings { get; set; } = null!;

	[JsonProperty] public string tohit { get; set; } = null!;

	[JsonProperty] public string TOHIT { get; set; } = null!;

	[JsonProperty] public string maxhit { get; set; } = null!;

	[JsonProperty] public string MAXHIT { get; set; } = null!;

	[JsonProperty("maxhit-*")] public string maxhit_star { get; set; } = null!;

	[JsonProperty("MAXHIT-*")] public string MAXHIT_star { get; set; } = null!;

	[JsonProperty] public string healed { get; set; } = null!;

	[JsonProperty("healed%")] public string healed_percent { get; set; } = null!;

	[JsonProperty] public string enchps { get; set; } = null!;

	[JsonProperty("enchps-*")] public string enchps_star { get; set; } = null!;

	[JsonProperty] public string ENCHPS { get; set; } = null!;

	[JsonProperty("ENCHPS-k")] public string ENCHPS_k { get; set; } = null!;

	[JsonProperty("ENCHPS-m")] public string ENCHPS_m { get; set; } = null!;

	[JsonProperty("ENCHPS-*")] public string ENCHPS_star { get; set; } = null!;

	[JsonProperty] public string critheals { get; set; } = null!;

	[JsonProperty("critheal%")] public string critheal_percent { get; set; } = null!;

	[JsonProperty] public string heals { get; set; } = null!;

	[JsonProperty] public string cures { get; set; } = null!;

	[JsonProperty] public string maxheal { get; set; } = null!;

	[JsonProperty] public string MAXHEAL { get; set; } = null!;

	[JsonProperty] public string maxhealward { get; set; } = null!;

	[JsonProperty] public string MAXHEALWARD { get; set; } = null!;

	[JsonProperty("maxheal-*")] public string maxheal_star { get; set; } = null!;

	[JsonProperty("MAXHEAL-*")] public string MAXHEAL_star { get; set; } = null!;

	[JsonProperty("maxhealward-*")] public string maxhealward_star { get; set; } = null!;

	[JsonProperty("MAXHEALWARD-*")] public string MAXHEALWARD_star { get; set; } = null!;

	[JsonProperty] public string damagetaken { get; set; } = null!;

	[JsonProperty("damagetaken-*")] public string damagetaken_star { get; set; } = null!;

	[JsonProperty] public string healstaken { get; set; } = null!;

	[JsonProperty("healstaken-*")] public string healstaken_star { get; set; } = null!;

	[JsonProperty] public string powerdrain { get; set; } = null!;

	[JsonProperty("powerdrain-*")] public string powerdrain_star { get; set; } = null!;

	[JsonProperty] public string powerheal { get; set; } = null!;

	[JsonProperty("powerheal-*")] public string powerheal_star { get; set; } = null!;

	[JsonProperty] public string kills { get; set; } = null!;

	[JsonProperty] public string deaths { get; set; } = null!;

	[JsonProperty] public string threatstr { get; set; } = null!;

	[JsonProperty] public string threatdelta { get; set; } = null!;

	[JsonProperty] public string Last10DPS { get; set; } = null!;

	[JsonProperty] public string Last30DPS { get; set; } = null!;

	[JsonProperty] public string Last60DPS { get; set; } = null!;

	[JsonProperty] public string Job { get; set; } = null!;

	[JsonProperty] public string ParryPct { get; set; } = null!;

	[JsonProperty] public string BlockPct { get; set; } = null!;

	[JsonProperty] public string IncToHit { get; set; } = null!;

	[JsonProperty] public string OverHealPct { get; set; } = null!;

	[JsonProperty] public string DirectHitPct { get; set; } = null!;

	[JsonProperty] public string DirectHitCount { get; set; } = null!;

	[JsonProperty] public string CritDirectHitCount { get; set; } = null!;

	[JsonProperty] public string CritDirectHitPct { get; set; } = null!;

	[JsonProperty] public string overHeal { get; set; } = null!;

	[JsonProperty] public string damageShield { get; set; } = null!;

	[JsonProperty] public string absorbHeal { get; set; } = null!;
}