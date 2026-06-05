using System;
using System.Runtime.InteropServices;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
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

	// TODO: all of this could be refactored into structures of some sort
	// instead of just being loose variables everywhere.

	// A piece of code that reads the pointer to the list of all entities, that we
	// refer to as the charmap. The pointer is the 4 byte ?????????.
	private static readonly string kCharmapSignature = "574883EC??488B1D????????488BF233D2";

	private static readonly int kCharmapSignatureOffset = -9;

	// The signature finds a pointer in the executable code which uses RIP addressing.
	private static readonly bool kCharmapSignatureRIP = true;

	// The pointer is to a structure as:
	//
	// CharmapStruct* outer;  // The pointer found from the signature.
	// CharmapStruct {
	//   EntityStruct* player;
	// }
	private static readonly int kCharmapStructOffsetPlayer = 0;

	// In combat boolean.
	// Variable is set at 83FA587D70534883EC204863C2410FB6D8381C08744E (offset=0)
	// via a mov [rax+rcx],bl line.
	// This sig below finds the calling function that sets rax(offset) and rcx(base address).
	private static readonly string kInCombatSignature = "84C07425450FB6C7488D0D";
	private static readonly int kInCombatBaseOffset = 0;
	private static readonly bool kInCombatBaseRIP = true;
	private static readonly int kInCombatOffsetOffset = 5;
	private static readonly bool kInCombatOffsetRIP = false;

	// A piece of code that reads the job data.
	// The pointer of interest is the first ???????? in the signature.
	private static readonly string kJobDataSignature = "488B0D????????4885C90F84????????488B05????????3C03";

	private static readonly int kJobDataSignatureOffset = -22;

	// The signature finds a pointer in the executable code which uses RIP addressing.
	private static readonly bool kJobDataSignatureRIP = true;

	internal override void ReadSignatures() {
		var p = SigScan(kCharmapSignature, kCharmapSignatureOffset, kCharmapSignatureRIP);
		if (p.Count != 1) {
			logger_.Log(LogLevel.Error, "Charmap signature found " + p.Count + " matches");
		} else {
			player_ptr_addr_ = IntPtr.Add(p[0], kCharmapStructOffsetPlayer);
		}

		p = SigScan(kInCombatSignature, kInCombatBaseOffset, kInCombatBaseRIP);
		if (p.Count != 1) {
			logger_.Log(LogLevel.Error, "In combat signature found " + p.Count + " matches");
		} else {
			var baseAddress = p[0];
			p = SigScan(kInCombatSignature, kInCombatOffsetOffset, kInCombatOffsetRIP);
			if (p.Count != 1) {
				logger_.Log(LogLevel.Error, "In combat offset signature found " + p.Count + " matches");
			} else {
				// Abuse sigscan here to return 64-bit "pointer" which we will mask into the 32-bit immediate integer we need.
				// TODO: maybe sigscan should be able to return different types?
				var offset = (int)((ulong)p[0] & 0xFFFFFFFF);
				in_combat_addr_ = IntPtr.Add(baseAddress, offset);
			}
		}
	}

	public override unsafe EntityData GetEntityDataFromByteArray(byte[] source) {
		fixed (byte* p = source) {
			var mem = *(EntityMemory*)&p[0];

			// dump '\0' string terminators
			var memoryName = Encoding.UTF8.GetString(mem.Name, EntityMemory.nameBytes)
				.Split(['\0'], 2)[0];

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
			if (entity.type == EntityType.PC || entity.type == EntityType.Monster) {
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
					for (var i = 0; i < job_bytes.Length; ++i) {
						if (entity.debug_job != "")
							entity.debug_job += " ";
						entity.debug_job += string.Format("{0:x2}", job_bytes[i]);
					}
				}
			}

			return entity;
		}
	}

	internal override EntityData GetEntityData(IntPtr entity_ptr) {
		if (entity_ptr == IntPtr.Zero)
			return null;
		var source = Read8(entity_ptr, EntityMemory.Size);
		return GetEntityDataFromByteArray(source);
	}

	public override EntityData GetSelfData() {
		if (!HasProcess() || player_ptr_addr_ == IntPtr.Zero)
			return null;

		var entity_ptr = ReadIntPtr(player_ptr_addr_);
		if (entity_ptr == IntPtr.Zero)
			return null;
		return GetEntityData(entity_ptr);
		;
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