using System;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ContentFinderSettings;

public abstract class ContentFinderSettingsMemory : IContentFinderSettingsMemory {
	private struct ContentFinderSettingsImpl : ContentFinderSettings {
		public bool inContentFinderContent { get; set; }

		public byte unrestrictedParty { get; set; }

		public byte minimalItemLevel { get; set; }

		public byte silenceEcho { get; set; }

		public byte explorerMode { get; set; }

		public byte levelSync { get; set; }
	}

	public bool IsValid() => true;

	public virtual void ScanPointers() { }

	public abstract Version GetVersion();

	public ContentFinderSettings GetContentFinderSettings() {
		unsafe {
			var cf = ContentsFinder.Instance();
			var settings = new ContentFinderSettingsImpl {
				inContentFinderContent = InfoProxyCrossRealm.IsLocalPlayerInParty(),
				unrestrictedParty = Convert.ToByte(cf->IsUnrestrictedParty),
				minimalItemLevel = Convert.ToByte(cf->IsMinimalIL),
				levelSync = Convert.ToByte(cf->IsLevelSync),
				silenceEcho = Convert.ToByte(cf->IsSilenceEcho),
				explorerMode = Convert.ToByte(cf->IsExplorerMode)
			};
			return settings;
		}
	}
}