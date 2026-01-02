using Newtonsoft.Json;

namespace IINACT.Latihas.Overlay;

public class CombatDataWrapper {
    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("msgtype")] public string MsgType { get; set; }

    [JsonProperty("msg")] public CombatData Msg { get; set; }
}

public class CombatData {
    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("Encounter")] public Encounter Encounter { get; set; }

    [JsonProperty("Combatant")] public Dictionary<string, Combatant> Combatant { get; set; }

    [JsonProperty("isActive")] public string IsActive { get; set; }
}

public class Encounter {
    [JsonProperty("n")] public string n { get; set; }

    [JsonProperty("t")] public string t { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("duration")] public string duration { get; set; }

    [JsonProperty("DURATION")] public string DURATION { get; set; }

    [JsonProperty("damage")] public string damage { get; set; }

    [JsonProperty("damage-m")] public string damage_m { get; set; }

    [JsonProperty("damage-*")] public string damage_star { get; set; }

    [JsonProperty("DAMAGE-k")] public string DAMAGE_k { get; set; }

    [JsonProperty("DAMAGE-m")] public string DAMAGE_m { get; set; }

    [JsonProperty("DAMAGE-b")] public string DAMAGE_b { get; set; }

    [JsonProperty("DAMAGE-*")] public string DAMAGE_star { get; set; }

    [JsonProperty("dps")] public string dps { get; set; }

    [JsonProperty("dps-*")] public string dps_star { get; set; }

    [JsonProperty("DPS")] public string DPS { get; set; }

    [JsonProperty("DPS-k")] public string DPS_k { get; set; }

    [JsonProperty("DPS-m")] public string DPS_m { get; set; }

    [JsonProperty("DPS-*")] public string DPS_star { get; set; }

    [JsonProperty("encdps")] public string encdps { get; set; }

    [JsonProperty("encdps-*")] public string encdps_star { get; set; }

    [JsonProperty("ENCDPS")] public string ENCDPS { get; set; }

    [JsonProperty("ENCDPS-k")] public string ENCDPS_k { get; set; }

    [JsonProperty("ENCDPS-m")] public string ENCDPS_m { get; set; }

    [JsonProperty("ENCDPS-*")] public string ENCDPS_star { get; set; }

    [JsonProperty("hits")] public string hits { get; set; }

    [JsonProperty("crithits")] public string crithits { get; set; }

    [JsonProperty("crithit%")] public string crithit_percent { get; set; }

    [JsonProperty("misses")] public string misses { get; set; }

    [JsonProperty("hitfailed")] public string hitfailed { get; set; }

    [JsonProperty("swings")] public string swings { get; set; }

    [JsonProperty("tohit")] public string tohit { get; set; }

    [JsonProperty("TOHIT")] public string TOHIT { get; set; }

    [JsonProperty("maxhit")] public string maxhit { get; set; }

    [JsonProperty("MAXHIT")] public string MAXHIT { get; set; }

    [JsonProperty("maxhit-*")] public string maxhit_star { get; set; }

    [JsonProperty("MAXHIT-*")] public string MAXHIT_star { get; set; }

    [JsonProperty("healed")] public string healed { get; set; }

    [JsonProperty("enchps")] public string enchps { get; set; }

    [JsonProperty("enchps-*")] public string enchps_star { get; set; }

    [JsonProperty("ENCHPS")] public string ENCHPS { get; set; }

    [JsonProperty("ENCHPS-k")] public string ENCHPS_k { get; set; }

    [JsonProperty("ENCHPS-m")] public string ENCHPS_m { get; set; }

    [JsonProperty("ENCHPS-*")] public string ENCHPS_star { get; set; }

    [JsonProperty("heals")] public string heals { get; set; }

    [JsonProperty("critheals")] public string critheals { get; set; }

    [JsonProperty("critheal%")] public string critheal_percent { get; set; }

    [JsonProperty("cures")] public string cures { get; set; }

    [JsonProperty("maxheal")] public string maxheal { get; set; }

    [JsonProperty("MAXHEAL")] public string MAXHEAL { get; set; }

    [JsonProperty("maxhealward")] public string maxhealward { get; set; }

    [JsonProperty("MAXHEALWARD")] public string MAXHEALWARD { get; set; }

    [JsonProperty("maxheal-*")] public string maxheal_star { get; set; }

    [JsonProperty("MAXHEAL-*")] public string MAXHEAL_star { get; set; }

    [JsonProperty("maxhealward-*")] public string maxhealward_star { get; set; }

    [JsonProperty("MAXHEALWARD-*")] public string MAXHEALWARD_star { get; set; }

    [JsonProperty("damagetaken")] public string damagetaken { get; set; }

    [JsonProperty("damagetaken-*")] public string damagetaken_star { get; set; }

    [JsonProperty("healstaken")] public string healstaken { get; set; }

    [JsonProperty("healstaken-*")] public string healstaken_star { get; set; }

    [JsonProperty("powerdrain")] public string powerdrain { get; set; }

    [JsonProperty("powerdrain-*")] public string powerdrain_star { get; set; }

    [JsonProperty("powerheal")] public string powerheal { get; set; }

    [JsonProperty("powerheal-*")] public string powerheal_star { get; set; }

    [JsonProperty("kills")] public string kills { get; set; }

    [JsonProperty("deaths")] public string deaths { get; set; }

    [JsonProperty("CurrentZoneName")] public string CurrentZoneName { get; set; }

    [JsonProperty("Last10DPS")] public string Last10DPS { get; set; }

    [JsonProperty("Last30DPS")] public string Last30DPS { get; set; }

    [JsonProperty("Last60DPS")] public string Last60DPS { get; set; }
}

public class Combatant {
    [JsonProperty("n")] public string n { get; set; }

    [JsonProperty("t")] public string t { get; set; }

    [JsonProperty("name")] public string name { get; set; }

    [JsonProperty("duration")] public string duration { get; set; }

    [JsonProperty("DURATION")] public string DURATION { get; set; }

    [JsonProperty("damage")] public string damage { get; set; }

    [JsonProperty("damage-m")] public string damage_m { get; set; }

    [JsonProperty("damage-b")] public string damage_b { get; set; }

    [JsonProperty("damage-*")] public string damage_star { get; set; }

    [JsonProperty("DAMAGE-k")] public string DAMAGE_k { get; set; }

    [JsonProperty("DAMAGE-m")] public string DAMAGE_m { get; set; }

    [JsonProperty("DAMAGE-b")] public string DAMAGE_b { get; set; }

    [JsonProperty("DAMAGE-*")] public string DAMAGE_star { get; set; }

    [JsonProperty("damage%")] public string damage_percent { get; set; }

    [JsonProperty("dps")] public string dps { get; set; }

    [JsonProperty("dps-*")] public string dps_star { get; set; }

    [JsonProperty("DPS")] public string DPS { get; set; }

    [JsonProperty("DPS-k")] public string DPS_k { get; set; }

    [JsonProperty("DPS-m")] public string DPS_m { get; set; }

    [JsonProperty("DPS-*")] public string DPS_star { get; set; }

    [JsonProperty("encdps")] public string encdps { get; set; }

    [JsonProperty("encdps-*")] public string encdps_star { get; set; }

    [JsonProperty("ENCDPS")] public string ENCDPS { get; set; }

    [JsonProperty("ENCDPS-k")] public string ENCDPS_k { get; set; }

    [JsonProperty("ENCDPS-m")] public string ENCDPS_m { get; set; }

    [JsonProperty("ENCDPS-*")] public string ENCDPS_star { get; set; }

    [JsonProperty("hits")] public string hits { get; set; }

    [JsonProperty("crithits")] public string crithits { get; set; }

    [JsonProperty("crithit%")] public string crithit_percent { get; set; }

    [JsonProperty("crittypes")] public string crittypes { get; set; }

    [JsonProperty("misses")] public string misses { get; set; }

    [JsonProperty("hitfailed")] public string hitfailed { get; set; }

    [JsonProperty("swings")] public string swings { get; set; }

    [JsonProperty("tohit")] public string tohit { get; set; }

    [JsonProperty("TOHIT")] public string TOHIT { get; set; }

    [JsonProperty("maxhit")] public string maxhit { get; set; }

    [JsonProperty("MAXHIT")] public string MAXHIT { get; set; }

    [JsonProperty("maxhit-*")] public string maxhit_star { get; set; }

    [JsonProperty("MAXHIT-*")] public string MAXHIT_star { get; set; }

    [JsonProperty("healed")] public string healed { get; set; }

    [JsonProperty("healed%")] public string healed_percent { get; set; }

    [JsonProperty("enchps")] public string enchps { get; set; }

    [JsonProperty("enchps-*")] public string enchps_star { get; set; }

    [JsonProperty("ENCHPS")] public string ENCHPS { get; set; }

    [JsonProperty("ENCHPS-k")] public string ENCHPS_k { get; set; }

    [JsonProperty("ENCHPS-m")] public string ENCHPS_m { get; set; }

    [JsonProperty("ENCHPS-*")] public string ENCHPS_star { get; set; }

    [JsonProperty("critheals")] public string critheals { get; set; }

    [JsonProperty("critheal%")] public string critheal_percent { get; set; }

    [JsonProperty("heals")] public string heals { get; set; }

    [JsonProperty("cures")] public string cures { get; set; }

    [JsonProperty("maxheal")] public string maxheal { get; set; }

    [JsonProperty("MAXHEAL")] public string MAXHEAL { get; set; }

    [JsonProperty("maxhealward")] public string maxhealward { get; set; }

    [JsonProperty("MAXHEALWARD")] public string MAXHEALWARD { get; set; }

    [JsonProperty("maxheal-*")] public string maxheal_star { get; set; }

    [JsonProperty("MAXHEAL-*")] public string MAXHEAL_star { get; set; }

    [JsonProperty("maxhealward-*")] public string maxhealward_star { get; set; }

    [JsonProperty("MAXHEALWARD-*")] public string MAXHEALWARD_star { get; set; }

    [JsonProperty("damagetaken")] public string damagetaken { get; set; }

    [JsonProperty("damagetaken-*")] public string damagetaken_star { get; set; }

    [JsonProperty("healstaken")] public string healstaken { get; set; }

    [JsonProperty("healstaken-*")] public string healstaken_star { get; set; }

    [JsonProperty("powerdrain")] public string powerdrain { get; set; }

    [JsonProperty("powerdrain-*")] public string powerdrain_star { get; set; }

    [JsonProperty("powerheal")] public string powerheal { get; set; }

    [JsonProperty("powerheal-*")] public string powerheal_star { get; set; }

    [JsonProperty("kills")] public string kills { get; set; }

    [JsonProperty("deaths")] public string deaths { get; set; }

    [JsonProperty("threatstr")] public string threatstr { get; set; }

    [JsonProperty("threatdelta")] public string threatdelta { get; set; }

    [JsonProperty("Last10DPS")] public string Last10DPS { get; set; }

    [JsonProperty("Last30DPS")] public string Last30DPS { get; set; }

    [JsonProperty("Last60DPS")] public string Last60DPS { get; set; }

    [JsonProperty("Job")] public string Job { get; set; }

    [JsonProperty("ParryPct")] public string ParryPct { get; set; }

    [JsonProperty("BlockPct")] public string BlockPct { get; set; }

    [JsonProperty("IncToHit")] public string IncToHit { get; set; }

    [JsonProperty("OverHealPct")] public string OverHealPct { get; set; }

    [JsonProperty("DirectHitPct")] public string DirectHitPct { get; set; }

    [JsonProperty("DirectHitCount")] public string DirectHitCount { get; set; }

    [JsonProperty("CritDirectHitCount")] public string CritDirectHitCount { get; set; }

    [JsonProperty("CritDirectHitPct")] public string CritDirectHitPct { get; set; }

    [JsonProperty("overHeal")] public string overHeal { get; set; }

    [JsonProperty("damageShield")] public string damageShield { get; set; }

    [JsonProperty("absorbHeal")] public string absorbHeal { get; set; }
}