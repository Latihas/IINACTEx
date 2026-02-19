using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Dalamud.Plugin.Services;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Storages;
using SilverDasher.ACT.Views;

namespace SilverDasher.ACT;

public class SilverDasher : IActPluginV1 {
    internal static SilverDasher Instance;
    internal Painter Painter;

    internal Negotiator Negotiator;

    internal Notifier Notifier;

    internal Overseer Overseer;

    internal Messager Messager;

    internal Agent Agent;

    internal Keeper Keeper;

    internal Primal Primal;

    internal Logger Logger;

    // internal Task Feed;

    private CancellationTokenSource tokenSource = new();

    internal bool badWorldMessageShown;
    internal static IClientState ClientState;
    private static IFramework Framework;
    private readonly List<Doppelganger> Doppelgangers = [];
    internal static Form FormContainer;
    internal static string Datadir;

    public SilverDasher(string datadir, IClientState clientState, IFramework framework) {
        Datadir = datadir;
        ClientState = clientState;
        Framework = framework;
        Instance = this;
    }

    public static readonly List<string> FileLogs = [];

    private static void WriteLog(IFramework _) {
        if (!Directory.Exists(DataStorage.LogPath)) Directory.CreateDirectory(DataStorage.LogPath);
        lock (FileLogs) {
            File.AppendAllText(DataStorage.LogFile, string.Join("\n", FileLogs) + '\n');
            FileLogs.Clear();
        }
    }

    public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
        foreach (var actPlugin in ActGlobals.oFormActMain.ActPlugins.Where(actPlugin => actPlugin.pluginFile.Name == "SilverDasher.dll"))
            FormContainer = actPlugin.PluginForm;
        SummonDoppelgangers(pluginScreenSpace, pluginStatusText);
        StartLoop(tokenSource.Token);
        Painter.SetPluginStatus("召唤出了银山雀儿。(Initialized)");
        Framework.Update += WriteLog;
    }

    public void DeInitPlugin() {
        DismissDoppelgangers();
        Framework.Update -= WriteLog;
    }


    private void SummonDoppelgangers(TabPage pluginScreenSpace, Label pluginStatusText) {
        Doppelgangers.Add(Logger = new Logger(this));
        Doppelgangers.Add(Agent = new Agent(this));
        Doppelgangers.Add(Keeper = new Keeper(this));
        Doppelgangers.Add(Painter = new Painter(this, pluginStatusText, pluginScreenSpace));
        Doppelgangers.Add(Negotiator = new Negotiator(this));
        Doppelgangers.Add(Overseer = new Overseer(this));
        Doppelgangers.Add(Notifier = new Notifier(this));
        Doppelgangers.Add(Messager = new Messager(this));
        Doppelgangers.Add(Primal = new Primal(this));
        foreach (var doppelganger in Doppelgangers) doppelganger.Init();
        Resignal(tokenSource.Token);
    }

    private void DismissDoppelgangers() {
        tokenSource.Cancel();
        tokenSource.Dispose();
        tokenSource = null;
        foreach (var doppelganger in Doppelgangers) doppelganger.Deinit();
    }

    private void Resignal(CancellationToken token) {
        _ = Task.Run(async () => {
            while (!token.IsCancellationRequested) {
                await Task.Delay(TimeSpan.FromSeconds(28800.0), token);
                RestartLoop();
            }
        }, token);
    }

    private void StartLoop(CancellationToken token, bool refresh = false) {
        badWorldMessageShown = false;
        Task.Run(async () => {
            await Agent.UpdateData(refresh, token);
            while (!token.IsCancellationRequested && Keeper.RUNNING) {
                try {
                    if (Keeper.PlayerWorldID == 0) Negotiator.GetPlayerInfo(0u, "");
                    if (Keeper.PlayerWorldID != 0 && Keeper.CurrentWorldID != 0 && Keeper.PlayerName != "") await Auth(token);
                    // else {
                    //     Logger.Log("Game isn't running. Retrying in 5 seconds.");
                    // }
                }
                catch (Exception ex) {
                    Trace.WriteLine(ex.ToString());
                    Logger.Log(ex.StackTrace);
                }
                finally {
                    await Task.Delay(TimeSpan.FromSeconds(5.0), token);
                }
            }
        }, token);
    }

    private Task Auth(CancellationToken token) {
        return Task.Run(async () => {
            var r = AuthResult.ERROR;
            while (!token.IsCancellationRequested && r == AuthResult.ERROR && Keeper.RUNNING) {
                try {
                    r = await Agent.Setup(token);
                    Painter.pluginControl.SetPluginStatus(PluginStatus.CONNECTING);
                    if (r == AuthResult.SUCCESS) {
                        Painter.pluginControl.SetPluginStatus(PluginStatus.CONNECTED);
                        Messager.StartLoop();
                    }
                    else {
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
                }
                catch (Exception ex) {
                    Trace.WriteLine(ex.ToString());
                    Logger.Log(ex.StackTrace);
                }
                finally {
                    await Task.Delay(TimeSpan.FromSeconds(15.0), token);
                }
            }
        }, token);
    }

    internal void RestartLoop(bool refresh = false) {
        Painter.pluginControl.ButtonRestartToggle("请等待1分钟左右……", false);
        Painter.pluginControl.CheckBoxCrossWorldToggle(false);
        if (Messager.IsRunning()) Messager.Disconnect();
        Agent.session = "";
        Keeper.RUNNING = false;
        Painter.pluginControl.SetPluginStatus(PluginStatus.SLEEPING);
        var token = tokenSource.Token;
        Task.Run(async () => {
            await Task.Delay(TimeSpan.FromSeconds(30.0), token);
            Keeper.RUNNING = true;
            StartLoop(token, refresh);
            Painter.pluginControl.ButtonRestartToggle("刷新配置并重新连接", true);
            Painter.pluginControl.CheckBoxCrossWorldToggle(true);
        }, token);
    }
}