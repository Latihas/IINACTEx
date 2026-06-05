using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ContentFinderSettings;

public interface ContentFinderSettings {
	bool inContentFinderContent { get; }
	byte unrestrictedParty { get; }
	byte minimalItemLevel { get; }
	byte silenceEcho { get; }
	byte explorerMode { get; }
	byte levelSync { get; }

	// TODO: Maybe we can track these down if they're ever actually needed?
	// byte lootRules { get; }
	// byte limitedLevelingRoulette { get; }
}

public interface IContentFinderSettingsMemory : IVersionedMemory {
	ContentFinderSettings GetContentFinderSettings();
}

internal class ContentFinderSettingsMemoryManager : IContentFinderSettingsMemory {
	private readonly IContentFinderSettingsMemory memory;

	public ContentFinderSettingsMemoryManager(TinyIoCContainer container) {
		container.Register<IContentFinderSettingsMemory71, ContentFinderSettingsMemory71>();
		memory = container.Resolve<IContentFinderSettingsMemory71>();
		memory.ScanPointers();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => true;

	Version IVersionedMemory.GetVersion() => memory.GetVersion();

	public ContentFinderSettings GetContentFinderSettings() => memory.GetContentFinderSettings();
}