using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Enums;
using SilverDasher.ACT.ViewModels;

namespace SilverDasher.ACT.Views;

public partial class PluginControl {
    private readonly Painter Painter;

    // public CheckTreeNode AvailableNodes;

    // public CheckTreeNode SubscriptedNodes;

    private static readonly Dictionary<PluginStatus, string> ChineseStatusText = new() {
        {
            PluginStatus.SLEEPING, "睡觉中"
        }, {
            PluginStatus.INITIALIZED, "待命"
        }, {
            PluginStatus.CONNECTING, "连线中"
        }, {
            PluginStatus.CONNECTED, "在线"
        }, {
            PluginStatus.EXPIRED, "已过期"
        }, {
            PluginStatus.BANNED, "封印中"
        }, {
            PluginStatus.BLOCKED, "迷路中"
        }, {
            PluginStatus.FAILED, "连接失败"
        }, {
            PluginStatus.LEFT, "消失了"
        }
    };

    // public bool Initing = true;

    // internal TreeView treeAvailable;
    //
    // internal Label lblStatus;
    //
    // internal TreeView treeSubscribed;
    //
    // internal Button ButtonTestTTS;
    //
    // internal CheckBox TTSHealthy;
    //
    // internal CheckBox TTSTaunted;
    //
    // internal CheckBox TTSBullying;
    //
    // internal CheckBox TTSDied;
    //
    // internal Button ButtonTestToast;
    //
    // internal CheckBox ToastHealthy;
    //
    // internal CheckBox ToastTaunted;
    //
    // internal CheckBox ToastBullying;
    //
    // internal CheckBox ToastDied;
    //
    // internal RadioButton ToastTypeUWP;
    //
    // internal RadioButton ToastTypeLegacy;
    //
    // internal CheckBox CrossWorldHunt;
    //
    // internal CheckBox CWHuntSS;
    //
    // internal CheckBox CWHuntS;
    //
    // internal CheckBox CWHuntA;
    //
    // internal CheckBox CWHuntB;
    //
    // internal CheckBox CrossDCHunt;
    //
    // internal CheckBox CDCHuntSS;
    //
    // internal CheckBox CDCHuntS;
    //
    // internal CheckBox CDCHuntA;
    //
    // internal CheckBox CDCHuntB;
    //
    // internal CheckBox CrossWorldFate;
    //
    // internal CheckBox CWFateC;
    //
    // internal CheckBox CWFateS;
    //
    // internal Label CWStatus;
    //
    // internal Button ButtonRestart;
    //
    // internal CheckBox checkExtended;
    //
    // internal TextBox textLog;
    //
    // internal Label lblStatusBottom;
    //
    // private bool _contentLoaded;

    private PluginViewModel ViewModel => DataContext as PluginViewModel;

    internal PluginControl(Painter painter) {
        Painter = painter;
        InitializeComponent();
        Init();
    }

    private void Init() {
        ViewModel.SetPatchNames(Painter.Keeper.Patches.PatchByCode);
        ViewModel.SetupHuntMobs(Painter.Keeper.Mobs.HuntByBnpcNameID, Painter.Keeper.SpHunts.HuntGroupsTree, Keeper.Config.HuntSubscriptions);
        ViewModel.SetupFates(Painter.Keeper.Fates.FateByID, Painter.Keeper.SpFates.FateGroupsTree, Keeper.Config.FateSubscriptions);
        ToastTypeLegacy.IsChecked = Keeper.Config.IsToastTypeLegacy;
        ToastTypeUWP.IsChecked = Keeper.Config.IsToastTypeUWP;
        // Initing = false;
    }

    private void checkAvailable_CheckedChanged(object sender, EventArgs e) {
        var checkBox = (CheckBox)sender;
        var array = ((string)checkBox.Tag).Split('-');
        var text = array[0];
        var text2 = array[1];
        var text3 = array[2];
        List<string> ids = null;
        switch (text) {
            case "hunt":
                switch (text2) {
                    case "all":
                        ids = Painter.Keeper.Mobs.Query();
                        break;
                    case "patch":
                        ids = Painter.Keeper.Mobs.Query(int.Parse(text3));
                        break;
                    case "rank":
                        ids = Painter.Keeper.Mobs.Query(0, (Rank)Enum.Parse(typeof(Rank), text3, true));
                        break;
                    case "map":
                        ids = Painter.Keeper.Mobs.Query(0, Rank.Unknown, int.Parse(text3));
                        break;
                    case "group":
                        ids = (from hid in Painter.Keeper.SpHunts.HuntGroupById[text3].TraverseGetItems()
                            select hid.ToString()).ToList();
                        break;
                    case "gid":
                        text3 = array[3];
                        break;
                }
                break;
            case "fate":
                switch (text2) {
                    case "all":
                        ids = Painter.Keeper.Fates.Query();
                        break;
                    case "allsp":
                        ids = Painter.Keeper.SpFates.Spfates.Select(fid => fid.ToString()).ToList();
                        break;
                    case "patch":
                        ids = Painter.Keeper.Fates.Query(int.Parse(text3));
                        break;
                    case "map":
                        ids = Painter.Keeper.Fates.Query(0, int.Parse(text3));
                        break;
                    case "group":
                        ids = (from fid in Painter.Keeper.SpFates.FateGroupById[text3].TraverseGetItems()
                            select fid.ToString()).ToList();
                        break;
                    case "gid":
                        text3 = array[3];
                        break;
                }
                break;
        }
        Painter.Messager.EditSubscription(checkBox.IsChecked, text, text3, ids, SilverDasher.Instance.tokenSource.Token);
    }

    // private void checkSubscripted_CheckedChanged(object sender, EventArgs e) {
    //     Painter.Log(sender.ToString());
    // }
    //
    // private void checkCurrentWorldOnly_CheckedChanged(object sender, EventArgs e) {
    //     Painter.Messager.Resubscribe();
    // }

    public void SetPluginStatus(PluginStatus s) {
        // if (ActGlobals.oFormActMain.InvokeRequired) {
        SilverDasher.FormContainer.Invoke(() => {
            lblStatus.Content = Enum.GetName(s.GetType(), s);
            lblStatusBottom.Content = ChineseStatusText[s];
        });
        // }
        // else {
        //     lblStatus.Content = Enum.GetName(s.GetType(), s);
        //     lblStatusBottom.Content = ChineseStatusText[s];
        // }
    }

    public void Log(string s) {
        // if (ActGlobals.oFormActMain.InvokeRequired)
        // {
        SilverDasher.FormContainer.Invoke(() => {
            textLog.AppendText(s);
            textLog.AppendText("\n");
        });
        // }
        // else
        // {
        // 	textLog.AppendText(s);
        // 	textLog.AppendText("\n");
        // }
    }

    private void CheckBoxTTSChecked(object sender, RoutedEventArgs e) {
        if (!IsInitialized) return;
        var valueOrDefault = ((CheckBox)sender).IsChecked.GetValueOrDefault();
        TTSHealthy.IsEnabled = valueOrDefault;
        TTSTaunted.IsEnabled = valueOrDefault;
        TTSBullying.IsEnabled = valueOrDefault;
        TTSDied.IsEnabled = valueOrDefault;
    }

    private void CheckBoxToastChecked(object sender, RoutedEventArgs e) {
        if (!IsInitialized) return;
        var valueOrDefault = ((CheckBox)sender).IsChecked.GetValueOrDefault();
        ToastHealthy.IsEnabled = valueOrDefault;
        ToastTaunted.IsEnabled = valueOrDefault;
        ToastBullying.IsEnabled = valueOrDefault;
        ToastDied.IsEnabled = valueOrDefault;
    }

    private void CheckBoxCWHChecked(object sender, RoutedEventArgs e) {
        if (!IsInitialized) return;
        var valueOrDefault = ((CheckBox)sender).IsChecked.GetValueOrDefault();
        CWHuntSS.IsEnabled = valueOrDefault;
        CWHuntS.IsEnabled = valueOrDefault;
        CWHuntA.IsEnabled = valueOrDefault;
        CWHuntB.IsEnabled = valueOrDefault;
    }

    private void CheckBoxCDCHChecked(object sender, RoutedEventArgs e) {
    }

    private void CheckBoxCWFChecked(object sender, RoutedEventArgs e) {
        if (!IsInitialized) return;
        var valueOrDefault = ((CheckBox)sender).IsChecked.GetValueOrDefault();
        CWFateC.IsEnabled = valueOrDefault;
        CWFateS.IsEnabled = valueOrDefault;
    }

    private void ButtonTestTTSClicked(object sender, RoutedEventArgs e) => Notifier.TestTTS();

    private void ButtonTestToastClicked(object sender, RoutedEventArgs e) => Painter.Notifier.TestToast();

    private void ButtonRestartClicked(object sender, RoutedEventArgs e) => Painter.Self.RestartLoop(true);

    private void RadioButtonToastTypeChecked(object sender, RoutedEventArgs e) {
        var obj = (RadioButton)sender;
        var valueOrDefault = obj.IsChecked.GetValueOrDefault();
        var text = obj.Name.Replace("ToastType", "");
        if (text != Keeper.Config.ToastType && valueOrDefault)
            Keeper.Config.ToastType = text;
    }

    private void CheckBoxCrossWorld(object sender, RoutedEventArgs e) {
        if (!IsInitialized) return;
        Painter.Messager.Resubscribe((string)((CheckBox)sender).Tag, SilverDasher.Instance.tokenSource.Token);
    }

    private void CheckBoxCrossDC(object sender, RoutedEventArgs e) {
        // _ = IsInitialized;
    }

    internal void ButtonRestartToggle(string content, bool enabled) {
        Dispatcher.Invoke(() => {
            ButtonRestart.Content = content;
            ButtonRestart.IsEnabled = enabled;
        });
    }

    internal void CheckBoxCrossWorldToggle(bool enabled) {
        if (IsInitialized) {
            Dispatcher.Invoke(() => {
                CrossWorldFate.IsEnabled = enabled;
                CWFateC.IsEnabled = enabled;
                CWFateS.IsEnabled = enabled;
                CrossWorldHunt.IsEnabled = enabled;
                CWHuntSS.IsEnabled = enabled;
                CWHuntS.IsEnabled = enabled;
                CWHuntA.IsEnabled = enabled;
                CWHuntB.IsEnabled = enabled;
                CWStatus.Visibility = enabled ? Visibility.Hidden : Visibility.Visible;
            });
        }
    }

    private void ButtonClearLogClicked(object sender, RoutedEventArgs e) => SilverDasher.FormContainer.Invoke(textLog.Clear);
}