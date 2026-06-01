using Dalamud.Common;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Utils;

public class Interfaces {
	public abstract class I_IINACTEx_Plugin {
		public const string WindowPrefix = "IINACTEx ";
		protected const string MainWindowCommandName = "/iinact";
		protected const string EndEncCommandName = "/endenc";
		public const string OverlayCommandName = "/iinactoverlay";
		public DalamudStartInfo DalamudStartInfo { get; init; }
		protected abstract IDalamudPluginInterface pluginInterface { get; set; }
		protected abstract ICommandManager commandManager { get; set; }
		protected abstract IClientState clientState { get; set; }
		protected abstract IDataManager dataManager { get; set; }
		protected abstract IChatGui chatGui { get; set; }
		protected abstract IFramework framework { get; set; }
		protected abstract ICondition condition { get; set; }
		protected abstract IGameInteropProvider gameInteropProvider { get; set; }
		protected abstract ISigScanner sigScanner { get; set; }
		protected abstract INotificationManager notificationManager { get; set; }
		protected abstract IPluginLog log { get; set; }
		protected abstract ITargetManager targetManager { get; set; }
		protected abstract IObjectTable objectTable { get; set; }
		protected abstract IGameGui gameGui { get; set; }
		protected abstract ITextureProvider textureProvider { get; set; }
	}
}