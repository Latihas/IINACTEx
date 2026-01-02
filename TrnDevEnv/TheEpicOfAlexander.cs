// using System;
// using System.Linq;
// using System.Numerics;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Newtonsoft.Json;
// using Dalamud.Utility.Numerics;
// using KodakkuAssist.Data;
// using KodakkuAssist.Script;
// using KodakkuAssist.Module.GameEvent;
// using KodakkuAssist.Module.Draw;
// using KodakkuAssist.Module.Draw.Manager;
//
// namespace Cyf5119Script.Shadowbringers.TheEpicOfAlexander;
//
// [ScriptType(guid: "E047803D-38D5-45B4-AF48-71C0691CDCC9", name: "亚历山大绝境战", territorys: [887], version: "0.0.2.9", author: "Cyf5119", note: Note, updateInfo: UpdateInfo)]
// public class TheEpicOfAlexander
// {
//     private const string Note = "有问题来DC反馈。\n画图基于设置的小队职能进行绘制，请确保设置准确无误。\n/e KASCLEAR 清理残余画图";
//     private const string UpdateInfo = "有问题来DC反馈。\n更新水雷指路，默认三雷ST";
//     
//     #region 用户设置
//
//     [UserSetting("P2仅显示自己的传毒提示")] public static bool P2RotsSelfOnly { get; set; } = false;
//     [UserSetting("P2三雷ST")] public static bool P2ThirdLightningSt { get; set; } = true;
//
//     [UserSetting("飞机劈冲颜色")] public static ScriptColor LimitCutColor { get; set; } = new() { V4 = new Vector4(1, 0.2f, 0.2f, 1) };
//     [UserSetting("P2蓝毒颜色")] public static ScriptColor P2Blue { get; set; } = new() { V4 = new Vector4(102 / 255f, 136 / 255f, 187 / 255f, 1) };
//     [UserSetting("P2橙毒颜色")] public static ScriptColor P2Orange { get; set; } = new() { V4 = new Vector4(204 / 255f, 136 / 255f, 102 / 255f, 1) };
//     [UserSetting("P2紫毒颜色")] public static ScriptColor P2Purple { get; set; } = new() { V4 = new Vector4(85 / 255f, 34 / 255f, 153 / 255f, 1) };
//     [UserSetting("P2绿毒颜色")] public static ScriptColor P2Green { get; set; } = new() { V4 = new Vector4(51 / 255f, 85 / 255f, 17 / 255f, 1) };
//     [UserSetting("超级跳与回头扫颜色")] public static ScriptColor SuperJumpColor { get; set; } = new() { V4 = new Vector4(0.6f, 0.2f, 1, 1) };
//     [UserSetting("P2水属性压缩颜色")] public static ScriptColor CompressedWaterColor { get; set; } = new() { V4 = new Vector4(.2f, 1, 1, 1) };
//     [UserSetting("P2雷属性压缩颜色")] public static ScriptColor CompressedLightningColor { get; set; } = new() { V4 = new Vector4(0.6f, 0.2f, 1, 1) };
//
//     [UserSetting("十字圣礼颜色")] public static ScriptColor SacramentColor { get; set; } = new() { V4 = new Vector4(.2f, 1, 1, 1) };
//     
//     #endregion
//
//     private static readonly Vector3 Center = new(100, 0, 100);
//
//     public void Init(ScriptAccessory sa)
//     {
//         P0Reset();
//         P1Reset();
//         P2Reset();
//         P3Reset();
//         P4Reset();
//         sa.Method.RemoveDraw(".*");
//     }
//
//     #region P0
//
//     private uint _phase = 0;
//
//     private List<uint> _p0LimitCutList = [0, 0, 0, 0, 0, 0, 0, 0];
//     private bool _p0LimitCutEnabled = false;
//     private uint _p0LimitCutTimes = 0;
//
//     private void P0Reset()
//     {
//         _phase = 0;
//         P0LimitCutReset();
//     }
//
//     [ScriptMethod(name: "clear draw", eventType: EventTypeEnum.Chat, eventCondition: ["Type:Echo", "Message:KASCLEAR"], userControl: false)]
//     public void clear_draw(Event evt, ScriptAccessory sa) => sa.Method.RemoveDraw(".*");
//
//     # region 阶段控制
//
//     [ScriptMethod(name: "阶段控制P1-流体摆动", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18864", "TargetIndex:1"], userControl: false)]
//     public void PhaseControl1(Event evt, ScriptAccessory sa) => _phase = _phase == 0 ? 100 : _phase;
//
//     [ScriptMethod(name: "阶段控制P1-倾泻", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18470"], userControl: false)]
//     public void PhaseControl1_1(Event evt, ScriptAccessory sa) => _phase += 1;
//
//     [ScriptMethod(name: "阶段控制P1.5-麻将点名", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^(00(4F|5[0123456]))$"], userControl: false)]
//     public void PhaseControl1_5(Event evt, ScriptAccessory sa) => _phase = _phase < 200 ? 150 : _phase;
//
//     [ScriptMethod(name: "阶段控制P2-正义飞踢", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18516", "TargetIndex:1"], userControl: false)]
//     public void PhaseControl2(Event evt, ScriptAccessory sa) => _phase = 200;
//
//     [ScriptMethod(name: "阶段控制P2-制导", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18479"], userControl: false)]
//     public void PhaseControl2_1(Event evt, ScriptAccessory sa) => _phase = 210;
//
//     [ScriptMethod(name: "阶段控制P2-大地导弹", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18510"], userControl: false)]
//     public void PhaseControl2_2(Event evt, ScriptAccessory sa) => _phase = 220;
//
//     [ScriptMethod(name: "阶段控制P2-大火炎放射", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18501"], userControl: false)]
//     public void PhaseControl2_3(Event evt, ScriptAccessory sa) => _phase = 230;
//
//     [ScriptMethod(name: "阶段控制P2-终审闭庭", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18864", "TargetIndex:1"], userControl: false)]
//     public void PhaseControl2_4(Event evt, ScriptAccessory sa) => _phase = 340;
//
//     [ScriptMethod(name: "阶段控制P3-时间停止", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18470"], userControl: false)]
//     public void PhaseControl3(Event evt, ScriptAccessory sa) => _phase = 300;
//
//     [ScriptMethod(name: "阶段控制P3-时空潜行阵列", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18543"], userControl: false)]
//     public void PhaseControl3_1(Event evt, ScriptAccessory sa) => _phase = 310;
//
//     [ScriptMethod(name: "阶段控制P3-次元断绝阵列", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18542"], userControl: false)]
//     public void PhaseControl3_2(Event evt, ScriptAccessory sa) => _phase = 320;
//
//     [ScriptMethod(name: "阶段控制P3-召唤亚历山大", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:19029"], userControl: false)]
//     public void PhaseControl3_3(Event evt, ScriptAccessory sa) => _phase = 330;
//
//     [ScriptMethod(name: "阶段控制P4-终审判决", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18557"], userControl: false)]
//     public void PhaseControl4(Event evt, ScriptAccessory sa) => _phase = 400;
//
//     [ScriptMethod(name: "阶段控制P4-未来观测α", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18555"], userControl: false)]
//     public void PhaseControl4_1(Event evt, ScriptAccessory sa) => _phase = 410;
//
//     [ScriptMethod(name: "阶段控制P4-未来观测β", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:19219"], userControl: false)]
//     public void PhaseControl4_2(Event evt, ScriptAccessory sa) => _phase = 420;
//
//     [ScriptMethod(name: "阶段控制P4-神圣大审判", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18574"], userControl: false)]
//     public void PhaseControl4_3(Event evt, ScriptAccessory sa) => _phase = 430;
//
//     [ScriptMethod(name: "阶段控制P4-时空干涉", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18582"], userControl: false)]
//     public void PhaseControl4_4(Event evt, ScriptAccessory sa) => _phase = 440;
//
//     #endregion
//
//     
//     #region 麻将控制
//
//     private void P0LimitCutReset()
//     {
//         _p0LimitCutList = [0, 0, 0, 0, 0, 0, 0, 0];
//         _p0LimitCutEnabled = false;
//         _p0LimitCutTimes = 0;
//     }
//
//     [ScriptMethod(name: "通用-麻将记录", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^(00(4F|5[0123456]))$"], userControl: false)]
//     public void LimitCutRecord(Event evt, ScriptAccessory sa)
//     {
//         var idx = (int)evt.IconId() - 79;
//         if (idx < 0 || idx > 7) return;
//         _p0LimitCutList[idx] = evt.TargetId();
//     }
//
//     [ScriptMethod(name: "通用-麻将执行", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["Id:7747", "SourceDataId:11342"])]
//     public void LimitCutPlay(Event evt, ScriptAccessory sa)
//     {
//         if (!_p0LimitCutEnabled) return;
//
//         var dp = sa.FastDp("阿尔法之剑", evt.SourceId(), 1100, 25 + 5);
//         dp.TargetObject = _p0LimitCutList[(int)(_p0LimitCutTimes * 2)];
//         dp.Radian = float.Pi / 2;
//         dp.Color = LimitCutColor.V4;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//
//         dp = sa.FastDp("超级摧毁者冲击", evt.SourceId(), 2600, new Vector2(10, 50 + 5));
//         dp.TargetObject = _p0LimitCutList[(int)(_p0LimitCutTimes * 2 + 1)];
//         dp.Color = LimitCutColor.V4;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
//
//         _p0LimitCutTimes++;
//     }
//
//     private bool GetLimitCut(uint id, out int index)
//     {
//         if (!_p0LimitCutList.Contains(id))
//         {
//             index = 0;
//             return false;
//         }
//
//         index = _p0LimitCutList.IndexOf(id);
//         return true;
//     }
//
//     #endregion
//
//     
//     #region 超级跳与回头扫
//
//     // 18505->读条超级跳越 18506->实际伤害超级跳越
//     [ScriptMethod(name: "通用-超级跳跃", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18505"])]
//     public void SuperJump(Event evt, ScriptAccessory sa)
//     {
//         // 第一个读条是3.9秒
//         var dp = sa.FastDp("超级跳跃", new Vector3(0), 4200, 10);
//         dp.CentreResolvePattern = PositionResolvePatternEnum.PlayerFarestOrder;
//         dp.CentreOrderIndex = 1;
//         dp.Color = SuperJumpColor.V4.WithW(0.05f);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//         dp.ScaleMode = ScaleMode.ByTime;
//         dp.Color = SuperJumpColor.V4.WithW(3);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     // 18507->末世宣言 对自身使用 18508->末世宣言 回头扇形多次 角度90 半径25？
//     [ScriptMethod(name: "通用-末世宣言", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18507"])]
//     public void ApocalypticRay(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("末世宣言", evt.SourceId(), 5000, 25);
//         dp.Radian = float.Pi / 2;
//         dp.Color = SuperJumpColor.V4.WithW(0.05f);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//         dp.ScaleMode = ScaleMode.ByTime;
//         dp.Color = SuperJumpColor.V4.WithW(3);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//     }
//
//     #endregion
//
//     #endregion
//
//     
//     #region P1
//
//     private uint _p1LiquidFluidTimes = 0;
//     private uint _p1HandFluidTimes = 0;
//     private uint _p1CascadeTimes = 0;
//     private List<Vector3> _p1RangePos = [];
//     private Vector3 _p1Vector = new();
//     private Dictionary<uint, uint> _p1Tether = [];
//     private Vector3 _p1HawkBlasterVector = new();
//     private readonly object _p1HawkBlasterLocker = new();
//     private uint _p1HawkBlasterTimes = 0;
//
//     private void P1Reset()
//     {
//         _p1LiquidFluidTimes = 0;
//         _p1HandFluidTimes = 0;
//         _p1CascadeTimes = 0;
//         _p1RangePos.Clear();
//         _p1Vector = new Vector3();
//         _p1Tether = [];
//         _p1HawkBlasterVector = new Vector3();
//         _p1HawkBlasterTimes = 0;
//     }
//
//     #region 流体摆动与强袭
//
//     [ScriptMethod(name: "P1-首次流体摆动", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18808"])]
//     public void FluidSwing0(Event evt, ScriptAccessory sa)
//     {
//         // P1BOSS平A 应该是10.2秒左右 但用触发清除就好了 摆了
//         if (_p1LiquidFluidTimes > 0) return;
//         _p1LiquidFluidTimes = 1;
//         var dp = sa.FastDp($"流体摆动_{_p1LiquidFluidTimes}", evt.SourceId(), 7000, 11.5f);
//         dp.Delay = 5000;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//     }
//
//     [ScriptMethod(name: "P1-后续流体摆动", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18864", "TargetIndex:1"])]
//     public void FluidSwing(Event evt, ScriptAccessory sa)
//     {
//         sa.Method.RemoveDraw($"流体摆动_{_p1LiquidFluidTimes}");
//         _p1LiquidFluidTimes += 1;
//         if (_p1LiquidFluidTimes < 2 || _p1LiquidFluidTimes > 3) return;
//         // 18864->流体摆动 r=8+3.5
//         var dp = sa.FastDp($"流体摆动_{_p1LiquidFluidTimes}", evt.SourceId(), 5000, 11.5f);
//         dp.Delay = _p1LiquidFluidTimes switch
//         {
//             2 => 26300 - 5000,
//             3 => 19100 - 5000,
//             // 4 => 77400,  一般不会打到这里，不管了
//             _ => 99999
//         };
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//     }
//
//     [ScriptMethod(name: "P1-首次流体强袭", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:11336"])]
//     public void FluidStrike1(Event evt, ScriptAccessory sa)
//     {
//         _p1HandFluidTimes = 2;
//         var dp = sa.FastDp($"流体强袭_{_p1HandFluidTimes}", evt.SourceId(), 5000, 11.6f);
//         dp.Delay = 17400 - 5000;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//     }
//
//     [ScriptMethod(name: "P1-后续流体强袭", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18871", "TargetIndex:1"])]
//     public void FluidStrike2(Event evt, ScriptAccessory sa)
//     {
//         sa.Method.RemoveDraw($"流体强袭_{_p1HandFluidTimes}");
//         _p1HandFluidTimes += 1;
//         if (_p1HandFluidTimes < 3 || _p1HandFluidTimes > 3) return;
//         // 18871->流体强袭 r=10+1.6
//         var dp = sa.FastDp($"流体强袭_{_p1HandFluidTimes}", evt.SourceId(), 5000, 11.6f);
//         dp.Delay = _p1HandFluidTimes switch
//         {
//             3 => 19300 - 5000,
//             // 4 => 77400,  一般不会打到这里，不管了
//             _ => 99999
//         };
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//     }
//
//     #endregion
//
//     [ScriptMethod(name: "P1-倾泻计数", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18470"], userControl: false)]
//     public void Cascade(Event evt, ScriptAccessory sa)
//     {
//         _p1CascadeTimes += 1;
//         _p1RangePos.Clear();
//         _p1Vector = new Vector3(0);
//     }
//
//     [ScriptMethod(name: "P1-进1.5禁用倾泻相关", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18480", "TargetIndex:1"], userControl: false)]
//     public void CascadeLock(Event evt, ScriptAccessory sa) => _p1CascadeTimes = 10;
//
//     [ScriptMethod(name: "P1-正方向计算，关了P1就乱指", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:11337"], userControl: false)]
//     public void AddLiquidRage(Event evt, ScriptAccessory sa)
//     {
//         lock (_p1RangePos)
//         {
//             if (_p1CascadeTimes > 5) return; // 防止在P2触发
//             _p1RangePos.Add(evt.SourcePosition() - Center);
//             if (_p1RangePos.Count != 3) return;
//             foreach (var pos in _p1RangePos)
//                 _p1Vector += pos;
//             _p1Vector = Vector3.Normalize(_p1Vector);
//
//             var myIdx = sa.MyIndex();
//             if (_p1CascadeTimes != 1) return;
//             if (new List<int>() { 2, 4, 5 }.Contains(myIdx)) return;
//             var wpos = myIdx switch
//             {
//                 0 => new Vector3(-00.0f, 0, -19.0f),
//                 1 => new Vector3(-13.5f, 0, +13.5f),
//                 3 => new Vector3(-17.0f, 0, -07.5f),
//                 6 => new Vector3(+17.0f, 0, -07.5f),
//                 7 => new Vector3(-07.5f, 0, +17.0f),
//                 _ => Vector3.Zero
//             };
//             wpos = wpos.V3YRotate(_p1Vector.V3YAngle()) + Center;
//             var dp = sa.WaypointDp(wpos, 10000);
//             sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//         }
//     }
//
//     #region 狩猎人偶
//
//     [ScriptMethod(name: "P1-狩猎人偶", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:11338"])]
//     public void JagdDoll(Event evt, ScriptAccessory sa)
//     {
//         var dollIdx = (int)((evt.SourcePosition().V3YAngle(Center) - _p1Vector.V3YAngle() + 360) % 360 / 90);
//         var myIdx = sa.MyIndex();
//         var myDollIdx = myIdx switch
//         {
//             4 => 2,
//             5 => 1,
//             6 => 0,
//             7 => 3,
//             _ => 9
//         };
//
//         var isMyDoll = false;
//         var dp = sa.Data.GetDefaultDrawProperties();
//         if (dollIdx == myDollIdx)
//         {
//             isMyDoll = true;
//             dp = sa.FastDp("狩猎人偶连线", sa.Data.Me, 6000, 5, true);
//             dp.TargetObject = evt.SourceId();
//             dp.ScaleMode = ScaleMode.YByDistance;
//             dp.Color = dp.Color.WithW(5);
//             sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Line, dp);
//         }
//
//         // 18462 castType=5 r=8+0.8
//         // 6.024  16.693  27.289
//         dp = sa.FastDp($"狩猎人偶-{evt.SourceId()}", evt.SourceId(), 4000, 8.8f, isMyDoll);
//         dp.Delay = 2000;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//         dp.Delay = 12600; // 16.693
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//         dp.Delay = 23300; // 27.289
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     // jagddoll tether 0011->player 0029->boss
//     [ScriptMethod(name: "P1-狩猎人偶清除", eventType: EventTypeEnum.Tether, eventCondition: ["Id:0029"], userControl: false)]
//     public void JagdDollClear(Event evt, ScriptAccessory sa) =>
//         sa.Method.RemoveDraw($"狩猎人偶-{evt.SourceId()}");
//
//     #endregion
//
//     
//     #region 万变水波
//
//     private void ProteanWaves(ScriptAccessory sa, uint sid, uint delay, uint times)
//     {
//         var dp = sa.FastDp("万变水波", sid, 2100, 40);
//         dp.Delay = delay;
//         dp.Radian = float.Pi / 180 * 30;
//         dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
//         for (uint i = 1; i <= times; i++)
//         {
//             dp.TargetOrderIndex = i;
//             sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//         }
//     }
//
//     [ScriptMethod(name: "P1-万变水波-活水之怒", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18869"])]
//     public void ProteanWaveRange(Event evt, ScriptAccessory sa)
//     {
//         ProteanWaves(sa, evt.SourceId(), 3000, 1);
//     }
//
//     [ScriptMethod(name: "P1-万变水波-有生命活水", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18466"])]
//     public void ProteanWaveBoss(Event evt, ScriptAccessory sa)
//     {
//         var sid = evt.SourceId();
//         var dp = sa.FastDp("万变水波", sid, 2100, 40);
//         dp.Radian = float.Pi / 180 * 30;
//         dp.Delay = 3000;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//         ProteanWaves(sa, sid, 3000, 4);
//         dp.Delay = 6100;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//         ProteanWaves(sa, sid, 6100, 4);
//
//         var myIdx = sa.MyIndex();
//         if (_p1CascadeTimes == 1)
//         {
//             var wpos1 = myIdx switch
//             {
//                 0 => new Vector3(+02.0f, 0, -02.0f),
//                 1 => new Vector3(+00.0f, 0, +02.8f),
//                 2 => new Vector3(-08.0f, 0, +12.0f),
//                 3 => new Vector3(-12.0f, 0, +08.0f),
//                 4 => new Vector3(-02.8f, 0, +00.0f),
//                 5 => new Vector3(+02.8f, 0, +00.0f),
//                 6 => new Vector3(+12.0f, 0, +08.0f),
//                 7 => new Vector3(+08.0f, 0, +12.0f),
//                 _ => Center
//             };
//             var wpos2 = myIdx switch
//             {
//                 0 => new Vector3(+04.0f, 0, -04.0f),
//                 1 => new Vector3(+00.0f, 0, +05.6f),
//                 2 => new Vector3(-02.0f, 0, +02.0f),
//                 3 => new Vector3(-02.8f, 0, +00.0f),
//                 4 => new Vector3(-04.0f, 0, -04.0f),
//                 5 => new Vector3(+04.0f, 0, -04.0f),
//                 6 => new Vector3(+02.8f, 0, +00.0f),
//                 7 => new Vector3(+02.0f, 0, +02.0f),
//                 _ => Center
//             };
//             wpos1 = wpos1.V3YRotate(_p1Vector.V3YAngle()) + Center;
//             wpos2 = wpos2.V3YRotate(_p1Vector.V3YAngle()) + Center;
//
//             dp = sa.WaypointDp(wpos1, 5100);
//             sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//             dp = sa.WaypointDp(wpos2, 3100, 5100);
//             sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//         }
//         else if (_p1CascadeTimes == 2)
//         {
//             var wpos1 = myIdx switch
//             {
//                 0 => new Vector3(+02.0f, 0, -02.0f),
//                 1 => new Vector3(+00.0f, 0, +02.8f),
//                 2 => new Vector3(-08.0f, 0, -12.0f),
//                 3 => new Vector3(-12.0f, 0, -08.0f),
//                 4 => new Vector3(-02.8f, 0, +00.0f),
//                 5 => new Vector3(+02.8f, 0, +00.0f),
//                 6 => new Vector3(+12.0f, 0, +08.0f),
//                 7 => new Vector3(+08.0f, 0, +12.0f),
//                 _ => Center
//             };
//             var wpos2 = myIdx switch
//             {
//                 0 => new Vector3(+04.0f, 0, -04.0f),
//                 1 => new Vector3(-09.0f, 0, +15.0f),
//                 2 => new Vector3(-02.8f, 0, +00.0f),
//                 3 => new Vector3(-02.0f, 0, -02.0f),
//                 4 => new Vector3(-09.0f, 0, +06.0f),
//                 5 => new Vector3(+12.0f, 0, -08.0f),
//                 6 => new Vector3(+02.8f, 0, +00.0f),
//                 7 => new Vector3(+02.0f, 0, +02.0f),
//                 _ => Center
//             };
//             wpos1 = wpos1.V3YRotate(_p1Vector.V3YAngle()) + Center;
//             wpos2 = wpos2.V3YRotate(_p1Vector.V3YAngle()) + Center;
//
//             dp = sa.WaypointDp(wpos1, 5100);
//             sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//             dp = sa.WaypointDp(wpos2, 6100, 5100);
//             sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//         }
//     }
//
//     #endregion
//
//     [ScriptMethod(name: "P1-栓塞", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:11339"])]
//     public void Embolus(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp($"栓塞-{evt.SourceId()}", evt.SourceId(), 30000, 1);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     [ScriptMethod(name: "P1-栓塞清除", eventType: EventTypeEnum.RemoveCombatant, eventCondition: ["DataId:11339"], userControl: false)]
//     public void EmbolusClear(Event evt, ScriptAccessory sa)
//     {
//         sa.Method.RemoveDraw($"栓塞-{evt.SourceId()}");
//     }
//
//     [ScriptMethod(name: "P1-排水", eventType: EventTypeEnum.Tether, eventCondition: ["Id:0003"])]
//     public void Drainage(Event evt, ScriptAccessory sa)
//     {
//         // 18471 7.3s
//         var sid = evt.SourceId();
//         var tid = evt.TargetId();
//         lock (_p1Tether)
//         {
//             if (_p1Tether.ContainsKey(sid))
//             {
//                 sa.Method.RemoveDraw($"排水 {sid} {_p1Tether[sid]}");
//                 _p1Tether[sid] = tid;
//             }
//             else
//                 _p1Tether.Add(sid, tid);
//
//             var dp = sa.FastDp($"排水 {sid} {tid}", tid, 10000, 6);
//             sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//             // dp.CentreResolvePattern = PositionResolvePatternEnum.TetherTarget;
//         }
//     }
//
//     [ScriptMethod(name: "P1-排水清除", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18471"], userControl: false)]
//     public void DrainageClear(Event evt, ScriptAccessory sa)
//     {
//         foreach (var item in _p1Tether)
//         {
//             sa.Method.RemoveDraw($"排水 {item.Key} {item.Value}");
//         }
//     }
//
//     #region 鹰式破坏炮
//
//     private static readonly List<int> P2StartDir = [4, 0, 6, 2, 3, 7, 5, 1];
//
//     [ScriptMethod(name: "P1.5-地火", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18480", "TargetIndex:1"])]
//     public void HawkBlaster(Event evt, ScriptAccessory sa)
//     {
//         // 鹰式破坏炮 间隔2.2s 10m圆形
//         _p0LimitCutEnabled = true;
//         lock (_p1HawkBlasterLocker)
//         {
//             _p1HawkBlasterTimes++;
//             HawkBlasterCalculate(evt, sa, _p1HawkBlasterTimes);
//
//             if (_p1HawkBlasterTimes == 1)
//             {
//                 var isLeft = (evt.EffectPosition().V3YAngle(Center) + 22.5f) % 360 > 180;
//                 _p1HawkBlasterVector = isLeft ? evt.EffectPosition() : evt.EffectPosition().V3YRotate(Center, 180);
//                 HawkBlasterWaypoint(sa);
//             }
//
//             if (_p1HawkBlasterTimes > 17)
//             {
//                 _p1HawkBlasterTimes = 0;
//                 var wPos = new Vector3(0, 0, 3.5f).V3YRotate(P2StartDir[sa.MyIndex()] * 45) + Center;
//                 var dp = sa.WaypointDp(wPos, 21500, 3000);
//                 sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//             }
//         }
//     }
//
//     private void HawkBlasterWaypoint(ScriptAccessory sa)
//     {
//         var wpos = _p1HawkBlasterVector;
//         if (!GetLimitCut(sa.Data.Me, out var myIdx)) return;
//         wpos = ((myIdx % 4) < 2) ? wpos : wpos.V3YRotate(Center, 180);
//
//         var dp = sa.WaypointDp(wpos, 2200);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//
//         // myIdx /= 2;
//         float rot = myIdx switch
//         {
//             0 => 270,
//             1 => 270 + 22.5f,
//             2 => 225,
//             3 => 225 + 22.5f,
//             4 => 135,
//             5 => 135 + 22.5f,
//             6 => 45,
//             7 => 45 + 22.5f,
//             _ => 0
//         };
//         uint dura = (int)(myIdx / 2) switch
//         {
//             0 => 7500,  // 7.45
//             1 => 12100, // 12.083
//             2 => 16700, // 16.726
//             3 => 21400, // 21.36
//             _ => 999999
//         };
//         wpos = wpos.V3YRotate(Center, rot);
//
//         dp = sa.WaypointDp(wpos, 5000, dura - 5000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//     }
//
//     private void HawkBlasterCalculate(Event evt, ScriptAccessory sa, uint state)
//     {
//         var nextPos = evt.EffectPosition().V3YRotate(Center, -45);
//         if (state is 7 or 8)
//             HawkBlasterDraw(sa, nextPos, 4400);
//         if (state is 17 or 8)
//             HawkBlasterDraw(sa, Center, 2200);
//         if (!new List<uint> { 7, 8, 9, 16, 17, 18 }.Contains(state))
//             HawkBlasterDraw(sa, nextPos, 2200);
//     }
//
//     private void HawkBlasterDraw(ScriptAccessory sa, Vector3 pos, uint dura)
//     {
//         var dp = sa.FastDp("鹰式破坏炮", pos, dura, 10);
//         dp.Color = sa.Data.DefaultDangerColor.WithW(0.03f);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//
//         dp.Color = sa.Data.DefaultDangerColor.WithW(3);
//         dp.ScaleMode = ScaleMode.ByTime;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     #endregion
//
//     #endregion
//
//     
//     #region P2
//
//     private uint _p2WaterTimes = 0;
//     private uint _p2LightingTimes = 0;
//     
//     private void P2Reset()
//     {
//         _p2WaterTimes = 0;
//         _p2LightingTimes = 0;
//     }
//
//     #region 传毒
//
//     // 蓝α 橙β 紫γ 绿δ
//     private static List<uint> _decreeNisis = [2222, 2223, 2137, 2138];
//     private static List<uint> _judgmentNisis = [2224, 2225, 2139, 2140];
//     private static ScriptColor[] _nisiColors = [P2Blue, P2Orange, P2Purple, P2Green];
//
//     [ScriptMethod(name: "上毒", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(222[23]|213[78])$"])]
//     public void FinalDecreeNisi(Event evt, ScriptAccessory sa)
//     {
//         sa.Method.RemoveDraw($"P2-毒-{evt.StatusId()}");
//     }
//
//     private void P2PassRot(ScriptAccessory sa, uint state)
//     {
//         foreach (var player in sa.GetParty())
//         {
//             var nisi = _decreeNisis.FirstOrDefault(x => player.HasStatus(x));
//             if (nisi is 0)
//                 continue;
//
//             var sid = player.EntityId;
//             var idx = sa.Data.PartyList.IndexOf(sid);
//             var tid = sa.Data.PartyList[idx > 3 ? idx - 4 : idx + 4];
//
//             var dp = sa.FastDp($"P2-毒-{nisi}", sid, 10000, 5);
//             dp.Color = _nisiColors[_decreeNisis.IndexOf(nisi)].V4.WithW(5);
//             dp.ScaleMode = ScaleMode.YByDistance;
//
//             if (state == 1)
//             {
//                 if (idx % 4 == 1)
//                     dp.Delay = 10000;
//             }
//             else if (state == 2)
//             {
//                 if (idx % 4 > 1)
//                 {
//                     List<int> lst = idx < 4 ? [6, 7] : [2, 3];
//                     var tplayer = sa.GetParty()
//                         .Where(x => lst.Contains(sa.Data.PartyList.IndexOf(x.EntityId)))
//                         .OrderBy(x => Vector3.Distance(player.Position, x.Position)).FirstOrDefault();
//                     if (tplayer is null)
//                         continue;
//                     tid = tplayer.EntityId;
//                 }
//             }
//             else if (state == 3)
//             {
//                 List<int> lst = idx < 4 ? [4, 5, 6, 7] : [0, 1, 2, 3];
//                 var tplayer = sa.GetParty()
//                     .FirstOrDefault(x => lst.Contains(sa.Data.PartyList.IndexOf(x.EntityId))
//                                          && x.HasStatus(_judgmentNisis[_decreeNisis.IndexOf(nisi)]));
//                 if (tplayer is null)
//                     continue;
//                 tid = tplayer.EntityId;
//                 dp.Delay = 5000;
//             }
//
//             if (P2RotsSelfOnly && sa.Data.Me != sid && sa.Data.Me != tid)
//                 continue;
//             dp.TargetObject = tid;
//
//             sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Line, dp);
//         }
//     }
//
//     [ScriptMethod(name: "一传提示", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18486"])]
//     public void P2PassRot1(Event evt, ScriptAccessory sa) => P2PassRot(sa, 1);
//
//     [ScriptMethod(name: "二传提示", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18511", "TargetIndex:1"])]
//     public void P2PassRot2(Event evt, ScriptAccessory sa) => P2PassRot(sa, 2);
//
//     [ScriptMethod(name: "三传提示", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18502", "TargetIndex:1"])]
//     public void P2PassRot3(Event evt, ScriptAccessory sa) => P2PassRot(sa, 3);
//
//     #endregion
//
//     [ScriptMethod(name: "P2-激光战轮", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18517"])]
//     public void EyeOfTheChakram(Event evt, ScriptAccessory sa)
//     {
//         // 加上目标圈
//         var dp = sa.FastDp("激光战轮", evt.SourceId(), 6000, new Vector2(6, 70 + 3));
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
//     }
//
//     [ScriptMethod(name: "P2-制导", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18481"])]
//     public void HawkBlasterP2(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("鹰式破坏炮", evt.EffectPosition(), 5000, 10);
//         dp.Color = sa.Data.DefaultDangerColor.WithW(0.03f);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//         dp.Color = sa.Data.DefaultDangerColor.WithW(3f);
//         dp.ScaleMode = ScaleMode.ByTime;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     [ScriptMethod(name: "P2-回旋碎踢", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:19058"])]
//     public void SpinCrusher(Event evt, ScriptAccessory sa)
//     {
//         // 加上目标圈
//         var dp = sa.FastDp("回旋碎踢", evt.SourceId(), 3000, 5 + 5);
//         dp.Radian = float.Pi / 180 * 120;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//     }
//
//     [ScriptMethod(name: "P2-水属性压缩", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2142"])]
//     public async void CompressedWater(Event evt, ScriptAccessory sa)
//     {
//         _p2WaterTimes++;
//         var dp = sa.FastDp("水属性压缩", evt.TargetId(), 5000, 8);
//         dp.Delay = 24000;
//         dp.Color = CompressedWaterColor.V4;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//         
//         var me = sa.GetMe();
//         var myIdx = sa.MyIndex();
//         Vector3 wpos = Center;
//         
//         await Task.Delay(1000);
//         if (me.HasStatus(2143)) return;
//         switch (_p2WaterTimes)
//         {
//             case 1:
//                 if (myIdx < 2 || myIdx == 5)
//                     return;
//                 if (myIdx < 4 && sa.Data.Me != evt.TargetId())
//                     return;
//                 wpos = new Vector3(86, 0, 100);
//                 break;
//             case 2:
//                 if (myIdx < 4 || myIdx == 5)
//                     return;
//                 wpos = new Vector3(116, 0, 100);
//                 break;
//             case 3:
//                 if (myIdx < 2 || myIdx == 5)
//                     return;
//                 if (me.HasStatus(2144)) // 水弱
//                     return;
//                 wpos = new Vector3(116, 0, 100);
//                 break;
//             default:
//                 return;
//         }
//         dp = sa.WaypointDp(wpos, 8000, 20000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, sa.Data.Me == evt.TargetId() ? DrawTypeEnum.Displacement : DrawTypeEnum.Line, dp);
//     }
//
//     [ScriptMethod(name: "P2-雷属性压缩", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2143"])]
//     public async void CompressedLightning(Event evt, ScriptAccessory sa)
//     {
//         _p2LightingTimes++;
//         var dp = sa.FastDp("雷属性压缩", evt.TargetId(), 5000, 8);
//         dp.Delay = 24000;
//         dp.Color = CompressedLightningColor.V4;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//         
//         var me = sa.GetMe();
//         var myIdx = sa.MyIndex();
//         Vector3 wpos = Center;
//         
//         await Task.Delay(1000);
//         if (me.HasStatus(2142)) return;
//         switch (_p2LightingTimes)
//         {
//             case 1:
//                 if (myIdx < 2)
//                     return;
//                 if (myIdx > 3 && sa.Data.Me != evt.TargetId())
//                     return;
//                 wpos = new Vector3(106.8f, 0, 100);
//                 break;
//             case 2:
//                 if (myIdx < 2 || myIdx > 3)
//                     return;
//                 wpos = Center;
//                 break;
//             case 3:
//                 if (myIdx == 0 || myIdx == 5)
//                     return;
//                 if (!P2ThirdLightningSt && myIdx == 1)
//                     return;
//                 if (P2ThirdLightningSt && myIdx > 3) // 雷弱
//                     return;
//                 if (me.HasStatus(2145))
//                     return;
//                 wpos = P2ThirdLightningSt ? new Vector3(90, 0, 110) : new Vector3(93.2f, 0, 100);
//                 break;
//             default:
//                 return;
//         }
//         dp = sa.WaypointDp(wpos, 8000, 20000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, sa.Data.Me == evt.TargetId() ? DrawTypeEnum.Displacement : DrawTypeEnum.Line, dp);
//     }
//
//     [ScriptMethod(name: "P2-水雷清除", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18492"], userControl: false)]
//     public void CompressedClear(Event evt, ScriptAccessory sa)
//     {
//         sa.Method.RemoveDraw("水属性压缩");
//         sa.Method.RemoveDraw("雷属性压缩");
//     }
//
//     [ScriptMethod(name: "P2-冰圈点名", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:0043"])]
//     public void IceMissile1(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("冰圈1", evt.TargetId(), 5100, 9);
//         dp.InnerScale = new Vector2(6);
//         dp.Radian = float.Pi * 2;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
//     }
//
//     [ScriptMethod(name: "P2-冰圈生成", eventType: EventTypeEnum.ObjectChanged, eventCondition: ["DataId:2004365", "Operate:Add"])]
//     public void IceMissile2(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("冰圈2", evt.SourceId(), 4000, 9);
//         dp.InnerScale = new Vector2(6);
//         dp.Radian = float.Pi * 2;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
//     }
//
//     [ScriptMethod(name: "P2-等离子护盾", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:11343"])]
//     public void PlasmaShield(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("等离子护盾", evt.SourceId(), 30000, 5, true);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//     }
//
//     [ScriptMethod(name: "P2-等离子护盾清除", eventType: EventTypeEnum.RemoveCombatant, eventCondition: ["DataId:11343"], userControl: false)]
//     public void PlasmaShieldClear(Event evt, ScriptAccessory sa)
//     {
//         sa.Method.RemoveDraw("等离子护盾");
//     }
//
//     // 18501->读条大火炎放射 18502->实际伤害大火炎放射
//     [ScriptMethod(name: "P2-大火炎放射", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18501"])]
//     public void FlareThrower(Event evt, ScriptAccessory sa)
//     {
//         // 第一个读条是3.9秒
//         var dp = sa.FastDp("大火炎放射", evt.SourceId(), 4200, 100);
//         dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
//         dp.TargetOrderIndex = 1;
//         dp.Radian = float.Pi / 2;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//     }
//
//     [ScriptMethod(name: "P2-螺旋桨强风排队提示", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18482"])]
//     public void PropellerWind(Event evt, ScriptAccessory sa)
//     {
//         var ice = sa.Data.Objects.FirstOrDefault(x => x.DataId == 0x2C81);
//         if (ice == null) return;
//         var dp = sa.FastDp("螺旋桨强风排队提示", evt.SourceId(), 6000, new Vector2(1, 40));
//         dp.TargetObject = ice.EntityId;
//         dp.Color = sa.Data.DefaultSafeColor;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);
//     }
//
//     [ScriptMethod(name: "P2-双重火箭飞拳", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18503"])]
//     public void DoubleRocketPunch(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("双重火箭飞拳", evt.TargetId(), 4000, 3);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     #endregion
//
//     
//     #region P3
//
//     private void P3Reset()
//     {
//     }
//
//     private void AlphaSword(ScriptAccessory sa, uint duration, uint delay)
//     {
//         // 18539 阿尔法之剑 扇形90 半径25 间隔1.07约成1.1
//         var cruiseChaser = sa.Data.Objects.FirstOrDefault(x => x.DataId == 11342);
//         if (cruiseChaser == null) return;
//         var dp = sa.FastDp("Alpha Sword", cruiseChaser.EntityId, duration, 25 + 5);
//         dp.Delay = delay;
//         dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
//         for (uint i = 0; i < 3; i++)
//         {
//             dp.TargetOrderIndex = i + 1;
//             dp.DestoryAt = duration + i * 1100;
//             sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//         }
//     }
//
//     private void FlareThrower(ScriptAccessory sa, uint duration, uint delay, uint times)
//     {
//         // 18540 大火炎放射 扇形90 半径100 间隔[2.141, 2.314]暂取2.3
//         var bruteJustice = sa.Data.Objects.FirstOrDefault(x => x.DataId == 11340);
//         if (bruteJustice == null) return;
//         var dp = sa.FastDp("Flare Thrower", bruteJustice.EntityId, duration, 100);
//         dp.Delay = delay;
//         dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
//         for (uint i = 0; i < times; i++)
//         {
//             dp.TargetOrderIndex = i + 1;
//             dp.DestoryAt = duration + i * 2300;
//             sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//         }
//     }
//
//     [ScriptMethod(name: "P3-过场时停", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18522"])]
//     public void TemporalStasis(Event evt, ScriptAccessory sa)
//     {
//         // 开始咏唱到上buff有9.1s
//         // 咏唱到第一刀是13s
//         AlphaSword(sa, 6900, 6100);
//         // 咏唱到第一喷是13.3s
//         FlareThrower(sa, 7200, 6100, 2);
//         // 这里时间停止了，也不需要特别准
//
//         var myself = sa.GetMe();
//         var myIdx = sa.MyIndex();
//         var isTN = myIdx < 4;
//         var cruiseChaser = sa.Data.Objects.FirstOrDefault(x => x.DataId == 11342);
//         if (cruiseChaser == null) return;
//         var ccVector = Vector3.Normalize(cruiseChaser.Position - Center);
//
//         Vector3 wpos;
//         if (myself.HasStatus(1121)) // 判决确定：加重罪 电
//             wpos = ccVector.V3YRotate(180) * 18;
//         else if (myself.HasStatus(1123)) // 判决确定：强制接近命令 近
//             wpos = new Vector3(+6, 0, isTN ? -1.5f : +1.5f);
//         else if (myself.HasStatus(1124)) // 判决确定：禁止接近命令 远
//             wpos = new Vector3(isTN ? (ccVector.X < 0 ? -16 : -18) : (ccVector.X < 0 ? 18 : 16), 0, 0);
//         else // 闲人
//             wpos = new Vector3(-6, 0, isTN ? -1.5f : +1.5f);
//
//         wpos += Center;
//         var dp = sa.WaypointDp(wpos, 9100);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//     }
//
//     
//     #region 死刑
//     
//     [ScriptMethod(name: "P3-圆形死刑", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:19072"])]
//     public void ChasteningHeat(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("神罚射线", evt.TargetId(), 5000, 5);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     [ScriptMethod(name: "P3-扇形死刑", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:19072"])]
//     public void DivineSpear(Event evt, ScriptAccessory sa)
//     {
//         // 19072 神罚射线 圆形死刑 半径5
//         // 19074 圣炎 扇形90 半径17+7.2
//         var dp = sa.FastDp("圣炎", evt.SourceId(), 9400, 17 + 7.2f);
//         dp.Delay = 5000;
//         dp.Radian = float.Pi / 2;
//         dp.TargetResolvePattern = PositionResolvePatternEnum.OwnerEnmityOrder;
//         dp.TargetOrderIndex = 1;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
//     }
//
//     #endregion
//
//     
//     #region 一运
//
//     /*
//      * 28.062 读条时空潜行阵列
//      * 35.184 亚历山大消失
//      * 37.332 亚历山大出现
//      * 53.167 结晶生成
//      */
//
//     [ScriptMethod(name: "P3-一运放结晶提示", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18543"])]
//     public void JudgmentCrystalTips(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("一运真心提示", Center, 15000, 2);
//         dp.Delay = 9300;
//         dp.Color = new Vector4(.2f, 1, 1, 1);
//         dp.TargetObject = evt.SourceId();
//         dp.ScaleMode = ScaleMode.YByDistance;
//         dp.Rotation = float.Pi;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
//
//         // 暂不可行
//         // dp.Scale = new Vector2(5);
//         // dp.Color = sa.Data.DefaultSafeColor.WithW(5);
//         // dp.Offset = new Vector3(5.5f, 0, 0);
//         // sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Line, dp);
//         // dp.Offset = new Vector3(-5.5f, 0, 0);
//         // sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Line, dp);
//     }
//     
//     [ScriptMethod(name: "P3-一运正义喷火", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18523"])]
//     public void PlayFlareThrower(Event evt, ScriptAccessory sa) => FlareThrower(sa, 4600, 15500, 3);
//     // 18527 审判结晶
//
//     [ScriptMethod(name: "P3一运后半飞机劈刀", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18527"])]
//     public void PlayAlphaSword(Event evt, ScriptAccessory sa)
//     {
//         // 18527 十字圣礼
//         if (evt.TargetIndex() != 1) return;
//         AlphaSword(sa, 5000, 1000);
//     }
//
//     [ScriptMethod(name: "P3-一运后半", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18526", "TargetIndex:1"])]
//     public void Inception(Event evt, ScriptAccessory sa)
//     {
//         // TODO 11422 为一运后半真心位移目标，亚历山大出现放十字圣礼之处
//         var alex = sa.Data.Objects.FirstOrDefault(x => x.DataId == 11422);
//         if (alex == null) return;
//         var dp = sa.FastDp("十字圣礼", alex.EntityId, 8300, new Vector2(16, 100));
//         dp.Color = SacramentColor.V4.WithW(0.5f);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
//         dp.Rotation = float.Pi / 2;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
//
//         bool isLeft;
//         var myIdx = sa.MyIndex();
//         if (myIdx < 2)
//             isLeft = true;
//         else if (myIdx < 4)
//             isLeft = false;
//         else
//             isLeft = ((IBattleChara?)sa.Data.Objects.SearchByEntityId(sa.Data.Me))?.HasStatus(1122) ?? false;
//         var wpos = new Vector3(isLeft ? -19 : +19, 0, 0).V3YRotate((Center - alex.Position).V3YAngle()) + Center;
//         dp = sa.WaypointDp(wpos, 8000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//
//         var stack = sa.GetParty().FirstOrDefault(x => x.StatusList.Any(s => s.StatusId == 1122));
//         if (stack != null)
//         {
//             dp = sa.FastDp("集团罪", stack.EntityId, 8000, 4, isLeft);
//             sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//         }
//
//         if (myIdx == 1)
//             wpos = new Vector3(119, 0, 100);
//         else if (myIdx == 2)
//             wpos = new Vector3(100, 0, 98.5f);
//         else if (myIdx == 3)
//             wpos = new Vector3(100, 0, 101.5f);
//         else if (myIdx > 3 && (sa.GetMe()?.HasStatus(1124) ?? false))
//             wpos = new Vector3(98.5f, 0, 100);
//         else
//             wpos = new Vector3(106, 0, 100);
//         dp = sa.WaypointDp(wpos, 6400, 8000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//     }
//
//     #endregion
//
//     
//     #region 二运
//
//     [ScriptMethod(name: "P3-二运飞机", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:19215"])]
//     public void LimitCutP3(Event evt, ScriptAccessory sa)
//     {
//         // 19215 限制器减档
//         P0LimitCutReset();
//         _p0LimitCutEnabled = true;
//     }
//
//     [ScriptMethod(name: "P3-二运指路一阶段", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^(00(4F|5[0123456]))$"])]
//     public void LimitCutP3Guide1(Event evt, ScriptAccessory sa)
//     {
//         if (!sa.Data.Objects.Any(x => x is IBattleChara y && y.CastActionId == 18534)) return;
//         if (evt.TargetId() != sa.Data.Me) return;
//         var myIdx = (int)evt.IconId() - 79;
//         if (myIdx < 0 || myIdx > 7) return;
//         var bruteJustice = sa.Data.Objects.FirstOrDefault(x => x.DataId == 11340);
//         if (bruteJustice == null) return;
//         // 以A为北 方便计算
//         var bjRight = bruteJustice.Position.X - 100 > 0;
//
//         var dp = myIdx switch
//         {
//             0 => sa.WaypointDp(new Vector3(bjRight ? +13 : -13, 0, -13) + Center, 9500),
//             1 => sa.WaypointDp(new Vector3(bjRight ? -13 : +13, 0, -13) + Center, 9500),
//             2 => sa.WaypointDp(new Vector3(bjRight ? +13 : -13, 0, +13) + Center, 9500),
//             3 => sa.WaypointDp(new Vector3(bjRight ? -13 : +13, 0, +13) + Center, 9500),
//             4 => sa.WaypointDp(new Vector3(bjRight ? +19 : -19, 0, +00) + Center, 3300),
//             5 => sa.WaypointDp(new Vector3(bjRight ? -19 : +19, 0, +00) + Center, 3300),
//             6 => sa.WaypointDp(new Vector3(bjRight ? +19 : -19, 0, +00) + Center, 9500),
//             7 => sa.WaypointDp(new Vector3(bjRight ? -19 : +19, 0, +00) + Center, 9500),
//             // _ => sa.WaypointDp(Center, 9500)
//         };
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//         // 9.5 到飞机第一刀
//         // 忏悔区大小依次为 8 6 3？
//     }
//
//     [ScriptMethod(name: "P3-二运指路二阶段", eventType: EventTypeEnum.ObjectChanged, eventCondition: ["DataId:2007519", "Operate:Add"])]
//     public void LimitCutP3Guide2(Event evt, ScriptAccessory sa)
//     {
//         // 6.226 到飞机第一刀
//         if (!GetLimitCut(sa.Data.Me, out var myIdx)) return;
//         var pos = evt.SourcePosition();
//         var objRight = pos.X - 100 > 0;
//         var bruteJustice = sa.Data.Objects.FirstOrDefault(x => x.DataId == 11340);
//         if (bruteJustice == null) return;
//         // 以A为北 方便计算
//         var bjRight = bruteJustice.Position.X - 100 > 0;
//
//         if ((myIdx % 2 == 0) != (bjRight == objRight)) return;
//         // 玩家是否同侧 与 正义灵泉是否同侧  不同则停止
//         DrawPropertiesEdit dp1, dp2;
//         switch (myIdx)
//         {
//             case 0:
//                 dp1 = sa.WaypointDp(new Vector3(bjRight ? +19 : -19, 0, +00) + Center, 8600, 6300);
//                 dp2 = sa.WaypointDp(new Vector3(MathF.Sign(pos.X - 100) * 13 + 100, 0, pos.Z), 4200, 14900);
//                 break;
//             case 1:
//                 dp1 = sa.WaypointDp(new Vector3(bjRight ? -19 : +19, 0, +00) + Center, 8600, 6300);
//                 dp2 = sa.WaypointDp(new Vector3(MathF.Sign(pos.X - 100) * 13 + 100, 0, pos.Z), 4200, 14900);
//                 break;
//             case 2:
//                 dp1 = sa.WaypointDp(new Vector3(bjRight ? +19 : -19, 0, +00) + Center, 4200, 10700);
//                 dp2 = dp1;
//                 dp2.DestoryAt = 1;
//                 break;
//             case 3:
//                 dp1 = sa.WaypointDp(new Vector3(bjRight ? -19 : +19, 0, +00) + Center, 4200, 10700);
//                 dp2 = dp1;
//                 dp2.DestoryAt = 1;
//                 break;
//             case 4:
//                 // dp1 = sa.WaypointDp(new Vector3(MathF.Sign(pos.X - 100) * 18 + 100, 0, pos.Z), 10700);
//                 dp1 = sa.WaypointDp(new Vector3(MathF.Sign(pos.X - 100) * 17 + 100, 0, 100), 10700);
//                 dp2 = sa.WaypointDp(new Vector3(bjRight ? +13 : -13, 0, -13) + Center, 4200, 10700);
//                 break;
//             case 5:
//                 // dp1 = sa.WaypointDp(new Vector3(MathF.Sign(pos.X - 100) * 18 + 100, 0, pos.Z), 10700);
//                 dp1 = sa.WaypointDp(new Vector3(MathF.Sign(pos.X - 100) * 17 + 100, 0, 100), 10700);
//                 dp2 = sa.WaypointDp(new Vector3(bjRight ? -13 : +13, 0, -13) + Center, 4200, 10700);
//                 break;
//             case 6:
//                 // dp1 = sa.WaypointDp(new Vector3(MathF.Sign(pos.X - 100) * 16 + 100, 0, pos.Z), 4200, 10700);
//                 dp1 = sa.WaypointDp(new Vector3(MathF.Sign(pos.X - 100) * 16 + 100, 0, MathF.Sign(pos.Z - 100) * 3 + 100), 4200, 10700);
//                 dp2 = sa.WaypointDp(new Vector3(bjRight ? +13 : -13, 0, +13) + Center, 4200, 14900);
//                 break;
//             case 7:
//                 // dp1 = sa.WaypointDp(new Vector3(MathF.Sign(pos.X - 100) * 16 + 100, 0, pos.Z), 4200, 10700);
//                 dp1 = sa.WaypointDp(new Vector3(MathF.Sign(pos.X - 100) * 16 + 100, 0, MathF.Sign(pos.Z - 100) * 3 +100), 4200, 10700);
//                 dp2 = sa.WaypointDp(new Vector3(bjRight ? -13 : +13, 0, +13) + Center, 4200, 14900);
//                 break;
//             default:
//                 return;
//         }
//
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp1);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp2);
//     }
//
//     [ScriptMethod(name: "P3-二运十字圣礼", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18519"])]
//     public void Sacrament(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("十字圣礼", evt.SourceId(), 6000, new Vector2(16, 200));
//         dp.Color = SacramentColor.V4.WithW(0.5f);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
//         dp.Rotation = float.Pi / 2;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
//     }
//
//     [ScriptMethod(name: "P3-二运后分摊", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:19025"])]
//     public void IncineratingHeat(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("净化射线", evt.TargetId(), 5000, 5);
//         dp.Color = sa.Data.DefaultSafeColor;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     #endregion
//
//     
//     #endregion
//
//     
//     #region P4
//
//     private uint _p4FinalWordLightPlayer;
//     private uint _p4FinalWordDarkPlayer;
//     private Dictionary<uint, uint> _p4ShadowDict = new();
//     private List<uint> _p4ShadowPlayers = [];
//     private bool[] _p4OrdainList = [false, false];
//     private readonly List<Vector3> _p4AlmightyJudgments = [];
//
//     private void P4Reset()
//     {
//         _p4FinalWordLightPlayer = 0;
//         _p4FinalWordDarkPlayer = 0;
//         _p4ShadowDict.Clear();
//         _p4ShadowPlayers.Clear();
//         _p4OrdainList = [false, false];
//         _p4AlmightyJudgments.Clear();
//     }
//
//     
//     #region 开场
//
//     [ScriptMethod(name: "P4-开场大光", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2153"])]
//     public void FinalWordContactRegulation(Event evt, ScriptAccessory sa)
//     {
//         _p4FinalWordLightPlayer = evt.TargetId();
//         if (evt.TargetId() != sa.Data.Me) return;
//
//         var dp = sa.WaypointDp(new Vector3(100, 0, 81), 10000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//     }
//
//     [ScriptMethod(name: "P4-开场大暗", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2155"])]
//     public void FinalWordEscapeDetection(Event evt, ScriptAccessory sa)
//     {
//         _p4FinalWordDarkPlayer = evt.TargetId();
//         if (evt.TargetId() != sa.Data.Me) return;
//
//         var dp = sa.WaypointDp(new Vector3(100, 0, 114), 10000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//     }
//
//     [ScriptMethod(name: "P4-开场小光", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2152"])]
//     public async void FinalWordContactProhibition(Event evt, ScriptAccessory sa)
//     {
//         if (evt.TargetId() != sa.Data.Me) return;
//
//         var dp = sa.WaypointDp(new Vector3(100, 0, 112), 10000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//
//         await Task.Delay(100);
//         if (_p4FinalWordLightPlayer == 0) return;
//
//         dp = sa.FastDp("小光", evt.TargetId(), 9900, new Vector2(2, 20), true);
//         dp.TargetObject = _p4FinalWordLightPlayer;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);
//
//         dp = sa.FastDp("大光", _p4FinalWordLightPlayer, 9900, 22);
//         dp.InnerScale = new Vector2(21.8f);
//         dp.Radian = float.Pi * 2;
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Donut, dp);
//     }
//
//     [ScriptMethod(name: "P4-开场小暗", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2154"])]
//     public async void FinalWordEscapeProhibition(Event evt, ScriptAccessory sa)
//     {
//         if (evt.TargetId() != sa.Data.Me) return;
//
//         var dp = sa.WaypointDp(new Vector3(100, 0, 112), 10000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//
//         await Task.Delay(100);
//         if (_p4FinalWordDarkPlayer == 0) return;
//
//         dp = sa.FastDp("小暗", evt.TargetId(), 9900, new Vector2(2, 20), true);
//         dp.TargetObject = _p4FinalWordDarkPlayer;
//         dp.Rotation = float.Pi;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);
//
//         dp = sa.FastDp("大暗", _p4FinalWordDarkPlayer, 9900, 5, true);
//         dp.InnerScale = new Vector2(4.8f);
//         dp.Radian = float.Pi * 2;
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Donut, dp);
//     }
//
//     [ScriptMethod(name: "P4-开场动提示", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18558"])]
//     public void OrdainedMotion(Event evt, ScriptAccessory sa) => sa.Method.TextInfo("动", 4000, true);
//
//     [ScriptMethod(name: "P4-开场静提示", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18559"])]
//     public void OrdainedStillness(Event evt, ScriptAccessory sa) => sa.Method.TextInfo("静", 4000, true);
//
//     #endregion
//
//     
//     #region 幻影记录
//
//     [ScriptMethod(name: "P4幻影连线记录", eventType: EventTypeEnum.Tether, eventCondition: ["Id:0062"], userControl: false)]
//     public void ShadowsRecord(Event evt, ScriptAccessory sa)
//     {
//         lock (_p4ShadowDict)
//         {
//             _p4ShadowDict.Add(evt.SourceId(), evt.TargetId());
//             if (_p4ShadowDict.Count > 7)
//             {
//                 _p4ShadowDict = _p4ShadowDict.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
//                 _p4ShadowPlayers = _p4ShadowDict.Keys.ToList();
//             }
//         }
//     }
//
//     [ScriptMethod(name: "P4幻影连线记录清除", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18578"], userControl: false)]
//     public void ShadowsReset(Event evt, ScriptAccessory sa)
//     {
//         _p4ShadowDict.Clear();
//         _p4ShadowPlayers.Clear();
//     }
//
//     private bool P4GetIndex(uint id, out int index)
//     {
//         if (!_p4ShadowPlayers.Contains(id))
//         {
//             index = 0;
//             return false;
//         }
//
//         index = _p4ShadowPlayers.IndexOf(id);
//         return true;
//     }
//
//     #endregion
//
//     
//     #region 一测
//
//     [ScriptMethod(name: "P4-一测一阶段", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18556"])]
//     public void FateCalibrationAlpha(Event evt, ScriptAccessory sa)
//     {
//         // 未来确定α
//         // 0为分摊 1为大圈 234为电 567无
//         if (!P4GetIndex(sa.Data.Me, out var myidx)) return;
//         var wpos = myidx == 1 ? new Vector3(100, 0, 81) : new Vector3(100, 0, 119);
//
//         var dp = sa.WaypointDp(wpos, 10000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//
//         dp = sa.FastDp("一测大圈", _p4ShadowPlayers[1], 32200, 30);
//         dp.InnerScale = new Vector2(29.8f);
//         dp.Rotation = float.Pi * 2;
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Donut, dp);
//
//         var isStack = myidx < 1 || myidx > 4;
//         dp = sa.FastDp("一测分摊", _p4ShadowPlayers[0], 32200, 4, isStack);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     [ScriptMethod(name: "P4-一测二阶段", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18591", "TargetIndex:1"])]
//     public void AlphaSacrament(Event evt, ScriptAccessory sa)
//     {
//         // 18591->幻影十字圣礼 18569->十字圣礼 间隔18.1
//         var dp = sa.FastDp("1测十字圣礼", evt.SourceId(), 18100, new Vector2(16, 100));
//         dp.Color = SacramentColor.V4.WithW(0.5f);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
//         dp.Rotation = float.Pi / 2;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
//         // (78.29|91.01|0.00) (108.99|78.29|0.00) (121.71|91.01|0.00)
//
//         var pos = evt.SourcePosition() - Center;
//         if (pos.Z > -15) return;
//         pos = Vector3.Normalize(pos.WithX(-pos.X)) * 18;
//         Vector3 wpos = pos + Center;
//         if (!P4GetIndex(sa.Data.Me, out var myidx)) return;
//         switch (myidx)
//         {
//             case 0 or 5 or 6 or 7:
//                 wpos = Vector3.Transform(pos, Matrix4x4.CreateRotationY(float.Pi / 180 * -165)) + Center;
//                 break;
//             case 2 or 3 or 4:
//                 wpos = Vector3.Transform(pos, Matrix4x4.CreateRotationY(float.Pi / 180 * 165)) + Center;
//                 break;
//             case 1:
//                 break;
//         }
//
//         dp = sa.WaypointDp(wpos, 18100);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//     }
//
//     [ScriptMethod(name: "P4-一测动静提示", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(1921[34]|1858[56])$", "TargetIndex:1"])]
//     public void FateCalibrationAlphaOrdain(Event evt, ScriptAccessory sa)
//     {
//         string str;
//         switch (evt.ActionId())
//         {
//             case 19213:
//                 _p4OrdainList[0] = true;
//                 break;
//             case 19214:
//                 _p4OrdainList[0] = false;
//                 break;
//             case 18585:
//                 _p4OrdainList[1] = true;
//                 str = $"一{(_p4OrdainList[0] ? "动" : "静")}，二{(_p4OrdainList[1] ? "动" : "静")}";
//                 sa.Method.TextInfo(str, 5000, true);
//                 sa.Method.SendChat("/e " + str);
//                 break;
//             case 18586:
//                 _p4OrdainList[1] = false;
//                 str = $"一{(_p4OrdainList[0] ? "动" : "静")}，二{(_p4OrdainList[1] ? "动" : "静")}";
//                 sa.Method.TextInfo(str, 5000, true);
//                 sa.Method.SendChat("/e " + str);
//                 break;
//         }
//     }
//
//     #endregion
//
//     
//     #region 二测
//
//     [ScriptMethod(name: "P4-二测指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:19220"])]
//     public void FateCalibrationBeta(Event evt, ScriptAccessory sa)
//     {
//         if (!P4GetIndex(sa.Data.Me, out var myIdx)) return;
//         var isLight = myIdx % 2 > 0;
//         // 0为大暗 1为大光 2小暗 3小光 4近线小暗 5近小光 6远线小暗 7远小光
//         // 怀疑分摊在3
//         DrawPropertiesEdit dp;
//
//         if (myIdx > 1)
//         {
//             if (isLight)
//             {
//                 dp = sa.FastDp("2测小光", sa.Data.Me, 40000, new Vector2(2, 20), true);
//                 dp.TargetObject = _p4ShadowPlayers[1];
//                 sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);
//
//                 dp = sa.FastDp("2测大光", _p4ShadowPlayers[1], 40000, 22);
//                 dp.InnerScale = new Vector2(21.8f);
//                 dp.Radian = float.Pi * 2;
//                 sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Donut, dp);
//             }
//             else
//             {
//                 dp = sa.FastDp("2测小暗", sa.Data.Me, 40000, new Vector2(2, 20), true);
//                 dp.TargetObject = _p4ShadowPlayers[0];
//                 dp.Rotation = float.Pi;
//                 sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);
//
//                 dp = sa.FastDp("2测大暗", _p4ShadowPlayers[0], 40000, 5, true);
//                 dp.InnerScale = new Vector2(4.8f);
//                 dp.Radian = float.Pi * 2;
//                 sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Donut, dp);
//             }
//         }
//
//         var wpos = myIdx switch
//         {
//             0 => new Vector3(119.0f, 0, 100.0f),
//             1 => new Vector3(093.8f, 0, 083.1f),
//             2 => new Vector3(116.0f, 0, 100.0f),
//             6 => new Vector3(116.0f, 0, 101.7f),
//             _ => new Vector3(116.0f, 0, 098.3f),
//         };
//         dp = sa.WaypointDp(wpos, 40000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//
//         dp = sa.FastDp("连带神判", _p4ShadowPlayers[3], 7200, 4, true);
//         dp.Delay = 40000;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//
//         switch (myIdx)
//         {
//             case 0:
//                 dp = sa.WaypointDp(new Vector3(119, 0, 100), 8200);
//                 dp.Delay = 40000;
//                 sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//                 break;
//             case 2:
//                 dp = sa.WaypointDp(new Vector3(81, 0, 100), 8200);
//                 dp.Delay = 40000;
//                 sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//                 break;
//             case 6:
//                 dp = sa.WaypointDp(new Vector3(100, 0, 119), 8200);
//                 dp.Delay = 40000;
//                 sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//                 break;
//         }
//     }
//
//     [ScriptMethod(name: "P4-二测超级跳", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18589", "TargetIndex:1"])]
//     public void BetaJJump(Event evt, ScriptAccessory sa)
//     {
//         // 18589->幻影正义之跃 18565->正义之跃 间隔33.027
//         var dp = sa.FastDp("正义之跃", evt.SourceId(), 5000, 10);
//         dp.Delay = 28000;
//         dp.CentreResolvePattern = PositionResolvePatternEnum.PlayerFarestOrder;
//         dp.CentreOrderIndex = 1;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     [ScriptMethod(name: "P4-二测分散", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18592", "TargetIndex:1"])]
//     public void BetaSpread(Event evt, ScriptAccessory sa)
//     {
//         // 18592->幻影制导动画技能分散 18861->制导动画技能 间隔33.613
//         var dp = sa.FastDp("2测分散", 0, 6100, 6);
//         dp.Delay = 27500;
//         foreach (var aid in sa.Data.PartyList)
//         {
//             dp.Owner = aid;
//             sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//         }
//     }
//
//     [ScriptMethod(name: "P4-二测分摊及指路", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18593", "TargetIndex:1"])]
//     public void BetaStack(Event evt, ScriptAccessory sa)
//     {
//         // 18593->幻影制导动画技能分摊 18862->制导动画技能
//         if (!P4GetIndex(sa.Data.Me, out var myIdx)) return;
//         var isLight = myIdx % 2 > 0;
//
//         var dp = sa.FastDp("2测分摊", _p4ShadowPlayers[0], 6100, 6, !isLight);
//         dp.Delay = 27500;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//
//         dp = sa.FastDp("2测分摊", _p4ShadowPlayers[1], 6100, 6, isLight);
//         dp.Delay = 27500;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//
//         if (myIdx < 2) return;
//         dp = sa.WaypointDp(_p4ShadowPlayers[isLight ? 1 : 0], 6100);
//         dp.Delay = 27500;
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//     }
//
//     [ScriptMethod(name: "P4-二测月环及指路", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:18590", "TargetIndex:1"])]
//     public void BetaDonut(Event evt, ScriptAccessory sa)
//     {
//         // 18590->幻影拜火圣礼 18566->拜火圣礼 月环
//         var dp = sa.FastDp("2测月环", evt.SourceId(), 5000, 60);
//         dp.Delay = 28000;
//         dp.InnerScale = new Vector2(8);
//         dp.Radian = float.Pi * 2;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
//
//         dp = sa.WaypointDp(evt.SourceId(), 5000);
//         dp.Delay = 28000;
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//     }
//
//     #endregion
//
//     
//     #region 死刑
//
//     [ScriptMethod(name: "P4-死刑", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18578"])]
//     public void OrdainedCapitalPunishment(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("加重诛罚", evt.SourceId(), 8300, 4);
//         dp.CentreResolvePattern = PositionResolvePatternEnum.OwnerEnmityOrder;
//         dp.CentreOrderIndex = 1;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     [ScriptMethod(name: "P4-易伤死刑", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18577"])]
//     public void OrdainedPunishment(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("诛罚", evt.TargetId(), 5000, 5);
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     #endregion
//
//     
//     #region 地火
//
//     [ScriptMethod(name: "P4-地火分摊范围", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18580"])]
//     public void IrresistibleGrace(Event evt, ScriptAccessory sa)
//     {
//         var dp = sa.FastDp("株连", evt.TargetId(), 5000, 6, true);
//         dp.InnerScale = new Vector2(5.8f);
//         dp.Radian = float.Pi * 2;
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Donut, dp);
//     }
//
//     private static readonly List<Vector3> FirePos = [new(92, 0, 108), new(100, 0, 108), new(108, 0, 108)];
//     private static readonly Vector3 AnotherFirePos = new(108, 0, 100);
//
//     private static bool AlmostEqual(Vector3 v1, Vector3 v2) => Vector3.Distance(v1, v2) < 1;
//
//     [ScriptMethod(name: "P4-地火清除", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18574"], userControl: false)]
//     public void AlmightyJudgmentClear(Event evt, ScriptAccessory sa) => _p4AlmightyJudgments.Clear();
//
//     [ScriptMethod(name: "P4-地火预兆", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18575"])]
//     public void AlmightyJudgment(Event evt, ScriptAccessory sa)
//     {
//         // 从演示咏唱到实际判定约8s 两次地火间隔是2s
//         // 实际判定为 18576
//         var dp = sa.FastDp("神圣大审判", evt.EffectPosition(), 2000, 6);
//         dp.Delay = 6000;
//         sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
//     }
//
//     [ScriptMethod(name: "P4-地火指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:18575"])]
//     public void AlmightyJudgmentGuide(Event evt, ScriptAccessory sa)
//     {
//         var pos = evt.EffectPosition();
//         foreach (var fire in FirePos)
//         {
//             if (!AlmostEqual(pos, fire)) continue;
//             _p4AlmightyJudgments.Add(fire);
//             if (_p4AlmightyJudgments.Count == 2)
//                 AlmightyJudgmentGuider(sa);
//         }
//     }
//
//     private void AlmightyJudgmentGuider(ScriptAccessory sa)
//     {
//         Vector3 wpos1, wpos2;
//         if (AlmostEqual(_p4AlmightyJudgments[1], FirePos[1]))
//         {
//             wpos1 = AlmostEqual(_p4AlmightyJudgments[0], FirePos[0]) ? FirePos[2] : AnotherFirePos;
//             wpos2 = AlmostEqual(_p4AlmightyJudgments[0], FirePos[0]) ? AnotherFirePos : FirePos[2];
//         }
//         else if (AlmostEqual(_p4AlmightyJudgments[0], FirePos[1]))
//         {
//             wpos1 = AlmostEqual(_p4AlmightyJudgments[1], FirePos[0]) ? FirePos[2] : FirePos[0];
//             wpos2 = FirePos[1];
//         }
//         else
//         {
//             wpos1 = FirePos[1];
//             wpos2 = _p4AlmightyJudgments[0];
//         }
//
//         var dp = sa.WaypointDp(wpos1, 6000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//
//         dp = sa.WaypointDp(wpos2, 4000, 6000);
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//
//         dp = sa.FastDp("地火预指路", wpos1, 6000, 2);
//         dp.TargetPosition = wpos2;
//         dp.ScaleMode = ScaleMode.YByDistance;
//         sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
//     }
//
//     #endregion
//     
//     #endregion
// }
//
//
// #region Helpers
//
// public static class EventExtensions
// {
//     private static bool ParseHexId(string? idStr, out uint id)
//     {
//         id = 0;
//         if (string.IsNullOrEmpty(idStr)) return false;
//         try
//         {
//             var idStr2 = idStr.Replace("0x", "");
//             id = uint.Parse(idStr2, System.Globalization.NumberStyles.HexNumber);
//             return true;
//         }
//         catch (Exception)
//         {
//             return false;
//         }
//     }
//
//     public static uint Id(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<uint>(evt["Id"]);
//     }
//
//     public static uint ActionId(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<uint>(evt["ActionId"]);
//     }
//
//     public static uint SourceId(this Event evt)
//     {
//         return ParseHexId(evt["SourceId"], out var id) ? id : 0;
//     }
//
//     public static uint TargetId(this Event evt)
//     {
//         return ParseHexId(evt["TargetId"], out var id) ? id : 0;
//     }
//
//     public static uint IconId(this Event evt)
//     {
//         return ParseHexId(evt["Id"], out var id) ? id : 0;
//     }
//
//     public static Vector3 SourcePosition(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<Vector3>(evt["SourcePosition"]);
//     }
//
//     public static Vector3 TargetPosition(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<Vector3>(evt["TargetPosition"]);
//     }
//
//     public static Vector3 EffectPosition(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<Vector3>(evt["EffectPosition"]);
//     }
//
//     public static float SourceRotation(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<float>(evt["SourceRotation"]);
//     }
//
//     public static float TargetRotation(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<float>(evt["TargetRotation"]);
//     }
//
//     public static string SourceName(this Event evt)
//     {
//         return evt["SourceName"];
//     }
//
//     public static string TargetName(this Event evt)
//     {
//         return evt["TargetName"];
//     }
//
//     public static uint DurationMilliseconds(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<uint>(evt["DurationMilliseconds"]);
//     }
//
//     public static uint Index(this Event evt)
//     {
//         return ParseHexId(evt["Index"], out var id) ? id : 0;
//     }
//
//     public static uint State(this Event evt)
//     {
//         return ParseHexId(evt["State"], out var id) ? id : 0;
//     }
//
//     public static uint DirectorId(this Event evt)
//     {
//         return ParseHexId(evt["DirectorId"], out var id) ? id : 0;
//     }
//
//     public static uint StatusId(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<uint>(evt["StatusID"]);
//     }
//
//     public static uint StackCount(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<uint>(evt["StackCount"]);
//     }
//
//     public static uint Param(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<uint>(evt["Param"]);
//     }
//
//     public static uint TargetIndex(this Event evt)
//     {
//         return JsonConvert.DeserializeObject<uint>(evt["TargetIndex"]);
//     }
// }
//
// public static class ScriptAccessoryExtensions
// {
//     public static DrawPropertiesEdit FastDp(this ScriptAccessory sa, string name, uint owner, uint duration, float radius, bool safe = false)
//     {
//         return FastDp(sa, name, owner, duration, new Vector2(radius), safe);
//     }
//
//     public static DrawPropertiesEdit FastDp(this ScriptAccessory sa, string name, uint owner, uint duration, Vector2 scale, bool safe = false)
//     {
//         var dp = sa.Data.GetDefaultDrawProperties();
//         dp.Name = name;
//         dp.Color = safe ? sa.Data.DefaultSafeColor : sa.Data.DefaultDangerColor;
//         dp.Owner = owner;
//         dp.DestoryAt = duration;
//         dp.Scale = scale;
//         return dp;
//     }
//
//     public static DrawPropertiesEdit FastDp(this ScriptAccessory sa, string name, Vector3 pos, uint duration, float radius, bool safe = false)
//     {
//         return FastDp(sa, name, pos, duration, new Vector2(radius), safe);
//     }
//
//     public static DrawPropertiesEdit FastDp(this ScriptAccessory sa, string name, Vector3 pos, uint duration, Vector2 scale, bool safe = false)
//     {
//         var dp = sa.Data.GetDefaultDrawProperties();
//         dp.Name = name;
//         dp.Color = safe ? sa.Data.DefaultSafeColor : sa.Data.DefaultDangerColor;
//         dp.Position = pos;
//         dp.DestoryAt = duration;
//         dp.Scale = scale;
//         return dp;
//     }
//
//     public static DrawPropertiesEdit WaypointDp(this ScriptAccessory sa, uint target, uint duration, uint delay = 0, string name = "Waypoint")
//     {
//         var dp = sa.Data.GetDefaultDrawProperties();
//         dp.Name = name;
//         dp.Color = sa.Data.DefaultSafeColor;
//         dp.Owner = sa.Data.Me;
//         dp.TargetObject = target;
//         dp.DestoryAt = duration;
//         dp.Delay = delay;
//         dp.Scale = new Vector2(2);
//         dp.ScaleMode = ScaleMode.YByDistance;
//         return dp;
//     }
//
//     public static DrawPropertiesEdit WaypointDp(this ScriptAccessory sa, Vector3 pos, uint duration, uint delay = 0, string name = "Waypoint")
//     {
//         var dp = sa.Data.GetDefaultDrawProperties();
//         dp.Name = name;
//         dp.Color = sa.Data.DefaultSafeColor;
//         dp.Owner = sa.Data.Me;
//         dp.TargetPosition = pos;
//         dp.DestoryAt = duration;
//         dp.Delay = delay;
//         dp.Scale = new Vector2(2);
//         dp.ScaleMode = ScaleMode.YByDistance;
//         return dp;
//     }
//
//     public static int MyIndex(this ScriptAccessory sa)
//     {
//         return sa.Data.PartyList.IndexOf(sa.Data.Me);
//     }
//
//     public static IEnumerable<IPlayerCharacter> GetParty(this ScriptAccessory sa)
//     {
//         foreach (var pid in sa.Data.PartyList)
//         {
//             var obj = sa.Data.Objects.SearchByEntityId(pid);
//             if (obj is IPlayerCharacter character) yield return character;
//         }
//     }
//
//     public static IPlayerCharacter? GetMe(this ScriptAccessory sa)
//     {
//         return (IPlayerCharacter?)sa.Data.Objects.SearchByEntityId(sa.Data.Me);
//     }
// }
//
// public static class IbcHelper
// {
//     public static bool HasStatus(this IBattleChara chara, uint statusId)
//     {
//         return chara.StatusList.Any(x => x.StatusId == statusId);
//     }
//
//     public static bool HasStatus(this IBattleChara chara, uint[] statusIds)
//     {
//         return chara.StatusList.Any(x => statusIds.Contains(x.StatusId));
//     }
// }
//
// public static class MathHelper
// {
//     public static float V3YAngle(this Vector3 v, bool toRadian = false)
//     {
//         return V3YAngle(v, Vector3.Zero, toRadian);
//     }
//
//     public static float V3YAngle(this Vector3 v, Vector3 origin, bool toRadian = false)
//     {
//         var angle = ((MathF.Atan2(v.Z - origin.Z, v.X - origin.X) - MathF.Atan2(1, 0)) / float.Pi * -180 + 360) % 360;
//         return toRadian ? angle / 180 * float.Pi : angle;
//     }
//
//     public static Vector3 V3YRotate(this Vector3 v, float angle, bool isRadian = false)
//     {
//         return V3YRotate(v, Vector3.Zero, angle, isRadian);
//     }
//
//     public static Vector3 V3YRotate(this Vector3 v, Vector3 origin, float angle, bool isRadian = false)
//     {
//         var radian = isRadian ? angle : angle / 180 * float.Pi;
//         return Vector3.Transform(v - origin, Matrix4x4.CreateRotationY(radian)) + origin;
//     }
// }
//
// #endregion

