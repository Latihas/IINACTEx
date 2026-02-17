using System;
using System.Collections.Generic;
using Advanced_Combat_Tracker;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SilverDasher.ACT.Models;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace SilverDasher.ACT.Doppelgangers;

internal class Notifier : Doppelganger
{
	private bool ToastFailed;

	private readonly JsonSerializerSettings unpackSettings = new JsonSerializerSettings
	{
		NullValueHandling = NullValueHandling.Ignore,
		ContractResolver = StrNullToEmptyContractResolver.DefaultInstance
	};

	internal Notifier(SilverDasher self)
		: base(self)
	{
	}

	internal override void Init()
	{
	}

	internal override void Deinit()
	{
	}

	internal void Unpack(string topic, string message)
	{
		dynamic val = JsonConvert.DeserializeObject<JObject>(message, unpackSettings);
		string[] array = topic.Split('/');
		string text = array[1];
		if (!base.Keeper.Worlds.TryGetByLabel(text, out var w))
		{
			Log("Invalid world " + text + " for topic " + topic + ".");
			return;
		}
		string text2 = array[2];
		string s = array[3];
		if (text2 == "hunt")
		{
			int num = int.Parse(s);
			int map = (int)val.m.Value;
			int num2 = 0;
			int hp = 100;
			Coordinate coords = null;
			try
			{
				num2 = (int)val.i.Value;
				if (val.c != null)
				{
					Coordinate coordinate = new Coordinate();
					coordinate.x = (int)val.c.x.Value;
					coordinate.y = (int)val.c.y.Value;
					coords = coordinate;
				}
				if (val.hp != null)
				{
					hp = (int)val.hp.Value;
				}
			}
			catch (Exception)
			{
			}
			base.Keeper.ReceivedMobUpdate(w, map, num2, num, hp, coords);
			if (Keeper.Config.ExtendedReport)
			{
				string s2 = $"{w.Name} - {val.m.Value}{num2} - {num}";
				Log(s2);
			}
		}
		else if (text2 == "fate")
		{
			int num3 = int.Parse(s);
			int map2 = (int)val.m.Value;
			int num4 = 0;
			int hp2 = 0;
			Coordinate coords2 = null;
			try
			{
				num4 = (int)val.i.Value;
				if (val.c != null)
				{
					Coordinate coordinate = new Coordinate();
					coordinate.x = (int)val.c.x.Value;
					coordinate.y = (int)val.c.y.Value;
					coords2 = coordinate;
				}
				if (val.hp != null)
				{
					hp2 = 100 - (int)val.hp.Value;
				}
			}
			catch (Exception)
			{
			}
			base.Keeper.ReceivedFateUpdate(w, map2, num4, num3, hp2, coords2);
			if (Keeper.Config.ExtendedReport)
			{
				string s3 = $"{w.Name} - {val.m.Value}{num4} - {num3}";
				Log(s3);
			}
		}
		else
		{
			Log("Unknown packet type " + text2 + ".");
		}
	}

	public void NotifyMobStatusChanged(string worldId, int instance, HuntMob mob)
	{
		Notify(worldId, instance, mob, mob.State);
	}

	public void NotifyFateStatusChanged(string worldId, int instance, Fate fate)
	{
		Notify(worldId, instance, fate, fate.State);
	}

	public void Notify(string worldId, int instance, GameDynamicObject gobj, HuntState status)
	{
		if (base.Keeper.InDuty() && Keeper.Config.PauseInDuty)
		{
			return;
		}
		World byLabel = base.Keeper.Worlds.GetByLabel(worldId);
		string text = "";
		if (!base.Keeper.Territories.TryGet((int)gobj.TerritoryID, out var item))
		{
			Log($"处理信息时收到未能识别的地图编码。{gobj.TerritoryID}");
			return;
		}
		if (instance > 0)
		{
			text = $"[{instance}]";
		}
		string text2 = ((!item.IsDataCenterMap) ? $"{byLabel.Name} - {item}{text} - {gobj.Name}" : $"{byLabel.DataCenter} - {item}{text} - {gobj.Name}");
		string coord = "";
		if (gobj.Coordinate != null)
		{
			coord = "(" + gobj.Coordinate.displayX + ", " + gobj.Coordinate.displayY + ") ";
		}
		SendToast(text2, status, coord);
		SendTTS(text2, status, coord);
		Log("已尝试推送 " + text2 + ", " + base.Keeper.Mobs.GetStateName(status));
	}

	public void WriteLogline(World world, int instance, GameDynamicObject gobj)
	{
		if (Keeper.Config.WriteACTLog)
		{
			string logLine = "";
			Territory territory = base.Keeper.Territories.Get(gobj.TerritoryID);
			if (gobj is HuntMob)
			{
				HuntMob huntMob = gobj as HuntMob;
				logLine = $"00|{DateTime.Now:O}|0|SilverDasher|{gobj.TypeName}|{gobj.Id}|{gobj.Name}|{world.Id}|{world.Name}|{gobj.TerritoryID}|{territory}|{instance}|{huntMob.RankRaw}|{gobj.Coordinate?.displayX}|{gobj.Coordinate?.displayY}|{gobj.Progress}";
			}
			else if (gobj is Fate)
			{
				Fate fate = gobj as Fate;
				logLine = $"00|{DateTime.Now:O}|0|SilverDasher|{gobj.TypeName}|{gobj.Id}|{gobj.Name}|{world.Id}|{world.Name}|{gobj.TerritoryID}|{territory}|{instance}|{fate.Special}|{gobj.Coordinate?.displayX}|{gobj.Coordinate?.displayY}|{gobj.Progress}";
			}
			// ActGlobals.oFormActMain.ParseRawLogLine(isImport: false, DateTime.Now, logLine);
			ActGlobals.oFormActMain.ParseRawLogLine( logLine);
		}
	}

	public void SendTTS(string message, HuntState status, string coord)
	{
		if (Keeper.Config.TTS && Keeper.Config.StatusPushable("TTS", status))
		{
			if (Keeper.Config.TTSExtend)
			{
				message += coord;
			}
			ActGlobals.oFormActMain.TTS(message + base.Keeper.Mobs.GetStateName(status));
		}
	}

	public bool SendToast(string message, HuntState status, string coord = "", bool checkPermit = true)
	{
		if (checkPermit)
		{
			if (!Keeper.Config.SystemToast)
			{
				return false;
			}
			if (!Keeper.Config.StatusPushable("Toast", status))
			{
				return false;
			}
		}
		try
		{
			if ("Legacy" == Keeper.Config.ToastType || ToastFailed)
			{
				Version version = Environment.OSVersion.Version;
				Version value = new Version("6.2");
				if (version.CompareTo(value) >= 0)
				{
					XmlDocument templateContent = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
					XmlNodeList elementsByTagName = templateContent.GetElementsByTagName("text");
					string[] array = new string[2]
					{
						message,
						coord + base.Keeper.Mobs.GetStateName(status)
					};
					for (int i = 0; i < elementsByTagName.Length; i++)
					{
						if (i < array.Length)
						{
							((IReadOnlyList<IXmlNode>)elementsByTagName)[i].AppendChild(templateContent.CreateTextNode(array[i]));
						}
					}
					ToastNotification notification = new ToastNotification(templateContent);
					ToastNotificationManager.CreateToastNotifier("Advanced Combat Tracker").Show(notification);
				}
				else
				{
					Log("您的系统不受支持。");
				}
			}
			else if ("UWP" == Keeper.Config.ToastType)
			{
				new ToastContentBuilder().AddText(message).AddText(coord + base.Keeper.Mobs.GetStateName(status)).Show();
			}
			return true;
		}
		catch
		{
			Log("Failed to push toast message " + message + ".");
			if (!ToastFailed || !checkPermit)
			{
				ToastFailed = true;
				Utils.ShowMessageBox("推送通知消息失败。您可尝试在设置中修改通知消息推送类型来尝试解决该问题。");
			}
			return false;
		}
	}

	public void SendToast(string message)
	{
		SendToast(message, HuntState.Unknown);
	}

	public void TestTTS()
	{
		ActGlobals.oFormActMain.TTS("亚以太利斯 - 艾欧泽亚 - 光之战士 (1.0，1.0) 健康");
	}

	public void TestToast()
	{
		try
		{
			SendToast("亚以太利斯 - 艾欧泽亚 - 光之战士", HuntState.Healthy, "(1.0，1.0)", checkPermit: false);
		}
		catch (Exception ex)
		{
			Log(ex.ToString());
			Log(ex.Message);
			Log("Failed to push test toast message.");
			Log("推送测试消息失败。");
			Utils.ShowMessageBox("推送测试消息失败。");
		}
	}
}
