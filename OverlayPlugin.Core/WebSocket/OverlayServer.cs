using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace RainbowMage.OverlayPlugin.WebSocket;

internal class OverlayServer(IPAddress address, int port, TinyIoCContainer container) : WsServer(address, port) {
	private TinyIoCContainer Container { get; } = container;
	private ILogger Logger { get; } = container.Resolve<ILogger>();

	protected override TcpSession CreateSession() => new OverlaySession(this, Container);

	protected override void OnError(SocketError error) {
		Logger.Log(LogLevel.Error, $"Overlay WebSocket server caught an error with code {error}");
	}
}