using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.Handlers.Ipc;

public class IpcHandlerController(TinyIoCContainer container) : IDisposable {
	private TinyIoCContainer Container { get; } = container;
	private ILogger Logger { get; } = container.Resolve<ILogger>();
	private ConcurrentDictionary<string, IHandler> Handlers { get; } = new();
	private IHandlerFactory HandlerFactory { get; } = new HandlerFactory();
	private IHandlerFactory LegacyHandlerFactory { get; } = new LegacyHandlerFactory();

	public bool CreateSubscriber(string name) => CreateSubscriber(name, HandlerFactory);
	public bool CreateLegacySubscriber(string name) => CreateSubscriber(name, LegacyHandlerFactory);

	private bool CreateSubscriber(string name, IHandlerFactory handlerFactory) {
		try {
			var handler = handlerFactory.Create(name, Container);
			if (Handlers.TryAdd(name, handler)) {
				Logger.Log(LogLevel.Debug, $"Successfully added IPC handler {name}");
				return true;
			}
			Logger.Log(LogLevel.Error, $"Failed adding already existing IPC handler {name}");
			handler.Dispose();
			return false;
		} catch (Exception ex) {
			Logger.Log(LogLevel.Error, $"Failed creating IPC handler {name}: {ex}");
			return false;
		}
	}

	public bool Unsubscribe(string name) {
		if (!Handlers.Remove(name, out var handler)) {
			Logger.Log(LogLevel.Warning, $"Cannot unsubscribe from non-existing IPC handler {name}");
			return false;
		}
		handler.Dispose();
		return true;
	}

	public void Dispose() {
		foreach (var (_, handler) in Handlers) handler.Dispose();
	}
}