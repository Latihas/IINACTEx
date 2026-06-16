using System;
using System.IO;
using System.Net;
using Advanced_Combat_Tracker;

namespace RainbowMage.OverlayPlugin.WebSocket;

public class ServerController(TinyIoCContainer container) {
	public EventHandler<StateChangedArgs>? OnStateChanged;

	private TinyIoCContainer Container { get; } = container;
	private ILogger Logger { get; } = container.Resolve<ILogger>();
	private OverlayServer? Server { get; set; }
	private IPluginConfig Config { get; } = container.Resolve<IPluginConfig>();
	public bool Failed { get; private set; }
	public Exception? LastException { get; private set; }
	public bool Running => Server?.IsAccepting ?? false;
	public string? Address => Server?.Address;
	public int? Port => Server?.Port;
	public bool Secure => false;
	public Uri Uri => new($"{(Secure ? "wss" : "ws")}://{Address}:{Port}");

	public void Stop() {
		try {
			Server?.Stop();
		} catch (Exception e) {
			LastException = e;
			Logger.Log(LogLevel.Error, Resources.WSShutdownError, e);
		}

		Failed = false;

		OnStateChanged?.Invoke(null, new StateChangedArgs(false, false));
	}

	public void Restart() {
		Stop();
		Start();
	}

	public bool IsSSLPossible() => File.Exists(GetCertPath());

	private static bool IsAssemblyLoadContextException(Exception ex) {
		if (ex == null) return false;

		var message = ex.Message;
		if (message.Contains("AssemblyLoadContext") ||
		    message.Contains("unloading") ||
		    message.Contains("unloaded")) {
			return true;
		}

		if (ex.InnerException != null) {
			return IsAssemblyLoadContextException(ex.InnerException);
		}

		return false;
	}

	public void Start() {
		Failed = false;

		try {
			// TODO: add SSL support
			// var sslPath = GetCertPath();
			// var secure = _cfg.WSServerSSL && File.Exists(sslPath);

			var address = Config.WSServerIP == "*" ? IPAddress.Any : IPAddress.Parse(Config.WSServerIP);

			Server = new OverlayServer(address, Config.WSServerPort, Container);
			Server.OptionReuseAddress = true;

			Server.Start();

			OnStateChanged?.Invoke(this, new StateChangedArgs(true, false));
		} catch (Exception e) when (IsAssemblyLoadContextException(e)) {
			Failed = true;
			LastException = e;
			Logger.Log(LogLevel.Error, "WebSocket 服务器启动失败: AssemblyLoadContext 正在卸载或已卸载。这通常发生在插件热重载时。请重新加载插件或重开游戏。");
			OnStateChanged?.Invoke(this, new StateChangedArgs(false, true));
		} catch (Exception e) {
			Failed = true;
			LastException = e;
			Logger.Log(LogLevel.Error, Resources.WSStartFailed, e);
			OnStateChanged?.Invoke(this, new StateChangedArgs(false, true));
		}
	}

	public string GetModernUrl(string url) {
		if (url.Contains('?'))
			url += "&";
		else
			url += "?";

		url += "OVERLAY_WS=ws";
		if (Config.WSServerSSL) url += "s";
		url += "://";
		if (Config.WSServerIP is "*" or "0.0.0.0")
			url += "127.0.0.1";
		else
			url += Config.WSServerIP;

		url += ":" + Config.WSServerPort + "/ws";
		return url;
	}

	public string GetCertPath() {
		var path = Path.Combine(
			ActGlobals.oFormActMain.AppDataFolder.FullName,
			"Config",
			"OverlayPluginSSL.p12");

		return path;
	}

	public class StateChangedArgs(bool running, bool failed) : EventArgs {
		public bool Running { get; private set; } = running;
		public bool Failed { get; private set; } = failed;
	}
}