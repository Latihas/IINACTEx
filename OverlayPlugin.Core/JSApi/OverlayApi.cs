using System;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;

namespace RainbowMage.OverlayPlugin;

internal class OverlayApi(TinyIoCContainer container, IApiBase receiver) {
	public static event EventHandler<BroadcastMessageEventArgs> BroadcastMessage;
	public static event EventHandler<SendMessageEventArgs> SendMessage;
	public static event EventHandler<SendMessageEventArgs> OverlayMessage;

	private readonly EventDispatcher dispatcher = container.Resolve<EventDispatcher>();
	private readonly ILogger logger = container.Resolve<ILogger>();

	public void broadcastMessage(string msg) {
		logger.Log(LogLevel.Error,
			$"{receiver.Name}: OverlayPluginApi.broadcastMessage() is deprecated and will be removed in future OverlayPlugin versions!");
		BroadcastMessage(this, new BroadcastMessageEventArgs(msg));
	}

	public void sendMessage(string target, string msg) {
		logger.Log(LogLevel.Error,
			$"{receiver.Name}: OverlayPluginApi.sendMessage() is deprecated and will be removed in future OverlayPlugin versions!");
		SendMessage(this, new SendMessageEventArgs(target, msg));
	}

	public void overlayMessage(string target, string msg) {
		logger.Log(LogLevel.Error,
			$"{receiver.Name}: OverlayPluginApi.overlayMessage() is deprecated and will be removed in future OverlayPlugin versions!");
		if (target == receiver.Name) {
			receiver.OverlayMessage(msg);
		} else {
			OverlayMessage(this, new SendMessageEventArgs(target, msg));
		}
	}

	public void endEncounter() {
		ActGlobals.oFormActMain.EndCombat(true);
	}

	// Also handles (un)subscription to make switching between this and WS easier.
	public void callHandler(string data, object callback) {
		// Tell the overlay that the page is using the modern API.
		receiver.InitModernAPI();

		Task.Run(() => {
			var result = dispatcher.ProcessHandlerMessage(receiver, data);
			if (callback != null) {
				//Renderer.ExecuteCallback(callback, result?.ToString(Newtonsoft.Json.Formatting.None));
			}
		});
	}
}

public class BroadcastMessageEventArgs(string message) : EventArgs {
	public string Message { get; private set; } = message;
}

public class SendMessageEventArgs(string target, string message) : EventArgs {
	public string Target { get; private set; } = target;
	public string Message { get; private set; } = message;
}

public class EndEncounterEventArgs : EventArgs;