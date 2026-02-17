using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Zodiark.Memory;

namespace Zodiark.Scanner;

public sealed class SignatureScanner
{
	private readonly ProcessMemory ProcessMemory;

	public bool Is32BitProcess { get; }

	public IntPtr SearchBase => Module.BaseAddress;

	public IntPtr TextSectionBase => new IntPtr(SearchBase.ToInt64() + TextSectionOffset);

	public long TextSectionOffset { get; private set; }

	public int TextSectionSize { get; private set; }

	public IntPtr DataSectionBase => new IntPtr(SearchBase.ToInt64() + DataSectionOffset);

	public long DataSectionOffset { get; private set; }

	public int DataSectionSize { get; private set; }

	public ProcessModule Module { get; private set; }

	private IntPtr TextSectionTop => TextSectionBase + TextSectionSize;

	public SignatureScanner(ZodiarkProcess zodiark, ProcessModule module)
	{
		Module = module;
		Is32BitProcess = !Environment.Is64BitProcess;
		ProcessMemory = zodiark.Memory;
		SetupSearchSpace(module);
	}

	public IntPtr ScanText(string signature)
	{
		IntPtr textSectionBase = TextSectionBase;
		IntPtr intPtr = Scan(textSectionBase, TextSectionSize, signature);
		if (ProcessMemory.ReadByte(intPtr) == 232)
		{
			return ReadCallSig(intPtr);
		}
		return intPtr;
	}

	public IntPtr GetStaticAddressFromSig(string signature, int offset = 0)
	{
		IntPtr pointer = ScanText(signature);
		pointer = IntPtr.Add(pointer, offset);
		long num = (long)Module.BaseAddress;
		long num2;
		do
		{
			pointer = IntPtr.Add(pointer, 1);
			num2 = ProcessMemory.ReadInt32(pointer) + (long)pointer + 4 - num;
		}
		while (num2 < DataSectionOffset || num2 > DataSectionOffset + DataSectionSize);
		return IntPtr.Add(pointer, ProcessMemory.ReadInt32(pointer) + 4);
	}

	public IntPtr ScanData(string signature)
	{
		return Scan(DataSectionBase, DataSectionSize, signature);
	}

	public IntPtr ScanModule(string signature)
	{
		return Scan(SearchBase, Module.ModuleMemorySize, signature);
	}

	public unsafe IntPtr Scan(IntPtr baseAddress, int size, string signature)
	{
		byte?[] array = SigToNeedle(signature);
		byte[] buffer = new byte[size];
		ProcessMemory.ReadBytes(baseAddress, buffer, size);
		for (long num = 0L; num < size - array.Length; num++)
		{
			if (IsMatch(array, buffer, num))
			{
				return (IntPtr)new UIntPtr(Convert.ToUInt64(baseAddress.ToInt64() + num)).ToPointer();
			}
		}
		throw new KeyNotFoundException("Can't find a signature of " + signature);
	}

	private IntPtr ResolveRelativeAddress(IntPtr nextInstAddr, int relOffset)
	{
		if (Is32BitProcess)
		{
			throw new NotSupportedException("32 bit is not supported.");
		}
		return nextInstAddr + relOffset;
	}

	private void SetupSearchSpace(ProcessModule module)
	{
		IntPtr baseAddress = module.BaseAddress;
		int num = ProcessMemory.ReadInt32(baseAddress, 60);
		IntPtr intPtr = baseAddress + num;
		IntPtr intPtr2 = intPtr + 4;
		short num2 = ProcessMemory.ReadInt16(intPtr, 6);
		IntPtr intPtr3 = intPtr2 + 20;
		IntPtr intPtr4 = ((!Is32BitProcess) ? (intPtr3 + 240) : (intPtr3 + 224));
		IntPtr baseAddress2 = intPtr4;
		for (int i = 0; i < num2; i++)
		{
			switch (ProcessMemory.ReadInt64(baseAddress2))
			{
			case 500236121134L:
				TextSectionOffset = ProcessMemory.ReadInt32(baseAddress2, 12);
				TextSectionSize = ProcessMemory.ReadInt32(baseAddress2, 8);
				break;
			case 418564367406L:
				DataSectionOffset = ProcessMemory.ReadInt32(baseAddress2, 12);
				DataSectionSize = ProcessMemory.ReadInt32(baseAddress2, 8);
				break;
			}
			baseAddress2 += 40;
		}
	}

	private IntPtr ReadCallSig(IntPtr sigLocation)
	{
		int num = ProcessMemory.ReadInt32(IntPtr.Add(sigLocation, 1));
		return IntPtr.Add(sigLocation, 5 + num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsMatch(byte?[] needle, byte[] buffer, long offset)
	{
		for (int i = 0; i < needle.Length; i++)
		{
			byte? b = needle[i];
			if (b.HasValue)
			{
				byte b2 = buffer[offset + i];
				if (b != b2)
				{
					return false;
				}
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private byte?[] SigToNeedle(string signature)
	{
		signature = signature.Replace(" ", string.Empty);
		if (signature.Length % 2 != 0)
		{
			throw new ArgumentException("Signature without whitespaces must be divisible by two.", "signature");
		}
		int num = signature.Length / 2;
		byte?[] array = new byte?[num];
		for (int i = 0; i < num; i++)
		{
			string text = signature.Substring(i * 2, 2);
			if (text == "??" || text == "**")
			{
				array[i] = null;
			}
			else
			{
				array[i] = byte.Parse(text, NumberStyles.AllowHexSpecifier);
			}
		}
		return array;
	}
}
