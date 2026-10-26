using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Advanced_Combat_Tracker;

namespace RainbowMage.OverlayPlugin;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "EventNeverSubscribedTo.Global")]
public class Registry(TinyIoCContainer container) {
	private readonly List<Type> _overlays = [];
	private readonly List<IEventSource> _eventSources = [];
	private readonly List<IOverlayPreset> _overlayPresets = [];

	public IEnumerable<Type> Overlays => _overlays;

	public IEnumerable<IEventSource> EventSources => _eventSources;

	public IReadOnlyList<IOverlayPreset> OverlayPresets => _overlayPresets;

	public event EventHandler<EventSourceRegisteredEventArgs>? EventSourceRegistered;
	public event EventHandler<EventArgs>? EventSourcesStarted;

	public void RegisterOverlay<T>() where T : class, IOverlay {
		_overlays.Add(typeof(T));
		container.Register<T>();
	}

	public void UnregisterOverlay<T>() where T : class, IOverlay {
		_overlays.Remove(typeof(T));
	}

	public void StartEventSource<T>(T source) where T : class, IEventSource {
		container.BuildUp(source);
		_eventSources.Add(source);
		container.Register(source);

		source.LoadConfig(container.Resolve<IPluginConfig>());
		source.Start();

		EventSourceRegistered?.Invoke(null, new EventSourceRegisteredEventArgs(source));
	}

	public void RegisterOverlayPreset2(IOverlayPreset preset) {
		_overlayPresets.Add(preset);
	}

	public void StartEventSources() {
		EventSourcesStarted?.Invoke(this, EventArgs.Empty);
	}

	// For backwards compat only!!
	public static TinyIoCContainer GetContainer() => (TinyIoCContainer)ActGlobals.oFormActMain.OverlayPluginContainer;
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class EventSourceRegisteredEventArgs(IEventSource eventSource) : EventArgs {
	public IEventSource EventSource { get; private set; } = eventSource;
}
