using System.IO.Compression;

namespace FetchDependencies;

public class FetchDependencies {
    private const string VersionUrlGlobal = "https://www.iinact.com/updater/version";
    private const string VersionUrlChinese = "https://cninact.diemoe.net/CN解析/版本.txt";
    private const string PluginUrlGlobal = "https://www.iinact.com/updater/download";
    private const string PluginUrlChinese = "https://meowrs.com/https://raw.githubusercontent.com/NewMoe-Technology/FFXIV_ACT_Plugin_CN/refs/heads/main/SDK/Latest/FFXIV_ACT_Plugin.dll";

    private Version PluginVersion { get; }
    private string DependenciesDir { get; }
    private bool IsChinese { get; }
    private HttpClient HttpClient { get; }

    public FetchDependencies(Version version, string assemblyDir, bool isChinese, HttpClient httpClient) {
        PluginVersion = version;
        DependenciesDir = assemblyDir;
        IsChinese = isChinese;
        HttpClient = httpClient;
    }

    public void GetFfxivPlugin() {
        var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
        var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");

        if (!NeedsUpdate(pluginPath))
            return;

        // true ：统一使用 Global ZIP 逻辑
        // false ：国服使用独立 DLL (PluginUrlChinese)
        const bool useUnifiedGlobalZip = false; 

        if (useUnifiedGlobalZip || !IsChinese)
        {
            HandleZipDownloadAndExtract(PluginUrlGlobal, pluginZipPath);
        }
        else
        {
            DownloadFile(PluginUrlChinese, pluginPath);
        }

        CleanupDeucalion();

        var patcher = new Patcher(PluginVersion, DependenciesDir);
        patcher.MainPlugin();
        patcher.LogFilePlugin();
        patcher.MemoryPlugin();
    }

    private void HandleZipDownloadAndExtract(string url, string zipPath)
    {
        if (!File.Exists(zipPath))
            DownloadFile(url, zipPath);

        try
        {
            ZipFile.ExtractToDirectory(zipPath, DependenciesDir, true);
        }
        catch (InvalidDataException)
        {
            File.Delete(zipPath);
            DownloadFile(url, zipPath);
            ZipFile.ExtractToDirectory(zipPath, DependenciesDir, true);
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    private void CleanupDeucalion()
    {
        foreach (var deucalionDll in Directory.GetFiles(DependenciesDir, "deucalion*.dll"))
        {
            try { File.Delete(deucalionDll); } catch {}
        }
    }

    private bool NeedsUpdate(string dllPath)
    {
        if (!File.Exists(dllPath)) return true;
        try
        {
            using var plugin = new TargetAssembly(dllPath);

            if (!plugin.ApiVersionMatches())
                return true;
            
            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var remoteVersionString = HttpClient
                                      .GetStringAsync(IsChinese ? VersionUrlChinese : VersionUrlGlobal,
                                                      cancelAfterDelay.Token).Result;
            var remoteVersion = new Version(remoteVersionString);
            return remoteVersion > plugin.Version;
        }
        catch
        {
            return false;
        }
    }

    private void DownloadFile(string url, string path)
    {
        using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var downloadStream = HttpClient
                                   .GetStreamAsync(url,
                                                   cancelAfterDelay.Token).Result;
        using var zipFileStream = new FileStream(path, FileMode.Create);
        downloadStream.CopyTo(zipFileStream);
        zipFileStream.Close();
    }
}