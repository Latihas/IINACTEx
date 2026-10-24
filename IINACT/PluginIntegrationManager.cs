using System.Diagnostics.CodeAnalysis;
using Advanced_Combat_Tracker;
using PostNamazu.Common.Localization;
using PostNamazu.TriggerHoster;
using TriggernometryProxy;

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
		// try {
		var plugin = ActGlobals.oFormActMain.TriggernometryPlugin;

		if (plugin == null) {
			PostNamazu.Plugin.PluginUi.Log(L.Get("PostNamazu/trigNotFound"));
			return;
		}
		_triggerHoster = new Program((ProxyPlugin)plugin, PostNamazu.Plugin.GetCommandKeys(), PostNamazu.Plugin.DoAction, PostNamazu.Plugin.PluginUi.Log);
		PostNamazu.Plugin.PluginUi.Log(L.Get("PostNamazu/trig"));
		// } catch (Exception ex) {
		// 	PostNamazu.Plugin.PluginUi.Log(ex.Message);
		// }
	}

	/// <summary>
	///     OverlayPlugin集成初始化
	/// </summary>
	private void InitializeOverlayIntegration() {
		_overlayHoster = new OverlayHoster.Program(PostNamazu.Plugin.DoAction);
	}
}