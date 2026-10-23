using System;

namespace RainbowMage.OverlayPlugin;

public interface IOverlayPreset {
	public string Name { get; }

	public Uri? ToOverlayUri(Uri webSocketServer);
}