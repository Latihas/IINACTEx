using System.Net.WebSockets;
using System.Text;

namespace IINACT.Latihas.Overlay;

public delegate void DataReceivedHandler(string data);

public class WebSocketClient : IDisposable {
    private readonly CancellationTokenSource _cts = new();
    private readonly ClientWebSocket _ws = new();
    public bool Ready;

    public event DataReceivedHandler? OnDataReceived;

    public async Task Connect(string url) {
        try {
            await _ws.ConnectAsync(new Uri(url), _cts.Token).ConfigureAwait(false);
            Plugin.Log.Info("WebSocket连接成功");
            _ = ReceiveLoop();
            Ready = true;
        }
        catch (Exception ex) {
            Plugin.Log.Warning($"WebSocket连接失败: {ex.Message}");
            Dispose();
            throw;
        }
    }

    private async Task ReceiveLoop() {
        var buffer = new byte[1024 * 8];
        var dataBuffer = new StringBuilder();

        try {
            while (_ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested) {
                var result = await _ws.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cts.Token
                ).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Text) {
                    dataBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    if (result.EndOfMessage) {
                        OnDataReceived?.Invoke(dataBuffer.ToString());
                        dataBuffer.Clear();
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close) {
                    await _ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "收到关闭请求",
                        CancellationToken.None
                    ).ConfigureAwait(false);
                    break;
                }
            }
        }
        catch (Exception ex) {
            Plugin.Log.Debug(ex.ToString());
        }
        finally {
            _ws.Dispose();
        }
    }


    public void Dispose() {
        try {
            _cts.Cancel();
            _cts.Dispose();
            if (_ws.State is WebSocketState.Open or WebSocketState.Connecting) {
                _ws.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Dispose关闭",
                    CancellationToken.None
                ).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
        catch (Exception) {
            //
        }
    }
}