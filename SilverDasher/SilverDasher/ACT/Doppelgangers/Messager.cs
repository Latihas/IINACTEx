using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using Newtonsoft.Json;
using SilverDasher.ACT.Models;
using SilverDasher.ACT.Models.Messages;
using SilverDasher.ACT.Storages;
using SilverDasher.ACT.Views;

namespace SilverDasher.ACT.Doppelgangers;

internal class Messager : Doppelganger
{
	private IMqttClient Tomestone;

	private ConcurrentQueue<Message> Messages = new();

	private List<(string, string)> Subscriptions = [];

	private Dictionary<(string, string), string> SubDict = new();

	internal Messager(SilverDasher plugin)
		: base(plugin)
	{
	}

	internal override void Init()
	{
		Tomestone = new MqttFactory().CreateMqttClient();
		foreach (int huntSubscription in Keeper.Config.HuntSubscriptions)
		{
			Subscriptions.Add(("hunt", huntSubscription.ToString()));
		}
		foreach (int fateSubscription in Keeper.Config.FateSubscriptions)
		{
			Subscriptions.Add(("fate", fateSubscription.ToString()));
		}
	}

	internal override void Deinit()
	{
		if (Tomestone != null)
		{
			if (Tomestone.IsConnected)
			{
				Tomestone.DisconnectAsync();
			}
			Tomestone.Dispose();
			Painter.pluginControl.SetPluginStatus(PluginStatus.INITIALIZED);
		}
	}

	public void StartLoop()
	{
		if (string.IsNullOrEmpty(Agent.session))
		{
			Log("You should not launch before authentication successes.");
			return;
		}
		MqttClientOptions tomestoneConfig = new MqttClientOptionsBuilder().WithCredentials(Tailor.Judge() + Convert.ToBase64String(Encoding.UTF8.GetBytes(Keeper.PlayerName)) + Keeper.PlayerWorldID, Tailor.Seal(Agent.session, Keeper.PlayerName + Keeper.PlayerWorld)).WithTls(delegate(MqttClientOptionsBuilderTlsParameters o)
		{
			o.SslProtocol = SslProtocols.Tls12;
		}).WithWebSocketServer(DataStorage.SilverDasherTree)
			.Build();
		Tomestone.ApplicationMessageReceivedAsync += Unpack;
		Task.Run(async delegate
		{
			int i = 0;
			while (Keeper.RUNNING)
			{
				try
				{
					_ = 2;
					try
					{
						if (!Tomestone.IsConnected)
						{
							Painter.pluginControl.SetPluginStatus(PluginStatus.CONNECTING);
							await Tomestone.ConnectAsync(tomestoneConfig);
							Painter.pluginControl.SetPluginStatus(PluginStatus.CONNECTED);
							await Subscribe(Subscriptions);
						}
						Negotiator.GetPlayerInfo(0u, "");
						Negotiator.ScanMobs();
						if (i == 1)
						{
							Keeper.ReportFateStatus();
							Keeper.CurrentFates.Clear();
						}
						Keeper.ReportMobStatus();
						Message result;
						while (Messages.TryDequeue(out result))
						{
							await Tomestone.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("upload/u/" + Agent.session).WithPayload(Tailor.Weave(JsonConvert.SerializeObject(result), Agent.session)).Build());
						}
					}
					catch (Exception ex)
					{
						Log(ex.ToString());
						Log(ex.StackTrace);
					}
				}
				finally
				{
					await Task.Delay(TimeSpan.FromSeconds(6.0));
					i++;
					if (i > 3)
					{
						i = 0;
					}
				}
			}
		});
	}

	public Task Subscribe(List<(string, string)> subscriptions)
	{
		return Task.Run(async delegate
		{
			if (subscriptions.Count != 0)
			{
				List<List<string>> list = [];
				List<string> list2 = [];
				list.Add(list2);
				int num = 0;
				foreach (var subscription in subscriptions)
				{
					if (num > 40)
					{
						list2 = [];
						list.Add(list2);
						num = 0;
					}
					string text = Keeper.GetCurrentWorld().Label;
					var (text2, _) = subscription;
					HuntMob item3;
					if (text2 == "fate")
					{
						if (Keeper.Fates.TryGet(int.Parse(subscription.Item2), out var item))
						{
							if (Keeper.Territories.TryGet((int)item.TerritoryID, out var item2) && item2.IsDataCenterMap)
							{
								text = "+";
							}
							if (Keeper.Config.CrossWorldFate && ((item.Special && Keeper.Config.CWFateSpecial) || (!item.Special && Keeper.Config.CWFateCommon)))
							{
								text = "+";
							}
						}
					}
					else if (text2 == "hunt" && Keeper.Config.CrossWorldHunt && Keeper.Mobs.TryGet(int.Parse(subscription.Item2), out item3) && Keeper.Config.CWHunts[item3.Rank])
					{
						text = "+";
					}
					string text3 = Keeper.GetCurrentWorld().DataCenterLabel + "/" + text + "/" + subscription.Item1 + "/" + subscription.Item2;
					Debug("Adding " + text3 + " to subscription.");
					SubDict[subscription] = text3;
					list2.Add(text3);
					num++;
				}
				foreach (List<string> item5 in list)
				{
					MqttClientSubscribeOptions mqttClientSubscribeOptions = new MqttClientSubscribeOptions();
					foreach (string item6 in item5)
					{
						MqttTopicFilter item4 = new MqttTopicFilter
						{
							Topic = item6
						};
						mqttClientSubscribeOptions.TopicFilters.Add(item4);
					}
					MqttClientSubscribeResult mqttClientSubscribeResult = await Tomestone.SubscribeAsync(mqttClientSubscribeOptions);
					Log((mqttClientSubscribeResult.ReasonString != null) ? mqttClientSubscribeResult.ReasonString : "Subscription success.");
				}
			}
		});
	}

	internal Task UnsubscribeAll()
	{
		return Task.Run(async delegate
		{
			MqttClientUnsubscribeOptions options = new MqttClientUnsubscribeOptions
			{
				TopicFilters = { "#" }
			};
			Log((await Tomestone.UnsubscribeAsync(options)).ReasonString);
		});
	}

	public Task Unsubscribe(List<(string, string)> unsubscriptions)
	{
		return Task.Run(async delegate
		{
			List<List<string>> list = [];
			List<string> list2 = [];
			list.Add(list2);
			int num = 0;
			foreach (var unsubscription in unsubscriptions)
			{
				if (num > 20)
				{
					list2 = [];
					list.Add(list2);
					num = 0;
				}
				_ = Keeper.GetCurrentWorld().Label;
				SubDict.TryGetValue(unsubscription, out var value);
				string text = Keeper.GetCurrentWorld().DataCenterLabel + "/+/" + unsubscription.Item1 + "/" + unsubscription.Item2;
				if (value == null)
				{
					value = Keeper.GetCurrentWorld().DataCenterLabel + "/" + Keeper.GetCurrentWorld().Label + "/" + unsubscription.Item1 + "/" + unsubscription.Item2;
				}
				list2.Add(value);
				list2.Add(text);
				Debug("Unsubscripting " + value + ".");
				Debug("Unsubscripting " + text + ".");
				num++;
			}
			foreach (List<string> item in list)
			{
				MqttClientUnsubscribeOptions mqttClientUnsubscribeOptions = new MqttClientUnsubscribeOptions();
				foreach (string item2 in item)
				{
					mqttClientUnsubscribeOptions.TopicFilters.Add(item2);
				}
				MqttClientUnsubscribeResult mqttClientUnsubscribeResult = await Tomestone.UnsubscribeAsync(mqttClientUnsubscribeOptions);
				Log((mqttClientUnsubscribeResult.ReasonString != null) ? mqttClientUnsubscribeResult.ReasonString : "Unsubscription success.");
			}
		});
	}

	public bool IsRunning()
	{
		return Tomestone.IsConnected;
	}

	public void Disconnect()
	{
		if (Tomestone != null)
		{
			if (Tomestone.IsConnected)
			{
				Tomestone.DisconnectAsync();
			}
			Tomestone.ApplicationMessageReceivedAsync -= Unpack;
			Painter.pluginControl.SetPluginStatus(PluginStatus.INITIALIZED);
		}
	}

	public void QueueMessage(Message message)
	{
		Messages.Enqueue(message);
	}

	public void AddSubscription(string type, string id)
	{
		Subscriptions.Add((type, id));
		Subscribe([(type, id)]);
	}

	public void AddSubscriptions(string type, List<string> ids)
	{
		List<(string, string)> list = [];
		foreach (string id in ids)
		{
			Subscriptions.Add((type, id));
			list.Add((type, id));
		}
		Subscribe(list);
	}

	public void RemoveSubscription(string type, string id)
	{
		Subscriptions.Remove((type, id));
		Unsubscribe([(type, id)]);
	}

	public void RemoveSubscriptions(string type, List<string> ids)
	{
		List<(string, string)> list = [];
		foreach (string id in ids)
		{
			Subscriptions.Remove((type, id));
			list.Add((type, id));
		}
		Unsubscribe(list);
	}

	internal void Resubscribe(string tag = "")
	{
		if (Tomestone == null)
		{
			return;
		}
		Painter?.pluginControl?.CheckBoxCrossWorldToggle(enabled: false);
		List<(string, string)> filteredSub = [];
		if (tag == "")
		{
			filteredSub = Subscriptions;
		}
		else
		{
			filteredSub = Subscriptions.FindAll(((string, string) e) => e.Item1 == tag);
		}
		Task.Run(async delegate
		{
			await Unsubscribe(filteredSub);
			await Subscribe(filteredSub);
			Painter?.pluginControl?.CheckBoxCrossWorldToggle(enabled: true);
		});
	}

	public void EditSubscription(bool? edit, string type, string id, List<string> ids)
	{
		if (edit.GetValueOrDefault())
		{
			if (ids != null)
			{
				AddSubscriptions(type, ids);
			}
			else
			{
				AddSubscription(type, id);
			}
		}
		else if (ids != null)
		{
			RemoveSubscriptions(type, ids);
		}
		else
		{
			RemoveSubscription(type, id);
		}
		Keeper.Config.EditSubscription(edit, type, id, ids);
	}

	public async Task Unpack(MqttApplicationMessageReceivedEventArgs e)
	{
		string topic = e.ApplicationMessage.Topic;
		string @string = new UTF8Encoding().GetString(e.ApplicationMessage.Payload);
		if (Keeper.Config.ExtendedReport)
		{
			Log("Received " + topic);
			Log(@string);
		}
		try
		{
			Notifier.Unpack(topic, @string);
		}
		catch (Exception ex)
		{
			Log("Failed to parse message " + @string + " from topic " + e.ApplicationMessage.Topic + ".");
			Log($"{ex}");
			Log(ex.Message ?? "");
			Log(ex.StackTrace ?? "");
		}
		await e.AcknowledgeAsync(CancellationToken.None);
	}
}
