using System;
using System.Collections.Generic;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class Test : IScriptBase {
    public override uint[] TerritoryIds() => [348];
    public override List<TargetIcon> TargetIconList => [];
    public override List<StartsCasting> StartsCastingList => [];
    public override List<StatusAdd> StatusAddList => [
        new(() => DrawShape(new IGCircle(Me_Position_D, 5, 5000)), 0x171, TargetId: Me_HexID_D),
        new(() => DrawShape(new IGCone(Me_Position_D, 20, Me_Rotation_D, Math.PI, 5000)), 0x171, TargetId: Me_HexID_D),
        new((_, s, _, _) => DrawShape(new IGRect(Me_Position_D, GetGameObjectById_Position(s), 5000, 10)), 0x171, TargetId: Me_HexID_D)
    ];
}