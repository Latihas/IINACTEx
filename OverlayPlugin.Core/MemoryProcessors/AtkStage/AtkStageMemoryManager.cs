using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage;

public interface IAtkStageMemory : IVersionedMemory {
	IntPtr GetAddonAddress(string name);
	T? GetAddon<T>() where T : struct;
	object GetAddon(string name);
}

internal class AtkStageMemoryManager : IAtkStageMemory {
	private readonly TinyIoCContainer container;
	private readonly FFXIVRepository repository;
	private IAtkStageMemory memory;

	public AtkStageMemoryManager(TinyIoCContainer container) {
		this.container = container;
		container.Register<IAtkStageMemory62, AtkStageMemory62>();
		repository = container.Resolve<FFXIVRepository>();

		var memory = container.Resolve<FFXIVMemory>();
		memory.RegisterOnProcessChangeHandler(FindMemory);
	}

	private void FindMemory(object? sender, Process p) {
		memory = null;
		if (p == null) return;

		ScanPointers();
	}

	public void ScanPointers() {
		var candidates = new List<IAtkStageMemory> { container.Resolve<IAtkStageMemory62>() };
		memory = FFXIVMemory.FindCandidate(candidates, repository.GetMachinaRegion());
	}

	public bool IsValid() => memory != null && memory.IsValid();

	public Version GetVersion() => !IsValid() ? null : memory.GetVersion();

	public IntPtr GetAddonAddress(string name) {
		if (!IsValid()) {
			return IntPtr.Zero;
		}

		return memory.GetAddonAddress(name);
	}

	public T? GetAddon<T>() where T : struct => !IsValid() ? null : memory.GetAddon<T>();

	public object GetAddon(string name) => !IsValid() ? null : memory.GetAddon(name);
}