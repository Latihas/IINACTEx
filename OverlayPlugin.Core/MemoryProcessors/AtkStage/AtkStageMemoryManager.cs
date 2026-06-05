using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage;

public interface IAtkStageMemory : IVersionedMemory {
	IntPtr GetAddonAddress(string name);
	T? GetAddon<T>() where T : struct;
	object GetAddon(string name);
}

internal class AtkStageMemoryManager : IAtkStageMemory {
	private readonly IAtkStageMemory memory;

	public AtkStageMemoryManager(TinyIoCContainer container) {
		container.Register<IAtkStageMemory62, AtkStageMemory62>();
		memory = container.Resolve<IAtkStageMemory62>();
		memory.ScanPointers();
	}

	public void ScanPointers() {
	}

	public bool IsValid() => true;

	public Version GetVersion() => memory.GetVersion();

	public IntPtr GetAddonAddress(string name) => memory.GetAddonAddress(name);

	public T? GetAddon<T>() where T : struct => memory.GetAddon<T>();

	public object GetAddon(string name) => memory.GetAddon(name);
}