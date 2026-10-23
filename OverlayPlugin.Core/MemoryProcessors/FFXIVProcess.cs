using System;
using System.Diagnostics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.MemoryProcessors;

// Wrap Process so that Process.Handle can be overridden with a more restricted handle.
public class LimitedProcess(Process process) {
	public IntPtr Handle => -1;
	public int Id => process.Id;
	public ProcessModule MainModule => process.MainModule;
}

// Exposes the FFXIV game directly. Call FindProcess() regularly to update
// memory addresses when FFXIV is run or closed.
public abstract partial class FFXIVProcess(TinyIoCContainer container) {
	private readonly ILogger logger_ = container.Resolve<ILogger>();
	private readonly LimitedProcess process_ = new(Process.GetCurrentProcess());

	// Values found in the EntityStruct's job field.
	public enum EntityJob : byte {
		None = 0,
		GLA = 1,
		PGL = 2,
		MRD = 3,
		LNC = 4,
		ARC = 5,
		CNJ = 6,
		THM = 7,
		CRP = 8,
		BSM = 9,
		ARM = 10,
		GSM = 11,
		LTW = 12,
		WVR = 13,
		ALC = 14,
		CUL = 15,
		MIN = 16,
		BTN = 17,
		FSH = 18,
		PLD = 19,
		MNK = 20,
		WAR = 21,
		DRG = 22,
		BRD = 23,
		WHM = 24,
		BLM = 25,
		ACN = 26,
		SMN = 27,
		SCH = 28,
		ROG = 29,
		NIN = 30,
		MCH = 31,
		DRK = 32,
		AST = 33,
		SAM = 34,
		RDM = 35,
		BLU = 36,
		GNB = 37,
		DNC = 38,
		RPR = 39,
		SGE = 40,
		VPR = 41,
		PCT = 42,
		BST = 43
	}

	internal static bool IsGatherer(EntityJob job) => job is EntityJob.FSH or EntityJob.MIN or EntityJob.BTN;

	internal static bool IsCrafter(EntityJob job) =>
		job is EntityJob.CRP or EntityJob.BSM or EntityJob.ARM or EntityJob.GSM or EntityJob.LTW or EntityJob.WVR or EntityJob.ALC or EntityJob.CUL;

	[Serializable]
	public class EntityData {
		public string name;
		public uint id;
		public ObjectKind type = ObjectKind.None;
		public ushort distance;
		public float pos_x;
		public float pos_y;
		public float pos_z;
		public float rotation;
		public uint hp;
		public uint max_hp;
		public uint mp;
		public uint max_mp;
		public short gp;
		public short max_gp;
		public short cp;
		public short max_cp;
		public EntityJob job = EntityJob.None;
		public short level;
		// public string debug_job;
		public uint shield_value;

		public override bool Equals(object obj) =>
			obj is EntityData o &&
			id == o.id &&
			type == o.type &&
			distance == o.distance &&
			pos_x == o.pos_x &&
			pos_y == o.pos_y &&
			pos_z == o.pos_z &&
			rotation == o.rotation &&
			hp == o.hp &&
			max_hp == o.max_hp &&
			mp == o.mp &&
			max_mp == o.max_mp &&
			gp == o.gp &&
			max_gp == o.max_gp &&
			cp == o.cp &&
			max_cp == o.max_cp &&
			job == o.job &&
			level == o.level &&
			// debug_job == o.debug_job &&
			shield_value == o.shield_value;
	}

	// The op before the pointer wildcard in the signature reads a pointer-to-a-pointer
	// to the job-specific data structure. We call it |outer| below:
	//
	// JobDataOuterStruct* outer;  // This pointer found from the signature.
	// JobDataOuterStruct {
	//   JobDataInnerStruct* inner;  // This points to the address after it currently.
	//   JobDataInnerStruct {
	internal static int kJobDataOuterStructOffset = 0;

	public bool HasProcess() => true;
	public bool FindProcess() => true;
	public bool IsActive() => true;
	public abstract unsafe EntityData GetEntityDataFromByteArray(GameObject* GameObject);

	public unsafe bool GetInGameCombat() => Conditions.Instance()->InCombat;

	public abstract JObject? GetJobSpecificData(EntityJob job);
	internal abstract unsafe EntityData? GetEntityData(GameObject* entityPtr);
	public abstract EntityData? GetSelfData();
}