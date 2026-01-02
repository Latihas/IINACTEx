using System.Collections.Generic;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class Test : IScriptBase
{
    public override string[] TerritoryIds() => ["348"];
    public override List<TargetIcon> TargetIconList => [];
    public override List<StartsCasting> StartsCastingList => [];
    public override List<StatusAdd> StatusAddList =>
    [
        new(() => ScriptDrawList.Add(new IGCircle(Me_Position, 5, 5)), EffectId: 171, TargetId: Me_HexID),
        new(() => ScriptDrawList.Add(new IGCone(Me_Position, 10, Me_Rotation(), 45, 5)), EffectId: 171, TargetId: Me_HexID),
        new((_, _, t, _) => ScriptDrawList.Add(new IGLine(Me_Position, GetGameObjectById_Position(t), 10, 5)), EffectId: 171, TargetId: Me_HexID),
    ];
}
