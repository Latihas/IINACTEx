using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
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

		if (!File.Exists(pluginZipPath)) {
			DownloadPlugin(pluginZipPath);
		}

		if (IsChinese)
			DownloadFile(PluginUrlChinese, pluginPath);
		else {
			try {
				ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
			} catch (InvalidDataException) {
				DownloadPlugin(pluginZipPath);
				ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
			}
			File.Delete(pluginZipPath);
		}
		foreach (var deucalionDll in Directory.GetFiles(DependenciesDir, "deucalion*.dll"))
			File.Delete(deucalionDll);

		var patcher = new Patcher(PluginVersion, DependenciesDir);
		patcher.MainPlugin();
		patcher.LogFilePlugin();
		patcher.MemoryPlugin();
	}


	[GeneratedRegex(@"build_version\s*=\s*([0-9.]+)", RegexOptions.Multiline)]
	private static partial Regex DieMoeBuildVersionRegex();

	private bool NeedsUpdate(string dllPath) {
		if (!File.Exists(dllPath)) {
			if (!IsChinese) return true;
			try {
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
			} catch {
				return true;
			}
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
		} catch {
			return false;
		}
	}

	private void DownloadPlugin(string pluginZipPath) {
		try {
			DownloadFile(IsChinese ? PluginUrlChinese : PluginUrlGlobal, pluginZipPath);
		} catch {
			using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/ravahn/FFXIV_ACT_Plugin/releases/latest");
			request.Headers.UserAgent.ParseAdd("IINACT/1.0");
			using var response = HttpClient.Send(request);
			response.EnsureSuccessStatusCode();

			using var stream = response.Content.ReadAsStream();
			var json = JsonNode.Parse(stream);
			var downloadUrl = json?["assets"]?[0]?["browser_download_url"]?.ToString();

			if (string.IsNullOrEmpty(downloadUrl))
				throw new Exception("Could not find fallback download URL from GitHub API.");

			DownloadFile(downloadUrl, pluginZipPath);
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