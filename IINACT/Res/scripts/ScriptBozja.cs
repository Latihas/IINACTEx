using System.Collections.Generic;
using Triggernometry.Core;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class Bozja : IScriptBase {
	public override uint[] TerritoryIds() => [920, 975];

	public override List<StartsCasting> StartsCastingList => [
		new(MTTS("死刑，绕后"), Id: 0x5ECB), //左臂斩击
		new(MTTS("击退后穿"), Id: 0x5EBB), //左臂金属切割刀
		new(() => {
			DelayExec(MTTS("开光之幕帘"), 3000);
		}, Id: 0x5D5D), //无情交火
		new(MTTS("开无敌"), Id: 0x5D5C), //螺旋灾变
		new(MTTS("靠近"), Id: 0x5D4B), //装填冲击弹
		new(MTTS("靠近"), Id: 0x5D4B), //装填冲击弹
		new(MTTS("远离"), Id: 0x5D4C), //装填近距离弹
		new(MTTS("准备驱魔"), Id: 0x5D38) //炎帝热气烧
	];
	public override List<StatusAdd> StatusAddList => [
		new(() => {
			const string str = "开魔泉";
			TTS(str);
			RealPlugin.Instance.InvokeNamedCallback("command", $"/e {str}");
		}, 0xA00, TargetId: Me_HexID),
		new(() => {
			const string str = "开背水";
			TTS(str);
			RealPlugin.Instance.InvokeNamedCallback("command", $"/e {str}");
		}, 0x91C, TargetId: Me_HexID),
		new(() => {
			const string str = "开卓异，砸耀星，开团辅，开耀星";
			TTS(str);
			RealPlugin.Instance.InvokeNamedCallback("command", $"/e {str}");
		}, 0x916, TargetId: Me_HexID)
	];
}