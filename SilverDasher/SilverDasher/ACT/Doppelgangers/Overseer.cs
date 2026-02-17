using System;
using System.Linq;
using SilverDasher.ACT.Enums;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Doppelgangers;

internal class Overseer : Doppelganger
{
	public Overseer(SilverDasher self)
		: base(self)
	{
	}

	internal override void Init()
	{
	}

	internal void OnNetworkReceive(string connection, long epoch, byte[] message)
	{
		OpcodeType packetOpcodeType = GetPacketOpcodeType(message);
		Opcode opcode = base.Keeper.Opcodes.GetOpcode(packetOpcodeType);
		switch (packetOpcodeType)
		{
		case OpcodeType.ActorControlSelf:
			HandleActorControlSelf(message, opcode);
			break;
		case OpcodeType.FateInfo:
			HandleFateInfo(message, opcode);
			break;
		case OpcodeType.InitZone:
			HandleInitZone(message, opcode);
			break;
		}
	}

	private void HandleActorControlSelf(byte[] message, Opcode opcode)
	{
		if (message.Length != opcode.CnLength)
		{
			base.Logger.Debug($"Packet {opcode.Name} received unpredicted length. Expected {opcode.CnLength}, Received {message.Length}.");
			return;
		}
		byte[] array = message.Skip(32).ToArray();
		short type = BitConverter.ToInt16(array, 0);
		switch (opcode.GetPacketSubType(type))
		{
		case "FateProgress":
		{
			ushort num3 = BitConverter.ToUInt16(array, 4);
			byte b = array[8];
			base.Keeper.FateUpdate(num3, b);
			base.Logger.Debug($"ActorControlSelf FateProgress handled. Fate {num3} Progress {b}.");
			break;
		}
		case "FateEnd":
		{
			ushort num2 = BitConverter.ToUInt16(array, 4);
			base.Keeper.CurrentFates.Remove(num2);
			base.Logger.Debug($"ActorControlSelf FateEnd handled. Fate {num2} ended.");
			break;
		}
		case "FateStart":
		{
			ushort num = BitConverter.ToUInt16(array, 4);
			base.Keeper.FateUpdate(num, 0);
			base.Logger.Debug($"ActorControlSelf FateStart handled. Fate {num} started.");
			break;
		}
		}
	}

	private void HandleFateInfo(byte[] message, Opcode opcode)
	{
		if (message.Length != opcode.CnLength)
		{
			base.Logger.Debug($"Packet {opcode.Name} received unpredicted length. Expected {opcode.CnLength}, Received {message.Length}.");
			return;
		}
		byte[] value = message.Skip(32).ToArray();
		ushort fateId = BitConverter.ToUInt16(value, 0);
		uint startTime = BitConverter.ToUInt32(value, 8);
		uint duration = BitConverter.ToUInt32(value, 16);
		base.Keeper.FateUpdate(fateId, -1, null, startTime, duration);
	}

	private void HandleInitZone(byte[] message, Opcode opcode)
	{
		if (message.Length != opcode.CnLength)
		{
			base.Logger.Debug($"Packet {opcode.Name} received unpredicted length. Expected {opcode.CnLength}, Received {message.Length}.");
			return;
		}
		byte[] value = message.Skip(32).ToArray();
		BitConverter.ToUInt16(value, 0);
		ushort num = BitConverter.ToUInt16(value, 2);
		ushort num2 = BitConverter.ToUInt16(value, 4);
		if (num2 <= 3)
		{
			base.Keeper.NetworkInstance = num2;
			base.Keeper.NetworkMapID = num;
			base.Keeper.CurrentMobs.Clear();
			base.Keeper.CurrentFates.Clear();
			Log($"Network map changes to {num}");
			Log($"Network instance changes to {num2}");
		}
	}

	private void HandleCEDirector(byte[] message)
	{
	}

	internal OpcodeType GetPacketOpcodeType(byte[] message)
	{
		return base.Keeper.Opcodes.GetOpcodeType(BitConverter.ToUInt16(message, 18));
	}

	internal override void Deinit()
	{
	}
}
