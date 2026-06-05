using System;
using System.Linq;
using Newtonsoft.Json;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.JobGauge;

public abstract class JobGaugeMemory : IJobGaugeMemory {
	public struct JobGaugeImpl : IJobGauge {
		[JsonIgnore] public JobGaugeJob job;
		[JsonIgnore] public IBaseJobGauge? data;
		[JsonIgnore] public byte[] rawData;
		[JsonIgnore] public object baseObject;

		public JobGaugeJob Job => job;
		public IBaseJobGauge? Data => data;
		public int[] RawData => rawData.Select(b => (int)b).ToArray();
		public object BaseObject => baseObject;

		public bool Equals(IJobGauge? obj) {
			if (obj == null || GetType() != obj.GetType()) {
				return false;
			}

			if (obj.Job != Job) return false;
			var objRawData = obj.RawData;
			var rawData = RawData;
			if (objRawData.Length != rawData.Length) return false;
			return !objRawData.Where((t, i) => t != rawData[i]).Any();
		}
	}

	// protected FFXIVMemory memory = container.Resolve<FFXIVMemory>();
	public bool IsValid() => true;

	public void ScanPointers() {
	}

	public abstract Version GetVersion();
	public abstract IJobGauge GetJobGauge();
}