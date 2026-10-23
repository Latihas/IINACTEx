using System;
using Machina.FFXIV;

namespace RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

internal abstract class LineBaseCustom<
	PacketStruct_Global,
	PacketStruct_CN,
	PacketStruct_KR,
	PacketStruct_TC>
	where PacketStruct_Global : struct, IPacketStruct
	where PacketStruct_CN : struct, IPacketStruct
	where PacketStruct_KR : struct, IPacketStruct
	where PacketStruct_TC : struct, IPacketStruct {
	protected static FFXIVRepository ffxiv=null!;

	protected readonly Func<string, DateTime, bool>? logWriter;
	protected readonly RegionalizedPacketHelper<PacketStruct_Global, PacketStruct_CN, PacketStruct_KR, PacketStruct_TC> packetHelper;
	protected GameRegion? currentRegion;

	protected LineBaseCustom(TinyIoCContainer container, uint logFileLineID, string logLineName, string opcodeName) {
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		ffxiv ??= container.Resolve<FFXIVRepository>();
		ffxiv.RegisterNetworkParser(MessageReceived);

		var opcodeConfig = container.Resolve<OverlayPluginLogLineConfig>();

		packetHelper = RegionalizedPacketHelper<PacketStruct_Global, PacketStruct_CN, PacketStruct_KR, PacketStruct_TC>.CreateFromOpcodeConfig(opcodeConfig, opcodeName);

		var customLogLines = container.Resolve<FFXIVCustomLogLines>();
		logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry {
			Name = logLineName,
			Source = "OverlayPlugin",
			ID = logFileLineID,
			Version = 1
		});
	}


	protected virtual void MessageReceived(string id, long epoch, byte[] message) {
		currentRegion ??= ffxiv.GetMachinaRegion();
		var line = packetHelper[currentRegion.Value].ToString(epoch, message);
		if (line != null) logWriter?.Invoke(line,  ffxiv.EpochToDateTime(epoch));
	}
}