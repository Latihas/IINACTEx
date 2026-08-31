using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Machina.FFXIV;
using Machina.FFXIV.Headers;

namespace RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

internal class RegionalizedPacketHelper<
	PacketStruct_Global,
	PacketStruct_CN,
	PacketStruct_KR,
	PacketStruct_TC>(ushort globalOpcode, ushort cnOpcode, ushort krOpcode, ushort tcOpcode)
	where PacketStruct_Global : struct, IPacketStruct
	where PacketStruct_CN : struct, IPacketStruct
	where PacketStruct_KR : struct, IPacketStruct
	where PacketStruct_TC : struct, IPacketStruct {
	public readonly PacketHelper<PacketStruct_Global> global = new(globalOpcode);
	public readonly PacketHelper<PacketStruct_CN> cn = new(cnOpcode);
	public readonly PacketHelper<PacketStruct_KR> kr = new(krOpcode);
	public readonly PacketHelper<PacketStruct_TC> tc = new(tcOpcode);

	public IPacketHelper this[GameRegion gameRegion] => gameRegion switch {
		GameRegion.Global => global,
		GameRegion.Chinese => cn,
		GameRegion.Korean => kr,
		GameRegion.TraditionalChinese => tc,
		_ => global
	};

	public static RegionalizedPacketHelper<PacketStruct_Global, PacketStruct_CN, PacketStruct_KR, PacketStruct_TC>
		CreateFromMachina(string opcodeName) {
		var opcodes = FFXIVRepository.GetMachinaOpcodes();
		if (opcodes == null) {
			return null;
		}

		if (!opcodes.TryGetValue(GameRegion.Global, out var globalOpcodes)) {
			return null;
		}
		if (!opcodes.TryGetValue(GameRegion.Chinese, out var cnOpcodes)) {
			return null;
		}
		if (!opcodes.TryGetValue(GameRegion.Korean, out var krOpcodes)) {
			return null;
		}
		if (!opcodes.TryGetValue(GameRegion.TraditionalChinese, out var tcOpcodes)) {
			// @TODO: Once FFXIV_ACT_Plugin has TC opcodes for global release, remove this default
			tcOpcodes = new Dictionary<string, ushort>();
			// return null;
		}

		if (!globalOpcodes.TryGetValue(opcodeName, out var globalOpcode)) {
			globalOpcode = 0;
		}
		if (!cnOpcodes.TryGetValue(opcodeName, out var cnOpcode)) {
			cnOpcode = 0;
		}
		if (!krOpcodes.TryGetValue(opcodeName, out var krOpcode)) {
			krOpcode = 0;
		}
		if (!tcOpcodes.TryGetValue(opcodeName, out var tcOpcode)) {
			tcOpcode = 0;
		}

		return new RegionalizedPacketHelper<PacketStruct_Global, PacketStruct_CN, PacketStruct_KR, PacketStruct_TC>
			(globalOpcode, cnOpcode, krOpcode, tcOpcode);
	}

	public static RegionalizedPacketHelper<PacketStruct_Global, PacketStruct_CN, PacketStruct_KR, PacketStruct_TC>
		CreateFromOpcodeConfig(OverlayPluginLogLineConfig opcodeConfig, string opcodeName) {
		var globalOpcodeConfigEntry = opcodeConfig[opcodeName, nameof(GameRegion.Global)];
		var cnOpcodeConfigEntry = opcodeConfig[opcodeName, nameof(GameRegion.Chinese)];
		var krOpcodeConfigEntry = opcodeConfig[opcodeName, nameof(GameRegion.Korean)];
		var tcOpcodeConfigEntry = opcodeConfig[opcodeName, nameof(GameRegion.TraditionalChinese)];

		var globalOpcode = (ushort)(globalOpcodeConfigEntry?.opcode ?? 0);
		var cnOpcode = (ushort)(cnOpcodeConfigEntry?.opcode ?? 0);
		var krOpcode = (ushort)(krOpcodeConfigEntry?.opcode ?? 0);
		var tcOpcode = (ushort)(tcOpcodeConfigEntry?.opcode ?? 0);

		return new RegionalizedPacketHelper<PacketStruct_Global, PacketStruct_CN, PacketStruct_KR, PacketStruct_TC>
			(globalOpcode, cnOpcode, krOpcode, tcOpcode);
	}
}

public interface IPacketHelper {
	string? ToString(long epoch, byte[] message);
}

internal class PacketHelper<PacketStruct>(ushort opcode) : IPacketHelper
	where PacketStruct : struct, IPacketStruct {
	public readonly ushort Opcode = opcode;
	public readonly int headerSize = Marshal.SizeOf<Server_MessageHeader>();
	public readonly int packetSize = Marshal.SizeOf<PacketStruct>();

	/// <summary>
	///     Construct a string representation of a packet from a byte array
	/// </summary>
	/// <param name="epoch">epoch timestamp from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
	/// <param name="message">Message byte array received from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
	/// <returns>null for invalid packet, otherwise a constructed packet</returns>
	public string ToString(long epoch, byte[] message) {
		if (!ToStructs(message, out var header, out var packet)) {
			return null;
		}

		return ToString(epoch, header, packet);
	}

	/// <summary>
	///     Construct a string representation of a packet from a byte array
	/// </summary>
	/// <param name="epoch">epoch timestamp from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
	/// <param name="message">Message byte array received from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
	/// <returns>null for invalid packet, otherwise a constructed packet</returns>
	public string ToString(long epoch, Server_MessageHeader header, PacketStruct packet) => packet.ToString(epoch, header.ActorID);

	public unsafe bool ToStructs(byte[] message, out Server_MessageHeader header, out PacketStruct packet) {
		// Message is too short to contain this packet
		if (message.Length < headerSize + packetSize) {
			header = default;
			packet = default;

			return false;
		}


		fixed (byte* messagePtr = message) {
			var headerPtr = new IntPtr(messagePtr);
			header = Marshal.PtrToStructure<Server_MessageHeader>(headerPtr);

			if (header.MessageType != Opcode) {
				header = default;
				packet = default;

				return false;
			}

			var packetPtr = new IntPtr(messagePtr + headerSize);
			packet = Marshal.PtrToStructure<PacketStruct>(packetPtr);

			return true;
		}
	}
}

internal interface IPacketStruct {
	string ToString(long epoch, uint ActorID);
}

internal interface IHeaderStruct {
	uint ActorID { get; }
	uint Opcode { get; }
}