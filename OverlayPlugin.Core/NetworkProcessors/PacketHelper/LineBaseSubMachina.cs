using System;
using Machina.FFXIV;

namespace RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

public abstract class LineBaseSubMachina<PacketType> where PacketType : MachinaPacketWrapper, new() {
	protected readonly FFXIVRepository ffxiv;

	protected readonly Func<string, DateTime, bool>? logWriter;
	protected MachinaRegionalizedPacketHelper<PacketType> packetHelper;
	private GameRegion? currentRegion;

	public LineBaseSubMachina(TinyIoCContainer container, uint logFileLineID, string logLineName, string machinaPacketName) {
		ffxiv ??= container.Resolve<FFXIVRepository>();
		ffxiv.RegisterNetworkParser(MessageReceived);

		if (MachinaRegionalizedPacketHelper<PacketType>.Create(machinaPacketName, out packetHelper)) {
			var customLogLines = container.Resolve<FFXIVCustomLogLines>();
			logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry {
				Name = logLineName,
				Source = "OverlayPlugin",
				ID = logFileLineID,
				Version = 1
			});
		} else {
			var logger = container.Resolve<ILogger>();
			logger.Log(LogLevel.Error, $"Failed to initialize {logFileLineID}: Failed to create {machinaPacketName} packet helper from Machina structs");
		}
	}

	protected virtual void MessageReceived(string id, long epoch, byte[] message) {
		currentRegion ??= ffxiv.GetMachinaRegion();
		var line = packetHelper[currentRegion.Value].ToString(epoch, message);
		if (line != null) logWriter?.Invoke(line, ffxiv.EpochToDateTime(epoch));
	}
}