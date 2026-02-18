using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Zodiark.Memory;
using Zodiark.Native;
using Zodiark.Scanner;

namespace Zodiark;

public class ZodiarkProcess
{
	public IntPtr Handle { get; private set; }

	public IntPtr BaseAddress { get; private set; }

	public SignatureScanner Scanner { get; private set; }

	public Process Process { get; private set; }

	// public bool IsProcessAlive { get; private set; }

	public ProcessMemory Memory { get; private set; }

	public ZodiarkProcess(Process process)
	{
		OpenProcess(process);
		Memory = new ProcessMemory(this);
		Scanner = new SignatureScanner(this, Process.MainModule);
	}

	public void OpenProcess(Process process)
	{
		Process = process;
		BaseAddress = Process.MainModule.BaseAddress;
		// if (!Process.Responding)
		// {
		// 	throw new Exception("Target process id not responding");
		// }
		// if (process.MainModule == null)
		// {
		// 	throw new Exception("Process has no main module");
		// }
		// Process.EnterDebugMode();
		// int num = CheckSeDebugPrivilege(out var isDebugEnabled);
		// if (num != 0)
		// {
		// 	throw new Exception($"ERROR: CheckSeDebugPrivilege failed with error: {num}");
		// }
		// if (!isDebugEnabled)
		// {
		// 	throw new Exception("ERROR: SeDebugPrivilege not enabled. Please report this!");
		// }
		Handle = Kernel32.OpenProcess(2035711u, bInheritHandle: true, process.Id);
		// if (Handle == IntPtr.Zero)
		// {
		// 	int lastWin32Error = Marshal.GetLastWin32Error();
		// }
	}

	// private int CheckSeDebugPrivilege(out bool isDebugEnabled)
	// {
	// 	isDebugEnabled = false;
	// 	if (!Kernel32.OpenProcessToken(Kernel32.GetCurrentProcess(), 8u, out var tokenHandle))
	// 	{
	// 		return Marshal.GetLastWin32Error();
	// 	}
	// 	Kernel32.LUID lpLuid = default(Kernel32.LUID);
	// 	if (!Kernel32.LookupPrivilegeValue(null, "SeDebugPrivilege", ref lpLuid))
	// 	{
	// 		return Marshal.GetLastWin32Error();
	// 	}
	// 	Kernel32.PRIVILEGE_SET pRIVILEGE_SET = default(Kernel32.PRIVILEGE_SET);
	// 	pRIVILEGE_SET.PrivilegeCount = 1u;
	// 	pRIVILEGE_SET.Control = 1u;
	// 	pRIVILEGE_SET.Privilege = new Kernel32.LUID_AND_ATTRIBUTES[1];
	// 	Kernel32.PRIVILEGE_SET requiredPrivileges = pRIVILEGE_SET;
	// 	requiredPrivileges.Privilege[0].Luid = lpLuid;
	// 	requiredPrivileges.Privilege[0].Attributes = 2u;
	// 	if (!Kernel32.PrivilegeCheck(tokenHandle, ref requiredPrivileges, out var pfResult))
	// 	{
	// 		return Marshal.GetLastWin32Error();
	// 	}
	// 	isDebugEnabled = pfResult;
	// 	Kernel32.CloseHandle(tokenHandle);
	// 	return 0;
	// }

	// public bool GetIsProcessAlive()
	// {
	// 	if (Process == null || Process.HasExited)
	// 	{
	// 		return false;
	// 	}
	// 	if (!Process.Responding)
	// 	{
	// 		return false;
	// 	}
	// 	return true;
	// }

	// public SignatureScanner SetSignatureScanner(ProcessModule module)
	// {
	// 	return new SignatureScanner(this, module);
	// }

	// public SignatureScanner SetSignatureScanner(string moduleName)
	// {
	// 	ProcessModule module = Process.Modules.Cast<ProcessModule>().First((ProcessModule m) => m.ModuleName == moduleName);
	// 	return new SignatureScanner(this, module);
	// }
}
