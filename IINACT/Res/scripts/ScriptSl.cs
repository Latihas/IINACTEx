using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class ScriptSl : IScriptBase {
	public override uint[] TerritoryIds() => [0x55C];
	public override List<StartsCasting> StartsCastingList => [
		new(MTTS("击退"), Id: 0xC436), //巨浪，巨浪，不断地增长
		new(MTTS("多段伤害，避开人群"), Id: 0xC44F), //死亡轮回
		new(MTTS("火伤"), Id: 0xC437), //地狱之火炎
		new(MTTS("雷伤"), Id: 0xC446), //闪电
		new(MTTS("滑冰"), Id: 0xC439), //钻石星尘
		new(MTTS("连续击退"), Id: 0xC43B), //大气爆发
		new(MTTS("斜角击退"), Id: 0xC45F), //螺旋冲锋
		new(MTTS("准备击退到另外场地"), Id: 0xC470), //空降
		new(MTTS("离开前面"), Id: 0xC477), //黑暗吐息
		new(MTTS("月环"), Id: 0xC475), //神龙啸
		new(MTTS("冰柱"), Id: 0xC44A), //召唤冰柱
		new(MTTS("雷伤"), Id: 0xC438), //制裁之雷
		new(MTTS("火伤"), Id: 0xC444) //超新星
	];
	public override List<(Regex, Action<GroupCollection>)> CustomList => [
		new(new Regex("^.{14} 261 105:Add:.+?:BNpcNameID:1886:.+?:Heading:(?<r>[^:]+):.+?:PosX:(?<x>[^:]+):PosY:(?<y>[^:]+):PosZ:(?<z>[^:]+):"), Groups => {
			DrawShape(new IGRay(
				new Vector3(float.Parse(Groups["x"].Value), float.Parse(Groups["z"].Value), float.Parse(Groups["y"].Value)), 60,
				float.Parse(Groups["r"].Value), 10000, 10));
		}),
		new(new Regex("^.{14} SystemLogMessage 29:.{8}:AF2:00:00:00"), _ => TTS("转火核心")),
		new(new Regex("^.{14} Director 21:.{8}:.{8}:01:01:00:00"), _ => {
			ShowTexts([
				"——————————————————",
				"　　MT ST　　　|■ 连线+分摊",
				"D1　　　　D2　| MT/D1　ST/D2",
				"　D3　　D4　　| 　　 　 A",
				"　H1　　H2　　| H1/D3　H2/D4",
				"——————————————————",
				"■ 放尾巴",
				"【奶右下 → DPS左下 → T左上or中间】",
				"■ 死亡轮回",
				"【分摊 → MT无敌 → ST无敌 → 分摊】",
				"■ 钻石星辰：H2滑冰",
				"■ 大地吐息：奶妈=左、DPS=右",
				"■ 小怪阶段：MT=中间大龙、ST=其他"
			]);
		})
	];
	public override List<TargetIcon> TargetIconList => [
		new(MTTS("龙尾点你"), Me_HexID, 0x007E)
	];
}