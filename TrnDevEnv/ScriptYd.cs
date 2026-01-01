using System.Collections.Generic;
using Triggernometry.Core;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class ScriptYd : IScriptBase
{
    public override string[] TerritoryIds() => ["779","1318"];
    private string ydStatus = "";
    public override List<TargetIcon> TargetIconList =>
    [
        new(TTS("点你分散", 1000), TargetId: MeHexID, Id: "0017"),
        new(TTS("点你陨石"), TargetId: MeHexID, Id: "0083")
    ];
    public override List<StartsCasting> StartsCastingList =>
    [
        new(TTS("五加三"), Id: "B165"),
        new(TTS("子弹，分摊"), Id: "B130"),
        new(TTS("AOE"), Id: "B13B"),
        new(TTS("九连环，四次分摊"), Id: "B150"),
        new(TTS("死刑"), Id: "B12E"),
        new(() =>
        {
            TTS("范围死刑");
            RealPlugin.Instance.InvokeNamedCallback("PictoACT","Omen: gl_fan90_1bwf_k1\nt: 2.5\nPos: ${_entity[${id}].Pos}\nAngle: ${_entity[${id}].h}\nScale: 12,12");
        }, Id: "B12F"),
        new(() =>
        {
            if (ydStatus == "满月流") TTS("去左远离");
            else  if (ydStatus == "新月流") TTS("去左靠近");
        }, Id: "B14E"),
        new(() =>
        {
            if (ydStatus == "满月流") TTS("去右远离");
            else  if (ydStatus == "新月流") TTS("去右靠近");
        }, Id: "B14F"),
    ];
    public override List<StatusAdd> StatusAddList =>
    [
        new(() => ydStatus = "满月流", EffectId: "5FF"),
        new(() => ydStatus = "新月流", EffectId: "600"),
        new(TTS("去黑"), EffectId: "602", TargetId: MeHexID, Count: "04"),
        new(TTS("去白"), EffectId: "603", TargetId: MeHexID, Count: "04"),
    ];
}
