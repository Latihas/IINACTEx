using System.Collections.Generic;
using Triggernometry.Core;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class ScriptYd : IScriptBase
{
    public override string[] TerritoryIds() => ["779", "1318"];
    private string ydStatus = "";
    public override List<TargetIcon> TargetIconList =>
    [
        new(TTS("点你分散", 1000), TargetId: MeHexID, Id: 0x0017),
        new(TTS("点你陨石"), TargetId: MeHexID, Id: 0x0083)
    ];
    public override List<StartsCasting> StartsCastingList =>
    [
        new(TTS("五加三"), Id: 0xB165),
        new(TTS("子弹，分摊"), Id: 0xB130),
        new(TTS("AOE"), Id: 0xB13B),
        new(TTS("九连环，四次分摊"), Id: 0xB150),
        new(TTS("死刑"), Id: 0xB12E),
        new(() =>
        {
            TTS("范围死刑");
            RealPlugin.Instance.InvokeNamedCallback("PictoACT", "Omen: gl_fan90_1bwf_k1\nt: 2.5\nPos: ${_entity[${id}].Pos}\nAngle: ${_entity[${id}].h}\nScale: 12,12");
        }, Id: 0xB12F),
        new(() =>
        {
            if (ydStatus == "满月流") TTS("去左远离");
            else if (ydStatus == "新月流") TTS("去左靠近");
        }, Id: 0xB14E),
        new(() =>
        {
            if (ydStatus == "满月流") TTS("去右远离");
            else if (ydStatus == "新月流") TTS("去右靠近");
        }, Id: 0xB14F)
    ];
    public override List<StatusAdd> StatusAddList =>
    [
        new(() => ydStatus = "满月流", EffectId: 0x5FF),
        new(() => ydStatus = "新月流", EffectId: 0x600),
        new(TTS("去黑"), EffectId: 0x602, TargetId: MeHexID, Count: 0x04),
        new(TTS("去白"), EffectId: 0x603, TargetId: MeHexID, Count: 0x04)
    ];
}
