using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Numerics;
using Advanced_Combat_Tracker;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using static Advanced_Combat_Tracker.ActGlobals;

namespace IINACT.Latihas.Overlay;

public partial class OverlayWindow {
    private TreeNodeData? _currentSelectedNode;
    private string _tableType;
    private List<object> _currentTable = [];
    private const float DpiScale = 1;
    private const string _eDSort = "Damage";
    private const string _mDSort = "Damage";
    private const string _aTSort = "Time";
    private static readonly CultureInfo usCulture = new("en-US");
    private IDalamudTextureWrap? _currentTexture;
    private static readonly string[] clbAT = [
        // "EncId",
        "Time", "Attacker",
        // "SwingType",
        "AttackType", "DamageType", "Victim",
        // "DamageNum",
        "Damage", "Critical", "CriticalStr", "Special", "RelativeTime", "StatusDuration",
        // "OverHeal", 
        "DirectHit"
    ];
    private static readonly string[] clbCD = [
        // "EncId", "Combatant", "Grouping", 
        "Type",
        // "StartTime", "EndTime", "Duration", 
        "Damage", "EncDPS",
        // "CharDPS", "DPS",
        "Average",
        // "Median",
        "MinHit", "MaxHit", "Hits",
        // "CritHits", "Avoids", "Misses",
        "Swings",
        // "ToHit", "AvgDelay", "Crit%", "CritTypes", "ParryPct", "BlockPct", "OverHeal",
        "DirectHitPct",
        // "DirectHitCount", "CritDirectHitCount",
        "CritDirectHitPct"
    ];
    private static readonly string[] clbDT = [
        // "EncId", "Attacker", "Victim", "SwingType",
        "Type",
        // "StartTime", "EndTime", "Duration",
        "Damage", "EncDPS",
        // "CharDPS", "DPS", 
        "Average", "Median", "MinHit", "MaxHit", "Resist", "Hits",
        // "CritHits", "Avoids", "Misses",
        "Swings", "ToHit",
        // "AvgDelay",
        "Crit%",
        // "CritTypes", "Parry", "ParryPct", "Block", "BlockPct", "OverHeal", 
        "DirectHitPct",
        // "DirectHitCount", "CritDirectHitCount",
        "CritDirectHitPct"
    ];
    private static readonly string[] clbED = [
        // "EncId", "Ally", 
        "Name", "StartTime",
        // "EndTime", 
        "Duration", "Damage", "Damage%",
        // "Kills", "Healed", "Healed%", "CritHeals", "Heals", "Cures",
        "PowerDrain",
        // "PowerReplenish", "DPS",
        "EncDPS", "EncHPS",
        // "Hits", "CritHits", "Avoids", "Misses", "Swings", "HealingTaken",
        "DamageTaken", "Deaths",
        // "ToHit%", "CritDam%", "CritHeal%", "CritTypes", "Threat +/-", "ThreatDelta",
        "Job",
        // "ParryPct", "BlockPct","IncToHit",
        "OverHealPct", "DirectHitPct",
        // "DirectHitCount", "CritDirectHitCount",
        "CritDirectHitPct"
    ];

    private static readonly string[] clbZD = [
        // "EncId", 
        "Title", "StartTime", "EndTime", "Duration", "Damage", "EncDPS",
        // "Zone", 
        "Kills", "Deaths"
    ];
    private readonly Lock _textureLock = new();

    private static Bitmap GenDamageTypeGraph(DamageTypeData DamageTypeSource, int SizeX, int SizeY, string Sorting) {
        if (SizeX < 16 || SizeY < 16)
            return new Bitmap(16, 16);
        var bitmap = new Bitmap(SizeX, SizeY);
        try {
            var list = new List<AttackType>(DamageTypeSource.Items.Values);
            // ttg.Items.Clear();
            List<StrDouble> list2 = [];
            try {
                list.Sort(AttackType.ColumnDefs[Sorting].SortComparer);
                list.Reverse();
            }
            catch (Exception ex) {
                WriteExceptionLog(ex, "Sorting Failed " + Sorting);
                bitmap = GraphDrawMessage(ex.ToString(), 12f, bitmap);
            }
            var attackType = list.FirstOrDefault(item => item.Type == Trans["attackTypeTerm-all"]);
            if (Sorting == "Resist") {
                var dictionary = new Dictionary<string, long> {
                    {
                        Trans["attackTypeTerm-all"], 0L
                    }
                };
                if (attackType != null)
                    foreach (var masterSwing in attackType.Items) {
                        var text = masterSwing.DamageType;
                        if (text.StartsWith(Trans["specialAttackTerm-warded"] + "/"))
                            text = text[7..];
                        dictionary.TryAdd(text, 0L);
                        if ((long)masterSwing.Damage <= 0) continue;
                        var dictionary2 = dictionary;
                        var key = text;
                        dictionary2[key] += masterSwing.Damage;
                        dictionary2 = dictionary;
                        key = Trans["attackTypeTerm-all"];
                        dictionary2[key] += masterSwing.Damage;
                    }
                list2.AddRange(from item2 in dictionary where item2.Key != Trans["attackTypeTerm-all"] select new StrDouble(item2.Key, item2.Value / (double)dictionary[Trans["attackTypeTerm-all"]]));
            }
            else {
                foreach (var item3 in list.Where(item3 => item3.Type != Trans["attackTypeTerm-all"])) {
                    try {
                        var num = double.Parse(AttackType.ColumnDefs[Sorting].GetSqlData(item3), usCulture);
                        if (num > 0.0 && attackType != null)
                            list2.Add(new StrDouble(item3.Type, num / double.Parse(AttackType.ColumnDefs[Sorting].GetSqlData(attackType), usCulture)));
                    }
                    catch {
                        //
                    }
                }
            }
            var brush = new SolidBrush(Color.FromArgb(150, Color.FromArgb(-16777216)));
            var solidBrush = new SolidBrush(Color.FromArgb(-1842205));
            var brush2 = new SolidBrush(Color.FromArgb(-16777216));
            var brush3 = new SolidBrush(Color.FromArgb(-6250336));
            var pen = new Pen(Color.FromArgb(-16777216));
            // new Pen(Color.FromArgb(-6250336));
            SolidBrush[] array = [
                new(Color.FromArgb(-4194112)),
                new(Color.FromArgb(-32513)),
                new(Color.FromArgb(-16777024)),
                new(Color.FromArgb(-8355585)),
                new(Color.FromArgb(-16727872)),
                new(Color.FromArgb(-8323073)),
                new(Color.FromArgb(-16728064)),
                new(Color.FromArgb(-8323200)),
                new(Color.FromArgb(-4145152)),
                new(Color.FromArgb(-128)),
                new(Color.FromArgb(-4177920)),
                new(Color.FromArgb(-16256)),
                new(Color.FromArgb(-4194304)),
                new(Color.FromArgb(-32640)),
                new(Color.FromArgb(-65281)),
                new(Color.FromArgb(-12582848)),
                new(Color.FromArgb(-16776961)),
                new(Color.FromArgb(-16777152)),
                new(Color.FromArgb(-16711681)),
                new(Color.FromArgb(-16760768)),
                new(Color.FromArgb(-16711936)),
                new(Color.FromArgb(-16760832)),
                new(Color.FromArgb(-256)),
                new(Color.FromArgb(-12566528)),
                new(Color.FromArgb(-32768)),
                new(Color.FromArgb(-8372160)),
                new(Color.FromArgb(-65536)),
                new(Color.FromArgb(-12582912))
            ];
            Pen[] array2 = [
                new(Color.FromArgb(-4194112), 2f),
                new(Color.FromArgb(-32513), 2f),
                new(Color.FromArgb(-16777024), 2f),
                new(Color.FromArgb(-8355585), 2f),
                new(Color.FromArgb(-16727872), 2f),
                new(Color.FromArgb(-8323073), 2f),
                new(Color.FromArgb(-16728064), 2f),
                new(Color.FromArgb(-8323200), 2f),
                new(Color.FromArgb(-4145152), 2f),
                new(Color.FromArgb(-128), 2f),
                new(Color.FromArgb(-4177920), 2f),
                new(Color.FromArgb(-16256), 2f),
                new(Color.FromArgb(-4194304), 2f),
                new(Color.FromArgb(-32640), 2f),
                new(Color.FromArgb(-65281), 2f),
                new(Color.FromArgb(-12582848), 2f),
                new(Color.FromArgb(-16776961), 2f),
                new(Color.FromArgb(-16777152), 2f),
                new(Color.FromArgb(-16711681), 2f),
                new(Color.FromArgb(-16760768), 2f),
                new(Color.FromArgb(-16711936), 2f),
                new(Color.FromArgb(-16760832), 2f),
                new(Color.FromArgb(-256), 2f),
                new(Color.FromArgb(-12566528), 2f),
                new(Color.FromArgb(-32768), 2f),
                new(Color.FromArgb(-8372160), 2f),
                new(Color.FromArgb(-65536), 2f),
                new(Color.FromArgb(-12582912), 2f)
            ];
            var font = new Font("Arial", 8f);
            var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(solidBrush.Color);
            if (list2.Count == 0) {
                bitmap = GraphDrawMessage(Trans["graphAttackTypes-noDataError"], 12f, bitmap);
                return bitmap;
            }
            const float num2 = 16f * DpiScale;
            var rectangleF = new RectangleF(num2 / 4f, num2 / 4f, bitmap.Height - num2 / 2f, bitmap.Height - num2 / 2f);
            graphics.DrawRectangle(pen, rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
            var num3 = -90f;
            var num4 = rectangleF.Right + num2 * 2f;
            var num5 = rectangleF.Top;
            for (var j = 0; j < list2.Count; j++) {
                var strDouble = list2[j];
                var num6 = (float)strDouble.Val * 360f;
                if (j < array2.Length - 1) {
                    graphics.FillPie(array[j], rectangleF.X + num2 / 4f, rectangleF.Y + num2 / 4f, rectangleF.Width - num2 / 2f, rectangleF.Height - num2 / 2f, num3, num6);
                    graphics.FillRectangle(array[j], num4 - 28f * DpiScale, num5, 192f * DpiScale, 14f * DpiScale);
                    graphics.DrawRectangle(pen, num4 - 28f * DpiScale, num5, 192f * DpiScale, 14f * DpiScale);
                }
                else {
                    graphics.FillPie(brush3, rectangleF.X + num2 / 4f, rectangleF.Y + num2 / 4f, rectangleF.Width - num2 / 2f, rectangleF.Height - num2 / 2f, num3, num6);
                    graphics.DrawString(strDouble.Name, font, brush2, num4, num5);
                }
                try {
                    const float num7 = 26f * DpiScale;
                    var num8 = Convert.ToInt32(strDouble.Val * 100.0);
                    graphics.DrawString(num8 + "%", font, brush, num4 - num7 - 1f, num5 + 1f);
                    graphics.DrawString(num8 + "%", font, brush, num4 - num7 + 1f, num5 + 1f);
                    graphics.DrawString(num8 + "%", font, brush, num4 - num7, num5 - 1f + 1f);
                    graphics.DrawString(num8 + "%", font, brush, num4 - num7, num5 + 1f + 1f);
                    graphics.DrawString(num8 + "%", font, solidBrush, num4 - num7, num5 + 1f);
                }
                catch {
                    //
                }
                graphics.DrawString(strDouble.Name, font, brush, num4 + 1f, num5 + 1f);
                graphics.DrawString(strDouble.Name, font, brush, num4 - 1f, num5 + 1f);
                graphics.DrawString(strDouble.Name, font, brush, num4, num5 - 1f + 1f);
                graphics.DrawString(strDouble.Name, font, brush, num4, num5 + 1f + 1f);
                graphics.DrawString(strDouble.Name, font, solidBrush, num4, num5 + 1f);
                num3 += num6;
                num5 += 14f * DpiScale;
                if (num5 > rectangleF.Bottom - num2) {
                    num5 = rectangleF.Top;
                    num4 += 192f * DpiScale;
                }
            }
            graphics.DrawEllipse(pen, rectangleF.X + num2 / 4f, rectangleF.Y + num2 / 4f, rectangleF.Width - num2 / 2f, rectangleF.Height - num2 / 2f);
            graphics.DrawString(Sorting, font, brush2, rectangleF.Left + num2 / 8f, rectangleF.Top + num2 / 8f);
        }
        catch (ThreadAbortException) {
            // WriteInfoLog("GenDamageTypeGraph -> ThreadAbortException");
        }
        catch (Exception ex3) {
            WriteExceptionLog(ex3, string.Empty);
            bitmap = GraphDrawMessage(ex3.ToString(), 12f, bitmap);
        }
        // ActGlobals.oFormActMain.WriteDebugLog("GraphMDPie: " + (DateTime.Now - now).TotalMilliseconds.ToString("F"));
        return bitmap;
    }

    // ReSharper disable PossibleLossOfFraction
    private static Bitmap GenAttackTypeGraph(AttackType AttackTypeSource, int SizeX, int SizeY, string Sorting) {
        if (SizeX < 16 || SizeY < 16)
            return new Bitmap(16, 16);
        var bitmap = new Bitmap(SizeX, SizeY);
        try {
            var list = new List<MasterSwing>(AttackTypeSource.Items);
            try {
                list.Sort(new MasterSwing.DualComparison(Sorting, Sorting));
            }
            catch (Exception ex) {
                WriteExceptionLog(ex, string.Empty);
                bitmap = GraphDrawMessage(ex.ToString(), 12f, bitmap);
            }
            var solidBrush = new SolidBrush(Color.FromArgb(-1842205));
            var brush = new SolidBrush(Color.FromArgb(-16777216));
            var pen = new Pen(Color.FromArgb(-16777216));
            var pen2 = new Pen(Color.FromArgb(-6250336));
            var dictionary = new Dictionary<int, SolidBrush>();
            foreach (var item in CombatantData.SwingTypeToDamageTypeDataLinksOutgoing.Where(item => !dictionary.ContainsKey(item.Key))) {
                dictionary.Add(item.Key, new SolidBrush(Color.FromArgb(180, CombatantData.OutgoingDamageTypeDataObjects[item.Value[0]].TypeColor)));
            }
            foreach (var item2 in CombatantData.SwingTypeToDamageTypeDataLinksIncoming.Where(item2 => !dictionary.ContainsKey(item2.Key))) {
                dictionary.Add(item2.Key, new SolidBrush(Color.FromArgb(180, CombatantData.IncomingDamageTypeDataObjects[item2.Value[0]].TypeColor)));
            }
            var dictionary2 = new Dictionary<int, SolidBrush>();
            foreach (var item3 in CombatantData.SwingTypeToDamageTypeDataLinksOutgoing.Where(item3 => !dictionary2.ContainsKey(item3.Key))) {
                dictionary2.Add(item3.Key, new SolidBrush(Color.FromArgb(255, CombatantData.OutgoingDamageTypeDataObjects[item3.Value[0]].TypeColor)));
            }
            foreach (var item4 in CombatantData.SwingTypeToDamageTypeDataLinksIncoming.Where(item4 => !dictionary2.ContainsKey(item4.Key))) {
                dictionary2.Add(item4.Key, new SolidBrush(Color.FromArgb(255, CombatantData.IncomingDamageTypeDataObjects[item4.Value[0]].TypeColor)));
            }
            var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(solidBrush.Color);
            const float num = 16f * DpiScale;
            var rectangleF = new RectangleF(num / 4f, num / 4f, bitmap.Width - 1 - num * 4f, bitmap.Height - 1 - num * 2f);
            graphics.DrawRectangle(pen, rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
            var bottom = rectangleF.Bottom;
            float num2;
            try {
                num2 = rectangleF.Width / list.Count;
            }
            catch (Exception ex2) {
                bitmap = GraphDrawMessage(ex2.ToString(), 12f, bitmap);
                return bitmap;
            }
            var num3 = rectangleF.Left;
            var num4 = 1.0;
            foreach (var item5 in list.Where(item5 => (long)item5.Damage > num4)) {
                num4 = (long)item5.Damage;
            }
            var num5 = (long)Math.Pow(10.0, ((long)num4).ToString().ToCharArray().Length);

            while (num5 / 2 > num4) {
                num5 /= 2;
            }
            if (num5 / 1.25 > num4)
                num5 = Convert.ToInt64(num5 / 1.25);
            var num6 = rectangleF.Height / (double)num5;
            _ = 1.0 / num6;
            var font = new Font("Arial", 8f);
            graphics.DrawString("0", font, brush, rectangleF.Right + num / 4f, rectangleF.Bottom);
            graphics.DrawString(oFormActMain.CreateDamageString(num5, true, true), font, brush, rectangleF.Right + num / 4f, rectangleF.Top);
            try {
                var num7 = Convert.ToInt64(bottom - Convert.ToInt64(num5 / 4 * num6));
                graphics.DrawLine(pen2, rectangleF.Left, num7, rectangleF.Right, num7);
                graphics.DrawString(oFormActMain.CreateDamageString(num5 / 4, true, true), font, brush, rectangleF.Right + num / 4f, num7);
                num7 = Convert.ToInt64(bottom - Convert.ToInt64(num5 / 4 * 2 * num6));
                graphics.DrawLine(pen2, rectangleF.Left, num7, rectangleF.Right, num7);
                graphics.DrawString(oFormActMain.CreateDamageString(num5 / 4 * 2, true, true), font, brush, rectangleF.Right + num / 4f, num7);
                num7 = Convert.ToInt64(bottom - Convert.ToInt64(num5 / 4 * 3 * num6));
                graphics.DrawLine(pen2, rectangleF.Left, num7, rectangleF.Right, num7);
                graphics.DrawString(oFormActMain.CreateDamageString(num5 / 4 * 3, true, true), font, brush, rectangleF.Right + num / 4f, num7);
            }
            catch {
                //
            }
            // ttg.Items.Clear();
            for (var i = 0; i < list.Count; i++) {
                var masterSwing = list[i];
                var num8 = num3;
                var num9 = bottom - (long)masterSwing.Damage * (float)num6;
                var num10 = (long)masterSwing.Damage * (float)num6;
                if (i > 0 && masterSwing.Time != list[i - 1].Time)
                    graphics.DrawLine(pen2, num3, rectangleF.Top, num3, rectangleF.Bottom);
                graphics.FillRectangle(masterSwing.Critical ? dictionary2[masterSwing.SwingType] : dictionary[masterSwing.SwingType], num8, num9, num2, num10);
                graphics.DrawRectangle(pen, num8, num9, num2, num10);
                // ttg.Items.Add(new ToolTipRect(-1, $"{masterSwing.Time}\n{masterSwing.Attacker} -> {masterSwing.Victim}\n{masterSwing.AttackType} {(long)masterSwing.Damage:#,0}", num8, rectangleF.Y, w, rectangleF.Height));
                if (rectangleF.Width / list.Count > 16f * DpiScale) {
                    var s = (long)masterSwing.Damage > 0
                        ? oFormActMain.CreateDamageString(masterSwing.Damage, true, true)
                        : masterSwing.Damage.ToString(true);
                    if (rectangleF.Width / list.Count > 32f * DpiScale)
                        graphics.DrawString(s, font, brush, num3, bottom + num / 8f);
                    else if (i % 2 == 0) {
                        graphics.DrawString(s, font, brush, num3, bottom + num * 0.75f);
                    }
                    else {
                        graphics.DrawString(s, font, brush, num3, bottom + num / 8f);
                    }
                }
                num3 += num2;
            }
            graphics.DrawLine(pen2, num3, rectangleF.Top, num3, rectangleF.Bottom);
        }
        catch (Exception ex4) {
            WriteExceptionLog(ex4, string.Empty);
            bitmap = GraphDrawMessage(ex4.ToString(), 12f, bitmap);
        }
        // oFormActMain.WriteDebugLog("GraphHitBars: " + (DateTime.Now - now).TotalMilliseconds.ToString("F"));
        return bitmap;
    }


    private static Bitmap GenEncounterGraph(EncounterData? EncounterSource, int SizeX, int SizeY, string Sorting) {
        if (SizeX < 16 || SizeY < 16)
            return new Bitmap(16, 16);
        var bitmap = new Bitmap(SizeX, SizeY);
        if (EncounterSource == null) return bitmap;
        var list = new List<CombatantData>(EncounterSource.Items.Values);
        try {
            try {
                list.Sort(CombatantData.ColumnDefs[Sorting].SortComparer);
            }
            catch (Exception ex) {
                WriteExceptionLog(ex, "Failed Sorting");
                bitmap = GraphDrawMessage(ex.ToString(), 12f, bitmap);
                return bitmap;
            }
            list.Reverse();
            var list2 = !EncounterSource.GetIsSelective() || !EncounterSource.GetIgnoreEnemies() ? EncounterSource.GetAllies() : list;
            var brush = new SolidBrush(Color.FromArgb(100, Color.FromArgb(-16777216)));
            var solidBrush = new SolidBrush(Color.FromArgb(-1842205));
            var brush2 = new SolidBrush(Color.FromArgb(-16777216));
            var pen = new Pen(Color.FromArgb(-16777216));
            var pen2 = new Pen(Color.FromArgb(-6250336));
            // var solidBrush2 = new SolidBrush(System.Drawing.Color.FromArgb(-7667712));
            // var solidBrush3 = new SolidBrush(System.Drawing.Color.FromArgb(-16777077));
            var solidBrush2 = new SolidBrush(Color.Red);
            var solidBrush3 = new SolidBrush(Color.DeepSkyBlue);
            var brush3 = new SolidBrush(Color.FromArgb(-7077677));
            var brush4 = new SolidBrush(Color.FromArgb(-1));
            var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(solidBrush.Color);
            var num = 0.0;
            var list3 = new List<StrDouble>();
            foreach (var item in list.Where(item => list2.Count <= 0 || list2.IndexOf(item) != -1)) {
                try {
                    var num2 = double.Parse(CombatantData.ColumnDefs[Sorting].GetSqlData(item), usCulture);
                    if (num2 > 0.0) {
                        list3.Add(new StrDouble(item.Name, num2));
                        num += num2;
                    }
                }
                catch {
                    //
                }
            }
            const float num3 = 16f * DpiScale;
            var rectangleF = new RectangleF(num3 / 4f, num3 / 4f, bitmap.Width - 1 - num3 * 4f, bitmap.Height - 1 - num3);
            graphics.DrawRectangle(pen, rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
            // ttg.Items.Clear();
            if (list3.Count == 0) {
                bitmap = list.Count != 0 ? GraphDrawMessage(Trans["graphEncText-noDataSortingError"], 12f, bitmap) : GraphDrawMessage(Trans["graphEncText-noDataError"], 12f, bitmap);
                return bitmap;
            }
            var num4 = rectangleF.Width / list3.Count;
            var bottom = rectangleF.Bottom;
            var num5 = rectangleF.Left;
            var num6 = 1.0;
            foreach (var item2 in list3.Where(item2 => item2.Val > num6)) num6 = Math.Ceiling(item2.Val);
            num6 *= 1.1;
            var num7 = rectangleF.Height / num6;
            _ = 1.0 / num7;
            var font = new Font("Arial", 9f);
            var font2 = new Font("Consolas", 9f);
            graphics.DrawString("0", font, brush2, rectangleF.Right + 5f, rectangleF.Bottom);
            graphics.DrawString(oFormActMain.CreateDamageString((long)num6, true, true), font, brush2, rectangleF.Right + num3 / 3f, rectangleF.Top);
            try {
                var num8 = Convert.ToInt64(bottom - Convert.ToInt64(num6 / 4.0 * num7));
                graphics.DrawLine(pen2, rectangleF.Left, num8, rectangleF.Right, num8);
                graphics.DrawString(oFormActMain.CreateDamageString((long)(num6 / 4.0), true, true), font, brush2, rectangleF.Right + num3 / 3f,
                    num8);
                num8 = Convert.ToInt64(bottom - Convert.ToInt64(num6 / 4.0 * 2.0 * num7));
                graphics.DrawLine(pen2, rectangleF.Left, num8, rectangleF.Right, num8);
                graphics.DrawString(oFormActMain.CreateDamageString((long)(num6 / 4.0 * 2.0), true, true), font, brush2,
                    rectangleF.Right + num3 / 3f, num8);
                num8 = Convert.ToInt64(bottom - Convert.ToInt64(num6 / 4.0 * 3.0 * num7));
                graphics.DrawLine(pen2, rectangleF.Left, num8, rectangleF.Right, num8);
                graphics.DrawString(oFormActMain.CreateDamageString((long)(num6 / 4.0 * 3.0), true, true), font, brush2,
                    rectangleF.Right + num3 / 3f, num8);
            }
            catch {
                //
            }
            for (var i = 0; i < list3.Count; i++) {
                var strDouble = list3[i];
                var num9 = num5;
                var rect = new RectangleF(num9, (float)(bottom - strDouble.Val * num7), num4, (float)(strDouble.Val * num7));
                // ttg.Items.Add(new ToolTipRect(-1, $"{strDouble.Name}: {strDouble.Val:#,0}", rect.X, rectangleF.Y, rect.Width, rectangleF.Height));
                var s = oFormActMain.CreateDamageString((long)strDouble.Val, true, true);
                SolidBrush brush5;
                if (i % 2 == 0) {
                    graphics.FillRectangle(strDouble.Name == charName ? brush3 : solidBrush2, rect);
                    if (num4 > num3)
                        graphics.DrawString(s, font, solidBrush2, num9, bottom - (float)(strDouble.Val * num7) - num3 * 0.75f - 3f);
                    brush5 = solidBrush2;
                }
                else {
                    graphics.FillRectangle(strDouble.Name == charName ? brush3 : solidBrush3, rect);
                    if (num4 > num3)
                        graphics.DrawString(oFormActMain.CreateDamageString((long)strDouble.Val, true, true), font, solidBrush3, num9, bottom - 1f);
                    graphics.DrawLine(pen, num5, rectangleF.Bottom, num5, rectangleF.Bottom + num3 / 4f);
                    brush5 = solidBrush3;
                }
                try {
                    graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
                catch {
                    //
                }
                var stringFormat = new StringFormat();
                stringFormat.FormatFlags = StringFormatFlags.DirectionVertical;
                if (graphics.MeasureString(strDouble.Name, font2, 15, stringFormat).Height < (float)(strDouble.Val * num7) - num3)
                    graphics.DrawString(strDouble.Name, font2, brush4, num5 - num3 / 2f + num4 / 2f, bottom - (float)(strDouble.Val * num7) + num3 * 0.75f, stringFormat);
                else
                    graphics.DrawString(strDouble.Name, font2, brush5, num5 - num3 / 2f + num4 / 2f, rectangleF.Top, stringFormat);
                num5 += num4;
            }
            // if (opGraphing.cbSimpleGraphTotals.Checked)
            {
                var s2 = $"Total: {num:0,0}\nAVG: {num / list3.Count:0,0.00}";
                var font3 = new Font("Arial Black", rectangleF.Height / (10f * DpiScale));
                var sizeF = graphics.MeasureString(s2, font3);
                graphics.DrawString(s2, font3, brush, new PointF(rectangleF.Right - sizeF.Width - 1f, rectangleF.Y + rectangleF.Height / 2f - sizeF.Height / 2f + 1f));
            }
        }
        catch (ThreadAbortException) {
            // WriteInfoLog("GenEncounterGraph -> ThreadAbortException");
        }
        catch (Exception ex3) {
            WriteExceptionLog(ex3, string.Empty);
            bitmap = GraphDrawMessage(ex3.ToString(), 12f, bitmap);
        }
        // ActGlobals.oFormActMain.WriteDebugLog("GraphEncBars: " + (DateTime.Now - now).TotalMilliseconds.ToString("F"));
        return bitmap;
    }

    private class TreeNodeData {
        public readonly Guid Guid = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public string Tag { get; init; } = string.Empty;
        public int Index { get; init; }
        public TreeNodeData? Parent { get; init; }
        public List<TreeNodeData> Children { get; } = [];

        public Color ForeColor { get; set; } = Color.White;
        public object? Data { get; set; }
    }

    private readonly List<TreeNodeData> _treeNodes = [];

    internal static Bitmap GraphDrawMessage(string Message, float FontSize, Bitmap BlankImage) {
        var graphics = Graphics.FromImage(BlankImage);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.FromArgb(-1842205));
        graphics.DrawString(Message, new Font("Arial Black", FontSize, FontStyle.Regular), new SolidBrush(Color.FromArgb(-16777216)), 12f, 12f);
        return BlankImage;
    }

    public static void WriteExceptionLog(Exception ex, string MoreInfo) {
        var text = string.Empty;
        if (ex.InnerException != null)
            text = $"{ex.InnerException}{Environment.NewLine}";
        var value = $"***** {DateTime.Now:s} - {MoreInfo}\n{text}{ex}{Environment.NewLine}{Environment.StackTrace.Remove(0, 118)}{Environment.NewLine}*****";
        oFormActMain.PluginLog.Error(value);
    }

    private Vector2 GraphViewSize = Vector2.Zero;

    public void DrawACTStatics() {
        var totalAvail = ImGui.GetContentRegionAvail();
        _splitterWidth = Math.Clamp(_splitterWidth, 50, totalAvail.X - 50);
        if (ImGui.BeginChild("##LeftTreeView", new Vector2(_splitterWidth, -1), true)) {
            var UseActPic = Plugin.Configuration.UseActPic;
            if (ImGui.Checkbox("使用原版ACT绘图", ref UseActPic)) {
                Plugin.Configuration.UseActPic = UseActPic;
                Plugin.Configuration.Save();
            }
            ImGui.SetNextItemWidth(_splitterWidth * 0.3f);
            var ACTPicInterval = Plugin.Configuration.ACTPicInterval;
            if (ImGui.InputFloat("图表刷新频率(s)", ref ACTPicInterval)) {
                Plugin.Configuration.ACTPicInterval = ACTPicInterval;
                Plugin.Configuration.Save();
            }
            ImGui.Separator();
            PopulateTreeView();
            LoadNodeData();
            foreach (var rootNode in _treeNodes) DrawTreeNodeRecursive(rootNode);
            ImGui.EndChild();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - _splitterThickness / 2);
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 1f));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
        ImGui.Button("##VSplitter", new Vector2(_splitterThickness, -1));
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
        if (ImGui.IsItemActive() || _isDraggingSplitterV && ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
            _isDraggingSplitterV = true;
            var deltaX = ImGui.GetIO().MouseDelta.X;
            _splitterWidth += deltaX;
        }
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            _isDraggingSplitterV = false;
        ImGui.SameLine();
        var rightPanelWidth = totalAvail.X - _splitterWidth - _splitterThickness / 2;
        if (ImGui.BeginChild("##RightPanel", new Vector2(rightPanelWidth, -1), true)) {
            var rightPanelAvail = ImGui.GetContentRegionAvail();
            if (_splitterHeight <= 0) _splitterHeight = rightPanelAvail.Y;
            var dataListHeight = Math.Clamp(_splitterHeight, 50, rightPanelAvail.Y - 50);
            if (ImGui.BeginChild("##DataListView", new Vector2(-1, dataListHeight), true)) {
                DrawNavigationBar();
                DrawDataListView();
                ImGui.EndChild();
            }
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - _splitterThickness / 2);
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 1f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.Button("##HSplitter", new Vector2(-1, _splitterThickness));
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNs);
            if (ImGui.IsItemActive() || _isDraggingSplitterH && ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
                _isDraggingSplitterH = true;
                var deltaY = ImGui.GetIO().MouseDelta.Y;
                _splitterHeight += deltaY;
            }
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left)) _isDraggingSplitterH = false;
            var graphViewHeight = rightPanelAvail.Y - dataListHeight - _splitterThickness / 2;
            if (ImGui.BeginChild("##GraphView", new Vector2(-1, graphViewHeight), true)) {
                GraphViewSize = ImGui.GetContentRegionAvail();
                if (Plugin.Configuration.UseActPic) DrawGraph();
                else {
                    switch (_tableType) {
                        case "MD": // DamageTypeData - 饼图
                            if (_currentSelectedNode?.Data is DamageTypeData damageType)
                                DrawDamageTypeGraphImPlot(damageType, _mDSort);
                            break;
                        case "AT": // AttackType - 攻击柱状图
                            if (_currentSelectedNode?.Data is AttackType attackType)
                                DrawAttackTypeGraphImPlot(attackType, _aTSort);
                            break;
                        case "ED": // EncounterData - 遭遇战柱状图
                            if (_currentSelectedNode?.Data is EncounterData encounterData)
                                DrawEncounterGraphImPlot(encounterData, _eDSort);
                            break;
                        default:
                            ImGui.Text("暂无可用图表数据");
                            break;
                    }
                }
                ImGui.EndChild();
            }
            ImGui.EndChild();
        }
    }

    private float _splitterHeight = 200, _splitterWidth = 300;
    private bool _isDraggingSplitterH, _isDraggingSplitterV;
    private const float _splitterThickness = 10f;

    private void DrawNavigationBar() {
        if (_tableType != "EL") {
            if (ImGui.Button("←###NavBackBtn", new Vector2(32, 32))) _currentSelectedNode = _currentSelectedNode?.Parent;
            ImGui.SameLine();
        }
        ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.TabActive));
        ImGui.Button($"{_currentSelectedNode?.Text ?? "Encounters"}({_tableType})", new Vector2(-1, 32));
        ImGui.PopStyleColor();
    }


    private void DrawTreeNodeRecursive(TreeNodeData node) {
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
        if (node.Children.Count == 0) flags |= ImGuiTreeNodeFlags.Leaf;
        var textColor = ColorToImGui(node.ForeColor);
        ImGui.PushStyleColor(ImGuiCol.Text, textColor);
        var expanded = ImGui.TreeNodeEx($"{node.Text}###ACT_{node.Guid}", flags);
        if (ImGui.IsItemClicked()) _currentSelectedNode = node;
        ImGui.PopStyleColor();
        if (expanded) {
            foreach (var child in node.Children) DrawTreeNodeRecursive(child);
            ImGui.TreePop();
        }
    }

    private void DrawDataListView() {
        if (string.IsNullOrEmpty(_tableType)) {
            ImGui.Text("No data to display");
            return;
        }
        var columns = GetTableColumns();
        if (columns.Length == 0) return;

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2, 1));
        if (ImGui.BeginTable("##DataTable", columns.Length, Plugin.ImGuiTableFlag)) {
            foreach (var t in columns)
                ImGui.TableSetupColumn(t);
            ImGui.TableHeadersRow();
            for (var i = 0; i < _currentTable.Count; i++) {
                ImGui.TableNextRow();
                var rowData = GetRowData(i);
                if (rowData == null) continue;
                for (var j = 0; j < rowData.Count; j++) {
                    ImGui.TableNextColumn();
                    var cellColor = GetCellColor(i, j);
                    if (cellColor != Color.Transparent) {
                        ImGui.PushStyleColor(ImGuiCol.Text, ColorToImGui(cellColor));
                    }
                    ImGui.Text(rowData[j]);
                    if (cellColor != Color.Transparent)
                        ImGui.PopStyleColor();
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        HandleItemActivate(i);
                }
            }
            ImGui.EndTable();
        }
        ImGui.PopStyleVar();
    }

    private void DrawGraph() {
        lock (_textureLock) {
            if (_currentTexture != null) {
                var graphSize = ImGui.GetContentRegionAvail();
                var pos = ImGui.GetCursorPos();
                ImGui.SetCursorPos(pos);
                ImGui.Image(_currentTexture.Handle, graphSize);
            }
            else {
                ImGui.Text("No graph data");
            }
        }
    }


    private void PopulateTreeView(TreeNodeData? parentNode = null) {
        try {
            switch (parentNode) {
                case null: {
                    for (var i = 0; i < oFormActMain.ZoneList.Count; i++) {
                        TreeNodeData node;
                        if (_treeNodes.Count > i) node = _treeNodes[i];
                        else {
                            var zone = oFormActMain.ZoneList[i];
                            node = new TreeNodeData {
                                Text = zone.ToString(),
                                Tag = "ZoneData",
                                Index = i,
                                Data = zone
                            };
                            _treeNodes.Add(node);
                        }
                        _currentSelectedNode ??= node;
                        PopulateTreeView(node);
                    }
                    break;
                }
                case { Tag: "ZoneData", Data: ZoneData zoneData }: {
                    for (var i = 0; i < zoneData.Items.Count; i++) {
                        var encounter = zoneData.Items[i];
                        TreeNodeData node;
                        if (parentNode.Children.Count > i) {
                            node = parentNode.Children[i];
                            node.Text = encounter.ToString();
                            node.Data = encounter;
                            node.ForeColor = GetEncounterColor(encounter);
                        }
                        else {
                            node = new TreeNodeData {
                                Text = encounter.ToString(),
                                Tag = "EncounterData",
                                Index = i,
                                Parent = parentNode,
                                Data = encounter,
                                ForeColor = GetEncounterColor(encounter)
                            };
                            parentNode.Children.Add(node);
                        }

                        PopulateTreeView(node);
                    }
                    break;
                }
                case { Tag: "EncounterData", Data: EncounterData encounterData }: {
                    var combatantList = encounterData.Items.Values.ToList();
                    for (var idx = 0; idx < combatantList.Count; idx++) {
                        var combatant = combatantList[idx];
                        TreeNodeData node;
                        if (parentNode.Children.Count > idx) {
                            node = parentNode.Children[idx];
                            node.Text = combatant.ToString();
                            node.Data = combatant;
                        }
                        else {
                            node = new TreeNodeData {
                                Text = combatant.ToString(),
                                Tag = "CombatantData",
                                Index = idx,
                                Parent = parentNode,
                                Data = combatant
                            };
                            parentNode.Children.Add(node);
                        }

                        PopulateTreeView(node);
                    }
                    break;
                }
                case { Tag: "CombatantData", Data: CombatantData combatantData }: {
                    var damageTypeList = combatantData.Items.Values.ToList();
                    for (var idx = 0; idx < damageTypeList.Count; idx++) {
                        var damageType = damageTypeList[idx];
                        TreeNodeData node;
                        if (parentNode.Children.Count > idx) {
                            node = parentNode.Children[idx];
                            node.Text = damageType.ToString();
                            node.Data = damageType;
                        }
                        else {
                            node = new TreeNodeData {
                                Text = damageType.ToString(),
                                Tag = "DamageTypeData",
                                Index = idx,
                                Parent = parentNode,
                                Data = damageType
                            };
                            parentNode.Children.Add(node);
                        }

                        PopulateTreeView(node);
                    }
                    break;
                }
                case { Tag: "DamageTypeData", Data: DamageTypeData damageTypeData }: {
                    var attackTypeList = damageTypeData.Items.Values.ToList();
                    for (var idx = 0; idx < attackTypeList.Count; idx++) {
                        var attackType = attackTypeList[idx];
                        TreeNodeData node;
                        if (parentNode.Children.Count > idx) {
                            node = parentNode.Children[idx];
                            node.Text = attackType.ToString();
                            node.Data = attackType;
                        }
                        else {
                            node = new TreeNodeData {
                                Text = attackType.ToString(),
                                Tag = "AttackType",
                                Index = idx,
                                Parent = parentNode,
                                Data = attackType
                            };
                            parentNode.Children.Add(node);
                        }
                    }
                    break;
                }
            }
        }
        catch (Exception ex) {
            Plugin.Log.Error($"Failed to populate tree view: {ex}");
        }
    }

    private void LoadNodeData() {
        var canUpdateImage = Plugin.Configuration.UseActPic && (DateTime.Now - lastDraw).TotalSeconds > Plugin.Configuration.ACTPicInterval;
        if (canUpdateImage) {
            lock (_textureLock) {
                _currentTexture?.Dispose();
                _currentTexture = null;
            }
            lastDraw = DateTime.Now;
        }
        try {
            switch (_currentSelectedNode?.Tag) {
                case "ZoneData":
                    _tableType = "EL";
                    _currentTable = new List<object>(oFormActMain.ZoneList[_currentSelectedNode.Index].Items);
                    break;
                case "EncounterData":
                    _tableType = "ED";
                    var encounter = (EncounterData)_currentSelectedNode.Data!;
                    var combatantList = new List<object>(encounter.Items.Values);
                    combatantList.Sort();
                    _currentTable = combatantList;
                    if (canUpdateImage)
                        UpdateGraphTexture(GenEncounterGraph(encounter, (int)GraphViewSize.X, (int)GraphViewSize.Y, _eDSort));
                    break;
                case "CombatantData":
                    _tableType = "CD";
                    var combatant = (CombatantData)_currentSelectedNode.Data!;
                    _currentTable = new List<object>(combatant.Items.Values);
                    break;
                case "DamageTypeData":
                    _tableType = "MD";
                    var damageType = (DamageTypeData)_currentSelectedNode.Data!;
                    var attackTypeList = new List<object>(damageType.Items.Values);
                    attackTypeList.Sort();
                    _currentTable = attackTypeList;
                    if (canUpdateImage)
                        UpdateGraphTexture(GenDamageTypeGraph(damageType, (int)GraphViewSize.X, (int)GraphViewSize.Y, _mDSort));
                    break;
                case "AttackType":
                    _tableType = "AT";
                    var attackType = (AttackType)_currentSelectedNode.Data!;
                    var swingList = new List<object>(attackType.Items);
                    swingList.Sort();
                    _currentTable = swingList;
                    if (canUpdateImage)
                        UpdateGraphTexture(GenAttackTypeGraph(attackType, (int)GraphViewSize.X, (int)GraphViewSize.Y, _aTSort));
                    break;
            }
        }
        catch (Exception ex) {
            Plugin.Log.Error($"Failed to load node data: {ex}");
        }
    }

    private string[] GetTableColumns() {
        return _tableType switch {
            "EL" => clbZD,
            "ED" => clbED,
            "CD" => clbCD,
            "MD" => clbDT,
            "AT" => clbAT,
            _ => []
        };
    }

    [SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract")]
    private List<string>? GetRowData(int index) {
        if (index >= _currentTable.Count) {
            return null;
        }

        var row = new List<string>();
        var columns = GetTableColumns();
        try {
            switch (_tableType) {
                case "EL":
                    var encounter = (EncounterData)_currentTable[index];
                    row.AddRange(columns.Select(col => encounter.GetColumnByName(col) ?? "N/A"));
                    break;

                case "ED":
                    var combatant = (CombatantData)_currentTable[index];
                    row.AddRange(columns.Select(col => CombatantData.ColumnDefs[col].GetCellData(combatant) ?? "N/A"));
                    break;

                case "CD":
                    var damageType = (DamageTypeData)_currentTable[index];
                    row.AddRange(columns.Select(col => DamageTypeData.ColumnDefs[col].GetCellData(damageType) ?? "N/A"));
                    break;

                case "MD":
                    var attackType = (AttackType)_currentTable[index];
                    row.AddRange(columns.Select(col => AttackType.ColumnDefs[col].GetCellData(attackType) ?? "N/A"));
                    break;

                case "AT":
                    var swing = (MasterSwing)_currentTable[index];
                    row.AddRange(columns.Select(col => MasterSwing.ColumnDefs[col].GetCellData(swing) ?? "N/A"));
                    break;
            }
        }
        catch (Exception ex) {
            Plugin.Log.Error($"Failed to get row data: {ex}");
            row.AddRange(columns.Select(_ => "Error"));
        }
        return row;
    }

    private void HandleItemActivate(int index) {
        try {
            if (index >= _currentTable.Count) return;
            var rowDataObj = _currentTable[index];
            var childNode = _currentSelectedNode!.Children
                .FirstOrDefault(n => n.Data == rowDataObj);
            if (childNode == null) return;
            _currentSelectedNode = childNode;
        }
        catch (Exception ex) {
            Plugin.Log.Error($"Failed to activate item: {ex}");
        }
    }

    private DateTime lastDraw = DateTime.MinValue;

    private void UpdateGraphTexture(Bitmap bitmap) {
        lock (_textureLock) {
            _currentTexture?.Dispose();
            _currentTexture = Bitmap2Texture(bitmap);
        }
    }

    public static unsafe IDalamudTextureWrap Bitmap2Texture(Bitmap bitmap) {
        ReadOnlySpan<byte> pixelSpan;
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        try {
            pixelSpan = new ReadOnlySpan<byte>((void*)bitmapData.Scan0, bitmapData.Stride * bitmapData.Height);
        }
        finally {
            bitmap.UnlockBits(bitmapData);
        }
        return Plugin.TextureProvider.CreateFromRaw(RawImageSpecification.Rgba32(bitmap.Width, bitmap.Height), pixelSpan);
    }

    internal static uint ColorToImGui(Color color) =>
        ImGui.GetColorU32(new Vector4(
            color.R / 255f,
            color.G / 255f,
            color.B / 255f,
            color.A / 255f
        ));


    private static Color GetEncounterColor(EncounterData encounter) {
        try {
            return encounter.GetEncounterSuccessLevel() switch {
                1 => Color.FromArgb(-14513374),
                2 => Color.FromArgb(-29696),
                3 => Color.FromArgb(-2354116),
                _ => Color.FromArgb(-16744193)
            };
        }
        catch {
            return Color.White;
        }
    }


    private static Color GetCellColor(int rowIndex, int colIndex) {
        return colIndex % 2 == 0 ? Color.Transparent : Color.Bisque;
    }

    private const ImPlotFlags plotFlag = ImPlotFlags.NoTitle | ImPlotFlags.NoLegend | ImPlotFlags.NoFrame | ImPlotFlags.NoBoxSelect;

    private void DrawDamageTypeGraphImPlot(DamageTypeData damageTypeSource, string sorting) {
        var list = new List<AttackType>(damageTypeSource.Items.Values);
        List<StrDouble> dataList = [];
        try {
            list.Sort(AttackType.ColumnDefs[sorting].SortComparer);
            list.Reverse();
        }
        catch (Exception ex) {
            ImGui.Text($"排序失败: {ex.Message}");
            return;
        }
        var attackTypeAll = list.FirstOrDefault(item => item.Type == Trans["attackTypeTerm-all"]);
        if (sorting == "Resist") {
            var dict = new Dictionary<string, long> {
                {
                    Trans["attackTypeTerm-all"], 0L
                }
            };
            if (attackTypeAll != null) {
                foreach (var masterSwing in attackTypeAll.Items) {
                    var text = masterSwing.DamageType;
                    if (text.StartsWith(Trans["specialAttackTerm-warded"] + "/"))
                        text = text[7..];
                    dict.TryAdd(text, 0L);
                    if ((long)masterSwing.Damage <= 0) continue;
                    dict[text] += masterSwing.Damage;
                    dict[Trans["attackTypeTerm-all"]] += masterSwing.Damage;
                }
            }
            dataList.AddRange(dict.Where(kv => kv.Key != Trans["attackTypeTerm-all"])
                .Select(kv => new StrDouble(kv.Key, kv.Value / (double)dict[Trans["attackTypeTerm-all"]])));
        }
        else {
            foreach (var item in list.Where(item => item.Type != Trans["attackTypeTerm-all"])) {
                try {
                    var val = double.Parse(AttackType.ColumnDefs[sorting].GetSqlData(item), usCulture);
                    if (val > 0.0 && attackTypeAll != null)
                        dataList.Add(new StrDouble(item.Type, val / double.Parse(AttackType.ColumnDefs[sorting].GetSqlData(attackTypeAll), usCulture)));
                }
                catch {
                    //
                }
            }
        }
        if (dataList.Count == 0) {
            ImGui.Text(Trans["graphAttackTypes-noDataError"]);
            return;
        }
        var labels = dataList.Select(d => d.Name).ToArray();
        var values = dataList.Select(d => (float)d.Val * 100).ToArray();
        var size = Math.Min(GraphViewSize.X, GraphViewSize.Y);
        if (ImPlot.BeginPlot("##DamageTypePie", new Vector2(size, size), plotFlag)) {
            ImPlot.SetupAxes("Combatant", "Damage", ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.AutoFit);
            ImPlot.PlotPieChart(labels, ref values[0], values.Length, 0, 0, 1);
            ImPlot.EndPlot();
        }
        if (GraphViewSize.X > GraphViewSize.Y) ImGui.SameLine();
        ImGui.BeginChild("##DamageTypeLabels", GraphViewSize with {
            X = 200
        });
        for (var i = 0; i < dataList.Count; i++) {
            var item = dataList[i];
            var percent = (int)(item.Val * 100);
            ImGui.Button($"##color_{i}", new Vector2(16, 16));
            ImGui.SameLine();
            ImGui.Text($"{percent}% - {item.Name}");
        }
        ImGui.EndChild();
    }

    private void DrawAttackTypeGraphImPlot(AttackType attackTypeSource, string sorting) {
        var list = new List<MasterSwing>(attackTypeSource.Items);
        try {
            list.Sort(new MasterSwing.DualComparison(sorting, sorting));
        }
        catch (Exception ex) {
            ImGui.Text($"排序失败: {ex.Message}");
            return;
        }
        var damageValues = list.Where(swing => swing.Damage > 1).Select(swing => (long)swing.Damage).ToArray();
        if (damageValues.Length == 0) {
            ImGui.Text("无攻击数据");
            return;
        }
        var yData = list.Select(swing => (double)swing.Damage).ToArray();
        if (ImPlot.BeginPlot("##AttackTypeBars", GraphViewSize, plotFlag)) {
            ImPlot.SetupAxes("Hit", "Damage", ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.AutoFit);
            ImPlot.PlotBars("##Bar", ref yData[0], yData.Length);
            for (var i = 0; i < list.Count; i++) {
                var swing = list[i];
                if (GraphViewSize.X / list.Count > 16 * DpiScale) {
                    var damageStr = oFormActMain.CreateDamageString(swing.Damage, true, true);
                    ImPlot.PlotText(damageStr, i, swing.Damage / 2);
                }
            }
            ImPlot.EndPlot();
        }
    }

    private void DrawEncounterGraphImPlot(EncounterData? encounterSource, string sorting) {
        try {
            if (encounterSource == null) {
                ImGui.Text("无遭遇战数据");
                return;
            }

            var list = new List<CombatantData>(encounterSource.Items.Values);
            try {
                list.Sort(CombatantData.ColumnDefs[sorting].SortComparer);
                list.Reverse();
            }
            catch (Exception ex) {
                ImGui.Text($"排序失败: {ex.Message}");
                return;
            }

            var list2 = !encounterSource.GetIsSelective() || !encounterSource.GetIgnoreEnemies() ? encounterSource.GetAllies() : list;
            var dataList = new List<StrDouble>();
            double total = 0;
            foreach (var item in list.Where(item => list2.Contains(item))) {
                try {
                    var val = double.Parse(CombatantData.ColumnDefs[sorting].GetSqlData(item), usCulture);
                    if (!(val > 0)) continue;
                    dataList.Add(new StrDouble(item.Name, val));
                    total += val;
                }
                catch {
                    //
                }
            }
            if (dataList.Count == 0) {
                ImGui.Text(list.Count > 0 ? Trans["graphEncText-noDataSortingError"] : Trans["graphEncText-noDataError"]);
                return;
            }
            var yVals = dataList.Select(d => (float)d.Val).ToArray();
            var labels = dataList.Select(d => d.Name).ToArray();
            if (ImPlot.BeginPlot("##Encounter Damage Distribution", GraphViewSize, plotFlag)) {
                ImPlot.SetupAxes("Combatant", "Damage", ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.AutoFit);
                ImPlot.PlotBars("##Damage per Combatant", ref yVals[0], yVals.Length);
                ImPlot.PlotText($"Total: {total:N0}\nAvg: {total / dataList.Count:N2}", dataList.Count, yVals.Max() / 2);
                for (var i = 0; i < dataList.Count; i++)
                    ImPlot.PlotText(labels[i], i, yVals[i] / 2, ImPlotTextFlags.Vertical);
                ImPlot.EndPlot();
            }
        }
        catch (Exception ex) {
            ImGui.Text($"绘制遭遇战图表失败: {ex.Message}");
        }
    }
}