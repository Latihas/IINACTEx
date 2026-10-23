using System;
using System.Diagnostics;
using Machina.FFXIV;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors;

internal class NetworkParser {
	public event EventHandler<OnlineStatusChangedArgs>? OnOnlineStatusChanged;

	private class ActorControlPacket : MachinaPacketWrapper {
		public override string ToString(long epoch, uint ActorID) => "";
	}

	private readonly MachinaRegionalizedPacketHelper<ActorControlPacket>? actorControlPacketHelper;

	private readonly FFXIVRepository ffxiv;
	private GameRegion? currentRegion;

	private const string machinaPacketName = "ActorControl";

	public NetworkParser(TinyIoCContainer container) {
		ffxiv ??= container.Resolve<FFXIVRepository>();
		ffxiv.RegisterNetworkParser(Parse);

		if (!MachinaRegionalizedPacketHelper<ActorControlPacket>.Create(machinaPacketName, out actorControlPacketHelper)) {
			container.Resolve<ILogger>().Log(LogLevel.Error, $"Failed to initialize NetworkParser: Failed to create {machinaPacketName} packet helper from Machina structs");
		}
	}

	private void Parse(string id, long epoch, byte[] message) {
		if (actorControlPacketHelper == null) return;
		currentRegion ??= ffxiv.GetMachinaRegion();
		if (currentRegion == null) return;
		var helper = (MachinaPacketHelper<ActorControlPacket>)actorControlPacketHelper[currentRegion.Value];
		if (helper.ToStructs(message, out var header, out var packet)) {
			var category = packet.Get<Server_ActorControlCategory>("category");
			if (category != Server_ActorControlCategory.StatusUpdate) return;

			var actorID = header.ActorID;
			var param1 = packet.Get<uint>("param1");

			OnOnlineStatusChanged?.Invoke(null, new OnlineStatusChangedArgs(actorID, param1));
		}
	}
}

public class OnlineStatusChangedArgs(uint target, uint status) : EventArgs {
	public uint Target { get; private set; } = target;
	public uint Status { get; private set; } = status;
}