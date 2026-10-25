using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace RainbowMage.OverlayPlugin.MemoryProcessors;

public class FFXIVProcessDalamud(TinyIoCContainer container) : FFXIVProcess {
	private readonly IObjectTable ObjectTable = container.Resolve<IObjectTable>();

	public override unsafe EntityData GetEntityDataFromByteArray(GameObject* source) {
		var mem = Marshal.PtrToStructure<Character>((IntPtr)source);
		var entity = new EntityData {
			name = mem.Name.ToString(),
			id = mem.EntityId,
			type = mem.ObjectKind,
			distance = mem.NextDistance,
			pos_x = mem.Position.X,
			pos_y = mem.Position.Y,
			pos_z = mem.Position.Z,
			rotation = mem.Rotation
		};
		if (entity.type is ObjectKind.Pc or ObjectKind.BattleNpc) {
			entity.job = (EntityJob)mem.ClassJob;
			entity.hp = mem.Health;
			entity.max_hp = mem.MaxHealth;
			entity.mp = mem.Mana;
			// This doesn't exist in memory, so just send the right value.
			// As there are other versions that still have it, don't change the event.
			entity.max_mp = mem.MaxMana;
			entity.shield_value = mem.ShieldValue * entity.max_hp / 100;

			if (IsGatherer(entity.job)) {
				entity.gp = (short)mem.GatheringPoints;
				entity.max_gp = (short)mem.MaxGatheringPoints;
			}
			if (IsCrafter(entity.job)) {
				entity.cp = (short)mem.CraftingPoints;
				entity.max_cp = (short)mem.MaxCraftingPoints;
			}
			entity.level = mem.Level;
		}
		return entity;
	}


	internal override unsafe EntityData? GetEntityData(GameObject* entity_ptr) =>
		entity_ptr == null ? null : GetEntityDataFromByteArray(entity_ptr);

	public override unsafe EntityData? GetSelfData() {
		var p = ObjectTable.LocalPlayer;
		return p == null ? null : GetEntityData((GameObject*)p.Address);
	}

	public sealed class JobGaugeStructResolver : DefaultContractResolver {
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
			var prop = base.CreateProperty(member, memberSerialization);
			if (prop.PropertyName is "VirtualTable" or "JobGauge") prop.Ignored = true;
			return prop;
		}
	}

	public override unsafe JObject? GetJobSpecificData(EntityJob job) {
		var settings = JsonSerializer.Create(new JsonSerializerSettings { ContractResolver = new JobGaugeStructResolver() });
		var jg = JobGaugeManager.Instance();
		// The pointer can be null when not logged in.
		if (jg == null) return null;
		if (jg->CurrentGauge == null) return null;
		return job switch {
			EntityJob.RDM => JObject.FromObject(jg->RedMage, settings),
			EntityJob.WAR => JObject.FromObject(jg->Warrior, settings),
			EntityJob.DRK => JObject.FromObject(jg->DarkKnight, settings),
			EntityJob.PLD => JObject.FromObject(jg->Paladin, settings),
			EntityJob.GNB => JObject.FromObject(jg->Gunbreaker, settings),
			EntityJob.BRD => JObject.FromObject(jg->Bard, settings),
			EntityJob.DNC => JObject.FromObject(jg->Dancer, settings),
			EntityJob.DRG => JObject.FromObject(jg->Dragoon, settings),
			EntityJob.NIN => JObject.FromObject(jg->Ninja, settings),
			EntityJob.THM => JObject.FromObject(Marshal.PtrToStructure<ThaumaturgeJobMemory>(new IntPtr(&jg->EmptyGauge)), settings),
			EntityJob.BLM => JObject.FromObject(jg->BlackMage, settings),
			EntityJob.WHM => JObject.FromObject(jg->WhiteMage, settings),
			EntityJob.ACN => JObject.FromObject(Marshal.PtrToStructure<ArcanistJobMemory>(new IntPtr(&jg->EmptyGauge)), settings),
			EntityJob.SMN => JObject.FromObject(jg->Summoner, settings),
			EntityJob.SCH => JObject.FromObject(jg->Scholar, settings),
			EntityJob.MNK => JObject.FromObject(jg->Monk, settings),
			EntityJob.MCH => JObject.FromObject(jg->Machinist, settings),
			EntityJob.AST => JObject.FromObject(jg->Astrologian, settings),
			EntityJob.SAM => JObject.FromObject(jg->Samurai, settings),
			EntityJob.SGE => JObject.FromObject(jg->Sage, settings),
			EntityJob.RPR => JObject.FromObject(jg->Reaper, settings),
			EntityJob.VPR => JObject.FromObject(jg->Viper, settings),
			EntityJob.PCT => JObject.FromObject(jg->Pictomancer, settings),
			_ => null
		};
	}


	[StructLayout(LayoutKind.Explicit)]
	public struct ThaumaturgeJobMemory {
		[FieldOffset(0x02)] public sbyte umbralStacks; // Positive = Umbral Fire Stacks, Negative = Umbral Ice Stacks.
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct ArcanistJobMemory {
		[FieldOffset(0x04)] public byte aetherflowStacks;
	}
}