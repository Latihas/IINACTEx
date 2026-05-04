using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class ScriptYd : IScriptBase {
	private enum YdStatus {
		满月流,
		新月流
	}

	public override uint[]? TerritoryIds() => [1318];
	private YdStatus? ydStatus;
	public override List<TargetIcon> TargetIconList => [
		new(MTTS("点你分散", 1000), Me_HexID, 0x0017),
		new(MTTS("点你陨石"), Me_HexID, 0x0083)
	];
	private readonly Action<ulong, int, ulong> a = (src, _, dst) => {
		TTS("范围死刑");
		DrawShape(new IGCone(GetGameObjectById_Position(src), 12, BossFacingToTarget(src, dst), Deg2Rad(90), 8000));
	};
	public override List<StartsCasting> StartsCastingList => [
		new(MTTS("五加三"), Id: 0xB165),
		new((src, _, _) => {
			TTS("子弹，分摊");
			DelayExec(() => DrawShape(new IGCone(GetGameObjectById_Position(src), 40, Deg2Rad(0), Deg2Rad(45), 5000)), 5000);
		}, Id: 0xB130),
		new((src, _, _) => {
			TTS("长枪，分散");
			DelayExec(() => {
				DrawShape(new IGCone(GetGameObjectById_Position(src), 40, Deg2Rad(0), Deg2Rad(45), 5000));
				DrawShape(new IGCone(GetGameObjectById_Position(src), 40, -Deg2Rad(90), Deg2Rad(45), 5000));
				DrawShape(new IGCone(GetGameObjectById_Position(src), 40, Deg2Rad(180), Deg2Rad(45), 5000));
			}, 5000);
		}, Id: 0xB131),
		new(MTTS("大AOE"), Id: 0xB13B),
		new(MTTS("九连环，四次分摊"), Id: 0xB150),
		new(MTTS("AOE"), Id: 0xB12E),
		new(a, Id: 0xB12F),
		new(a, Id: 0xB16A),
		new((src, _, _) => {
			var center = GetGameObjectById_Position(src)();
			DrawShape(new IGCone(center, 20, GetGameObjectById_Rotation(src)() - Deg2Rad(75), Deg2Rad(210), 8000));
			if (ydStatus == YdStatus.满月流) {
				TTS("去左远离");
				DrawShape(new IGCircle(center, 10, 8000));
			} else if (ydStatus == YdStatus.新月流) {
				TTS("去左靠近");
				DrawShape(new IGRing(center, 20, 5, 8000));
			}
		}, Id: 0xB14E),
		new((src, _, _) => {
			var center = GetGameObjectById_Position(src)();
			DrawShape(new IGCone(center, 20, GetGameObjectById_Rotation(src)() + Deg2Rad(75), Deg2Rad(210), 8000));
			if (ydStatus == YdStatus.满月流) {
				TTS("去右远离");
				DrawShape(new IGCircle(center, 10, 8000));
			} else if (ydStatus == YdStatus.新月流) {
				TTS("去右靠近");
				DrawShape(new IGRing(center, 20, 5, 8000));
			}
		}, Id: 0xB14F)
	];
	public override List<StatusAdd> StatusAddList => [
		new(() => ydStatus = YdStatus.满月流, 0x5FF),
		new(() => ydStatus = YdStatus.新月流, 0x600),
		new(MTTS("去黑"), 0x602, TargetId: Me_HexID, Count: 0x04),
		new(MTTS("去白"), 0x603, TargetId: Me_HexID, Count: 0x04)
	];
	public override List<(Regex, Action<GroupCollection>)> CustomList => [
		new(new Regex("^.{14} 261 105:Add:(?<id>.{8}):BNpcID:4A7A:BNpcNameID:1C3D:"), Groups => {
			var id = Convert.ToUInt64(Groups["id"].Value, 16);
			DrawShape(new IGCircle(GetGameObjectById_Position(id)(), 10, 5000));
		})
	];
}