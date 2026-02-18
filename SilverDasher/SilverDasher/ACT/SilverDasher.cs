#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Views;

namespace SilverDasher.ACT;

public class SilverDasher : IActPluginV1 {
    internal Painter Painter;

    internal Negotiator Negotiator;

    internal Notifier Notifier;

    internal Overseer Overseer;

    internal Messager Messager;

    internal Agent Agent;

    internal Keeper Keeper;

    internal Primal Primal;

    internal Logger Logger;

    internal Task Feed;

    internal CancellationTokenSource tokenSource = new();

    internal bool badWorldMessageShown;

    private readonly List<Doppelganger> Doppelgangers = [];
    public static Form FormContainer;

    public SilverDasher() {
        foreach (var actPlugin in ActGlobals.oFormActMain.ActPlugins.Where(actPlugin => actPlugin.pluginFile.Name == "SilverDasher.dll"))
            FormContainer = actPlugin.PluginForm;
    }

    public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
        SummonDoppelgangers(pluginScreenSpace, pluginStatusText);
        StartLoop();
        Painter.SetPluginStatus("召唤出了银山雀儿。(Initialized)");
    }

    public void DeInitPlugin() {
        DismissDoppelgangers();
    }

    private void SummonDoppelgangers(TabPage pluginScreenSpace, Label pluginStatusText) {
        Logger = new Logger(this);
        Keeper = new Keeper(this);
        Painter = new Painter(this, pluginStatusText, pluginScreenSpace);
        Negotiator = new Negotiator(this);
        Overseer = new Overseer(this);
        Notifier = new Notifier(this);
        Agent = new Agent(this);
        Messager = new Messager(this);
        Primal = new Primal(this);
        Doppelgangers.Add(Logger);
        Doppelgangers.Add(Agent);
        Doppelgangers.Add(Keeper);
        Doppelgangers.Add(Painter);
        Doppelgangers.Add(Negotiator);
        Doppelgangers.Add(Overseer);
        Doppelgangers.Add(Notifier);
        Doppelgangers.Add(Messager);
        Doppelgangers.Add(Primal);
        foreach (Doppelganger doppelganger in Doppelgangers) {
            doppelganger.Init();
        }
        Resignal();
    }

    private void DismissDoppelgangers() {
        foreach (Doppelganger doppelganger in Doppelgangers) {
            doppelganger.Deinit();
        }
        tokenSource.Cancel();
    }

    internal void Resignal() {
        Feed = Task.Run(async delegate {
            while (true) {
                await Task.Delay(TimeSpan.FromSeconds(28800.0));
                RestartLoop();
            }
        }, tokenSource.Token);
    }

    internal void StartLoop(bool refresh = false) {
        badWorldMessageShown = false;
        Task.Run(async delegate {
            await Agent.UpdateData(refresh);
            while (Keeper.RUNNING) {
                try {
                    if (Keeper.PlayerWorldID == 0) {
                        Negotiator.GetPlayerInfo(0u, "");
                    }
                    if (Keeper.PlayerWorldID != 0 && Keeper.CurrentWorldID != 0 && Keeper.PlayerName != "") {
                        Auth();
                    }
                    else {
                        Logger.Log("Game isn't running. Retrying in 5 seconds.");
                    }
                }
                catch (Exception ex) {
                    Trace.WriteLine(ex.ToString());
                    Logger.Log(ex.StackTrace);
                }
                finally {
                    await Task.Delay(TimeSpan.FromSeconds(5.0));
                }
            }
        });
    }

    internal void Auth() {
        Task.Run(async delegate {
            AuthResult r = AuthResult.ERROR;
            while (r == AuthResult.ERROR && Keeper.RUNNING) {
                try {
                    r = await Agent.Setup();
                    Painter.pluginControl.SetPluginStatus(PluginStatus.CONNECTING);
                    if (r != 0) {
                        Logger.Log("Authentication Failed! Status " + Enum.GetName(r.GetType(), r) + ".");
                        switch (r) {
                            case AuthResult.BLOCKED:
                                Logger.Log("你的银山雀儿似乎不是很愿意飞离你，还想在你肩上多睡一会儿。\n（您暂时无法使用银山雀儿提供的服务。Your location has been blocked temporarily.）");
                                Utils.ShowMessageBox("你的银山雀儿似乎不是很愿意飞离你，还想在你肩上多睡一会儿。\n（您暂时无法使用银山雀儿提供的服务。您所在网段可能被其他人用于不正当用途，请稍后再试。Your location has been blocked temporarily.）");
                                Painter.pluginControl.SetPluginStatus(PluginStatus.BANNED);
                                break;
                            case AuthResult.BANNED:
                                Logger.Log("你的银山雀儿叼着一封拒签信回来了。\n（由于您的不当行为，您暂时无法使用银山雀儿提供的服务。Your account has been banned temporarily.）");
                                Utils.ShowMessageBox("你的银山雀儿叼着一封拒签信回来了。\n（由于您的不当行为，您暂时无法使用银山雀儿提供的服务。Your account has been banned temporarily.）");
                                Painter.pluginControl.SetPluginStatus(PluginStatus.BLOCKED);
                                break;
                            case AuthResult.LEFT:
                                Logger.Log("你的银山雀儿飞走了。没有再回来。\n（您已被封禁。You have been banned permenantly.）");
                                Utils.ShowMessageBox("你的银山雀儿飞走了。没有再回来。\n（您已被封禁。You have been banned permenantly.）");
                                Painter.pluginControl.SetPluginStatus(PluginStatus.LEFT);
                                break;
                            case AuthResult.EXPIRED:
                                Logger.Log("你的银山雀儿没有吃饱饭，不想动弹。\n（你的插件版本已过期，请更新后使用。Your plugin has expired, please update.）");
                                Utils.ShowMessageBox("你的银山雀儿没有吃饱饭，不想动弹。\n你的银山雀儿插件版本已过期，请更新后使用。\nYour plugin has expired, please update.");
                                Painter.pluginControl.SetPluginStatus(PluginStatus.EXPIRED);
                                break;
                            case AuthResult.ERROR:
                                Logger.Log("你的银山雀儿没能找到大树的方向。\n（网络连接失败。将在10秒后自动重试。Network error. Retrying in 10 seconds.）");
                                Painter.pluginControl.SetPluginStatus(PluginStatus.FAILED);
                                break;
                            case AuthResult.FAIL:
                                Logger.Log("你的银山雀儿似乎还没有睡醒。再让它睡一会儿叭~\n（意外的错误。请联系插件维护组取得帮助。Unknown error. Contact maintainer to get some help.）");
                                Painter.pluginControl.SetPluginStatus(PluginStatus.FAILED);
                                break;
                        }
                    }
                    else {
                        Painter.pluginControl.SetPluginStatus(PluginStatus.CONNECTING);
                        Messager.StartLoop();
                    }
                }
                catch (Exception ex) {
                    Trace.WriteLine(ex.ToString());
                    Logger.Log(ex.StackTrace);
                }
                finally {
                    await Task.Delay(TimeSpan.FromSeconds(15.0));
                }
            }
        });
    }

    internal void RestartLoop(bool refresh = false) {
        Painter.pluginControl.ButtonRestartToggle("请等待1分钟左右……", enabled: false);
        Painter.pluginControl.CheckBoxCrossWorldToggle(enabled: false);
        if (Messager.IsRunning()) {
            Messager.Disconnect();
        }
        Agent.session = "";
        Keeper.RUNNING = false;
        Painter.pluginControl.SetPluginStatus(PluginStatus.SLEEPING);
        Task.Run(async delegate {
            await Task.Delay(TimeSpan.FromSeconds(30.0));
            Keeper.RUNNING = true;
            StartLoop(refresh);
            Painter.pluginControl.ButtonRestartToggle("刷新配置并重新连接", enabled: true);
            Painter.pluginControl.CheckBoxCrossWorldToggle(enabled: true);
        });
    }
}