using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Triggernometry;
using static Triggernometry.ConditionGroup;
using static Triggernometry.ConditionSingle;
using Action = Triggernometry.Action;

namespace IINACT.Latihas;

public partial class LWindow
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ActionWindow() : Window($"{WindowPrefix}ActionWindow")
    {
        internal static Action? Action;
        internal static Trigger? Trigger;
        internal static ConditionPanel ConditionPanel;

        internal void Open(Trigger trigger, Action action)
        {
            Trigger = trigger;
            Action = action;
            ConditionPanel = new ConditionPanel(action);
            Plugin.Instance.ActionWindow.IsOpen = true;
        }

        public void DeleteAction(Trigger trigger, Action action)
        {
            trigger.Actions.Remove(action);
            for (var i = 0; i < trigger.Actions.Count; i++)
                trigger.Actions[i].OrderNumber = (i + 1);
            if (Action == action)
            {
                Trigger = null;
                Action = null;
                Plugin.Instance.ActionWindow.IsOpen = false;
            }
        }


        public override void Draw()
        {
            if (Action == null) return;
            if (ImGui.Button("删除")) DeleteAction(Trigger!, Action);
            var currentActionTypeEnumItem = 0;
            if (Enum.TryParse(typeof(Action.ActionTypeEnum), Action.ActionType, out var actionType))
                currentActionTypeEnumItem = (int)actionType;
            if (ImGui.Combo("动作类型", ref currentActionTypeEnumItem,
                            Enum.GetValues<Action.ActionTypeEnum>()
                                .Select(i => i.ToString())
                                .ToList()))
                Action.ActionType = currentActionTypeEnumItem == 0 ? null : ((Action.ActionTypeEnum)currentActionTypeEnumItem).ToString();
            if (ImGui.BeginTabBar("## EActionTabBar"))
            {
                if (ImGui.BeginTabItem("特定动作设置"))
                {
                    switch (Action.ActionType)
                    {
                        case null or nameof(Action.ActionTypeEnum.SystemBeep):
                        {
                            var SystemBeepFreqExpression = Action.SystemBeepFreqExpression;
                            if (ImGui.InputText("频率", ref SystemBeepFreqExpression))
                                Action.SystemBeepFreqExpression = SystemBeepFreqExpression;
                            var SystemBeepLengthExpression = Action.SystemBeepLengthExpression;
                            if (ImGui.InputText("时长", ref SystemBeepLengthExpression))
                                Action.SystemBeepLengthExpression = SystemBeepLengthExpression;
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.PlaySound):
                        {
                            var PlaySoundFileExpression = Action.PlaySoundFileExpression;
                            if (ImGui.InputText("音频路径", ref PlaySoundFileExpression))
                                Action.PlaySoundFileExpression = PlaySoundFileExpression;
                            var PlaySoundVolumeExpression = Action.PlaySoundVolumeExpression;
                            if (ImGui.InputText("音量(0-100)", ref PlaySoundVolumeExpression))
                                Action.PlaySoundVolumeExpression = PlaySoundVolumeExpression;


                            break;
                        }
                        case nameof(Action.ActionTypeEnum.UseTTS):
                        {
                            var UseTTSTextExpression = Action.UseTTSTextExpression;
                            if (ImGui.InputText("播读内容", ref UseTTSTextExpression))
                                Action.UseTTSTextExpression = UseTTSTextExpression;


                            break;
                        }
                        case nameof(Action.ActionTypeEnum.Variable):
                        {
                            var currenVariableOpEnumItem = 0;
                            if (Enum.TryParse(typeof(Action.VariableOpEnum), Action.VariableOp, out var VariableOpEnumType))
                                currenVariableOpEnumItem = (int)VariableOpEnumType;
                            if (ImGui.Combo("操作", ref currenVariableOpEnumItem,
                                            Enum.GetValues<Action.VariableOpEnum>()
                                                .Select(i => i.ToString())
                                                .ToList()))
                                Action.VariableOp = currenVariableOpEnumItem == 0 ? null : ((Action.VariableOpEnum)currenVariableOpEnumItem).ToString();
                            if (Action.VariableOp == nameof(Action.VariableOpEnum.UnsetAll)) break;
                            var VariableName = Action.VariableName;
                            if (ImGui.InputText("源变量", ref VariableName))
                                Action.VariableName = VariableName;
                            if (Action.VariableOp is nameof(Action.VariableOpEnum.Unset)
                                or nameof(Action.VariableOpEnum.UnsetRegex)
                                or nameof(Action.VariableOpEnum.UnsetRegexUniversal)) break;
                            var VariableExpression = Action.VariableExpression;
                            if (ImGui.InputText("表达式", ref VariableExpression))
                                Action.VariableExpression = VariableExpression;
                            if (Action.VariableOp is nameof(Action.VariableOpEnum.QueryJsonPath)
                                or nameof(Action.VariableOpEnum.QueryJsonPathList))
                            {
                                var VariableJsonTarget = Action.VariableJsonTarget;
                                if (ImGui.InputText("目标变量", ref VariableJsonTarget))
                                    Action.VariableJsonTarget = VariableJsonTarget;
                            }
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.ListVariable):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.TableVariable):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.DictVariable):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.MessageBox):
                        {
                            var currenMessageBoxIconTypeEnumItem = 0;
                            if (Enum.TryParse(typeof(Action.MessageBoxIconTypeEnum), Action.MessageBoxIconType, out var MessageBoxIconType))
                                currenMessageBoxIconTypeEnumItem = (int)MessageBoxIconType;
                            if (ImGui.Combo("消息级别", ref currenMessageBoxIconTypeEnumItem,
                                            Enum.GetValues<Action.MessageBoxIconTypeEnum>()
                                                .Select(i => i.ToString()).ToList()))
                                Action.MessageBoxIconType = currenMessageBoxIconTypeEnumItem == 0 ? null : ((Action.MessageBoxIconTypeEnum)currenMessageBoxIconTypeEnumItem).ToString();
                            var MessageBoxText = Action.MessageBoxText;
                            if (ImGui.InputText("消息内容", ref MessageBoxText))
                                Action.MessageBoxText = MessageBoxText;
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.LogMessage):
                        {
                            var LogMessageText = Action.LogMessageText;
                            if (ImGui.InputText("日志文本", ref LogMessageText))
                                Action.LogMessageText = LogMessageText;
                            var LogProcess = Action.LogProcess != null && bool.Parse(Action.LogProcess);
                            if (ImGui.Checkbox("处理为待解析的日志行", ref LogProcess))
                                Action.LogProcess = LogProcess.ToString();
                            if (LogProcess)
                            {
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
                            else
                            {
                                var currenLogMessageEnumItem = 0;
                                if (Enum.TryParse(typeof(Action.LogMessageEnum), Action.LogLevel, out var LogMessage))
                                    currenLogMessageEnumItem = (int)LogMessage;
                                if (ImGui.Combo("触发器日志等级", ref currenLogMessageEnumItem,
                                                Enum.GetValues<Action.LogMessageEnum>()
                                                    .Select(i => i.ToString()).ToList()))
                                    Action.LogLevel = currenLogMessageEnumItem == 0 ? null : ((Action.LogMessageEnum)currenLogMessageEnumItem).ToString();
                            }
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.TextAura):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.Aura):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.Mouse):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.KeyPress):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.NamedCallback):
                        {
                            var NamedCallbackName = Action.NamedCallbackName;
                            if (ImGui.InputText("回调名称", ref NamedCallbackName))
                                Action.NamedCallbackName = NamedCallbackName;
                            var NamedCallbackParam = Action.NamedCallbackParam;
                            if (ImGui.InputTextMultiline("回调参数", ref NamedCallbackParam))
                                Action.NamedCallbackParam = NamedCallbackParam;
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.WindowMessage):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.DiskFile):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.LaunchProcess):
                        {
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
                            if (Enum.TryParse(typeof(System.Diagnostics.ProcessWindowStyle), Action.LaunchProcessWindowStyle, out var ProcessWindowStyle))
                                currenProcessWindowStyleItem = (int)ProcessWindowStyle;
                            if (ImGui.Combo("日志行类型", ref currenProcessWindowStyleItem,
                                            Enum.GetValues<System.Diagnostics.ProcessWindowStyle>()
                                                .Select(i => i.ToString()).ToList()))
                                Action.LaunchProcessWindowStyle = currenProcessWindowStyleItem == 0 ? null : ((System.Diagnostics.ProcessWindowStyle)currenProcessWindowStyleItem).ToString();
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.ExecuteScript):
                        {
                            var ExecScriptExpression = Action.ExecScriptExpression;
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.InputTextMultiline("脚本代码", ref ExecScriptExpression, 1145141, new Vector2(400, 300)))
                                Action.ExecScriptExpression = ExecScriptExpression;
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.Mutex):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.Loop):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.GenericJson):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.DiscordWebhook):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.LiveSplitControl):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.ObsControl):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.ActInteraction):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.Trigger):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.Folder):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.Repository):
                        {
                            ImGui.Text("还没做");
                            break;
                        }
                        case nameof(Action.ActionTypeEnum.Placeholder): break;
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("动作条件"))
                {
                    ConditionPanel.Draw();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("计划任务"))
                {
                    var currentRefireInterruptItem = Action.RefireInterrupt != null && bool.Parse(Action.RefireInterrupt) ? 0 : 1;
                    if (ImGui.Combo("触发器再次触发时，此动作若尚未结束", ref currentRefireInterruptItem,
                                    new[] { "保留队列中所有旧动作，", "中断队列中所有旧动作，" })) ;
                    Action.RefireInterrupt = (currentRefireInterruptItem == 1).ToString();
                    var currentRefireRequeueItem = Action.RefireRequeue != null && bool.Parse(Action.RefireRequeue) ? 1 : 0;
                    if (ImGui.Combo("## 触发器再次触发时，此动作若尚未结束", ref currentRefireRequeueItem,
                                    new[] { "并允许触发器再次触发", "并禁止触发器再次触发" })) ;
                    Action.RefireRequeue = (currentRefireRequeueItem == 0).ToString();
                    var ExecutionDelayExpression = Action.ExecutionDelayExpression;
                    if (ImGui.InputText("动作延迟(ms)", ref ExecutionDelayExpression))
                        Action.ExecScriptExpression = ExecutionDelayExpression;
                    var Asynchronous = Action.Asynchronous != null && bool.Parse(Action.Asynchronous);
                    if (ImGui.Checkbox("异步执行", ref Asynchronous))
                        Action.Asynchronous = Asynchronous.ToString();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("描述"))
                {
                    var description = Action.Description ?? "";
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputTextMultiline("## 描述", ref description))
                        Action.Description = description;
                    var DescriptionOverride = Action.DescriptionOverride != null && bool.Parse(Action.DescriptionOverride);
                    if (ImGui.Checkbox("覆盖此动作描述", ref DescriptionOverride))
                        Action.DescriptionOverride = DescriptionOverride.ToString();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
    }
}
