using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin;

/// <summary>
///     <see cref="RainbowMage.OverlayPlugin.IOverlay" /> の設定に必要なプロパティを定義します。
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public interface IOverlayConfig {
	string Name { get; set; }
	bool IsVisible { get; set; }
	bool IsClickThru { get; set; }
	bool HideOutOfCombat { get; set; }
	Point Position { get; set; }
	Size Size { get; set; }
	string Url { get; set; }
	int MaxFrameRate { get; set; }
	bool GlobalHotkeyEnabled { get; set; }
	Keys GlobalHotkey { get; set; }
	Keys GlobalHotkeyModifiers { get; set; }

	// IOverlayConfig 実装型 → IOverlay 実装型の逆引き用
	Type OverlayType { get; }
}