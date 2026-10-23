using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Machina.FFXIV;

namespace RainbowMage.OverlayPlugin.MemoryProcessors;

public interface IVersionedMemory {
	Version GetVersion();
	void ScanPointers();
	bool IsValid();
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public partial class FFXIVMemory {
	[SuppressGCTransition]
	[LibraryImport("SafeMemoryReader.dll")]
	private static partial int ReadMemory(nint dest, nint src, int size);


	public IntPtr GetBaseAddress() => Process.GetCurrentProcess().MainModule!.BaseAddress;

	// private void CloseProcessHandle() {
	// }

	public bool IsValid() => true;

	public static unsafe string GetStringFromBytes(byte* source, int size, int realSize = 0) {
		var bytes = ArrayPool<byte>.Shared.Rent(size);

		fixed (byte* bytesPtr = bytes) {
			if (ReadMemory((nint)bytesPtr, (nint)source, size) != 0) {
				ArrayPool<byte>.Shared.Return(bytes);
				throw new InvalidOperationException("Failed to read memory.");
			}
		}

		for (var i = 0; i < size; i++) {
			if (bytes[i] != 0) {
				continue;
			}

			realSize = i;
			break;
		}

		var ret = Encoding.UTF8.GetString(bytes, 0, realSize);
		ArrayPool<byte>.Shared.Return(bytes);
		return ret;
	}

	// public static string GetStringFromBytes(byte[] source, int offset = 0, int size = 256) {
	// 	var bytes = ArrayPool<byte>.Shared.Rent(size);
	// 	Array.Copy(source, offset, bytes, 0, size);
	// 	var realSize = 0;
	// 	for (var i = 0; i < size; i++) {
	// 		if (bytes[i] != 0) {
	// 			continue;
	// 		}
	//
	// 		realSize = i;
	// 		break;
	// 	}
	//
	// 	var ret = Encoding.UTF8.GetString(bytes, 0, realSize);
	// 	ArrayPool<byte>.Shared.Return(bytes);
	// 	return ret;
	// }

	/// <summary>
	///     バッファの長さだけメモリを読み取ってバッファに格納
	/// </summary>
	public unsafe bool Peek(IntPtr address, byte[] buffer) {
		fixed (byte* bufferPtr = buffer) {
			var result = ReadMemory((nint)bufferPtr, address, buffer.Length);
			return result == 0;
		}
	}

	/// <summary>
	///     メモリから指定された長さだけ読み取りバイト配列として返す
	/// </summary>
	/// <param name="address">読み取る開始アドレス</param>
	/// <param name="length">読み取る長さ</param>
	/// <returns></returns>
	public byte[] GetByteArray(IntPtr address, int length) {
		var data = new byte[length];
		Peek(address, data);
		return data;
	}

	public unsafe int GetInt32(IntPtr address, int offset = 0) {
		int ret;
		var value = new byte[4];
		Peek(IntPtr.Add(address, offset), value);
		fixed (byte* p = &value[0]) {
			ret = *(int*)p;
		}
		return ret;
	}

	public byte[]? Read8(IntPtr addr, int count) {
		var data = new byte[count];
		return Peek(addr, data) ? data : null;
	}

	public IntPtr ReadIntPtr(IntPtr addr) {
		var buffer = Read8(addr, 8);
		return buffer == null ? IntPtr.Zero : new IntPtr(BitConverter.ToInt64(buffer, 0));
	}

	public List<IntPtr> SigScan(string pattern, int pattern_offset, bool rip_addressing, int rip_offset = 0) => [];

	// Returns the best candidate from a list of versioned memory candidates.
	public static T? FindCandidate<T>(List<T> candidates, GameRegion region) where T : IVersionedMemory => default;
}