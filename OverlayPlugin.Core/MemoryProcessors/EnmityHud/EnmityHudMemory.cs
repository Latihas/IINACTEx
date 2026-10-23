using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.EnmityHud;

public abstract class EnmityHudMemory(
	TinyIoCContainer container, int[] enmityHudPointerPath, int enmityHudCountOffset,
	int enmityHudEntryOffset, int enmityHudEntrySize) : IEnmityHudMemory {
	private readonly FFXIVMemory memory = container.Resolve<FFXIVMemory>();

	// public IntPtr enmityHudAddress = IntPtr.Zero;
	public IntPtr enmityHudDynamicAddress = IntPtr.Zero;

	private DateTimeOffset lastDateTimeDynamicAddressChecked = DateTimeOffset.MinValue;

	public bool IsValid() => true;

	public void ScanPointers() {
	}

	public abstract Version GetVersion();

	private bool GetDynamicPointerAddress() {
		var enmityHudAddress = FFXIVClientStructs.FFXIV.Component.GUI.AtkStage.Addresses.Instance.Value;

		// Resolve Dynamic Pointers.
		// The static sigscan address fetched in GetPointerAddress is statically allocated.
		// However, the HUD entry is dynamically allocated every time the HUD is refreshed.
		// Walk down the pointer tree to the actual address we want, determined by enmityHudPointerPath.
		if (DateTimeOffset.UtcNow - lastDateTimeDynamicAddressChecked < TimeSpan.FromSeconds(30)) return true;
		lastDateTimeDynamicAddressChecked = DateTimeOffset.UtcNow;
		foreach (var p in enmityHudPointerPath) {
			enmityHudAddress = memory.ReadIntPtr(enmityHudAddress + p);
			if (enmityHudAddress == IntPtr.Zero) {
				enmityHudDynamicAddress = IntPtr.Zero;
				return false;
			}
		}
		enmityHudDynamicAddress = new IntPtr(enmityHudAddress.ToInt64());
		return true;
	}

	public List<EnmityHudEntry> GetEnmityHudEntries() {
		var entries = new List<EnmityHudEntry>();

		if (!GetDynamicPointerAddress()) return entries;

		// Get EnmityHud Count, Empty(Min) = 0, Max = 8
		var count = memory.GetInt32(enmityHudDynamicAddress, enmityHudCountOffset);
		if (count < 0) count = 0;
		if (count > 8) count = 8;
		// Get data from memory (all 8 entries)
		var buffer = memory.GetByteArray(enmityHudDynamicAddress + enmityHudEntryOffset, 8 * enmityHudEntrySize);
		// Parse data
		for (var i = 0; i < count; i++) entries.Add(GetEnmityHudEntryFromBytes(buffer, i));
		return entries;
	}

	protected abstract EnmityHudEntry GetEnmityHudEntryFromBytes(byte[] source, int num);
}