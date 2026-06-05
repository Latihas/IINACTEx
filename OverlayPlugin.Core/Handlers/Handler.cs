using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.Handlers;

internal abstract class Handler(string name, ILogger logger, EventDispatcher eventDispatcher) : IHandler, IEventReceiver {
	public string Name { get; } = name;
	protected ILogger Logger { get; } = logger;
	private EventDispatcher Dispatcher { get; } = eventDispatcher;

	protected abstract void Send(JObject data);
	public void HandleEvent(JObject e) => Send(e);

	public void DataReceived(JObject data) {
		if (!data.TryGetValue("call", out var value)) return;

		var msgType = value?.ToString();
		switch (msgType) {
			case "subscribe":
				try {
					foreach (var item in data["events"]?.ToList() ?? [])
						Dispatcher.Subscribe(item.ToString(), this);
				} catch (Exception ex) {
					Logger.Log(LogLevel.Error, Resources.WSNewSubFail, ex);
				}

				return;
			case "unsubscribe":
				try {
					foreach (var item in data["events"]?.ToList() ?? [])
						Dispatcher.Unsubscribe(item.ToString(), this);
				} catch (Exception ex) {
					Logger.Log(LogLevel.Error, Resources.WSUnsubFail, ex);
				}

				return;
			default:
				Task.Run(() => {
					try {
						var response = Dispatcher.CallHandler(data);

						if (response != null && response.Type != JTokenType.Object)
							throw new Exception("Handler response must be an object or null");

						if (response == null) {
							response = new JObject();
							response["$isNull"] = true;
						}

						if (data.TryGetValue("rseq", out var value1)) response["rseq"] = value1;

						var jObject = response.ToObject<JObject>()!;

						Send(jObject);
					} catch (Exception ex) {
						Logger.Log(LogLevel.Error, Resources.WSHandlerException, ex);
					}
				});
				break;
		}
	}

	public virtual void Dispose() => Dispatcher.UnsubscribeAll(this);
}