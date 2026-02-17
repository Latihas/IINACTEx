using System;
using System.Diagnostics;
using Zodiark;

namespace SilverDasher.ACT.Doppelgangers;

internal class Primal : Doppelganger
{
	private ZodiarkProcess zame;

	private IntPtr? instance;

	public static IntPtr Handle { get; private set; }

	public IntPtr Instance
	{
		get
		{
			if (instance.HasValue)
			{
				return instance.Value;
			}
			_ = IntPtr.Zero;
			instance = zame.Scanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 40 3A C7 75 ?? 48 8D 0D", 2) + 32;
			IntPtr? intPtr = instance;
			IntPtr zero = IntPtr.Zero;
			if (!intPtr.HasValue)
			{
				_ = 1;
			}
			else if (intPtr.HasValue)
			{
				if (intPtr.GetValueOrDefault() != zero)
				{
					_ = 1;
				}
				else
					_ = 0;
			}
			else
				_ = 0;
			return instance.Value;
		}
	}

	internal Primal(SilverDasher self)
		: base(self)
	{
	}

	internal override void Init()
	{
		ChangeProcess(base.Negotiator.ffdata.GetCurrentFFXIVProcess());
	}

	internal override void Deinit()
	{
		zame = null;
	}

	internal void ChangeProcess(Process game)
	{
		if (game == null || game.HasExited)
		{
			zame = null;
		}
		else
		{
			zame = new ZodiarkProcess(game);
		}
	}

	internal byte GetCurrentInstance()
	{
		try
		{
			if (zame != null && zame.Process != null && !zame.Process.HasExited)
			{
				byte b = zame.Memory.Read<byte>(Instance);
				Debug($"Scanned instance {b}");
				return b;
			}
		}
		catch (Exception ex)
		{
			base.Logger.Debug(ex.ToString());
			base.Logger.Debug(ex.StackTrace);
		}
		return byte.MaxValue;
	}
}
