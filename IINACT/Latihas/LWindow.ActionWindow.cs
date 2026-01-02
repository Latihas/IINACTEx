using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Triggernometry.Core;
using Triggernometry.Localization;
using static Triggernometry.Core.ActionOld;

namespace IINACT.Latihas;

public partial class LWindow {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ActionWindow() : Window($"{WindowPrefix}ActionWindow") {
        internal static ActionOld? Action;
        internal static Trigger? Trigger;
        internal static ConditionPanel ConditionPanel;

        internal void Open(Trigger trigger, ActionOld action) {
            Trigger = trigger;
            Action = action;
            ConditionPanel = new ConditionPanel(action);
            Plugin.Instance.ActionWindow.IsOpen = true;
        }

        public void DeleteAction(Trigger trigger, ActionOld action) {
            trigger.Actions.Remove(action);
            for (var i = 0; i < trigger.Actions.Count; i++)
                trigger.Actions[i].OrderNumber = i + 1;
            if (Action == action) {
                Trigger = null;
                Action = null;
                Plugin.Instance.ActionWindow.IsOpen = false;
            }
        }

        public override void Draw() {
            if (Action == null) return;
            ImGui.Text("Id: ");
            ImGui.SameLine();
            var id = Action.Id.ToString();
            if (ImGui.Button(id)) ImGui.SetClipboardText(id);
            if (ImGui.Button("删除")) DeleteAction(Trigger!, Action);
            var currentActionTypeEnumItem = 0;
            currentActionTypeEnumItem = (int)Action.ActionType;
            if (ImGui.Combo("动作类型", ref currentActionTypeEnumItem,
                    Enum.GetValues<ActionTypeEnum>()
                        .Select(i => i.ToString())
                        .ToList()))
                Action.ActionType = (ActionTypeEnum)currentActionTypeEnumItem;
            if (ImGui.BeginTabBar("## EActionTabBar")) {
                if (ImGui.BeginTabItem("特定动作设置")) {
                    switch (Action.ActionType) {
                        case ActionTypeEnum.SystemBeep: {
                            var SystemBeepFreqExpression = Action.SystemBeepFreqExpression;
                            if (ImGui.InputText("频率", ref SystemBeepFreqExpression))
                                Action.SystemBeepFreqExpression = SystemBeepFreqExpression;
                            var SystemBeepLengthExpression = Action.SystemBeepLengthExpression;
                            if (ImGui.InputText("时长", ref SystemBeepLengthExpression))
                                Action.SystemBeepLengthExpression = SystemBeepLengthExpression;
                            break;
                        }
                        case ActionTypeEnum.PlaySound: {
                            var PlaySoundFileExpression = Action.PlaySoundFileExpression;
                            if (ImGui.InputText("音频路径", ref PlaySoundFileExpression))
                                Action.PlaySoundFileExpression = PlaySoundFileExpression;
                            var PlaySoundVolumeExpression = Action.PlaySoundVolumeExpression;
                            if (ImGui.InputText("音量(0-100)", ref PlaySoundVolumeExpression))
                                Action.PlaySoundVolumeExpression = PlaySoundVolumeExpression;
                            break;
                        }
                        case ActionTypeEnum.UseTTS: {
                            var UseTTSTextExpression = Action.UseTTSTextExpression;
                            if (ImGui.InputText("播读内容", ref UseTTSTextExpression))
                                Action.UseTTSTextExpression = UseTTSTextExpression;
                            break;
                        }
                        case ActionTypeEnum.Variable: {
                            var currenVariableOpEnumItem = 0;
                            if (Enum.TryParse(typeof(VariableOpEnum), Action.VariableOp, out var VariableOpEnumType))
                                currenVariableOpEnumItem = (int)VariableOpEnumType;
                            if (ImGui.Combo("操作", ref currenVariableOpEnumItem,
                                    Enum.GetValues<VariableOpEnum>()
                                        .Select(i => i.ToString())
                                        .ToList()))
                                Action.VariableOp = currenVariableOpEnumItem == 0 ? null : ((VariableOpEnum)currenVariableOpEnumItem).ToString();
                            if (currenVariableOpEnumItem == (int)VariableOpEnum.UnsetAll) break;
                            var VariableName = Action.VariableName;
                            if (ImGui.InputText("源变量", ref VariableName))
                                Action.VariableName = VariableName;
                            var VariablePersist = Action.VariablePersist != null && bool.Parse(Action.VariablePersist);
                            if (ImGui.Checkbox("源变量为永久变量", ref VariablePersist))
                                Action.VariablePersist = VariablePersist.ToString();
                            if (currenVariableOpEnumItem is (int)VariableOpEnum.Unset
                                or (int)VariableOpEnum.UnsetRegex
                                or (int)VariableOpEnum.UnsetRegexUniversal) break;
                            var VariableExpression = Action.VariableExpression;
                            if (ImGui.InputText("表达式", ref VariableExpression))
                                Action.VariableExpression = VariableExpression;
                            if (Action.VariableOp is nameof(VariableOpEnum.QueryJsonPath)
                                or nameof(VariableOpEnum.QueryJsonPathList)) {
                                var VariableJsonTarget = Action.VariableJsonTarget;
                                if (ImGui.InputText("目标变量", ref VariableJsonTarget))
                                    Action.VariableJsonTarget = VariableJsonTarget;
                                var VariableTargetPersist = Action.VariableTargetPersist != null && bool.Parse(Action.VariableTargetPersist);
                                if (ImGui.Checkbox("目标变量为永久变量", ref VariableTargetPersist))
                                    Action.VariableTargetPersist = VariableTargetPersist.ToString();
                            }
                            break;
                        }
                        case ActionTypeEnum.ListVariable: {
                            // var currenListVariableOpEnumItem = 0;
                            // if (Enum.TryParse(typeof(ListVariableOpEnum), Action.ListVariableOp, out var ListVariableOpEnumType))
                            //     currenListVariableOpEnumItem = (int)ListVariableOpEnumType;
                            // if (ImGui.Combo("操作", ref currenListVariableOpEnumItem,
                            //                 Enum.GetValues<ListVariableOpEnum>()
                            //                     .Select(i => i.ToString())
                            //                     .ToList()))
                            //     Action.ListVariableOp = currenListVariableOpEnumItem == 0 ? null : ((ListVariableOpEnum)currenListVariableOpEnumItem).ToString();
                            // if (currenListVariableOpEnumItem == (int)(ListVariableOpEnum.UnsetAll)) break;
                            // var ListVariableName = Action.ListVariableName;
                            // if (ImGui.InputText("源变量", ref ListVariableName))
                            //     Action.ListVariableName = ListVariableName;
                            // var ListSourcePersist = Action.ListSourcePersist != null && bool.Parse(Action.ListSourcePersist);
                            // if (ImGui.Checkbox("源变量为永久变量", ref ListSourcePersist))
                            //     Action.ListSourcePersist = ListSourcePersist.ToString();
                            // if (currenListVariableOpEnumItem == (int)ListVariableOpEnum.Unset) break;
                            // if (currenListVariableOpEnumItem is not (int)ListVariableOpEnum.Remove)
                            // {
                            //     var ListVariableExpEnumItem = 0;
                            //     if (Enum.TryParse(typeof(ListVariableExpTypeEnum), Action.ListVariableExpressionType, out var ListVariableExpressionType))
                            //         ListVariableExpEnumItem = (int)ListVariableExpressionType;
                            //     if (ImGui.Combo("表达类型", ref ListVariableExpEnumItem,
                            //                     Enum.GetValues<ListVariableExpTypeEnum>()
                            //                         .Select(i => i.ToString())
                            //                         .ToList()))
                            //         Action.ListVariableExpressionType = ListVariableExpEnumItem == 0 ? null : ((ListVariableExpTypeEnum)ListVariableExpEnumItem).ToString();
                            //     var ListVariableExpression = Action.ListVariableExpression;
                            //     if (ImGui.InputText("表达式", ref ListVariableExpression))
                            //         Action.ListVariableExpression = ListVariableExpression;
                            // }
                            //
                            // if (currenListVariableOpEnumItem is not (int)ListVariableOpEnum.Push)
                            // {
                            //     var ListVariableIndex = Action.ListVariableIndex;
                            //     if (ImGui.InputText("索引号", ref ListVariableIndex))
                            //         Action.ListVariableIndex = ListVariableIndex;
                            // }

                            ImGui.Text("还没做");
                            break;
                        }
                        case ActionTypeEnum.TableVariable: {
                            ImGui.Text("还没做");
                            break;
                        }
                        case ActionTypeEnum.DictVariable: {
                            ImGui.Text("还没做");
                            break;
                        }
                        case ActionTypeEnum.MessageBox: {
                            var currenMessageBoxIconTypeEnumItem = 0;
                            if (Enum.TryParse(typeof(MessageBoxIconTypeEnum), Action.MessageBoxIconType, out var MessageBoxIconType))
                                currenMessageBoxIconTypeEnumItem = (int)MessageBoxIconType;
                            if (ImGui.Combo("消息级别", ref currenMessageBoxIconTypeEnumItem,
                                    Enum.GetValues<MessageBoxIconTypeEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.MessageBoxIconType = currenMessageBoxIconTypeEnumItem == 0 ? null : ((MessageBoxIconTypeEnum)currenMessageBoxIconTypeEnumItem).ToString();
                            var MessageBoxText = Action.MessageBoxText;
                            if (ImGui.InputText("消息内容", ref MessageBoxText))
                                Action.MessageBoxText = MessageBoxText;
                            break;
                        }
                        case ActionTypeEnum.LogMessage: {
                            var LogMessageText = Action.LogMessageText;
                            if (ImGui.InputText("日志文本", ref LogMessageText))
                                Action.LogMessageText = LogMessageText;
                            var LogProcess = Action.LogProcess != null && bool.Parse(Action.LogProcess);
                            if (ImGui.Checkbox("处理为待解析的日志行", ref LogProcess))
                                Action.LogProcess = LogProcess.ToString();
                            if (LogProcess) {
                                var LogProcessACT = Action.LogProcessACT != null && bool.Parse(Action.LogProcessACT);
                                if (ImGui.Checkbox("将日志添加至ACT战斗记录", ref LogProcessACT))
                                    Action.LogProcessACT = LogProcessACT.ToString();
                                var currenSourceEnumItem = 0;
                                if (Enum.TryParse(typeof(LogEvent.SourceEnum), Action.LogMessageTarget, out var SourceEnumType))
                                    currenSourceEnumItem = (int)SourceEnumType;
                                if (ImGui.Combo("日志行类型", ref currenSourceEnumItem,
                                        Enum.GetValues<LogEvent.SourceEnum>()
                                            .Select(i => i.ToString()).ToList()))
                                    Action.LogMessageTarget = currenSourceEnumItem == 0 ? null : ((LogEvent.SourceEnum)currenSourceEnumItem).ToString();
                            }
                            else {
                                var currenLogMessageEnumItem = 0;
                                if (Enum.TryParse(typeof(LogMessageEnum), Action.LogLevel, out var LogMessage))
                                    currenLogMessageEnumItem = (int)LogMessage;
                                if (ImGui.Combo("触发器日志等级", ref currenLogMessageEnumItem,
                                        Enum.GetValues<LogMessageEnum>()
                                            .Select(i => i.ToString()).ToList()))
                                    Action.LogLevel = currenLogMessageEnumItem == 0 ? null : ((LogMessageEnum)currenLogMessageEnumItem).ToString();
                            }
                            break;
                        }
                        case ActionTypeEnum.TextAura: {
                            var TextAuraOpEnumItem = 0;
                            if (Enum.TryParse(typeof(AuraOpEnum), Action.TextAuraOp, out var TextAuraOp))
                                TextAuraOpEnumItem = (int)TextAuraOp;
                            if (ImGui.Combo("操作", ref TextAuraOpEnumItem,
                                    Enum.GetValues<AuraOpEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.TextAuraOp = TextAuraOpEnumItem == 0 ? null : ((AuraOpEnum)TextAuraOpEnumItem).ToString();
                            var TextAuraName = Action.TextAuraName;
                            if (ImGui.InputText("名称", ref TextAuraName))
                                Action.TextAuraName = TextAuraName;
                            var TextAuraExpression = Action.TextAuraExpression;
                            if (ImGui.InputText("文本", ref TextAuraExpression))
                                Action.TextAuraExpression = TextAuraExpression;
                            var TextAuraAlignmentEnumItem = 0;
                            if (Enum.TryParse(typeof(TextAuraAlignmentEnum), Action.TextAuraAlignment, out var TextAuraAlignment))
                                TextAuraAlignmentEnumItem = (int)TextAuraAlignment;
                            if (ImGui.Combo("文本对齐", ref TextAuraAlignmentEnumItem,
                                    Enum.GetValues<TextAuraAlignmentEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.TextAuraAlignment = TextAuraAlignmentEnumItem == 0 ? null : ((TextAuraAlignmentEnum)TextAuraAlignmentEnumItem).ToString();

                            var TextAuraTTLTickExpression = string.IsNullOrEmpty(Action.TextAuraTTLTickExpression) ? "1*1000-${_sincems}" : Action.TextAuraTTLTickExpression;
                            if (string.IsNullOrEmpty(Action.TextAuraTTLTickExpression) || ImGui.InputText("存续条件(>0)", ref TextAuraTTLTickExpression))
                                Action.TextAuraTTLTickExpression = TextAuraTTLTickExpression;
                            var TextAuraFontName = string.IsNullOrEmpty(Action.TextAuraFontName) ? "Microsoft YaHei UI" : Action.TextAuraFontName;
                            if (string.IsNullOrEmpty(Action.TextAuraFontName) || ImGui.InputText("字体名称", ref TextAuraFontName))
                                Action.TextAuraFontName = TextAuraFontName;
                            var TextAuraEffect = string.IsNullOrEmpty(Action.TextAuraEffect) ? "Bold" : Action.TextAuraEffect;
                            if (string.IsNullOrEmpty(Action.TextAuraEffect) || ImGui.InputText("字体效果", ref TextAuraEffect))
                                Action.TextAuraEffect = TextAuraEffect;
                            var TextAuraFontSize = string.IsNullOrEmpty(Action.TextAuraFontSize) ? "18" : Action.TextAuraFontSize;
                            if (string.IsNullOrEmpty(Action.TextAuraFontSize) || ImGui.InputText("字体大小", ref TextAuraFontSize))
                                Action.TextAuraFontSize = TextAuraFontSize;
                            ImGui.Separator();
                            var TextAuraForeground = Action.TextAuraForeground;
                            if (ImGui.InputText("前景颜色(文本)", ref TextAuraForeground))
                                Action.TextAuraForeground = TextAuraForeground;
                            var TextAuraBackground = Action.TextAuraBackground;
                            if (ImGui.InputText("背景颜色", ref TextAuraBackground))
                                Action.TextAuraBackground = TextAuraBackground;
                            var TextAuraOutline = Action.TextAuraOutline;
                            if (ImGui.InputText("轮廓颜色", ref TextAuraOutline))
                                Action.TextAuraOutline = TextAuraOutline;
                            ImGui.Separator();
                            var TextAuraXIniExpression = string.IsNullOrEmpty(Action.TextAuraXIniExpression) ? "500" : Action.TextAuraXIniExpression;
                            if (string.IsNullOrEmpty(Action.TextAuraXIniExpression) || ImGui.InputText("窗口初始坐标X", ref TextAuraXIniExpression))
                                Action.TextAuraXIniExpression = TextAuraXIniExpression;
                            var TextAuraYIniExpression = string.IsNullOrEmpty(Action.TextAuraYIniExpression) ? "500" : Action.TextAuraYIniExpression;
                            if (string.IsNullOrEmpty(Action.TextAuraYIniExpression) || ImGui.InputText("窗口初始坐标Y", ref TextAuraYIniExpression))
                                Action.TextAuraYIniExpression = TextAuraYIniExpression;
                            var TextAuraWIniExpression = string.IsNullOrEmpty(Action.TextAuraWIniExpression) ? "500" : Action.TextAuraWIniExpression;
                            if (string.IsNullOrEmpty(Action.TextAuraWIniExpression) || ImGui.InputText("窗口初始宽度W", ref TextAuraWIniExpression))
                                Action.TextAuraWIniExpression = TextAuraWIniExpression;
                            var TextAuraHIniExpression = string.IsNullOrEmpty(Action.TextAuraHIniExpression) ? "500" : Action.TextAuraHIniExpression;
                            if (string.IsNullOrEmpty(Action.TextAuraHIniExpression) || ImGui.InputText("窗口初始高度H", ref TextAuraHIniExpression))
                                Action.TextAuraHIniExpression = TextAuraHIniExpression;
                            var TextAuraOIniExpression = Action.TextAuraOIniExpression;
                            if (ImGui.InputText("窗口初始不透明度O", ref TextAuraOIniExpression))
                                Action.TextAuraOIniExpression = TextAuraOIniExpression;
                            ImGui.Separator();
                            ImGui.Text("刷新间隔20ms");
                            var TextAuraXTickExpression = Action.TextAuraXTickExpression;
                            if (ImGui.InputText("窗口刷新坐标X", ref TextAuraXTickExpression))
                                Action.TextAuraXTickExpression = TextAuraXTickExpression;
                            var TextAuraYTickExpression = Action.TextAuraYTickExpression;
                            if (ImGui.InputText("窗口刷新坐标Y", ref TextAuraYTickExpression))
                                Action.TextAuraYTickExpression = TextAuraYTickExpression;
                            var TextAuraWTickExpression = Action.TextAuraWTickExpression;
                            if (ImGui.InputText("窗口刷新宽度W", ref TextAuraWTickExpression))
                                Action.TextAuraWTickExpression = TextAuraWTickExpression;
                            var TextAuraHTickExpression = Action.TextAuraHTickExpression;
                            if (ImGui.InputText("窗口刷新高度H", ref TextAuraHTickExpression))
                                Action.TextAuraHTickExpression = TextAuraHTickExpression;
                            var TextAuraOTickExpression = Action.TextAuraOTickExpression;
                            if (ImGui.InputText("窗口刷新不透明度O", ref TextAuraOTickExpression))
                                Action.TextAuraOTickExpression = TextAuraOTickExpression;
                            break;
                        }
                        case ActionTypeEnum.Aura: {
                            ImGui.Text("暂不支持");
                            break;
                        }
                        case ActionTypeEnum.Mouse: {
                            var MouseOpTypeEnumItem = 0;
                            if (Enum.TryParse(typeof(MouseOpEnum), Action.MouseOpType, out var MouseOpType))
                                MouseOpTypeEnumItem = (int)MouseOpType;
                            if (ImGui.Combo("操作", ref MouseOpTypeEnumItem,
                                    Enum.GetValues<MouseOpEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.MouseOpType = MouseOpTypeEnumItem == 0 ? null : ((MouseOpEnum)MouseOpTypeEnumItem).ToString();
                            var MouseCoordTypeEnumItem = 0;
                            if (Enum.TryParse(typeof(MouseCoordEnum), Action.MouseCoordType, out var MouseCoordType))
                                MouseCoordTypeEnumItem = (int)MouseCoordType;
                            if (ImGui.Combo("坐标系", ref MouseCoordTypeEnumItem,
                                    Enum.GetValues<MouseCoordEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.MouseCoordType = MouseCoordTypeEnumItem == 0 ? null : ((MouseCoordEnum)MouseCoordTypeEnumItem).ToString();
                            var MouseX = Action.MouseX;
                            if (ImGui.InputText("X", ref MouseX))
                                Action.MouseX = MouseX;
                            var MouseY = Action.MouseY;
                            if (ImGui.InputText("Y", ref MouseY))
                                Action.MouseY = MouseY;
                            break;
                        }
                        case ActionTypeEnum.KeyPress: {
                            var KeypressTypeEnumItem = 0;
                            if (Enum.TryParse(typeof(KeypressTypeEnum), Action.KeypressType, out var KeypressType))
                                KeypressTypeEnumItem = (int)KeypressType;
                            if (ImGui.Combo("发送方式", ref KeypressTypeEnumItem,
                                    Enum.GetValues<KeypressTypeEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.KeypressType = KeypressTypeEnumItem == 0 ? null : ((KeypressTypeEnum)KeypressTypeEnumItem).ToString();
                            if (KeypressTypeEnumItem == (int)KeypressTypeEnum.SendKeys) {
                                var KeyPressExpression = Action.KeyPressExpression;
                                if (ImGui.InputText("按键命令", ref KeyPressExpression))
                                    Action.KeypressType = KeyPressExpression;
                            }
                            else {
                                var KeyPressProcId = Action.KeyPressProcId;
                                if (ImGui.InputText("进程ID", ref KeyPressProcId))
                                    Action.KeyPressProcId = KeyPressProcId;
                                var KeyPressWindow = Action.KeyPressWindow;
                                if (ImGui.InputText("窗口标题", ref KeyPressWindow))
                                    Action.KeyPressWindow = KeyPressWindow;
                                var KeyPressCode = Action.KeyPressCode;
                                if (ImGui.InputText("键码", ref KeyPressCode))
                                    Action.KeyPressCode = KeyPressCode;
                            }
                            break;
                        }
                        case ActionTypeEnum.NamedCallback: {
                            var NamedCallbackName = Action.NamedCallbackName;
                            if (ImGui.InputText("回调名称", ref NamedCallbackName))
                                Action.NamedCallbackName = NamedCallbackName;
                            var NamedCallbackParam = Action.NamedCallbackParam;
                            if (ImGui.InputTextMultiline("回调参数", ref NamedCallbackParam))
                                Action.NamedCallbackParam = NamedCallbackParam;
                            break;
                        }
                        case ActionTypeEnum.WindowMessage: {
                            var WmsgProcId = Action.WmsgProcId;
                            if (ImGui.InputText("进程ID", ref WmsgProcId))
                                Action.WmsgProcId = WmsgProcId;
                            var WmsgTitle = Action.WmsgTitle;
                            if (ImGui.InputText("窗口标题", ref WmsgTitle))
                                Action.WmsgTitle = WmsgTitle;
                            var WmsgCode = Action.WmsgCode;
                            if (ImGui.InputText("消息代码", ref WmsgCode))
                                Action.WmsgCode = WmsgCode;
                            var WmsgWparam = Action.WmsgWparam;
                            if (ImGui.InputText("Wparam", ref WmsgWparam))
                                Action.WmsgWparam = WmsgWparam;
                            var WmsgLparam = Action.WmsgLparam;
                            if (ImGui.InputText("Lparam", ref WmsgLparam))
                                Action.WmsgLparam = WmsgLparam;
                            break;
                        }
                        case ActionTypeEnum.DiskFile: {
                            var DiskFileOpEnumItem = 0;
                            if (Enum.TryParse(typeof(DiskFileOpEnum), Action.DiskFileOp, out var DiskFileOp))
                                DiskFileOpEnumItem = (int)DiskFileOp;
                            if (ImGui.Combo("操作", ref DiskFileOpEnumItem,
                                    Enum.GetValues<KeypressTypeEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.DiskFileOp = DiskFileOpEnumItem == 0 ? null : ((KeypressTypeEnum)DiskFileOpEnumItem).ToString();
                            var DiskFileOpName = Action.DiskFileOpName;
                            if (ImGui.InputText("文件名", ref DiskFileOpName))
                                Action.DiskFileOpName = DiskFileOpName;
                            var DiskFileOpVar = Action.DiskFileOpVar;
                            if (ImGui.InputText("变量名", ref DiskFileOpVar))
                                Action.DiskFileOpVar = DiskFileOpVar;
                            var DiskFileCache = Action.DiskFileCache != null && bool.Parse(Action.DiskFileCache);
                            if (ImGui.Checkbox("变量名为永久变量", ref DiskFileCache))
                                Action.DiskFileCache = DiskFileCache.ToString();
                            break;
                        }
                        case ActionTypeEnum.LaunchProcess: {
                            var LaunchProcessPathExpression = Action.LaunchProcessPathExpression;
                            if (ImGui.InputText("程序路径", ref LaunchProcessPathExpression))
                                Action.LaunchProcessPathExpression = LaunchProcessPathExpression;
                            var LaunchProcessCmdlineExpression = Action.LaunchProcessCmdlineExpression;
                            if (ImGui.InputText("命令行参数", ref LaunchProcessCmdlineExpression))
                                Action.LaunchProcessCmdlineExpression = LaunchProcessCmdlineExpression;
                            var LaunchProcessWorkingDirExpression = Action.LaunchProcessWorkingDirExpression;
                            if (ImGui.InputText("工作目录", ref LaunchProcessWorkingDirExpression))
                                Action.LaunchProcessWorkingDirExpression = LaunchProcessWorkingDirExpression;
                            var currenProcessWindowStyleItem = 0;
                            if (Enum.TryParse(typeof(ProcessWindowStyle), Action.LaunchProcessWindowStyle, out var ProcessWindowStyle))
                                currenProcessWindowStyleItem = (int)ProcessWindowStyle;
                            if (ImGui.Combo("日志行类型", ref currenProcessWindowStyleItem,
                                    Enum.GetValues<ProcessWindowStyle>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.LaunchProcessWindowStyle = currenProcessWindowStyleItem == 0 ? null : ((ProcessWindowStyle)currenProcessWindowStyleItem).ToString();
                            break;
                        }
                        case ActionTypeEnum.ExecuteScript: {
                            var ExecScriptExpression = Action.ExecScriptExpression;
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.InputTextMultiline("## 脚本代码", ref ExecScriptExpression, 1145141))
                                Action.ExecScriptExpression = ExecScriptExpression;
                            break;
                        }
                        case ActionTypeEnum.Mutex: {
                            var MutexOpEnumItem = 0;
                            if (Enum.TryParse(typeof(MutexOpEnum), Action.MutexOpType, out var MutexOpType))
                                MutexOpEnumItem = (int)MutexOpType;
                            if (ImGui.Combo("日志行类型", ref MutexOpEnumItem,
                                    Enum.GetValues<MutexOpEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.MutexOpType = MutexOpEnumItem == 0 ? null : ((MutexOpEnum)MutexOpEnumItem).ToString();
                            var MutexName = Action.MutexName;
                            if (ImGui.InputText("互斥锁名称", ref MutexName))
                                Action.MutexName = MutexName;
                            break;
                        }
                        case ActionTypeEnum.Loop: {
                            ImGui.Text("还没做");
                            break;
                        }
                        case ActionTypeEnum.GenericJson: {
                            var JsonEndpointExpression = Action.JsonEndpointExpression;
                            if (ImGui.InputText("目标URL", ref JsonEndpointExpression))
                                Action.JsonEndpointExpression = JsonEndpointExpression;
                            var JsonOperationEnumItem = 0;
                            if (Enum.TryParse(typeof(HTTPMethodEnum), Action.JsonOperationType, out var JsonOperationType))
                                JsonOperationEnumItem = (int)JsonOperationType;
                            if (ImGui.Combo("HTTP方法类型", ref JsonOperationEnumItem,
                                    Enum.GetValues<HTTPMethodEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.JsonOperationType = JsonOperationEnumItem == 0 ? null : ((HTTPMethodEnum)JsonOperationEnumItem).ToString();
                            var JsonPayloadExpression = Action.JsonPayloadExpression;
                            if (ImGui.InputText("发送内容", ref JsonPayloadExpression))
                                Action.JsonPayloadExpression = JsonPayloadExpression;
                            var JsonHeaderExpression = Action.JsonHeaderExpression;
                            if (ImGui.InputText("JSON头部", ref JsonHeaderExpression))
                                Action.JsonHeaderExpression = JsonHeaderExpression;
                            var JsonResultVariable = Action.JsonResultVariable;
                            if (ImGui.InputText("结果存储至变量", ref JsonResultVariable))
                                Action.JsonResultVariable = JsonResultVariable;
                            var JsonResultVariablePersist = Action.JsonResultVariablePersist != null && bool.Parse(Action.JsonResultVariablePersist);
                            if (ImGui.Checkbox("结果存储至永久变量", ref JsonResultVariablePersist))
                                Action.JsonResultVariablePersist = JsonResultVariablePersist.ToString();
                            var JsonFiringExpression = Action.JsonFiringExpression;
                            if (ImGui.InputText("文本解析响应结果", ref JsonFiringExpression))
                                Action.JsonFiringExpression = JsonFiringExpression;
                            var JsonCacheRequest = Action.JsonCacheRequest != null && bool.Parse(Action.JsonCacheRequest);
                            if (ImGui.Checkbox("响应结果缓存至磁盘", ref JsonCacheRequest))
                                Action.JsonCacheRequest = JsonCacheRequest.ToString();
                            break;
                        }
                        case ActionTypeEnum.DiscordWebhook: {
                            var DiscordWebhookURL = Action.DiscordWebhookURL;
                            if (ImGui.InputText("接口URL", ref DiscordWebhookURL))
                                Action.DiscordWebhookURL = DiscordWebhookURL;
                            var DiscordWebhookMessage = Action.DiscordWebhookMessage;
                            if (ImGui.InputText("发送消息", ref DiscordWebhookMessage))
                                Action.DiscordWebhookMessage = DiscordWebhookMessage;
                            var DiscordTts = Action.DiscordTts != null && bool.Parse(Action.DiscordTts);
                            if (ImGui.Checkbox("发送TTS消息", ref DiscordTts))
                                Action.DiscordTts = DiscordTts.ToString();
                            ImGui.Text("还没做");
                            break;
                        }
                        case ActionTypeEnum.LiveSplitControl: {
                            var LiveSplitControlEnumItem = 0;
                            if (Enum.TryParse(typeof(LiveSplitControlTypeEnum), Action.LiveSplitControlType, out var LiveSplitControlType))
                                LiveSplitControlEnumItem = (int)LiveSplitControlType;
                            if (ImGui.Combo("操作", ref LiveSplitControlEnumItem,
                                    Enum.GetValues<LiveSplitControlTypeEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.LiveSplitControlType = LiveSplitControlEnumItem == 0 ? null : ((LiveSplitControlTypeEnum)LiveSplitControlEnumItem).ToString();
                            if (LiveSplitControlEnumItem == (int)LiveSplitControlTypeEnum.CustomPayload) {
                                var LiveSplitCustomPayload = Action.LiveSplitCustomPayload;
                                if (ImGui.InputText("消息负载", ref LiveSplitCustomPayload))
                                    Action.LiveSplitCustomPayload = LiveSplitCustomPayload;
                                ImGui.Text("还没做");
                            }
                            ImGui.Text("还没做");
                            break;
                        }
                        case ActionTypeEnum.ObsControl: {
                            var OBSEndPoint = Action.OBSEndPoint;
                            if (ImGui.InputText("URL", ref OBSEndPoint))
                                Action.OBSEndPoint = OBSEndPoint;
                            var OBSPassword = Action.OBSPassword;
                            if (ImGui.InputText("密码", ref OBSPassword))
                                Action.OBSPassword = OBSPassword;
                            var OBSControlEnumItem = 0;
                            if (Enum.TryParse(typeof(ObsControlTypeEnum), Action.OBSControlType, out var OBSControlType))
                                OBSControlEnumItem = (int)OBSControlType;
                            if (ImGui.Combo("操作", ref OBSControlEnumItem,
                                    Enum.GetValues<ObsControlTypeEnum>()
                                        .Select(i => i.ToString()).ToList()))
                                Action.LiveSplitControlType = OBSControlEnumItem == 0 ? null : ((ObsControlTypeEnum)OBSControlEnumItem).ToString();
                            if (OBSControlEnumItem is (int)ObsControlTypeEnum.SetScene
                                or (int)ObsControlTypeEnum.ShowSource
                                or (int)ObsControlTypeEnum.HideSource) {
                                var OBSSceneName = Action.OBSSceneName;
                                if (ImGui.InputText("场景名称", ref OBSSceneName))
                                    Action.OBSSceneName = OBSSceneName;
                                if (OBSControlEnumItem != (int)ObsControlTypeEnum.SetScene) {
                                    var OBSSourceName = Action.OBSSourceName;
                                    if (ImGui.InputText("源名称", ref OBSSourceName))
                                        Action.OBSSourceName = OBSSourceName;
                                }
                            }
                            if (OBSControlEnumItem is (int)ObsControlTypeEnum.JSONPayload) {
                                var OBSJSONPayload = Action.OBSJSONPayload;
                                if (ImGui.InputText("JSON消息", ref OBSJSONPayload))
                                    Action.OBSJSONPayload = OBSJSONPayload;
                            }
                            break;
                        }
                        case ActionTypeEnum.ActInteraction: {
                            ImGui.Text("暂不支持");
                            break;
                        }
                        case ActionTypeEnum.Trigger: {
                            ImGui.Text("还没做");
                            break;
                        }
                        case ActionTypeEnum.Folder: {
                            ImGui.Text("还没做");
                            break;
                        }
                        case ActionTypeEnum.Repository: {
                            ImGui.Text("还没做");
                            break;
                        }
                        case ActionTypeEnum.Placeholder: break;
                    }
                    ImGui.Separator();
                    var key = Action.ActionType switch {
                        ActionTypeEnum.Variable => "rtbHelperVar" + (Action.VariableOp ?? ((VariableOpEnum)0).ToString()),
                        ActionTypeEnum.ListVariable => "rtbHelperLvar" + (Action.ListVariableOp ?? ((ListVariableOpEnum)0).ToString()),
                        ActionTypeEnum.TableVariable => "rtbHelperTvar" + (Action.TableVariableOp ?? ((TableVariableOpEnum)0).ToString()),
                        ActionTypeEnum.DictVariable => "rtbHelperDict" + (Action.DictVariableOp ?? ((DictVariableOpEnum)0).ToString()),
                        ActionTypeEnum.KeyPress => "rtbHelperSendKeys" + (Action.KeypressType ?? ((KeypressTypeEnum)0).ToString()),
                        ActionTypeEnum.NamedCallback => "rtbHelperCallback",
                        ActionTypeEnum.WindowMessage => "rtbHelperWmsg",
                        ActionTypeEnum.GenericJson => "rtbHelperJson",
                        _ => ""
                    };
                    if (key != "") ImGui.Text(I18n.Translate($"ActionForm/{key}", $"{key}.Text"));
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("动作条件")) {
                    ConditionPanel.Draw();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("计划任务")) {
                    var currentRefireInterruptItem = Action.RefireInterrupt ? 0 : 1;
                    if (ImGui.Combo("触发器再次触发时，此动作若尚未结束", ref currentRefireInterruptItem,
                            new[] {
                                "保留队列中所有旧动作，", "中断队列中所有旧动作，"
                            }))
                        Action.RefireInterrupt = currentRefireInterruptItem == 0;
                    var currentRefireRequeueItem = Action.RefireRequeue ? 1 : 0;
                    if (ImGui.Combo("## 触发器再次触发时，此动作若尚未结束", ref currentRefireRequeueItem,
                            new[] {
                                "并允许触发器再次触发", "并禁止触发器再次触发"
                            }))
                        Action.RefireRequeue = currentRefireRequeueItem == 1;
                    var ExecutionDelayExpression = Action.ExecutionDelayExpression;
                    if (ImGui.InputText("动作延迟(ms)", ref ExecutionDelayExpression))
                        Action.ExecutionDelayExpression = ExecutionDelayExpression;
                    var Asynchronous = Action.Asynchronous;
                    if (ImGui.Checkbox("异步执行", ref Asynchronous))
                        Action.Asynchronous = Asynchronous;
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("描述")) {
                    var description = Action.Description ?? "";
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputTextMultiline("## 描述", ref description))
                        Action.Description = description;
                    var DescriptionOverride = Action.DescriptionOverride;
                    if (ImGui.Checkbox("覆盖此动作描述", ref DescriptionOverride))
                        Action.DescriptionOverride = DescriptionOverride;
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
    }
}