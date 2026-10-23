using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ContentFinderSettings;

internal interface IContentFinderSettingsMemory71 : IContentFinderSettingsMemory;

internal class ContentFinderSettingsMemory71 : ContentFinderSettingsMemory, IContentFinderSettingsMemory71 {
	public override Version GetVersion() => new(7, 1);
}