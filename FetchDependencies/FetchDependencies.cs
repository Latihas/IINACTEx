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
	public string? LastLocalV {
		get {
			if (field == null) {
				using var plugin = new TargetAssembly(Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll"));
				field = plugin.GetDieMoeBuildVersion();
			}
			return field;
		}
		set;
	}

	public FetchDependencies(Version version, string assemblyDir, bool isChinese, int languagePreserve, HttpClient httpClient, IPluginLog log) {
		PluginVersion = version;
		DependenciesDir = assemblyDir;
		IsChinese = isChinese;
		HttpClient = httpClient;
		LanguagePreserve = languagePreserve;
		Log = log;
	}

	private const string VersionUrlGlobal = "https://www.iinact.com/updater/version";
	private const string VersionUrlChinese = "https://cdn.diemoe.net/files/ACT.DieMoe/Packs/FFXIV_ACT_Plugin/chinese.version";
	private const string VersionUrlChinese2 = "https://img.shields.io/github/v/release/NewMoe-Technology/FFXIV_ACT_Plugin_CN.json";
	private const string PluginUrlGlobal = "https://www.iinact.com/updater/download";
	private const string PluginUrlChinese = "https://meowrs.com/https://raw.githubusercontent.com/NewMoe-Technology/FFXIV_ACT_Plugin_CN/refs/heads/main/SDK/Latest/FFXIV_ACT_Plugin.dll";

	private Version PluginVersion { get; }
	private string DependenciesDir { get; }
	private bool IsChinese { get; }
	private int LanguagePreserve { get; }
	private HttpClient HttpClient { get; }
	internal static IPluginLog Log;
	public static string RemoteDieMoeBuildVersion = "";

	public bool CheckCnUpdate() => IsChinese && NeedsUpdate(Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll"));

	public void GetFfxivPlugin(bool canUpdate) {
		var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");
		if (File.Exists(pluginPath) && !canUpdate) return;
		var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
		if (!NeedsUpdate(pluginPath) && !canUpdate) return;
		Log.Warning(RemoteDieMoeBuildVersion);
		if (!File.Exists(pluginZipPath)) DownloadPlugin(pluginZipPath);
		if (IsChinese) DownloadFile(PluginUrlChinese, pluginPath);
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
		if (IsChinese && LanguagePreserve != 0 && LanguagePreserve != 1) patcher.ResourcePlugin(LanguagePreserve);
	}

	public string? GetFFXIV_ACT_PluginVersion() => new Patcher(PluginVersion, DependenciesDir).LogFilePluginVersion();

	[GeneratedRegex(@"display_at\s*=\s*([^\s]+)", RegexOptions.Multiline)]
	private static partial Regex DieMoeDisplayAtRegex();

	[GeneratedRegex("""
	                \s*"value":\s*"v?([^"]+)"
	                """, RegexOptions.Multiline)]
	private static partial Regex DieMoeGithubRegex();

	private string GetChineseVersionString() {
		using var ctso = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		var outerToken = ctso.Token;
		try {
			using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(outerToken);
			cts2.CancelAfter(TimeSpan.FromSeconds(10));
			var remoteVersionString2 = HttpClient.GetStringAsync(VersionUrlChinese2, cts2.Token)
				.GetAwaiter().GetResult();
			var match2 = DieMoeGithubRegex().Match(remoteVersionString2);
			return match2.Success ? match2.Groups[1].Value : string.Empty;
		} catch (Exception e) {
			Log.Warning(e.ToString());
		}
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(outerToken);
		cts.CancelAfter(TimeSpan.FromSeconds(10));
		var remoteVersionString = HttpClient.GetStringAsync(VersionUrlChinese, cts.Token)
			.GetAwaiter().GetResult();
		var match = DieMoeDisplayAtRegex().Match(remoteVersionString);
		return match.Success ? match.Groups[1].Value : string.Empty;
	}

	private bool NeedsUpdate(string dllPath) {
		if (!File.Exists(dllPath)) {
			if (!IsChinese) return true;
			try {
				var remotev = GetChineseVersionString();
				if (string.IsNullOrEmpty(remotev)) {
					Log.Error("Failed to parse DieMoe Plugin version string.");
					return true;
				}
				RemoteDieMoeBuildVersion = remotev;
				return true;
			} catch (Exception e) {
				Log.Warning(e.ToString());
				return true;
			}
		}
		try {
			using var plugin = new TargetAssembly(dllPath);
			if (!plugin.ApiVersionMatches()) return true;
			if (!IsChinese) {
				using var cancelAfterDelayG = new CancellationTokenSource(TimeSpan.FromSeconds(10));
				return new Version(HttpClient
					.GetStringAsync(VersionUrlGlobal, cancelAfterDelayG.Token).Result) > plugin.Version;
			}
			var remotev = GetChineseVersionString();
			if (string.IsNullOrEmpty(remotev)) {
				Log.Error("Failed to parse DieMoe Plugin version string.");
				return false;
			}
			RemoteDieMoeBuildVersion = remotev;
			LastLocalV = plugin.GetDieMoeBuildVersion();
			Log.Warning($"LocalV: {LastLocalV}, RemoteV: {remotev}");
			if (LastLocalV == null) return true;
			return string.CompareOrdinal(LastLocalV, remotev) == -1;
		} catch (Exception e) {
			Log.Info(e.ToString());
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
			.GetStreamAsync(url, cancelAfterDelay.Token).Result;
		using var zipFileStream = new FileStream(path, FileMode.Create);
		downloadStream.CopyTo(zipFileStream);
		zipFileStream.Close();
	}
}