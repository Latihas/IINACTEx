using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT.Doppelgangers;

internal class Agent : Doppelganger
{
	public string session;

	internal Encoding encoding = Encoding.UTF8;

	public Agent(SilverDasher plugin)
		: base(plugin)
	{
	}

	internal override void Init()
	{
	}

	internal override void Deinit()
	{
	}

	public async Task UpdateData(bool force)
	{
		string requestUriString = $"https://garlandtools.cn/silv/versions.json?t={DateTime.Now.Ticks}";
		HttpWebRequest request = WebRequest.Create(requestUriString) as HttpWebRequest;
		request.Method = "GET";
		Log("正在" + (force ? "强制" : "检查") + "更新资源文件。");
		await Task.Run(delegate
		{
			bool flag = false;
			if (force)
			{
				foreach (BaseStorage storage in Keeper.GetStorages())
				{
					UpdateResource(storage);
				}
				flag = true;
			}
			else
			{
				try
				{
					JObject jObject = JsonConvert.DeserializeObject<JObject>(new StreamReader((request.GetResponse() as HttpWebResponse).GetResponseStream(), encoding).ReadToEnd());
					foreach (BaseStorage storage2 in Keeper.GetStorages())
					{
						if (jObject[storage2.ResourceFileName.Split('.')[0]]!.ToObject<int>() > storage2.Version)
						{
							UpdateResource(storage2);
							if (storage2 is FateStorage || storage2 is MobStorage)
							{
								flag = true;
							}
						}
					}
					Log("检查更新资源文件完毕。");
				}
				catch (Exception ex)
				{
					Log(ex.ToString());
					Log(ex.Message);
					Log("检查更新资源文件失败。");
				}
			}
			if (flag)
			{
				Log("检测到狩猎/FATE列表有更新，重绘界面树。");
				Painter.pluginControl.Dispatcher.Invoke(delegate
				{
					Painter.RepaintNodes();
				});
			}
		});
	}

	public void UpdateResource(BaseStorage storage)
	{
		HttpWebRequest httpWebRequest = WebRequest.Create($"https://garlandtools.cn/silv/{storage.ResourceFileName}?t={DateTime.Now.Ticks}") as HttpWebRequest;
		httpWebRequest.Method = "GET";
		Log("正在更新资源文件" + storage.ResourceFileName + "。");
		try
		{
			string contents = new StreamReader((httpWebRequest.GetResponse() as HttpWebResponse).GetResponseStream(), encoding).ReadToEnd();
			File.WriteAllText(Path.Combine(Utils.GetPluginDirectory(), "data", storage.ResourceFileName), contents);
			storage.Load();
			Log("更新资源文件" + storage.ResourceFileName + "完毕。");
		}
		catch (Exception ex)
		{
			Log(ex.ToString());
			Log(ex.Message);
			Log("检查更新资源文件" + storage.ResourceFileName + "失败。");
		}
	}

	public async Task<AuthResult> Setup()
	{
		string name = Keeper.PlayerName;
		string server = Keeper.PlayerWorld;
		uint serverID = Keeper.PlayerWorldID;
		string url = DataStorage.SilverDasherNest + "wake";
		Encoding encoding = Encoding.UTF8;
		HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
		request.Method = "POST";
		request.ContentType = "application/x-www-form-urlencoded";
		return await Task.Run(delegate
		{
			try
			{
				byte[] bytes = encoding.GetBytes(url + "&i=" + Tailor.Judge() + "&n=" + name + "&s=" + serverID + "&v=" + Tailor.Seal(server, name) + "&ve=" + DataStorage.Version);
				request.ContentLength = bytes.Length;
				Stream requestStream = request.GetRequestStream();
				requestStream.Write(bytes, 0, bytes.Length);
				requestStream.Flush();
				requestStream.Close();
				string text = new StreamReader((request.GetResponse() as HttpWebResponse).GetResponseStream(), encoding).ReadToEnd();
				if (text == "Banned")
				{
					return AuthResult.BANNED;
				}
				if (text.StartsWith("Blocked"))
				{
					return AuthResult.BLOCKED;
				}
				if (text == "Left")
				{
					return AuthResult.LEFT;
				}
				if (text == "Expired")
				{
					return AuthResult.EXPIRED;
				}
				if (text.Length == 16)
				{
					session = text;
					return AuthResult.SUCCESS;
				}
				return AuthResult.FAIL;
			}
			catch (Exception ex)
			{
				FileLog(ex.ToString());
				FileLog(ex.Message);
				Log("协商时出现意外错误。");
				return AuthResult.ERROR;
			}
		});
	}
}
