using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using Newtonsoft.Json;
using SilverDasher.ACT.Models.Messages;
using SilverDasher.ACT.Storages;
using SilverDasher.ACT.Views;

namespace SilverDasher.ACT.Doppelgangers;

internal class Messager(SilverDasher plugin) : Doppelganger(plugin) {
    private IMqttClient Tomestone;

    private readonly ConcurrentQueue<Message> Messages = new();

    private readonly List<(string, string)> Subscriptions = [];

    private readonly Dictionary<(string, string), string> SubDict = new();

    internal override void Init() {
        Tomestone = new MqttFactory().CreateMqttClient();
        foreach (var huntSubscription in Keeper.Config.HuntSubscriptions) Subscriptions.Add(("hunt", huntSubscription.ToString()));
        foreach (var fateSubscription in Keeper.Config.FateSubscriptions) Subscriptions.Add(("fate", fateSubscription.ToString()));
    }

    internal override void Deinit() {
        if (Tomestone == null) return;
        if (Tomestone.IsConnected) Tomestone.DisconnectAsync().Wait(); //++
        Tomestone.Dispose();
        Painter.pluginControl.SetPluginStatus(PluginStatus.INITIALIZED);
    }

    public void StartLoop() {
        if (string.IsNullOrEmpty(Agent.session)) {
            Log("You should not launch before authentication successes.");
            return;
        }
        var tomestoneConfig = new MqttClientOptionsBuilder().WithCredentials(Tailor.Judge() + Convert.ToBase64String(Encoding.UTF8.GetBytes(Keeper.PlayerName)) + Keeper.PlayerWorldID, Tailor.Seal(Agent.session, Keeper.PlayerName + Keeper.PlayerWorld))
            .WithTls(delegate(MqttClientOptionsBuilderTlsParameters o) { o.SslProtocol = SslProtocols.Tls12; }).WithWebSocketServer(DataStorage.SilverDasherTree)
            .Build();
        Tomestone.ApplicationMessageReceivedAsync += Unpack;
        Task.Run(async delegate {
            var i = 0;
            while (Keeper.RUNNING) {
                try {
                    _ = 2;
                    try {
                        if (!Tomestone.IsConnected) {
                            Painter.pluginControl.SetPluginStatus(PluginStatus.CONNECTING);
                            await Tomestone.ConnectAsync(tomestoneConfig);
                            Painter.pluginControl.SetPluginStatus(PluginStatus.CONNECTED);
                            await Subscribe(Subscriptions);
                        }
                        Negotiator.GetPlayerInfo(0u, "");
                        Negotiator.ScanMobs();
                        if (i == 1) {
                            Keeper.ReportFateStatus();
                            Keeper.CurrentFates.Clear();
                        }
                        Keeper.ReportMobStatus();
                        while (Messages.TryDequeue(out var result)) {
                            await Tomestone.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("upload/u/" + Agent.session).WithPayload(Tailor.Weave(JsonConvert.SerializeObject(result), Agent.session)).Build());
                        }
                    }
                    catch (Exception ex) {
                        Log(ex.ToString());
                        Log(ex.StackTrace);
                    }
                }
                finally {
                    await Task.Delay(TimeSpan.FromSeconds(6.0));
                    if (++i > 3) i = 0;
                }
            }
        });
    }

    private Task Subscribe(List<(string, string)> subscriptions) {
        return Task.Run(async delegate {
            if (subscriptions.Count != 0) {
                List<List<string>> list = [];
                List<string> list2 = [];
                list.Add(list2);
                var num = 0;
                foreach (var subscription in subscriptions) {
                    if (num > 40) {
                        list2 = [];
                        list.Add(list2);
                        num = 0;
                    }
                    var text = Keeper.GetCurrentWorld().Label;
                    var (text2, _) = subscription;
                    switch (text2) {
                        case "fate": {
                            if (Keeper.Fates.TryGet(int.Parse(subscription.Item2), out var item) &&
                                (Keeper.Territories.TryGet((int)item.TerritoryID, out var item2) && item2.IsDataCenterMap ||
                                 Keeper.Config.CrossWorldFate && (item.Special && Keeper.Config.CWFateSpecial || !item.Special && Keeper.Config.CWFateCommon)))
                                text = "+";
                            break;
                        }
                        case "hunt" when Keeper.Config.CrossWorldHunt && Keeper.Mobs.TryGet(int.Parse(subscription.Item2), out var item3) && Keeper.Config.CWHunts[item3.Rank]:
                            text = "+";
                            break;
                    }
                    var text3 = Keeper.GetCurrentWorld().DataCenterLabel + "/" + text + "/" + subscription.Item1 + "/" + subscription.Item2;
                    Debug("Adding " + text3 + " to subscription.");
                    SubDict[subscription] = text3;
                    list2.Add(text3);
                    num++;
                }
                foreach (var item5 in list) {
                    var mqttClientSubscribeOptions = new MqttClientSubscribeOptions();
                    foreach (var item4 in item5.Select(item6 => new MqttTopicFilter {
                                 Topic = item6
                             }))
                        mqttClientSubscribeOptions.TopicFilters.Add(item4);
                    var mqttClientSubscribeResult = await Tomestone.SubscribeAsync(mqttClientSubscribeOptions);
                    Log(mqttClientSubscribeResult.ReasonString ?? "Subscription success.");
                }
            }
        });
    }

    // internal Task UnsubscribeAll() {
    //     return Task.Run(async delegate {
    //         var options = new MqttClientUnsubscribeOptions {
    //             TopicFilters = {
    //                 "#"
    //             }
    //         };
    //         Log((await Tomestone.UnsubscribeAsync(options)).ReasonString);
    //     });
    // }

    private Task Unsubscribe(List<(string, string)> unsubscriptions) {
        return Task.Run(async delegate {
            List<List<string>> list = [];
            List<string> list2 = [];
            list.Add(list2);
            var num = 0;
            foreach (var unsubscription in unsubscriptions) {
                if (num > 20) {
                    list2 = [];
                    list.Add(list2);
                    num = 0;
                }
                _ = Keeper.GetCurrentWorld().Label;
                SubDict.TryGetValue(unsubscription, out var value);
                var text = Keeper.GetCurrentWorld().DataCenterLabel + "/+/" + unsubscription.Item1 + "/" + unsubscription.Item2;
                value ??= Keeper.GetCurrentWorld().DataCenterLabel + "/" + Keeper.GetCurrentWorld().Label + "/" + unsubscription.Item1 + "/" + unsubscription.Item2;
                list2.Add(value);
                list2.Add(text);
                Debug("Unsubscripting " + value + ".");
                Debug("Unsubscripting " + text + ".");
                num++;
            }
            foreach (var item in list) {
                var mqttClientUnsubscribeOptions = new MqttClientUnsubscribeOptions();
                foreach (var item2 in item)
                    mqttClientUnsubscribeOptions.TopicFilters.Add(item2);
                var mqttClientUnsubscribeResult = await Tomestone.UnsubscribeAsync(mqttClientUnsubscribeOptions);
                Log(mqttClientUnsubscribeResult.ReasonString ?? "Unsubscription success.");
            }
        });
    }

    public bool IsRunning() => Tomestone.IsConnected;

    public void Disconnect() {
        if (Tomestone == null) return;
        if (Tomestone.IsConnected) Tomestone.DisconnectAsync();
        Tomestone.ApplicationMessageReceivedAsync -= Unpack;
        Painter.pluginControl.SetPluginStatus(PluginStatus.INITIALIZED);
    }

    public void QueueMessage(Message message) {
        Messages.Enqueue(message);
    }

    private void AddSubscription(string type, string id) {
        Subscriptions.Add((type, id));
        Subscribe([(type, id)]);
    }

    private void AddSubscriptions(string type, List<string> ids) {
        List<(string, string)> list = [];
        foreach (var id in ids) {
            Subscriptions.Add((type, id));
            list.Add((type, id));
        }
        Subscribe(list);
    }

    private void RemoveSubscription(string type, string id) {
        Subscriptions.Remove((type, id));
        Unsubscribe([(type, id)]);
    }

    private void RemoveSubscriptions(string type, List<string> ids) {
        List<(string, string)> list = [];
        foreach (var id in ids) {
            Subscriptions.Remove((type, id));
            list.Add((type, id));
        }
        Unsubscribe(list);
    }

    internal void Resubscribe(string tag = "") {
        if (Tomestone == null) return;
        Painter?.pluginControl?.CheckBoxCrossWorldToggle(false);
        var filteredSub = tag == "" ? Subscriptions : Subscriptions.FindAll(e => e.Item1 == tag);
        Task.Run(async delegate {
            await Unsubscribe(filteredSub);
            await Subscribe(filteredSub);
            Painter?.pluginControl?.CheckBoxCrossWorldToggle(true);
        });
    }

    public void EditSubscription(bool? edit, string type, string id, List<string> ids) {
        if (edit.GetValueOrDefault()) {
            if (ids != null) {
                AddSubscriptions(type, ids);
            }
            else {
                AddSubscription(type, id);
            }
        }
        else if (ids != null) {
            RemoveSubscriptions(type, ids);
        }
        else {
            RemoveSubscription(type, id);
        }
        Keeper.Config.EditSubscription(edit, type, id, ids);
    }

    private async Task Unpack(MqttApplicationMessageReceivedEventArgs e) {
        var topic = e.ApplicationMessage.Topic;
        var @string = new UTF8Encoding().GetString(e.ApplicationMessage.PayloadSegment);
        if (Keeper.Config.ExtendedReport) {
            Log("Received " + topic);
            Log(@string);
        }
        try {
            Notifier.Unpack(topic, @string);
        }
        catch (Exception ex) {
            Log("Failed to parse message " + @string + " from topic " + e.ApplicationMessage.Topic + ".");
            Log($"{ex}");
            Log(ex.Message);
            Log(ex.StackTrace ?? "");
        }
        await e.AcknowledgeAsync(CancellationToken.None);
    }
}