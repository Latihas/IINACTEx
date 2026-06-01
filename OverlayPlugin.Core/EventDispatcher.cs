using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin;

public class EventDispatcher(TinyIoCContainer container) {
	private readonly ILogger _logger = container.Resolve<ILogger>();
	private readonly ConcurrentDictionary<string, Func<JObject, JToken>> handlers = [];
	private readonly ConcurrentDictionary<string, List<IEventReceiver>> eventFilter = [];
	private readonly ConcurrentDictionary<string, Func<JObject>> stateCallbacks = [];

	private void Log(LogLevel level, string message, params object[] args) {
		_logger.Log(level, string.Format(message, args));
	}

	public void RegisterHandler(string name, Func<JObject, JToken> handler) {
		if (!handlers.TryAdd(name, handler)) {
			throw new Exception(string.Format(Resources.DuplicateHandlerError, name));
		}
	}

	public void RegisterEventTypes(List<string> names) {
		foreach (var name in names) {
			eventFilter[name] = [];
		}
	}

	public void RegisterEventType(string name) {
		RegisterEventType(name, null);
	}

	public void RegisterEventType(string name, Func<JObject> initCallback) {
		eventFilter[name] = [];

		if (initCallback != null)
			stateCallbacks[name] = initCallback;
	}

	public void Subscribe(string eventName, IEventReceiver receiver) {
		if (!eventFilter.TryGetValue(eventName, out var value)) {
			Log(LogLevel.Error, Resources.MissingEventSubError, eventName);
			return;
		}

		if (stateCallbacks.TryGetValue(eventName, out var callback)) {
			var ev = callback();
			if (ev != null) receiver.HandleEvent(ev);
		}

		lock (value) {
			if (!eventFilter[eventName].Contains(receiver)) {
				eventFilter[eventName].Add(receiver);
			}
		}
	}

	public void Unsubscribe(string eventName, IEventReceiver receiver) {
		if (eventFilter.ContainsKey(eventName)) {
			lock (eventFilter[eventName]) {
				eventFilter[eventName].Remove(receiver);
			}
		}
	}

	public void UnsubscribeAll(IEventReceiver receiver) {
		foreach (var item in eventFilter.Values) {
			lock (item) {
				item.Remove(receiver);
			}
		}
	}

	// Can be used to check that an event will be delivered before building
	// an expensive JObject that would otherwise be thrown away.
	public bool HasSubscriber(string eventName) {
		if (!eventFilter.ContainsKey(eventName))
			return false;
		lock (eventFilter[eventName]) {
			return eventFilter[eventName].Count > 0;
		}
	}

	public void DispatchEvent(JObject e) {
		var eventType = e["type"].ToString();
		if (!eventFilter.TryGetValue(eventType, out var value)) 
			throw new Exception(string.Format(Resources.MissingEventDispatchError, eventType));


		lock (value) {
			foreach (var receiver in value) {
				try {
					receiver.HandleEvent(e);
				} catch (Exception ex) {
					Log(LogLevel.Error, Resources.EventHandlerException, eventType, receiver, ex);
				}
			}
		}
	}

	public JToken CallHandler(JObject e) {
		var handlerName = e["call"].ToString();
		if (!handlers.TryGetValue(handlerName, out var value)) 
			throw new Exception(string.Format(Resources.MissingHandlerError, handlerName));

		var result = value(e);
		if (result != null && result.Type != JTokenType.Object) {
			throw new Exception("Handler response must be an object or null");
		}

		return result;
	}

	public JToken ProcessHandlerMessage(IEventReceiver receiver, string data) {
		try {
			var message = JObject.Parse(data);
			if (!message.TryGetValue("call", out var value)) {
				_logger.Log(LogLevel.Error, Resources.OverlayApiInvalidHandlerCall, receiver.Name + ": " + data);
				return null;
			}

			var handler = value.ToString();
			if (handler == "subscribe") {
				if (!message.TryGetValue("events", out var value1)) {
					_logger.Log(LogLevel.Error, Resources.OverlayApiMissingEventsField,
						receiver.Name + ": " + data);
					return null;
				}

				foreach (var name in value1.ToList()) {
					Subscribe(name.ToString(), receiver);
					_logger.Log(LogLevel.Debug, Resources.OverlayApiSubscribed, receiver.Name, name.ToString());
				}

				return null;
			}
			if (handler == "unsubscribe") {
				if (!message.TryGetValue("events", out var value1)) {
					_logger.Log(LogLevel.Error, Resources.OverlayApiMissingEventsFieldUnsub,
						receiver.Name + ": " + data);
					return null;
				}

				foreach (var name in value1.ToList()) {
					Unsubscribe(name.ToString(), receiver);
				}

				return null;
			}

			return CallHandler(message);
		} catch (Exception e) {
			_logger.Log(LogLevel.Error, Resources.JsHandlerCallException, e);
			return null;
		}
	}
}