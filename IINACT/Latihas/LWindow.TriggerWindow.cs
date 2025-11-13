using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Triggernometry;

namespace IINACT.Latihas;

public static partial class LWindow
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TriggerWindow() : Window($"{WindowPrefix}TriggerWindow")
    {
        internal static Trigger? Trigger;
        internal static ConditionPanel ConditionPanel;

        public void Open(Trigger trigger)
        {
            Trigger = trigger;
            ConditionPanel = new ConditionPanel(trigger);
            IsOpen = true;
            TriggernometryProxy.ProxyPlugin.DalamudPlugin.ActionWindow.IsOpen = false;
        }

        private readonly Context fakectx = new();
        private static readonly Vector4 ColorGrey = new(0.5f, 0.5f, 0.5f, 1.0f);

        public override void Draw()
        {
            if (Trigger == null) return;
            ImGui.Text("Id: ");
            ImGui.SameLine();
            var id = Trigger.Id.ToString();
            if (ImGui.Button(id)) ImGui.SetClipboardText(id);
            var name = Trigger.Name;
            if (ImGui.InputText("触发器名称", ref name, 114514)) Trigger.Name = name;
            var regularExpression = Trigger.RegularExpression;
            if (ImGui.InputText("正则表达式", ref regularExpression, 114514)) Trigger.RegularExpression = regularExpression;
            var selected = -1;
            if (ImGui.BeginTabBar("tab"))
            {
                if (ImGui.BeginTabItem("触发器动作"))
                {
                    if (ImGui.Button("+"))
                    {
                        Trigger.Actions.Add(new Triggernometry.Action { OrderNumber = Trigger.Actions.Count + 1 });
                        selected = Trigger.Actions.Count - 1;
                    }
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.BeginListBox("## Actions"))
                    {
                        var data = Trigger.Actions!;
                        var Enabled = new bool[data.Count];
                        for (var i = 0; i < data.Count; i++)
                        {
                            var d = data[i].GetDescription(fakectx);
                            var isSelected = selected == i;
                            Enabled[i] = Trigger.Actions[i].Enabled == null || bool.Parse(Trigger.Actions[i].Enabled);
                            if (ImGui.Checkbox($"## Trigger_Action_Enabled_{i}", ref Enabled[i])) Trigger.Actions[i].Enabled = Enabled[i].ToString();
                            ImGui.SameLine();
                            if (!Enabled[i]) ImGui.PushStyleColor(ImGuiCol.Text, ColorGrey);
                            if (ImGui.Selectable(d, isSelected))
                                Plugin.Instance.ActionWindow.Open(Trigger, Trigger.Actions[i]);
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                ImGui.OpenPopup($"context_menu_action_{i}");
                            if (ImGui.BeginPopup($"context_menu_action_{i}"))
                            {
                                if (ImGui.MenuItem("删除"))
                                {
                                    Plugin.Instance.ActionWindow.DeleteAction(Trigger, Trigger.Actions[i]);
                                    break;
                                }
                                if (ImGui.MenuItem("测试"))
                                {
                                    Trigger.Actions[i].Execute(null, fakectx);
                                    break;
                                }
                                if (ImGui.MenuItem("测试(忽略条件)"))
                                {
                                    var bc = Trigger.Actions[i].Condition;
                                    Trigger.Actions[i].Condition = new ConditionGroup();
                                    Trigger.Actions[i].Execute(null, fakectx);
                                    Trigger.Actions[i].Condition = bc;
                                    break;
                                }
                                ImGui.EndPopup();
                            }
                            if (!Enabled[i]) ImGui.PopStyleColor();
                        }
                        ImGui.EndListBox();
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
                    var currenSourceItem = (int)Trigger.Source;
                    if (ImGui.Combo("触发器事件源", ref currenSourceItem,
                                    Enum.GetValues<Trigger.TriggerSourceEnum>()
                                        .Select(i => i.ToString()).ToList()))
                        Trigger.Source = (Trigger.TriggerSourceEnum)currenSourceItem;
                    var currenPrevActionsEnumItem = (int)Trigger.PrevActions;
                    if (ImGui.Combo("触发器再次触发时，此动作若尚未结束", ref currenPrevActionsEnumItem,
                                    new[] { "保留队列中所有旧动作，", "中断队列中所有旧动作，" }))
                        Trigger.PrevActions = ((Trigger.PrevActionsEnum)currenPrevActionsEnumItem);
                    var currenPrevActionsRefireEnumItem = (int)Trigger.PrevActionsRefire;
                    if (ImGui.Combo("## 触发器再次触发时，此动作若尚未结束", ref currenPrevActionsEnumItem,
                                    new[] { "并允许触发器再次触发", "并禁止触发器再次触发" }))
                        Trigger.PrevActionsRefire = ((Trigger.RefireEnum)currenPrevActionsRefireEnumItem);
                    var currenSchedulingEnumItem = (int)Trigger.Scheduling;
                    if (ImGui.Combo("计划", ref currenSchedulingEnumItem,
                                    Enum.GetValues<Trigger.SchedulingEnum>()
                                        .Select(i => i.ToString()).ToList()))
                        Trigger.Scheduling = ((Trigger.SchedulingEnum)currenSchedulingEnumItem);
                    var currenRefireEnumItem = (int)Trigger.PeriodRefire;
                    if (ImGui.Combo("触发器连续触发时", ref currenRefireEnumItem,
                                    Enum.GetValues<Trigger.RefireEnum>()
                                        .Select(i => i.ToString()).ToList()))
                        Trigger.PeriodRefire = ((Trigger.RefireEnum)currenRefireEnumItem);
                    var RefirePeriodExpression = Trigger.RefirePeriodExpression;
                    if (ImGui.InputText("触发冷却时间(ms)", ref RefirePeriodExpression))
                        Trigger.RefirePeriodExpression = RefirePeriodExpression;
                    var EditAutofire = Trigger.EditAutofire;
                    if (ImGui.Checkbox("保存更改后自动执行", ref EditAutofire))
                        Trigger.EditAutofire = EditAutofire;
                    var EditAutofireAllowCondition = Trigger.EditAutofireAllowCondition;
                    if (ImGui.Checkbox("保存更改后自动执行遵循条件", ref EditAutofireAllowCondition))
                        Trigger.EditAutofireAllowCondition = EditAutofireAllowCondition;
                    var Sequential = Trigger.Sequential;
                    if (ImGui.Checkbox("顺序执行", ref Sequential))
                        Trigger.Sequential = Sequential;
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("描述"))
                {
                    var description = Trigger.Description ?? "";
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputTextMultiline("## 描述", ref description))
                        Trigger.Description = description;
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
    }
}
