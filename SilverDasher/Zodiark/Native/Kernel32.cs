using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Zodiark.Native;

public static class Kernel32
{
	public struct LUID
	{
		public uint LowPart;

		public int HighPart;
	}

	public struct PRIVILEGE_SET
	{
		public uint PrivilegeCount;

		public uint Control;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
		public LUID_AND_ATTRIBUTES[] Privilege;
	}

	public struct LUID_AND_ATTRIBUTES
	{
		public LUID Luid;

		public uint Attributes;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int processId);

	[DllImport("kernel32.dll")]
	[SuppressUnmanagedCodeSecurity]
	public static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);

	[DllImport("kernel32.dll")]
	[SuppressUnmanagedCodeSecurity]
	public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

	[DllImport("kernel32.dll")]
	[SuppressUnmanagedCodeSecurity]
	public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

	[DllImport("kernel32.dll")]
	[SuppressUnmanagedCodeSecurity]
	public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

	[DllImport("kernel32.dll", SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	public static extern IntPtr GetCurrentProcess();

	[DllImport("advapi32.dll", SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

	[DllImport("advapi32.dll", SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref LUID lpLuid);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool PrivilegeCheck(IntPtr clientToken, ref PRIVILEGE_SET requiredPrivileges, out bool pfResult);

	[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	public static extern IntPtr GetExitCodeThread(IntPtr handle, out IntPtr lpExitCode);

	[DllImport("kernel32.dll")]
	[SuppressUnmanagedCodeSecurity]
	public static extern int CloseHandle(IntPtr hObject);
}
