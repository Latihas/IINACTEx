using System.IO.Compression;
using System.Text.RegularExpressions;
using Dalamud.Plugin.Services;

namespace FetchDependencies;

public partial class FetchDependencies {
    public FetchDependencies(Version version, string assemblyDir, bool isChinese, HttpClient httpClient, IPluginLog log) {
        PluginVersion = version;
        DependenciesDir = assemblyDir;
        IsChinese = isChinese;
        HttpClient = httpClient;
        Log = log;
    }

    private const string VersionUrlGlobal = "https://www.iinact.com/updater/version";
    private const string VersionUrlChinese = "https://cdn.diemoe.net/files/ACT.DieMoe/Packs/FFXIV_ACT_Plugin/chinese.version";
    private const string PluginUrlGlobal = "https://www.iinact.com/updater/download";
    private const string PluginUrlChinese = "https://meowrs.com/https://raw.githubusercontent.com/NewMoe-Technology/FFXIV_ACT_Plugin_CN/refs/heads/main/SDK/Latest/FFXIV_ACT_Plugin.dll";

    private Version PluginVersion { get; }
    private string DependenciesDir { get; }
    private bool IsChinese { get; }
    private HttpClient HttpClient { get; }
    internal static IPluginLog Log;
    public static string RemoteDieMoeBuildVersion = "";

    public void GetFfxivPlugin() {
        var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
        var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");

        if (!NeedsUpdate(pluginPath))
            return;

        if (IsChinese)
            DownloadFile(PluginUrlChinese, pluginPath);
        else
            HandleZipDownloadAndExtract(PluginUrlGlobal, pluginZipPath);

        CleanupDeucalion();

        var patcher = new Patcher(PluginVersion, DependenciesDir);
        patcher.MainPlugin();
        patcher.LogFilePlugin();
        patcher.MemoryPlugin();
    }

    private void HandleZipDownloadAndExtract(string url, string zipPath) {
        if (!File.Exists(zipPath))
            DownloadFile(url, zipPath);

        try {
            ZipFile.ExtractToDirectory(zipPath, DependenciesDir, true);
        }
        catch (InvalidDataException) {
            File.Delete(zipPath);
            DownloadFile(url, zipPath);
            ZipFile.ExtractToDirectory(zipPath, DependenciesDir, true);
        }
        finally {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    private void CleanupDeucalion() {
        foreach (var deucalionDll in Directory.GetFiles(DependenciesDir, "deucalion*.dll")) {
            try {
                File.Delete(deucalionDll);
            }
            catch {
            }
        }
    }

    [GeneratedRegex(@"build_version\s*=\s*([0-9.]+)", RegexOptions.Multiline)]
    private static partial Regex DieMoeBuildVersionRegex();

    private bool NeedsUpdate(string dllPath) {
        if (!File.Exists(dllPath)) {
            if (!IsChinese) return true;
            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var remoteVersionString = HttpClient.GetStringAsync(VersionUrlChinese, cancelAfterDelay.Token).Result;
            var match = DieMoeBuildVersionRegex().Match(remoteVersionString);
            var buildVersion = match.Success ? match.Groups[1].Value : string.Empty;
            if (string.IsNullOrEmpty(buildVersion)) {
                Log.Error($"Failed to parse DieMoe Plugin version string: {remoteVersionString}");
                return true;
            }
            RemoteDieMoeBuildVersion = buildVersion;
            return true;
        }
        try {
            using var plugin = new TargetAssembly(dllPath);

            if (!plugin.ApiVersionMatches())
                return true;

            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            if (!IsChinese)
                return new Version(HttpClient
                    .GetStringAsync(VersionUrlGlobal, cancelAfterDelay.Token).Result) > plugin.Version;
            var remoteVersionString = HttpClient.GetStringAsync(VersionUrlChinese, cancelAfterDelay.Token).Result;
            var match = DieMoeBuildVersionRegex().Match(remoteVersionString);
            var buildVersion = match.Success ? match.Groups[1].Value : string.Empty;
            if (string.IsNullOrEmpty(buildVersion)) {
                Log.Error($"Failed to parse DieMoe Plugin version string: {remoteVersionString}");
                return false;
            }
            RemoteDieMoeBuildVersion = buildVersion;
            var localVersion = plugin.GetDieMoeBuildVersion();
            return buildVersion != localVersion;
        }
        catch {
            return false;
        }
    }

    private void DownloadFile(string url, string path) {
        using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var downloadStream = HttpClient
            .GetStreamAsync(url,
                cancelAfterDelay.Token).Result;
        using var zipFileStream = new FileStream(path, FileMode.Create);
        downloadStream.CopyTo(zipFileStream);
        zipFileStream.Close();
    }
}