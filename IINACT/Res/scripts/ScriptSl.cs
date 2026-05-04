using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class ScriptSl : IScriptBase {
	public override uint[]? TerritoryIds() => [0x55C];
	public override List<StartsCasting> StartsCastingList => [
		new(MTTS("击退"), Id: 0xC436), //巨浪，巨浪，不断地增长
		new(MTTS("多段伤害，避开人群"), Id: 0xC44F), //死亡轮回
		new(MTTS("火伤，水圈集合"), Id: 0xC437), //地狱之火炎
		new(MTTS("雷伤，离开水圈分散"), Id: 0xC438), //制裁之雷
		new(MTTS("滑冰"), Id: 0xC439), //钻石星尘
		new(MTTS("连续击退"), Id: 0xC43B), //大气爆发
		new(MTTS("斜角击退"), Id: 0xC45F), //螺旋冲锋
		new(MTTS("准备击退到另外场地"), Id: 0xC470), //空降
		new(MTTS("离开前面"), Id: 0xC477), //黑暗吐息
		new(MTTS("月环"), Id: 0xC475), //神龙啸
		new(MTTS("冰柱"), Id: 0xC44A), //召唤冰柱
	];
	public override List<(Regex, Action<GroupCollection>)> CustomList => [
		new(new Regex("^.{14} 261 105:Add:.+?:BNpcNameID:1886:.+?:PosX:(?<x>[^:]+):PosY:(?<y>[^:]+):PosZ:(?<z>[^:]+):"), Groups => {
			DrawShape(new IGRect(
				new Vector3(float.Parse(Groups["x"].Value), float.Parse(Groups["y"].Value), float.Parse(Groups["z"].Value)),
				new Vector3(float.Parse(Groups["x"].Value), float.Parse(Groups["y"].Value), float.Parse(Groups["z"].Value) + 20),
				5000
				, 2.5f));
		})
	];
}