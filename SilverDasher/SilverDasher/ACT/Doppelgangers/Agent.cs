using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT.Doppelgangers;

internal class Agent(SilverDasher plugin) : Doppelganger(plugin) {
	public string session;

	private readonly Encoding encoding = Encoding.UTF8;

	internal override void Init() {
	}

	internal override void Deinit() {
	}

	public async Task UpdateData(bool force, CancellationToken token) {
		Log($"正在{(force ? "强制" : "检查")}更新资源文件。");
		var flag = false;
		Directory.CreateDirectory(Path.Combine(Utils.GetPluginDirectory(), "data"));
		if (force) {
			await Task.WhenAll(Keeper.GetStorages().Select(storage => UpdateResource(storage, token)));
			flag = true;
		} else {
			try {
				using var httpClient = new HttpClient();
				var responseContent = await httpClient.GetStringAsync(
					$"https://garlandtools.cn/silv/versions.json?t={DateTime.Now.Ticks}", token);
				var jObject = JsonConvert.DeserializeObject<JObject>(responseContent)!;
				List<Task> ts = [];
				foreach (var storage in Keeper.GetStorages()
					         .Where(s => s.ResourceFileName != null && jObject[s.ResourceFileName.Split('.')[0]]!.ToObject<int>() > s.Version)) {
					ts.Add(UpdateResource(storage, token));
					if (storage is FateStorage or MobStorage) flag = true;
				}
				await Task.WhenAll(ts);
				Log("检查更新资源文件完毕。");
			} catch (Exception ex) {
				Log(ex.ToString());
				Log(ex.Message);
				Log("检查更新资源文件失败。");
			}
		}
		if (flag) {
			Log("检测到狩猎/FATE列表有更新，重绘界面树。");
			Painter.RepaintNodes();
		}
	}

	private async Task UpdateResource(BaseStorage storage, CancellationToken token) {
		if (storage.ResourceFileName == null) return;
		var url = $"https://garlandtools.cn/silv/{storage.ResourceFileName}?t={DateTime.Now.Ticks}";
		Log($"正在更新资源文件{storage.ResourceFileName}。");
		try {
			using var httpClient = new HttpClient();
			using var response = await httpClient.GetAsync(url, token);
			response.EnsureSuccessStatusCode();
			await using var responseStream = await response.Content.ReadAsStreamAsync(token);
			using var streamReader = new StreamReader(responseStream, encoding);
			var contents = await streamReader.ReadToEndAsync(token);
			var savePath = Path.Combine(Utils.GetPluginDirectory(), "data", storage.ResourceFileName);
			await File.WriteAllTextAsync(savePath, contents, token);
			storage.Load();
			Log($"更新资源文件{storage.ResourceFileName}完毕。");
		} catch (Exception ex) {
			Log(ex.ToString());
			Log(ex.Message);
			Log($"检查更新资源文件{storage.ResourceFileName}失败。");
		}
	}

	public async Task<AuthResult> Setup(CancellationToken token) {
		var name = Keeper.PlayerName;
		var server = Keeper.PlayerWorld;
		var serverID = Keeper.PlayerWorldID;
		var url = $"{DataStorage.SilverDasherNest}wake";
		var enc = Encoding.UTF8;
		var requestData = $"{url}&i={Tailor.Judge()}&n={name}&s={serverID}&v={Tailor.Seal(server, name)}&ve={DataStorage.Version}";
		try {
			var content = new StringContent(requestData, enc, "application/x-www-form-urlencoded");
			using var httpClient = new HttpClient();
			using var response = await httpClient.PostAsync(url, content, token);
			response.EnsureSuccessStatusCode();
			var text = await response.Content.ReadAsStringAsync(token);
			switch (text) {
				case "Banned":
					return AuthResult.BANNED;
				case "Left":
					return AuthResult.LEFT;
				case "Expired":
					return AuthResult.EXPIRED;
			}
			if (text.StartsWith("Blocked"))
				return AuthResult.BLOCKED;
			if (text.Length != 16) return AuthResult.FAIL;
			session = text;
			return AuthResult.SUCCESS;
		} catch (Exception ex) {
			FileLog(ex.ToString());
			FileLog(ex.Message);
			Log("协商时出现意外错误。");
			return AuthResult.ERROR;
		}
	}
}