using System;
using System.Diagnostics.CodeAnalysis;
using Advanced_Combat_Tracker;
using PostNamazu.Common.Localization;
using PostNamazu.TriggerHoster;

// ReSharper disable once CheckNamespace
namespace PostNamazu.Common;

/// <summary>
///     插件集成管理器
/// </summary>
public class PluginIntegrationManager {
	private Program? _triggerHoster;
	private OverlayHoster.Program? _overlayHoster;

	/// <summary>
	///     初始化所有插件集成
	/// </summary>
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public void InitializeIntegrations() {
		InitializeTriggerIntegration();
		InitializeOverlayIntegration();
	}

	/// <summary>
	///     反初始化所有插件集成
	/// </summary>
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public void DeInitializeIntegrations() {
		_overlayHoster?.DeInit();
		_triggerHoster?.DeInit();
	}

	/// <summary>
	///     Triggernometry集成初始化
	/// </summary>
	private void InitializeTriggerIntegration() {
		try {
			var plugin = ActGlobals.oFormActMain.TriggernometryPlugin;

			if (plugin == null) {
				PostNamazu.Plugin.PluginUi.Log(L.Get("PostNamazu/trigNotFound"));
				return;
			}
			_triggerHoster = new Program(plugin) {
				PostNamazuDelegate = PostNamazu.Plugin.DoAction,
				LogDelegate = PostNamazu.Plugin.PluginUi.Log
			};
			_triggerHoster.Init(PostNamazu.Plugin.GetCommandKeys());
			PostNamazu.Plugin.PluginUi.Log(L.Get("PostNamazu/trig"));
		} catch (Exception ex) {
			PostNamazu.Plugin.PluginUi.Log(ex.Message);
		}
	}

	/// <summary>
	///     OverlayPlugin集成初始化
	/// </summary>
	private void InitializeOverlayIntegration() {
		try {
			_overlayHoster = new OverlayHoster.Program {
				PostNamazuDelegate = PostNamazu.Plugin.DoAction
			};
			_overlayHoster.Init();
			PostNamazu.Plugin.PluginUi.Log(L.Get("PostNamazu/op"));
		} catch (Exception ex) {
			PostNamazu.Plugin.PluginUi.Log(ex.Message);
		}
	}
}