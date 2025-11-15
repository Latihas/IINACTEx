using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using IINACT;

var action = () =>
{
    var bdl = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
    if (Plugin.GameGui.WorldToScreen(new Vector3(100, 0, 100), out var v1))
        bdl.AddCircleFilled(v1, 10f, 0xFFFF0000);
    if (Plugin.GameGui.WorldToScreen(Plugin.ClientState.LocalPlayer!.Position, out var v2))
        bdl.AddCircleFilled(v2, 10f, 0xFFFF0000);
    bdl.AddLine(v1, v2, 0xFFFF0000);
};
TriggernometryProxy.ProxyPlugin.PluginInterface.UiBuilder.Draw += action;
_ = Task.Run(async () =>
{
    await Task.Delay(5000);
    TriggernometryProxy.ProxyPlugin.PluginInterface.UiBuilder.Draw -= action;
});
