using System;
using System.Diagnostics;
using Machina.FFXIV;

namespace RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

internal abstract class LineBaseCustomMachina<
	PacketStruct_Global,
	PacketStruct_CN,
	PacketStruct_KR,
	PacketStruct_TC>
	where PacketStruct_Global : struct, IPacketStruct
	where PacketStruct_CN : struct, IPacketStruct
	where PacketStruct_KR : struct, IPacketStruct
	where PacketStruct_TC : struct, IPacketStruct {
	protected static FFXIVRepository ffxiv;

	protected readonly Func<string, DateTime, bool> logWriter;
	protected readonly RegionalizedPacketHelper<
		PacketStruct_Global,
		PacketStruct_CN,
		PacketStruct_KR,
		PacketStruct_TC> packetHelper;
	protected GameRegion? currentRegion;

	public LineBaseCustomMachina(TinyIoCContainer container, uint logFileLineID, string logLineName, string machinaPacketName) {
		ffxiv = ffxiv ?? container.Resolve<FFXIVRepository>();
		ffxiv.RegisterNetworkParser(MessageReceived);
		ffxiv.RegisterProcessChangedHandler(ProcessChanged);

		packetHelper = RegionalizedPacketHelper<
			PacketStruct_Global,
			PacketStruct_CN,
			PacketStruct_KR,
			PacketStruct_TC>.CreateFromMachina(machinaPacketName);

		if (packetHelper == null) {
			var logger = container.Resolve<ILogger>();
			logger.Log(LogLevel.Error, $"Failed to initialize {logLineName}: Failed to create {machinaPacketName} packet helper from Machina structs");
			return;
		}

		var customLogLines = container.Resolve<FFXIVCustomLogLines>();
		logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry {
			Name = logLineName,
			Source = "OverlayPlugin",
			ID = logFileLineID,
			Version = 1
		});
	}

	protected virtual void ProcessChanged(Process process) {
		if (!ffxiv.IsFFXIVPluginPresent())
			return;

		currentRegion = null;
	}

	protected virtual void MessageReceived(string id, long epoch, byte[] message) {
		if (packetHelper == null)
			return;

		if (currentRegion == null)
			currentRegion = ffxiv.GetMachinaRegion();

		if (currentRegion == null)
			return;

		var line = packetHelper[currentRegion.Value].ToString(epoch, message);

		if (line != null) {
			var serverTime = ffxiv.EpochToDateTime(epoch);
			logWriter(line, serverTime);
		}
	}
}