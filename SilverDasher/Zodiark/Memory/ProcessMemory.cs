// using System;
// using System.Diagnostics;
// using System.Runtime.InteropServices;
// using Zodiark.Native;
//
// namespace Zodiark.Memory;
//
// public class ProcessMemory {
//     public static IntPtr Handle { get; private set; }
//
//     public static IntPtr BaseAddress { get; private set; }
//
//     public Process Process { get; private set; }
//
//     public ProcessMemory(ZodiarkProcess zodiark) {
//         Handle = zodiark.Handle;
//         BaseAddress = zodiark.BaseAddress;
//         Process = zodiark.Process;
//     }
//
//     public byte ReadByte(IntPtr baseAddress, int offset = 0) {
//         var array = new byte[1];
//         ReadBytes(baseAddress + offset, array, 1);
//         return array[0];
//     }
//
//     public short ReadInt16(IntPtr baseAddress, int offset = 0) {
//         var array = new byte[2];
//         ReadBytes(baseAddress + offset, array, 2);
//         return BitConverter.ToInt16(array, 0);
//     }
//
//     public int ReadInt32(IntPtr baseAddress, int offset = 0) {
//         var array = new byte[4];
//         ReadBytes(baseAddress + offset, array, 4);
//         return BitConverter.ToInt32(array, 0);
//     }
//
//     public long ReadInt64(IntPtr baseAddress, int offset = 0) {
//         var array = new byte[8];
//         ReadBytes(baseAddress + offset, array, 8);
//         return BitConverter.ToInt64(array, 0);
//     }
//
//     public IntPtr ReadIntPtr(IntPtr baseAddress, int offset = 0) {
//         var array = new byte[8];
//         ReadBytes(baseAddress + offset, array, 8);
//         var num = BitConverter.ToUInt64(array, 0);
//         return (IntPtr)(long)num;
//     }
//
//     public T Read<T>(IntPtr address) where T : struct {
//         if (address == IntPtr.Zero) {
//             throw new Exception("Invalid address");
//         }
//         var num = Marshal.SizeOf(typeof(T));
//         var intPtr = Marshal.AllocHGlobal(num);
//         Kernel32.ReadProcessMemory(Handle, address, intPtr, num, out _);
//         T? val = Marshal.PtrToStructure<T>(intPtr);
//         Marshal.FreeHGlobal(intPtr);
//         if (val.HasValue) {
//             return val.Value;
//         }
//         throw new Exception($"Failed to read memory {typeof(T)} from address {address}");
//     }
//
//     public bool ReadBytes(IntPtr address, byte[] buffer, int size = -1) {
//         if (size <= 0) {
//             size = buffer.Length;
//         }
//         IntPtr lpNumberOfBytesRead;
//         return Kernel32.ReadProcessMemory(Handle, address, buffer, size, out lpNumberOfBytesRead);
//     }
// }

