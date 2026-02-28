using System.IO;
using System.Net.Http;

namespace IINACT.Latihas;

public class FileDownloader {
    private readonly string url, savePath;
    private readonly Action? completeCallback;
    internal float Progress;

    internal FileDownloader(string fileUrl, string savePath, Action? completeCallback = null) {
        url = fileUrl;
        this.savePath = savePath;
        this.completeCallback = completeCallback;
    }

    public async Task DownloadFileAsync() {
        if (File.Exists(savePath)) File.Delete(savePath);
        using (var httpClient = new HttpClient()) {
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            using (var headResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url))) {
                headResponse.EnsureSuccessStatusCode();
            }
            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)) {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                long downloadedBytes = 0;
                await using (var stream = await response.Content.ReadAsStreamAsync())
                await using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true)) // 异步写入
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0) {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        downloadedBytes += bytesRead;
                        Progress = 1f * downloadedBytes / totalBytes;
                    }
                }
            }
        }
        completeCallback?.Invoke();
    }
}