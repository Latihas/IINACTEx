using System.Numerics;
using Dalamud.Bindings.ImGui;
using Triggernometry;
using Action = Triggernometry.Action;

namespace IINACT.Latihas;

public partial class LWindow
{
    internal class ConditionPanel
    {
        private static dynamic AT;

        public ConditionPanel(dynamic at)
        {
            AT = at; 
        }

        internal ConditionComponent? Condition;

        public void Draw()
        {
            var windowContentWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
            const float rightPanelWidth = 300f;
            const float splitterWidth = 4f;
            var leftPanelWidth = windowContentWidth - rightPanelWidth - splitterWidth;
            leftPanelWidth = Math.Max(150, leftPanelWidth);
            ImGui.BeginChild("LeftConditionPanel", new Vector2(leftPanelWidth, 0), false, ImGuiWindowFlags.NoScrollbar);
            if (AT.Condition == null)
            {
                if (ImGui.Button("启用条件组")) AT.Condition = new ConditionGroup { Grouping = ConditionGroup.CndGroupingEnum.Or };
            }
            else BuildConditionTree(null, AT.Condition);
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.SetCursorPosX(leftPanelWidth);
            ImGui.InvisibleButton("ConditionSplitter", new Vector2(splitterWidth, ImGui.GetWindowHeight()), ImGuiButtonFlags.MouseButtonLeft);
            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
            ImGui.SameLine();
            ImGui.BeginChild("RightConditionPanel", new Vector2(rightPanelWidth, 0), true, ImGuiWindowFlags.NoScrollbar);
            ImGui.Text("条件属性");
            ImGui.Separator();
            if (Condition != null)
                switch (Condition)
                {
                    case ConditionGroup conditionGroup:
                    {
                        var grouping = (int)conditionGroup.Grouping - 1;
                        if (ImGui.Combo("分组类型", ref grouping, Enum.GetValues<ConditionGroup.CndGroupingEnum>().Select(i => i.ToString()).ToList()))
                            conditionGroup.Grouping = (ConditionGroup.CndGroupingEnum)(grouping + 1);
                        break;
                    }
                    case ConditionSingle conditionSingle:
                        ImGui.Text("左侧表达式L");
                        var ExpressionTypeL = (int)conditionSingle.ExpressionTypeL;
                        if (ImGui.Combo("左侧表达式L类型", ref ExpressionTypeL, Enum.GetValues<ConditionSingle.ExprTypeEnum>().Select(i => i.ToString()).ToList()))
                            conditionSingle.ExpressionTypeL = (ConditionSingle.ExprTypeEnum)ExpressionTypeL;
                        var ExpressionL = conditionSingle.ExpressionL;
                        if (ImGui.InputText("值L", ref ExpressionL))
                            conditionSingle.ExpressionL = ExpressionL;
                        ImGui.Text("操作符");
                        var ConditionType = (int)conditionSingle.ConditionType;
                        if (ImGui.Combo("操作符类型", ref ConditionType, Enum.GetValues<ConditionSingle.CndTypeEnum>().Select(i => i.ToString()).ToList()))
                            conditionSingle.ConditionType = (ConditionSingle.CndTypeEnum)ConditionType;
                        ImGui.Text("右侧表达式R");
                        var ExpressionTypeR = (int)conditionSingle.ExpressionTypeR;
                        if (ImGui.Combo("右侧表达式R类型", ref ExpressionTypeR, Enum.GetValues<ConditionSingle.ExprTypeEnum>().Select(i => i.ToString()).ToList()))
                            conditionSingle.ExpressionTypeR = (ConditionSingle.ExprTypeEnum)ExpressionTypeR;
                        var ExpressionR = conditionSingle.ExpressionR;
                        if (ImGui.InputText("值R", ref ExpressionR))
                            conditionSingle.ExpressionR = ExpressionR;
                        break;
                }
            else ImGui.TextDisabled("请先选择一个条件(组)");
            ImGui.EndChild();
            ImGui.EndTabItem();
        }

        private void BuildConditionTree(ConditionGroup? parent, ConditionComponent current)
        {
            switch (current)
            {
                case ConditionGroup group:
                    RenderConditionNode(
                        group.ToString(),
                        group,
                        parent is not null && !parent.Enabled,
                        () =>
                        {
                            foreach (var child in group.Children.ToList())
                                BuildConditionTree(group, child);
                        }
                    );
                    break;
                case ConditionSingle single:
                    RenderConditionNode(
                        single.ToString(),
                        single,
                        parent is not null && !parent.Enabled,
                        null
                    );
                    break;
            }
        }

        private static readonly Dictionary<long, bool> _nodeConditionExpandedStates = new();
        private static readonly Vector4 ColorGrey = new(0.5f, 0.5f, 0.5f, 1.0f);

        private void RenderConditionNode(string text, ConditionComponent condition, bool parentDisabled, System.Action? renderChildren)
        {
            var nodeId = condition.Id;
            _nodeConditionExpandedStates.TryAdd(nodeId, false);
            bool isDisabled = parentDisabled || !condition.Enabled;
            if (isDisabled) ImGui.PushStyleColor(ImGuiCol.Text, ColorGrey);
            var flags = ImGuiTreeNodeFlags.None;
            if (renderChildren == null)
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            bool isChecked = condition.Enabled;
            if (ImGui.Checkbox($"##check_{nodeId}", ref isChecked))
            {
                condition.Enabled = isChecked;
                condition.TriggerOnPropertyChange();
            }
            ImGui.SameLine();
            bool isExpanded = ImGui.TreeNodeEx(
                $"{text}##{nodeId}",
                flags
            );
            _nodeConditionExpandedStates[nodeId] = isExpanded;
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                Condition = condition;
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup($"ctx_{nodeId}");
            }
            if (ImGui.BeginPopup($"ctx_{nodeId}"))
            {
                bool tempPopped = false;
                if (isDisabled)
                {
                    ImGui.PopStyleColor();
                    tempPopped = true;
                }
                if (condition is ConditionGroup group)
                {
                    ImGui.Separator();
                    if (ImGui.MenuItem("添加条件组"))
                    {
                        var newGroup = new ConditionGroup { Parent = group, Grouping = ConditionGroup.CndGroupingEnum.Or };
                        group.AddChild(newGroup);
                    }
                    if (ImGui.MenuItem("添加条件"))
                    {
                        var newSingle = new ConditionSingle { Parent = group };
                        group.AddChild(newSingle);
                    }
                    ImGui.Separator();
                }
                if (ImGui.MenuItem("删除"))
                {
                    if (condition.Parent is ConditionGroup parentGroup)
                        parentGroup.RemoveChild(condition);
                    else if (condition == AT.Condition)
                    {
                        AT.Condition.Children.Clear();
                        AT.Condition.Enabled = false;
                    }
                }
                ImGui.EndPopup();
                if (tempPopped)
                    ImGui.PushStyleColor(ImGuiCol.Text, ColorGrey);
            }
            if (isExpanded && renderChildren != null)
            {
                ImGui.Indent();
                renderChildren();
                ImGui.Unindent();
                ImGui.TreePop();
            }
            if (isDisabled)
                ImGui.PopStyleColor();
        }
    }
}
