using System;
using System.Collections.Generic;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class Test : IScriptBase {
    public override uint[] TerritoryIds() => [348];
    public override List<TargetIcon> TargetIconList => [];
    public override List<StartsCasting> StartsCastingList => [];
    public override List<StatusAdd> StatusAddList => [
        new(() => ScriptDrawList.Add(new IGCircle(Me_Position, 5, 5000)), 0x171, TargetId: Me_HexID),
        new(() => ScriptDrawList.Add(new IGCone(Me_Position, 20, Me_Rotation, Math.PI, 5000)), 0x171, TargetId: Me_HexID),
        new((_, s, _, _) => ScriptDrawList.Add(new IGRect(Me_Position, GetGameObjectById_Position(s), 5000, 10)), 0x171, TargetId: Me_HexID)
    ];
}