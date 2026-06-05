using System;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ContentFinderSettings;

public abstract class ContentFinderSettingsMemory(TinyIoCContainer container)
	: IContentFinderSettingsMemory {
	private struct ContentFinderSettingsImpl : ContentFinderSettings {
		public bool inContentFinderContent { get; set; }

		public byte unrestrictedParty { get; set; }

		public byte minimalItemLevel { get; set; }

		public byte silenceEcho { get; set; }

		public byte explorerMode { get; set; }

		public byte levelSync { get; set; }
	}

	protected FFXIVMemory memory = container.Resolve<FFXIVMemory>();
	protected ILogger logger = container.Resolve<ILogger>();

	// protected IntPtr settingsAddress = IntPtr.Zero;
	// protected IntPtr inContentFinderAddress = IntPtr.Zero;

	protected void ResetPointers() {
		// settingsAddress = IntPtr.Zero;
		// inContentFinderAddress = IntPtr.Zero;
	}

	private bool HasValidPointers() => true;

	public bool IsValid() => true;

	public virtual void ScanPointers() { }

	public abstract Version GetVersion();

	public ContentFinderSettings GetContentFinderSettings() {
		unsafe {
			var settings = new ContentFinderSettingsImpl {
				inContentFinderContent = InfoProxyCrossRealm.IsLocalPlayerInParty()
			};
			var cf = ContentsFinder.Instance();
			settings.unrestrictedParty = Convert.ToByte(cf->IsUnrestrictedParty);
			settings.minimalItemLevel = Convert.ToByte(cf->IsMinimalIL);
			settings.levelSync = Convert.ToByte(cf->IsLevelSync);
			settings.silenceEcho = Convert.ToByte(cf->IsSilenceEcho);
			settings.explorerMode = Convert.ToByte(cf->IsExplorerMode);
			return settings;
		}
	}
}