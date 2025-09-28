using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Triggernometry;
using Triggernometry.PluginBridges;
using static Triggernometry.Action.ActionTypeEnum;
using static Triggernometry.FFXIV.Entity;
using static Triggernometry.FFXIV.Job.RoleType;
using static Triggernometry.Interpreter.StaticHelpers;
using static Triggernometry.RealPlugin;
using Action = Triggernometry.Action;
using Entity = Triggernometry.FFXIV.Entity;

public class LatihasTUW:ITrnNamedCallback
{
    private static string MyJob, MyName, hs, ttsafe, hsadd;
    private static bool Ubroadcast, Uthreebucket;
    private static Vector2 Zhuzi2;
    private static Ciyu ciyu;
    private static List<DHP43> p43dhDist = [];
    private static int p43dhCount, p43qdporder;
    private static readonly Player[] Players = new Player[8];
    private static readonly List<string> P43PlayerName = [];
    private static readonly List<Player> Threebuckets = [];
    private static readonly Trigger _tri = new();
    private static readonly List<Zhuzi> Zhuzis = [];
    private static readonly string[] JobOrder = ["MT", "ST", "H1", "H2", "D1", "D2", "D3", "D4"],
                                     TBJobOrder = ["MT", "ST", "D1", "D2", "D3", "D4", "H1", "H2"],
                                     Server =
                                     [
                                         "红玉海", "神意之地", "拉诺西亚", "幻影群岛", "萌芽池", "宇宙和音", "沃仙曦染", "晨曦王座", "白银乡", "白金幻象", "神拳痕", "潮风亭", "旅人栈桥", "拂晓之间", "龙巢神殿", "梦羽宝境", "紫水栈桥", "延夏", "静语庄园", "摩杜纳", "海猫茶屋", "柔风海湾", "琥珀原", "水晶塔", "银泪湖", "伊修加德", "太阳海岸", "红茶川",
                                         "Anima", "Belias", "Chocobo", "Hades", "Ixion", "Mandragora", "Masamune", "Pandaemonium", "Shinryu", "Titan", "Asura"
                                     ];

    private static void Place(string expr)
    {
        try
        {
            var sb = new StringBuilder("{");
            foreach (var s in expr.Split(';'))
            {
                var parts = s.Split(':');
                var p0 = parts[0];
                var p1 = parts[1];
                var name = p0 switch { "1" => "One", "2" => "Two", "3" => "Three", "4" => "Four", _ => p0 };
                if (p1 == "clear") sb.Append($"\"{name}\":{{}},");
                else
                {
                    var xy = p1.Split(',');
                    sb.Append($"\"{name}\":{{\"X\":{xy[0]},\"Z\":{xy[1]},\"Y\":0,\"Active\":true}},");
                }
            }
            plug.InvokeNamedCallback("place", sb.Append("}").ToString());
        }
        catch (Exception ex)
        {
            Log($"Place Error({expr}):{ex.StackTrace}");
        }
    }

    public void Callback(object _, string str)
    {
        try
        {
            var ss = str.Split(':');
            var p = ss[0].ToLower();
            if (p == "p0init") p0init(str);
            else if (ss.Length == 1)
            {
                if (p == "initplace") Place(StaticPlace.initPlace);
                else if (p == "clear2") Place(StaticPlace.clear2);
                else if (p == "p41place2") Place(StaticPlace.p41place2);
                else if (p == "p42place2") Place(StaticPlace.p42place2);
                else if (p == "p42place4d3") Place(StaticPlace.p42place4d3);
                else if (p == "p42place2rf") Place(StaticPlace.p42place2rf);
                else if (p == "p43place") Place(StaticPlace.startp43);
                else if (p == "p43place2") Place(StaticPlace.p43_2);
                else if (p == "ydsx") ydsx();
                else if (p == "p1place") p1place();
                else if (p == "p1ciyucs")
                    lock (ciyu)
                        ciyu.cs2 = true;
                else if (p == "p1fshp1") BelowHP(8722, 0.2, "请注意风神血量", "stop1");
                else if (p == "p1fshp2") BelowHP(8722, 0.15, "请注意风神血量", "stop2");
                else if (p == "p1fs1")
                {
                    var es = GetXYFromBnpcid(8723);
                    if (es.Count != 2) return;
                    FsPlace(GetDir(es[0]), GetDir(es[1]));
                }
                else if (p == "p1fs2") FsPlace("左西", "右东");
                else if (p == "p2zhuzi23") p2zhuzi23();
                else if (p == "p3lb") BelowHP(8727, 0.15, "请注意土神血量和LB槽", "stop2");
                else if (p == "p41") p41();
                else if (p == "p42hs") Place(GetDir(GetXYFromBnpcid(8730, 1)[0]) switch { "左上西北" or "右下东南" => "4:112,112", "左下西南" or "右上东北" => "4:88,112" });
                else if (p == "p43qdpclear" && p43qdporder != -1) plug.InvokeNamedCallback("command", $"/mk stop1 <{p43qdporder}>");
                else if (p == "jzha")
                {
                    Place(StaticPlace.p5jzhA);
                    Broadcast("A点", true);
                }
                else if (p == "jzh2")
                {
                    Place(StaticPlace.p5jzh2);
                    Broadcast("2点", true);
                }
                else if (p == "jzh3")
                {
                    Place(StaticPlace.p5jzh3);
                    Broadcast("3点", true);
                }
            }
            else
            {
                var a = ss[1];
                if (p == "broadcast") Broadcast(a);
                else if (p == "tts") PostTTS(a);
                else if (p == "rf") rf(a);
                else if (p == "xfzd")
                    Task.Run(async delegate
                    {
                        await Task.Delay(((int)float.Parse(a) - 3) * 1000);
                        PostTTS("准备爆炸");
                    });
                else if (p == "death") Death(a);
                else if (p == "p1ciyu")
                    lock (ciyu)
                        ciyu.zid = a;
                else if (p == "p1ciyudmg")
                {
                    var sx = a.Split(',');
                    if (sx[0] is "2B45" or "2B46") return;
                    lock (ciyu) ciyu.dmg.Add(sx[1]);
                }
                else if (p == "p2hs") p2hs(a);
                else if (p == "p2safe") p2safe(a);
                else if (p == "p2zhuzi") p2zhuzi(a);
                else if (p == "p2zhuzidmg") p2zhuzidmg(a);
                else if (p == "p2zhuzijx")
                    lock (Zhuzis)
                        foreach (var z in Zhuzis.Where(z => z.zid == a))
                            z.cs2 = true;
                else if (p == "p2hsjx")
                {
                    var e = GetEntityByID(a);
                    if (IdIsBNpcID(e, 8730)) hsadd = e.Address.ToString();
                }
                else if (p == "p2hscvfx") p2hscvfx(a);
                else if (p == "p3threebucket") ThreeBucket(a);
                else if (p == "p3fly") p3fly(a);
                else if (p == "p3ygjd") p3ygjd(a);
                else if (p == "p3nl")
                {
                    Broadcast("优先攻击石牢");
                    Mark(a, "attack1");
                }
                else if (p == "p41taitan") PostTip(a switch { "113.70" => "右右右", "86.30" => "左左左" });
                else if (p == "p43dh") p43dh(a);
                else if (p == "p43fq") p43dh_fq(a, "风枪");
                else if (p == "p43qdptrace") p43qdptrace(a);
            }
        }
        catch (Exception e)
        {
            Log($"Error: {str}");
            Log(e.StackTrace);
        }
    }

    private static void p0init(string str)
    {
        if (str.Contains(':'))
        {
            var ss = str.Split(':')[1];
            var tmp = ss.Split(',')[0].ToUpper();
            if (JobOrder.All(j => j != tmp))
            {
                if (tmp == Tank.ToString().ToUpper()) MyJob = "MT";
                else if (tmp == PureHealer.ToString().ToUpper()) MyJob = "H1";
                else if (tmp == BarrierHealer.ToString().ToUpper()) MyJob = "H2";
                else if (tmp == StrengthMelee.ToString().ToUpper()) MyJob = "D1";
                else if (tmp == DexterityMelee.ToString().ToUpper()) MyJob = "D2";
                else if (tmp == PhysicalRanged.ToString().ToUpper()) MyJob = "D3";
                else if (tmp == MagicalRanged.ToString().ToUpper()) MyJob = "D4";
            }
            else MyJob = tmp;
            GetPartyOrderInit(ss);
        }
        else if (MyJob != null)
        {
            InitParams();
            PostTip($"已使用{MyJob}进行初始化");
        }
    }

    private static void p1place()
    {
        InitParams();
        Place(StaticPlace.initPlace);
        Place(StaticPlace.clear2);
        Place(StaticPlace.clear3);
        Place(StaticPlace.clear4);
        if (MyJob == null) PostTTS("未设置职业，请检查设置。");
        if (Ubroadcast) Broadcast("已开启团队播报，请注意不要冲突");
        if (Uthreebucket) Broadcast("已开启三桶播报，请注意不要冲突");
    }

    private static void p2hscvfx(string address)
    {
        plug.InvokeNamedCallback("LockOn", address != hsadd ? $"{address},omen_laser_6_40_4sec_0t" : $"{address},omen_laser_6_60_4sec_0t");
        Task.Run(async delegate
        {
            await Task.Delay(GetScale("state") == "二运" ? 1500 : 2500);
            plug.InvokeNamedCallback("LockOn", $"{address},0");
        });
    }

    private static void BelowHP(uint bnpcid, double percent, string desc, string marktype)
    {
        foreach (var en in GetEntities().Where(x => IdIsBNpcID(x, bnpcid)))
        {
            if (!(1.0 * int.Parse(en.CurrentHP.ToString()) / int.Parse(en.MaxHP.ToString()) < percent)) return;
            Broadcast(desc, true);
            Mark(en.ID, marktype);
            return;
        }
    }

    private static void Mark(object hexId, string marktype)
    {
        if (Ubroadcast) plug.InvokeNamedCallback("mark", $"{{\"ActorID\":0x{hexId},\"MarkType\":\"{marktype}\"}}");
    }

    private static void Death(string id)
    {
        foreach (var z in Zhuzis.Where(z => z.zid == id && !z.cs2))
        {
            Broadcast($"柱炸:{string.Join("", z.dmg)}");
            break;
        }
        lock (ciyu)
            if (id == ciyu.zid && !ciyu.cs2)
                Broadcast($"羽炸:{string.Join("", ciyu.dmg)}");
    }

    private static void p43qdptrace(string xy)
    {
        var pos = GetXY(xy);
        Place($"2:{pos.X},{pos.Y}");
        if (pos is { X: > 100, Y: > 100 }) Broadcast("警告，潜地炮位置出现在右下");
    }

    private static void rf(string name)
    {
        var HDead = false;
        foreach (var p in Players)
        {
            if (BridgeFFXIV.GetNamedPartyMember(p.name).GetValue("currenthp").ToString() == "0" && p.job is "H1" or "H2")
            {
                Log("Dead: " + p.name);
                HDead = true;
            }
            if (p.name != name || MyName != name) continue;
            PostTip("热风点你，快出去！");
        }
        if (HDead) Broadcast("奶死亡，热风注意出人群", true);
    }

    private static void ydsx()
    {
        var TDead = false;
        foreach (var p in Players)
            if (BridgeFFXIV.GetNamedPartyMember(p.name).GetValue("currenthp").ToString() == "0" && p.job is "ST" or "MT")
            {
                Log("Dead: " + p.name);
                TDead = true;
            }
        var op = "二仇炮。";
        if (TDead)
        {
            op += "T死亡，二仇注意出人群";
            Broadcast(op, true);
        }
        PostTTS(op);
    }

    private static void p41()
    {
        var jll = GetXYFromBnpcid(8722, 1)[0];
        var tt = GetXYFromBnpcid(8727, 1)[0];
        var yflt = GetXYFromBnpcid(8730, 1)[0];
        var yflt_dir = GetDir(yflt);
        var jjsb = GetXYFromBnpcid(8734)[0];
        var jjsb_dir = GetDir(jjsb);
        var tt_dir = GetDir(tt);
        var result = new List<P41Info>();
        var yflt13 = yflt_dir is "右上东北" or "左下西南";
        if (jll.Y > 100 && "上北" != tt_dir)
        {
            if (jjsb_dir != "左上西北") result.Add(new P41Info("上北", "逆时针", 91, 83, yflt13));
            if (jjsb_dir != "右上东北") result.Add(new P41Info("上北", "顺时针", 109, 83, !yflt13));
        }
        if (jll.Y < 100 && "下南" != tt_dir)
        {
            if (jjsb_dir != "左下西南") result.Add(new P41Info("下南", "顺时针", 91, 117, !yflt13));
            if (jjsb_dir != "右下东南") result.Add(new P41Info("下南", "逆时针", 109, 117, yflt13));
        }
        if (jll.X > 100 && "左西" != tt_dir)
        {
            if (jjsb_dir != "左上西北") result.Add(new P41Info("左西", "顺时针", 83, 91, yflt13));
            if (jjsb_dir != "左下西南") result.Add(new P41Info("左西", "逆时针", 83, 109, !yflt13));
        }
        if (jll.X < 100 && "右东" != tt_dir)
        {
            if (jjsb_dir != "右上东北") result.Add(new P41Info("右东", "逆时针", 117, 91, !yflt13));
            if (jjsb_dir != "右下东南") result.Add(new P41Info("右东", "顺时针", 117, 109, yflt13));
        }
        var esresult = result.Where(t => t.canES).ToList();
        esresult.AddRange(result.Where(t => !t.canES));
        var recommand = esresult[0];
        PostTip(recommand.ToString());
        Place(recommand.first switch { "上北" => StaticPlace.safeA, "下南" => StaticPlace.safeC, "左西" => StaticPlace.safeD, "右东" => StaticPlace.safeB });
        Log($"可能安全点:{string.Join("|", esresult)}。神兵:{jjsb_dir},土神:{tt_dir},火神:{yflt_dir}。");
        Task.Run(async delegate
        {
            await Task.Delay(recommand.canES ? 1000 : 6000);
            Place($"4:{recommand.afterX},{recommand.afterY}");
        });
    }

    private static void p43dh_fq(string s, string desc)
    {
        if (s == MyName) PostTip(desc);
        else if (desc == "地火") desc += "(仅供参考)";
        lock (P43PlayerName)
        {
            P43PlayerName.Add(s);
            Broadcast($"{desc} {s}");
            if (P43PlayerName.Count == 5) p43qdp();
        }
    }

    private static void p43qdp()
    {
        var nm = "";
        foreach (var player in Players)
        {
            var pn = player.name;
            if (player.job is "MT" or "ST" || P43PlayerName.Contains(pn)) continue;
            if (nm == "") nm = pn;
            else return;
        }
        if (nm == MyName) PostTip("潜地炮");
        Broadcast($"潜地炮 {nm}");
        foreach (var player in Players)
            if (player.name == nm)
            {
                p43qdporder = player.partyorder;
                plug.InvokeNamedCallback("command", $"/mk stop1 <{p43qdporder}>");
                break;
            }
    }

    private static void p43dh(string xy)
    {
        var pos = GetXY(xy);
        lock (p43dhDist)
        {
            var iter = 0;
            foreach (var player in Players)
            {
                if (player.job is "ST" or "MT") continue;
                var pn = player.name;
                var pl = BridgeFFXIV.GetNamedPartyMember(pn);
                var dist = Vector2.DistanceSquared(pos, new Vector2(float.Parse(pl.GetValue("x").ToString()), float.Parse(pl.GetValue("y").ToString())));
                if (p43dhCount == 0) p43dhDist.Add(new DHP43(pn, dist));
                else
                {
                    p43dhDist[iter].dist = Math.Min(p43dhDist[iter].dist, dist);
                    iter++;
                }
            }
            if (++p43dhCount != 3) return;
            p43dhDist.Sort((a, b) => a.dist.CompareTo(b.dist));
            p43dh_fq(p43dhDist[0].name, "地火");
            p43dh_fq(p43dhDist[1].name, "地火");
            p43dh_fq(p43dhDist[2].name, "地火");
        }
    }

    private static void p2zhuzidmg(string st)
    {
        var ss = st.Split(',');
        if (ss[0] == "2B58") return;
        lock (Zhuzis)
            foreach (var z in Zhuzis.Where(z => z.zid == ss[2]))
            {
                z.dmg.Add(ss[1]);
                break;
            }
    }

    private static void p2zhuzi23()
    {
        var x = Zhuzi2.X switch { 84 or 93 => 88, 107 or 116 => 112 };
        var y = Zhuzi2.Y switch { 84 or 93 => 88, 107 or 116 => 112 };
        Place($"2:{x},{y};3:{200 - x},{200 - y}");
    }

    private static void p2zhuzimark(int a1, int a2, int a3, int a4)
    {
        Mark(Zhuzis[a1].zid, "attack1");
        Mark(Zhuzis[a2].zid, "attack2");
        Mark(Zhuzis[a3].zid, "attack3");
        Mark(Zhuzis[a4].zid, "attack4");
    }

    private static void p2zhuzi(string dxy)
    {
        var ss = dxy.Split(',');
        var d = ss[0];
        var x = ss[1];
        var y = ss[2];
        lock (Zhuzis)
        {
            if (x == "100.00" && y == "90.00") Zhuzis.Add(new Zhuzi(d, 1 << 7));
            else if (x == "107.00" && y == "93.00") Zhuzis.Add(new Zhuzi(d, 1 << 6));
            else if (x == "110.00" && y == "100.00") Zhuzis.Add(new Zhuzi(d, 1 << 5));
            else if (x == "107.00" && y == "107.00") Zhuzis.Add(new Zhuzi(d, 1 << 4));
            else if (x == "100.00" && y == "110.00") Zhuzis.Add(new Zhuzi(d, 1 << 3));
            else if (x == "93.00" && y is "107.00" or "106.95" or "107") Zhuzis.Add(new Zhuzi(d, 1 << 2));
            else if (x == "90.00" && y == "100.00") Zhuzis.Add(new Zhuzi(d, 1 << 1));
            else if (x == "93.00" && y == "93.00") Zhuzis.Add(new Zhuzi(d, 1));
            else Log($"Error: Zhuzi {dxy}.");
            if (Zhuzis.Count != 4) return;
            Zhuzis.Sort((a, b) => a.pos.CompareTo(b.pos));
            var mask = Zhuzis.Aggregate(0, (current, z) => current | z.pos);
            if (mask == 0b11010010)
            {
                Zhuzi2 = new Vector2(93, 116);
                p2zhuzimark(1, 0, 2, 3);
            }
            else if (mask == 0b01101001)
            {
                Zhuzi2 = new Vector2(84, 107);
                p2zhuzimark(1, 0, 2, 3);
            }
            else if (mask == 0b10110100)
            {
                Zhuzi2 = new Vector2(84, 93);
                p2zhuzimark(0, 3, 1, 2);
            }
            else if (mask == 0b01011010)
            {
                Zhuzi2 = new Vector2(93, 84);
                p2zhuzimark(0, 3, 1, 2);
            }
            else if (mask == 0b00101101)
            {
                Zhuzi2 = new Vector2(107, 84);
                p2zhuzimark(0, 3, 1, 2);
            }
            else if (mask == 0b10010110)
            {
                Zhuzi2 = new Vector2(116, 93);
                p2zhuzimark(3, 2, 0, 1);
            }
            else if (mask == 0b01001011)
            {
                Zhuzi2 = new Vector2(116, 107);
                p2zhuzimark(3, 2, 0, 1);
            }
            else if (mask == 0b10100101)
            {
                Zhuzi2 = new Vector2(107, 116);
                p2zhuzimark(2, 1, 3, 0);
            }
            Place($"2:{Zhuzi2.X},{Zhuzi2.Y}");
        }
    }

    private static void p3ygjd(string xy)
    {
        var ss = xy.Split(',');
        var x = ss[0];
        var y = ss[1];
        var ygjddir = "";
        if (x == "95.00" && y is "111.00" or "112.00" || x == "88.00" && y == "95.00" || x == "105.00" && y == "88.00" || x == "112.00" && y == "105.00") ygjddir = "右右右";
        else if (x == "105.00" && y == "112.00" || x == "88.00" && y == "105.00" || x == "95.00" && y == "88.00" || x is "111.00" or "112.00" && y == "95.00") ygjddir = "左左左";
        if (ygjddir == "") return;
        if (x == "95.00" && y is "111.00" or "112.00" || x is "111.00" or "112.00" && y == "95.00") Place("2:105,105");
        else if (x == "105.00" && y == "112.00" || x == "88.00" && y == "95.00") Place("2:95,105");
        else if (x == "105.00" && y == "88.00" || x == "88.00" && y == "105.00") Place("2:95,95");
        else if (x == "95.00" && y == "88.00" || x == "112.00" && y == "105.00") Place("2:105,95");
        var index = -1;
        if (ttsafe == "AAAA")
        {
            Place("3:100,105");
            index = 0;
        }
        else if (ttsafe == "BBBB")
        {
            Place("3:95,100");
            index = 1;
        }
        else if (ttsafe == "CCCC")
        {
            Place("3:100,95");
            index = 2;
        }
        else if (ttsafe == "DDDD")
        {
            Place("3:105,100");
            index = 3;
        }
        Place(StaticPlace.ygjdPlace4[index, ygjddir == "左左左" ? 0 : 1]);
        PostTip(ygjddir);
    }

    private static void Broadcast(string s, bool tip = false)
    {
        if (tip) PostTip(s, tts: false);
        plug.InvokeNamedCallback("command", Ubroadcast ? $"/p {s} <se.1>" : $"/e {s}");
    }

    private static string GetPScale(string name) => GetScalarVariable(false, "ptuw_" + name);
    private static string GetScale(string name) => GetScalarVariable(false, "tuw_" + name);

    private static void p3fly(string id)
    {
        var pos = GetXY(id);
        ttsafe = "";
        if (Math.Abs(pos.X - 100) <= 2 && Math.Abs(pos.Y - 114) <= 2)
        {
            ttsafe = "AAAA";
            Place(StaticPlace.safeA);
        }
        else if (Math.Abs(pos.X - 86) <= 2 && Math.Abs(pos.Y - 100) <= 2)
        {
            ttsafe = "BBBB";
            Place(StaticPlace.safeB);
        }
        else if (Math.Abs(pos.X - 100) <= 2 && Math.Abs(pos.Y - 86) <= 2)
        {
            ttsafe = "CCCC";
            Place(StaticPlace.safeC);
        }
        else if (Math.Abs(pos.X - 114) <= 2 && Math.Abs(pos.Y - 100) <= 2)
        {
            ttsafe = "DDDD";
            Place(StaticPlace.safeD);
        }
        if (ttsafe == "") Log($"Taitan Loc Fail At:({pos.X},{pos.Y})");
        Broadcast(ttsafe, true);
        PostTTS(ttsafe);
    }

    private static void PostTTS(string text)
    {
        plug.QueueAction(fakectx, _tri, null, new Action { ActionType = LogMessage.ToString(), LogMessageText = $"[Latihas TTS] {text}", LogProcess = "true" }, DateTime.Now, true);
    }

    private static void PostTip(string text, float x = 0, float y = 0, string id = "main", bool tts = true)
    {
        plug.QueueAction(fakectx, _tri, null, new Action { ActionType = LogMessage.ToString(), LogMessageText = $"[Latihas Tip] {text}:{x}:{y}:{id}", LogProcess = "true" }, DateTime.Now, true);
        if (tts) PostTTS(text);
    }

    private static void p2safe(string xy)
    {
        if (hs == "") return;
        var zzpos = GetXY(xy);
        string safe = null;
        if (Math.Abs(zzpos.X - 100) < 1 && zzpos.Y < 85 && hs != "上北" && hs != "下南") safe = "下南";
        if (Math.Abs(zzpos.X - 100) < 1 && zzpos.Y > 115 && hs != "上北" && hs != "下南") safe = "上北";
        if (Math.Abs(zzpos.Y - 100) < 1 && zzpos.X < 85 && hs != "左西" && hs != "右东") safe = "右东";
        if (Math.Abs(zzpos.Y - 100) < 1 && zzpos.X > 115 && hs != "左西" && hs != "右东") safe = "左西";
        if (safe is null) return;
        Place(safe switch { "上北" => StaticPlace.safeA, "下南" => StaticPlace.safeC, "左西" => StaticPlace.safeD, "右东" => StaticPlace.safeB });
        safe += "安全";
        PostTip(safe);
    }

    private static void p2hs(string xy)
    {
        var ss = xy.Split(',');
        var id = ss[0];
        if (!IdIsBNpcID(GetEntityByID(id), 8730)) return;
        hs = GetDir(ss[1], ss[2]);
        if (hs is "上北" or "下南") Place(hs switch { "上北" or "下南" => StaticPlace.p2hsWEsafe, "左西" or "右东" => StaticPlace.p2hsNSsafe });
        Place(StaticPlace.clear4);
        PostTip("火神" + hs, 50, 50, "main2");
    }

    private static bool IdIsBNpcID(Entity e, uint BNpcID) => e.BNpcID == BNpcID;

    private static void FsPlace(string pl1, string pl2)
    {
        PostTTS($"{pl1},{pl2}");
        Place(pl1 switch { "上北" => StaticPlace.p1fsN3, "下南" => StaticPlace.p1fsS3, "左西" => StaticPlace.p1fsW3, "右东" => StaticPlace.p1fsE3 });
        Place(pl2 switch { "上北" => StaticPlace.p1fsN4, "下南" => StaticPlace.p1fsS4, "左西" => StaticPlace.p1fsW4, "右东" => StaticPlace.p1fsE4 });
    }

    private static string GetDir(Vector2 v) => GetDir(v.X, v.Y);
    private static string GetDir(string x, string y) => GetDir(float.Parse(x), float.Parse(y));

    private static string GetDir(float x, float y)
    {
        return x switch { > 110 when y > 110 => "右下东南", > 110 when y < 90 => "右上东北", < 90 when y < 90 => "左上西北", < 90 when y > 110 => "左下西南", > 115 => "右东", < 85 => "左西", _ => y switch { < 85 => "上北", > 115 => "下南" } };
    }

    private static List<Vector2> GetXYFromBnpcid(uint arg, int reqHP = -1) => GetEntities().Where(en => IdIsBNpcID(en, arg) && (reqHP == -1 || int.Parse(en.CurrentHP.ToString()) == reqHP)).Select(en => new Vector2(en.PosX, en.PosY)).ToList();

    private static Vector2 GetXY(string id_xy)
    {
        if (id_xy.Contains(","))
        {
            var ss = id_xy.Split(',');
            return new Vector2(float.Parse(ss[0]), float.Parse(ss[1]));
        }
        if (id_xy.Any(c => c is (< '0' or > '9') and (< 'A' or > 'F') and (< 'a' or > 'f'))) return new Vector2();
        var e = GetEntityByID(id_xy);
        return new Vector2(e.PosX, e.PosY);
    }

    private static void ThreeBucket(string args)
    {
        foreach (var v in Players)
        {
            if (args != v.name) continue;
            lock (Threebuckets)
                switch (Threebuckets.Count)
                {
                    case 0: Threebuckets.Add(v); break;
                    case 1:
                        if (Threebuckets[0].storder > v.storder) Threebuckets.Insert(0, v);
                        else Threebuckets.Add(v);
                        break;
                    case 2:
                        if (Threebuckets[0].storder > v.storder) Threebuckets.Insert(0, v);
                        else if (Threebuckets[1].storder > v.storder) Threebuckets.Insert(1, v);
                        else Threebuckets.Add(v);
                        if (Uthreebucket)
                        {
                            plug.InvokeNamedCallback("command", $"/mk attack1 <{Threebuckets[0].partyorder}>");
                            plug.InvokeNamedCallback("command", $"/mk attack2 <{Threebuckets[1].partyorder}>");
                            plug.InvokeNamedCallback("command", $"/mk attack3 <{Threebuckets[2].partyorder}>");
                            Task.Run(async delegate
                            {
                                await Task.Delay(9000);
                                plug.InvokeNamedCallback("command", "/mk attack1 <attack1>");
                                plug.InvokeNamedCallback("command", "/mk attack2 <attack2>");
                                plug.InvokeNamedCallback("command", "/mk attack3 <attack3>");
                            });
                        }
                        else
                        {
                            plug.InvokeNamedCallback("command", $"/e attack1 <{Threebuckets[0].partyorder}>");
                            plug.InvokeNamedCallback("command", $"/e attack2 <{Threebuckets[1].partyorder}>");
                            plug.InvokeNamedCallback("command", $"/e attack3 <{Threebuckets[2].partyorder}>");
                        }
                        Threebuckets.Clear();
                        break;
                }
            break;
        }
    }

    private static void InitParams()
    {
        Threebuckets.Clear();
        P43PlayerName.Clear();
        Zhuzis.Clear();
        Zhuzi2 = new Vector2();
        hs = "";
        ttsafe = "";
        hsadd = "";
        ciyu = new Ciyu();
        p43qdporder = -1;
        p43dhCount = 0;
        p43dhDist = [];
        Ubroadcast = GetPScale("Ubroadcast") == "1";
        Uthreebucket = GetPScale("Uthreebucket") == "1";
        foreach (var p in Players) Log(p.ToString());
    }

    private static void GetPartyOrderInit(string args)
    {
        var ss = args.Split(',');
        var find = false;
        for (var i = 0; i < 8; i++)
        {
            var job = JobOrder[i];
            int num;
            if (MyJob == job)
            {
                num = 1;
                find = true;
            }
            else num = i + (find ? 1 : 2);
            var name = ss[num];
            foreach (var ser in Server)
            {
                if (!name.EndsWith(ser)) continue;
                name = name.Substring(0, name.Length - ser.Length);
                break;
            }
            Players[i] = new Player(job, num, name);
        }
        InitParams();
        var sb = new StringBuilder("小队初始化完成。职业").Append(MyJob).Append("。");
        if (Ubroadcast) sb.Append("启用团队播报。");
        if (Uthreebucket) sb.Append("启用三连桶点名。");
        var sbs = sb.ToString();
        Log(sbs);
        PostTTS(sbs);
    }

    private static void Log(string message) => plug.InvokeNamedCallback("command", $"/e {message}");

    private record DHP43(string name, float dist)
    {
        internal readonly string name = name;
        internal float dist = dist;
    }

    private record P41Info
    {
        internal readonly float afterX, afterY;
        internal readonly bool canES;
        private readonly string desc;
        internal readonly string first;

        internal P41Info(string first, string second, float afterX, float afterY, bool canES)
        {
            this.first = first;
            this.canES = canES;
            this.afterX = afterX;
            this.afterY = afterY;
            var sb = new StringBuilder(first).Append("然后").Append(second);
            if (canES) sb.Append("(提前安全)");
            desc = sb.ToString();
        }

        public override string ToString() => desc;
    }

    private record Ciyu
    {
        internal readonly List<string> dmg = [];
        internal bool cs2;
        internal string zid;
    }

    private record Zhuzi
    {
        internal readonly List<string> dmg = [];
        internal readonly int pos;
        internal readonly string zid;
        internal bool cs2;

        internal Zhuzi(string zid, int pos)
        {
            this.zid = zid;
            this.pos = pos;
        }
    }

    private static class StaticPlace
    {
        internal const string initPlace = "A:100,82;B:118,100;C:100,118;D:82,100;3:93,93;4:107,107;1:100,100",
                              clear2 = "2:clear",
                              clear3 = "3:clear",
                              clear4 = "4:clear",
                              safeA = "1:100,84;2:98,82;3:102,82;4:100,92.5",
                              safeB = "1:116,100;2:118,98;3:118,102;4:107.5,100",
                              safeC = "1:100,116;2:102,118;3:98,118;4:100,107.5",
                              safeD = "1:84,100;2:82,102;3:82,98;4:92.5,100",
                              p1fsN3 = "3:100,90",
                              p1fsN4 = "4:100,90",
                              p1fsS3 = "3:100,110",
                              p1fsS4 = "4:100,110",
                              p1fsW3 = "3:90,100",
                              p1fsW4 = "4:90,100",
                              p1fsE3 = "3:110,100",
                              p1fsE4 = "4:110,100",
                              p2hsWEsafe = "2:90,100;3:110,100",
                              p2hsNSsafe = "2:100,90;3:100,110",
                              p41place2 = "2:100,111",
                              p42place2 = "2:88,88",
                              p42place4d3 = "4:100,110",
                              p42place2rf = "2:100,112",
                              startp43 = "A:100,82;B:94.5,83;1:89.5,85.5;2:85.5,89.5;C:83,94.5;D:82,100;3:93,107;4:107,107",
                              p43_2 = "A:100,82;B:118,100;C:100,118;D:82,100;3:93,107;4:107,107;1:100,100",
                              p5jzhA = "B:101,81;C:101,83;D:99,81;4:99,83",
                              p5jzh2 = "B:89,87;C:89,89;D:87,87;4:87,89",
                              p5jzh3 = "B:94,92;C:94,94;D:92,92;4:92,94";
        internal static readonly string[,] ygjdPlace4 = { { "4:102.1,108.6", "4:97.9,108.6" }, { "4:91.4,102.1", "4:91.4,97.9" }, { "4:97.9,91.4", "4:102.1,91.4" }, { "4:108.6,97.9", "4:108.6,102.1" } };
    }

    private readonly record struct Player
    {
        internal readonly string job, name;
        internal readonly int partyorder, storder;

        internal Player(string job, int partyorder, string name)
        {
            this.job = job;
            if (job == MyJob) MyName = name;
            this.partyorder = partyorder;
            var o = 0;
            for (var i = 0; i < TBJobOrder.Length; i++)
            {
                if (job != TBJobOrder[i]) continue;
                o = i;
                break;
            }
            storder = o;
            this.name = name;
        }

        public override string ToString() => $"[{job}({partyorder})]:{name}";
    }

    public void Load()
    {
        Log("LatihasTUW注册成功");
    }
}
