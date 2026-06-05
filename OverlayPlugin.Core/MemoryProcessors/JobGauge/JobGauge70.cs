using System;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.JobGauge;

internal interface IJobGaugeMemory70 : IJobGaugeMemory;

internal partial class JobGaugeMemory70(TinyIoCContainer container) : JobGaugeMemory {
	public override Version GetVersion() => new(7, 0);

	public override IJobGauge? GetJobGauge() {
		if (!IsValid()) return null;

		var jobGaugeManager = GetJobGaugeManager();

		var ret = new JobGaugeImpl {
			baseObject = jobGaugeManager,
			job = (JobGaugeJob)jobGaugeManager.ClassJobID,
			rawData = jobGaugeManager.GetRawGaugeData
		};

		ret.data = ret.job switch {
			JobGaugeJob.WHM => jobGaugeManager.WhiteMage,
			JobGaugeJob.SCH => jobGaugeManager.Scholar,
			JobGaugeJob.AST => jobGaugeManager.Astrologian,
			JobGaugeJob.SGE => jobGaugeManager.Sage,
			JobGaugeJob.BRD => jobGaugeManager.Bard,
			JobGaugeJob.MCH => jobGaugeManager.Machinist,
			JobGaugeJob.DNC => jobGaugeManager.Dancer,
			JobGaugeJob.BLM => jobGaugeManager.BlackMage,
			JobGaugeJob.SMN => jobGaugeManager.Summoner,
			JobGaugeJob.RDM => jobGaugeManager.RedMage,
			JobGaugeJob.MNK => jobGaugeManager.Monk,
			JobGaugeJob.DRG => jobGaugeManager.Dragoon,
			JobGaugeJob.NIN => jobGaugeManager.Ninja,
			JobGaugeJob.SAM => jobGaugeManager.Samurai,
			JobGaugeJob.RPR => jobGaugeManager.Reaper,
			JobGaugeJob.DRK => jobGaugeManager.DarkKnight,
			JobGaugeJob.PLD => jobGaugeManager.Paladin,
			JobGaugeJob.WAR => jobGaugeManager.Warrior,
			JobGaugeJob.GNB => jobGaugeManager.Gunbreaker,
			_ => ret.data
		};

		return ret;
	}

	private unsafe JobGaugeManager GetJobGaugeManager() => Marshal.PtrToStructure<JobGaugeManager>(
		(IntPtr)FFXIVClientStructs.FFXIV.Client.Game.JobGaugeManager.Instance());
}