using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.Interop;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.MemoryProcessors;

public class FFXIVProcessKo(TinyIoCContainer container) : FFXIVProcess(container) {
	//
	// for FFXIV KO version: 5.2
	//
	// Latest KO version can be found at:
	// https://www.ff14.co.kr/news/notice?category=3
	//
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct EntityMemory {
		public static int Size => Marshal.SizeOf<EntityMemory>();

		// Unknown size, but this is the bytes up to the next field.
		public const int nameBytes = 68;

		[FieldOffset(0x30)] public fixed byte Name[nameBytes];

		[FieldOffset(0x74)] public uint id;

		[FieldOffset(0x8C)] public EntityType type;

		[FieldOffset(0x92)] public ushort distance;

		[FieldOffset(0xA0)] public Single pos_x;

		[FieldOffset(0xA4)] public Single pos_z;

		[FieldOffset(0xA8)] public Single pos_y;

		[FieldOffset(0xB0)] public Single rotation;

		[FieldOffset(0x1898)] public CharacterDetails charDetails;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct CharacterDetails {
		[FieldOffset(0x00)] public int hp;

		[FieldOffset(0x04)] public int max_hp;

		[FieldOffset(0x08)] public int mp;

		[FieldOffset(0x12)] public short gp;

		[FieldOffset(0x14)] public short max_gp;

		[FieldOffset(0x16)] public short cp;

		[FieldOffset(0x18)] public short max_cp;

		[FieldOffset(0x3E)] public EntityJob job;

		[FieldOffset(0x40)] public byte level;

		[FieldOffset(0x61)] public short shieldPercentage;
	}

	internal override void ReadSignatures() {
	}

	public override unsafe EntityData GetEntityDataFromByteArray(GameObject* source) {
		var mem = Marshal.PtrToStructure<EntityMemory>((IntPtr)source);


		// dump '\0' string terminators
		var memoryName = Encoding.UTF8.GetString(mem.Name, EntityMemory.nameBytes).Split(['\0'], 2)[0];

		var entity = new EntityData {
			name = memoryName,
			id = mem.id,
			type = mem.type,
			distance = mem.distance,
			pos_x = mem.pos_x,
			pos_y = mem.pos_y,
			pos_z = mem.pos_z,
			rotation = mem.rotation
		};
		if (entity.type is EntityType.PC or EntityType.Monster) {
			entity.job = mem.charDetails.job;

			entity.hp = mem.charDetails.hp;
			entity.max_hp = mem.charDetails.max_hp;
			entity.mp = mem.charDetails.mp;
			// This doesn't exist in memory, so just send the right value.
			// As there are other versions that still have it, don't change the event.
			entity.max_mp = 10000;
			entity.shield_value = mem.charDetails.shieldPercentage * entity.max_hp / 100;

			if (IsGatherer(entity.job)) {
				entity.gp = mem.charDetails.gp;
				entity.max_gp = mem.charDetails.max_gp;
			}
			if (IsCrafter(entity.job)) {
				entity.cp = mem.charDetails.cp;
				entity.max_cp = mem.charDetails.max_cp;
			}

			entity.level = mem.charDetails.level;

			var job_bytes = GetRawJobSpecificDataBytes();
			if (job_bytes != null) {
				foreach (var t in job_bytes) {
					if (entity.debug_job != "") entity.debug_job += " ";
					entity.debug_job += $"{t:x2}";
				}
			}
		}
		return entity;
	}


	internal override unsafe EntityData? GetEntityData(GameObject* entity_ptr) =>
		entity_ptr == null ? null : GetEntityDataFromByteArray(entity_ptr);

	public override unsafe EntityData? GetSelfData() {
		var gobs = CharacterManager.Instance()->BattleCharas.ToArray()
			.Select(entry => (Pointer<GameObject>)(GameObject*)entry.Value)
			.Where(entry => entry.Value != null).ToArray();
		return gobs.Length == 0 ? null : GetEntityData(gobs[0].Value);
	}

	public override unsafe JObject? GetJobSpecificData(EntityJob job) {
		var jg = JobGaugeManager.Instance();
		if (jg == null) {
			// The pointer can be null when not logged in.
			return null;
		}
		// fixed (byte* p = Read8(job_inner_ptr, kJobDataInnerStructSize)) {
		if (jg->CurrentGauge == null) return null;
		return job switch {
			EntityJob.RDM => JObject.FromObject(jg->RedMage),
			EntityJob.WAR => JObject.FromObject(jg->Warrior),
			EntityJob.DRK => JObject.FromObject(jg->DarkKnight),
			EntityJob.PLD => JObject.FromObject(jg->Paladin),
			EntityJob.GNB => JObject.FromObject(jg->Gunbreaker),
			EntityJob.BRD => JObject.FromObject(jg->Bard),
			EntityJob.DNC => JObject.FromObject(jg->Dancer),
			EntityJob.DRG => JObject.FromObject(jg->Dragoon),
			EntityJob.NIN => JObject.FromObject(jg->Ninja),
			EntityJob.THM => JObject.FromObject(Marshal.PtrToStructure<ThaumaturgeJobMemory>(new IntPtr(&jg->EmptyGauge))),
			EntityJob.BLM => JObject.FromObject(jg->BlackMage),
			EntityJob.WHM => JObject.FromObject(jg->WhiteMage),
			EntityJob.ACN => JObject.FromObject(Marshal.PtrToStructure<ArcanistJobMemory>(new IntPtr(&jg->EmptyGauge))),
			EntityJob.SMN => JObject.FromObject(jg->Summoner),
			EntityJob.SCH => JObject.FromObject(jg->Scholar),
			EntityJob.MNK => JObject.FromObject(jg->Monk),
			EntityJob.MCH => JObject.FromObject(jg->Machinist),
			EntityJob.AST => JObject.FromObject(jg->Astrologian),
			EntityJob.SAM => JObject.FromObject(jg->Samurai),
			EntityJob.SGE => JObject.FromObject(jg->Sage),
			EntityJob.RPR => JObject.FromObject(jg->Reaper),
			EntityJob.VPR => JObject.FromObject(jg->Viper),
			EntityJob.PCT => JObject.FromObject(jg->Pictomancer),
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