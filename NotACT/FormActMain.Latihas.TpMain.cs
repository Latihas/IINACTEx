using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
using static Advanced_Combat_Tracker.ActGlobals;
using Timer = System.Windows.Forms.Timer;

namespace Advanced_Combat_Tracker;

public class TpMain : Form {
    public delegate Bitmap EncounterGraphGenerator(EncounterData EncounterSource, int SizeX, int SizeY, string Sorting);

    public delegate Bitmap DamageTypeGraphGenerator(DamageTypeData DamageTypeSource, int SizeX, int SizeY, string Sorting);

    public delegate Bitmap AttackTypeGraphGenerator(AttackType AttackTypeSource, int SizeX, int SizeY, string Sorting);

    private CancellationTokenSource? graphingCts;
    private const float DpiScale = 1;
    private readonly Button btnNavBack;
    private readonly string[] clbAT = [
        // "EncId",
        "Time", "Attacker",
        // "SwingType",
        "AttackType", "DamageType", "Victim",
        // "DamageNum",
        "Damage", "Critical", "CriticalStr", "Special", "RelativeTime", "StatusDuration",
        // "OverHeal", 
        "DirectHit"
    ];
    private readonly string[] clbCD = [
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
    public readonly string[] clbDT = [
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
    private readonly string[] clbED = [
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

    private readonly string[] clbZD = [
        // "EncId", 
        "Title", "StartTime", "EndTime", "Duration", "Damage", "EncDPS",
        // "Zone", 
        "Kills", "Deaths"
    ];
    public readonly EncounterGraphGenerator GenerateEncounterGraph;
    public readonly DamageTypeGraphGenerator GenerateDamageTypeGraph;
    public readonly AttackTypeGraphGenerator GenerateAttackTypeGraph;
    private readonly Label lblDG;
    internal readonly ListViewNoFlicker lvDG;
    private readonly SortedDictionary<int, ListViewItem> lvDGItemsCache = new();
    private readonly PictureBox pbDG;
    private readonly Panel pDG;
    internal readonly TreeView tvDG;
    private readonly CultureInfo usCulture = new("en-US");
    private object currentTable = new List<object>();
    private volatile bool graphingThreadAlive;
    private TreeNode? lastSelectedNode;
    private int listViewWidthOffsetCalc = int.MinValue;
    internal volatile bool refreshTree = true;
    private bool resizeColumns;
    private string tableType = string.Empty;


    public TpMain() {
        GenerateEncounterGraph = GenEncounterGraph;
        GenerateDamageTypeGraph = GenDamageTypeGraph;
        GenerateAttackTypeGraph = GenAttackTypeGraph;
        SuspendLayout();
        tmrTick = new Timer {
            Interval = 1000
        };
        tmrTick.Tick += tmrTick_Tick;
        var splitContainerVertical = new SplitContainer {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 5,
            Size = new Size(1007, 540)
        };
        var pLeftView = new Panel {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 4, 4, 0)
        };

        var pTv = new Panel {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 1, 0, 0)
        };

        tvDG = new TreeView {
            Dock = DockStyle.Fill,
            HideSelection = false,
            PathSeparator = " | "
        };
        tvDG.AfterCheck += tv1_AfterCheck;
        tvDG.AfterExpand += tv1_AfterExpand;
        tvDG.AfterSelect += tv1_AfterSelect;

        pTv.Controls.Add(tvDG);
        pLeftView.Controls.Add(pTv);
        splitContainerVertical.Panel1.Controls.Add(pLeftView);

        var pRightView = new Panel {
            Dock = DockStyle.Fill,
            Padding = new Padding(2, 4, 0, 0)
        };

        var splitContainerHorizontal = new SplitContainer {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 5,
        };
        btnNavBack = new Button {
            FlatStyle = FlatStyle.Popup,
            Location = new Point(1, 1),
            Size = new Size(32, 32),
            Text = @"←",
            UseVisualStyleBackColor = true,
        };
        btnNavBack.Click += btnNavBack_Click;

        pDG = new Panel {
            Dock = DockStyle.Fill,
            Padding = new Padding(1, 1, 1, 5)
        };
        pDG.Resize += (_, _) => resizeColumns = true;
        lvDG = new ListViewNoFlicker {
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoArrange = false,
            CustomGridLines = true,
            FullRowSelect = true,
            GridLines = true,
            HideSelection = false,
            Location = new Point(1, 32),
            OwnerDraw = true,
            ShowGroups = false,
            Size = new Size(789, 246),
            UseCompatibleStateImageBehavior = false,
            View = View.Details,
            VirtualMode = true
        };
        lvDG.ColumnClick += lvDG_ColumnClick;
        lvDG.ItemActivate += lvDG_ItemActivate;
        lvDG.RetrieveVirtualItem += lvDG_RetrieveVirtualItem;

        lblDG = new Label {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoEllipsis = true,
            BackColor = Color.LightSteelBlue,
            BorderStyle = BorderStyle.Fixed3D,
            ForeColor = Color.MidnightBlue,
            Location = new Point(32, 1),
            Size = new Size(768, 32),
            Text = @"Encounters",
            TextAlign = ContentAlignment.MiddleLeft
        };

        pDG.Controls.Add(btnNavBack);
        pDG.Controls.Add(lvDG);
        pDG.Controls.Add(lblDG);
        splitContainerHorizontal.Panel1.Controls.Add(pDG);

        var pGraphing = new Panel {
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill
        };

        pbDG = new PictureBox {
            BackColor = SystemColors.ControlDarkDark,
            Cursor = Cursors.Cross,
            Dock = DockStyle.Fill,
            Padding = new Padding(1, 0, 0, 0)
        };
        splitContainerVertical.SplitterMoved += (_, _) => AfterResize();
        splitContainerHorizontal.SplitterMoved += (_, _) => AfterResize();
        FormClosing += (_, _) => {
            tmrTick.Enabled = false;
            graphingCts?.Cancel();
            graphingCts?.Dispose();
        };
        ResizeBegin += (_, _) => Resizing = true;
        ResizeEnd += (_, _) => {
            Resizing = false;
            AfterResize();
        };
        pGraphing.Controls.Add(pbDG);
        splitContainerHorizontal.Panel2.Controls.Add(pGraphing);
        pRightView.Controls.Add(splitContainerHorizontal);
        splitContainerVertical.Panel2.Controls.Add(pRightView);
        Controls.Add(splitContainerVertical);
        pRightView.ResumeLayout(false);
        pDG.ResumeLayout(false);
        pGraphing.ResumeLayout(false);
        ((ISupportInitialize)pbDG).EndInit();
        pLeftView.ResumeLayout(false);
        pTv.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
        tmrTick.Enabled = true;
    }

    private void AfterResize() {
        if (Resizing || lastSelectedNode == null) return;
        _tv1_AfterSelect(new TreeViewEventArgs(lastSelectedNode));
    }

    private bool Resizing;

    private void lvDG_RetrieveVirtualItem(object? _, RetrieveVirtualItemEventArgs e) {
        if (lvDGItemsCache.TryGetValue(e.ItemIndex, out var value)) {
            e.Item = value;
            return;
        }
        const bool @checked = false;
        var listViewItem = new ListViewItem {
            UseItemStyleForSubItems = false
        };
        var num = -1;
        switch (tableType) {
            case "EL": {
                var encounterData = ((List<EncounterData>)currentTable)[e.ItemIndex];
                switch (encounterData.GetEncounterSuccessLevel()) {
                    case 1:
                        listViewItem.ForeColor = Color.FromArgb(-14513374);
                        break;
                    case 2:
                        listViewItem.ForeColor = Color.FromArgb(-29696);
                        break;
                    case 3:
                        listViewItem.ForeColor = Color.FromArgb(-2354116);
                        break;
                    default:
                        listViewItem.ForeColor = Color.FromArgb(-16744193);
                        break;
                }
                foreach (var text2 in clbZD) {
                    num++;
                    if (num == 0)
                        try {
                            listViewItem.Text = encounterData.GetColumnByName(text2);
                        }
                        catch (Exception ex3) {
                            listViewItem.Text = Trans["ui-tableColumnError"];
                            WriteExceptionLog(ex3, $"{tableType}: {text2}");
                        }
                    else
                        try {
                            listViewItem.SubItems.Add(encounterData.GetColumnByName(text2));
                        }
                        catch (Exception ex4) {
                            listViewItem.SubItems.Add(Trans["ui-tableColumnError"]);
                            WriteExceptionLog(ex4, $"{tableType}: {text2}");
                        }
                }
                e.Item = listViewItem;
                break;
            }
            case "ED": {
                var combatantData = ((List<CombatantData>)currentTable)[e.ItemIndex];
                var list = (List<CombatantData>)currentTable;
                List<CombatantData>? list2 = [];
                if (list.Count > 0) list2 = list[0].Parent.GetAllies();
                var flag = list2?.Contains(combatantData) ?? false;
                var flag2 = combatantData.Name == charName;
                foreach (var text5 in clbED) {
                    num++;
                    if (num == 0)
                        try {
                            listViewItem.Text = CombatantData.ColumnDefs[text5].GetCellData(combatantData);
                            listViewItem.SubItems[0].ForeColor =
                                flag
                                    ? Color.FromArgb(-167441)
                                    : CombatantData.ColumnDefs[text5].GetCellForeColor(combatantData) == Color.Transparent
                                        ? lvDG.ForeColor
                                        : CombatantData.ColumnDefs[text5].GetCellForeColor(combatantData).ToAdjustedColor(@checked);
                            listViewItem.SubItems[0].BackColor =
                                flag2
                                    ? Color.FromArgb(-16711808)
                                    : CombatantData.ColumnDefs[text5].GetCellBackColor(combatantData) == Color.Transparent
                                        ? lvDG.BackColor
                                        : CombatantData.ColumnDefs[text5].GetCellBackColor(combatantData).ToAdjustedColor(@checked);
                        }
                        catch (Exception ex9) {
                            listViewItem.Text = Trans["ui-tableColumnError"];
                            WriteExceptionLog(ex9, $"{tableType}: {text5}");
                        }
                    else
                        try {
                            listViewItem.SubItems.Add(CombatantData.ColumnDefs[text5].GetCellData(combatantData),
                                CombatantData.ColumnDefs[text5].GetCellForeColor(combatantData) == Color.Transparent
                                    ? lvDG.ForeColor
                                    : CombatantData.ColumnDefs[text5].GetCellForeColor(combatantData).ToAdjustedColor(@checked),
                                flag2
                                    ? Color.FromArgb(-16711808)
                                    : CombatantData.ColumnDefs[text5].GetCellBackColor(combatantData) == Color.Transparent
                                        ? lvDG.BackColor
                                        : CombatantData.ColumnDefs[text5].GetCellBackColor(combatantData).ToAdjustedColor(@checked), lvDG.Font);
                        }
                        catch (Exception ex10) {
                            listViewItem.SubItems.Add(Trans["ui-tableColumnError"]);
                            WriteExceptionLog(ex10, $"{tableType}: {text5}");
                        }
                }
                e.Item = listViewItem;
                break;
            }
            case "CD": {
                var data2 = ((List<DamageTypeData>)currentTable)[e.ItemIndex];
                foreach (var text3 in clbCD) {
                    num++;
                    if (num == 0)
                        try {
                            listViewItem.Text = DamageTypeData.ColumnDefs[text3].GetCellData(data2);
                            listViewItem.SubItems[0].ForeColor = DamageTypeData.ColumnDefs[text3].GetCellForeColor(data2) == Color.Transparent
                                ? lvDG.ForeColor
                                : DamageTypeData.ColumnDefs[text3].GetCellForeColor(data2).ToAdjustedColor(@checked);
                            listViewItem.SubItems[0].BackColor = DamageTypeData.ColumnDefs[text3].GetCellBackColor(data2) == Color.Transparent
                                ? lvDG.BackColor
                                : DamageTypeData.ColumnDefs[text3].GetCellBackColor(data2).ToAdjustedColor(@checked);
                        }
                        catch (Exception ex5) {
                            listViewItem.Text = Trans["ui-tableColumnError"];
                            WriteExceptionLog(ex5, $"{tableType}: {text3}");
                        }
                    else
                        try {
                            listViewItem.SubItems.Add(DamageTypeData.ColumnDefs[text3].GetCellData(data2),
                                DamageTypeData.ColumnDefs[text3].GetCellForeColor(data2) == Color.Transparent ? lvDG.ForeColor : DamageTypeData.ColumnDefs[text3].GetCellForeColor(data2).ToAdjustedColor(@checked),
                                DamageTypeData.ColumnDefs[text3].GetCellBackColor(data2) == Color.Transparent ? lvDG.BackColor : DamageTypeData.ColumnDefs[text3].GetCellBackColor(data2).ToAdjustedColor(@checked),
                                lvDG.Font);
                        }
                        catch (Exception ex6) {
                            listViewItem.SubItems.Add(Trans["ui-tableColumnError"]);
                            WriteExceptionLog(ex6, $"{tableType}: {text3}");
                        }
                }
                e.Item = listViewItem;
                break;
            }
            case "MD": {
                var data3 = ((List<AttackType>)currentTable)[e.ItemIndex];
                foreach (var text4 in clbDT) {
                    num++;
                    if (num == 0)
                        try {
                            listViewItem.Text = AttackType.ColumnDefs[text4].GetCellData(data3);
                            listViewItem.SubItems[0].ForeColor = AttackType.ColumnDefs[text4].GetCellForeColor(data3) == Color.Transparent
                                ? lvDG.ForeColor
                                : AttackType.ColumnDefs[text4].GetCellForeColor(data3).ToAdjustedColor(@checked);
                            listViewItem.SubItems[0].BackColor = AttackType.ColumnDefs[text4].GetCellBackColor(data3) == Color.Transparent
                                ? lvDG.BackColor
                                : AttackType.ColumnDefs[text4].GetCellBackColor(data3).ToAdjustedColor(@checked);
                        }
                        catch (Exception ex7) {
                            listViewItem.Text = Trans["ui-tableColumnError"];
                            WriteExceptionLog(ex7, $"{tableType}: {text4}");
                        }
                    else
                        try {
                            listViewItem.SubItems.Add(AttackType.ColumnDefs[text4].GetCellData(data3),
                                AttackType.ColumnDefs[text4].GetCellForeColor(data3) == Color.Transparent ? lvDG.ForeColor : AttackType.ColumnDefs[text4].GetCellForeColor(data3).ToAdjustedColor(@checked),
                                AttackType.ColumnDefs[text4].GetCellBackColor(data3) == Color.Transparent ? lvDG.BackColor : AttackType.ColumnDefs[text4].GetCellBackColor(data3).ToAdjustedColor(@checked), lvDG.Font);
                        }
                        catch (Exception ex8) {
                            listViewItem.SubItems.Add(Trans["ui-tableColumnError"]);
                            WriteExceptionLog(ex8, $"{tableType}: {text4}");
                        }
                }
                e.Item = listViewItem;
                break;
            }
            case "AT": {
                var data = ((List<MasterSwing>)currentTable)[e.ItemIndex];
                foreach (var text in clbAT) {
                    num++;
                    if (num == 0)
                        try {
                            listViewItem.Text = MasterSwing.ColumnDefs[text].GetCellData(data);
                            listViewItem.SubItems[0].ForeColor = MasterSwing.ColumnDefs[text].GetCellForeColor(data) == Color.Transparent
                                ? lvDG.ForeColor
                                : MasterSwing.ColumnDefs[text].GetCellForeColor(data).ToAdjustedColor(@checked);
                            listViewItem.SubItems[0].BackColor = MasterSwing.ColumnDefs[text].GetCellBackColor(data) == Color.Transparent
                                ? lvDG.BackColor
                                : MasterSwing.ColumnDefs[text].GetCellBackColor(data).ToAdjustedColor(@checked);
                        }
                        catch (Exception ex) {
                            listViewItem.Text = Trans["ui-tableColumnError"];
                            WriteExceptionLog(ex, $"{tableType}: {text}");
                        }
                    else
                        try {
                            listViewItem.SubItems.Add(MasterSwing.ColumnDefs[text].GetCellData(data),
                                MasterSwing.ColumnDefs[text].GetCellForeColor(data) == Color.Transparent ? lvDG.ForeColor : MasterSwing.ColumnDefs[text].GetCellForeColor(data).ToAdjustedColor(@checked),
                                MasterSwing.ColumnDefs[text].GetCellBackColor(data) == Color.Transparent ? lvDG.BackColor : MasterSwing.ColumnDefs[text].GetCellBackColor(data).ToAdjustedColor(@checked), lvDG.Font);
                        }
                        catch (Exception ex2) {
                            listViewItem.SubItems.Add(Trans["ui-tableColumnError"]);
                            WriteExceptionLog(ex2, $"{tableType}: {text}");
                        }
                }
                e.Item = listViewItem;
                break;
            }
        }
        if (e.Item != null) lvDGItemsCache.Add(e.ItemIndex, e.Item);
    }

    private void lvDG_ColumnClick(object? _, ColumnClickEventArgs e) {
        var num = -1;
        var column = e.Column;
        switch (tableType) {
            case "ED": {
                foreach (var text2 in clbED) {
                    // if (clbED.GetItemChecked(j))
                    if (column != ++num) continue;
                    // if (eDSort == text2)
                    // opMainTableGen.cbReverseSort.Checked = !opMainTableGen.cbReverseSort.Checked;
                    eDSort2 = eDSort;
                    // opTableEncounter.btnEDSort2.Text = opTableEncounter.btnEDSort.Text;
                    eDSort = text2;
                    // opTableEncounter.btnEDSort.Text = text2;
                    break;
                }
                break;
            }
            case "MD": {
                foreach (var text3 in clbDT) {
                    // if (clbDT.GetItemChecked(k))
                    num++;
                    if (column == num) {
                        // if (mDSort == text3)
                        // opMainTableGen.cbReverseSort.Checked = !opMainTableGen.cbReverseSort.Checked;
                        mDSort2 = mDSort;
                        // opTableDamageType.btnMDSort2.Text = opTableDamageType.btnMDSort.Text;
                        mDSort = text3;
                        // opTableDamageType.btnMDSort.Text = text3;
                        break;
                    }
                }
                break;
            }
            case "AT": {
                foreach (var text in clbAT) {
                    // if (opTableAttackType.clbAT.GetItemChecked(i))
                    num++;
                    if (column == num) {
                        // if (aTSort == text)
                        //     opMainTableGen.cbReverseSort.Checked = !opMainTableGen.cbReverseSort.Checked;
                        aTSort2 = aTSort;
                        // opTableAttackType.btnATSort2.Text = opTableAttackType.btnATSort.Text;
                        aTSort = text;
                        // opTableAttackType.btnATSort.Text = text;
                        break;
                    }
                }
                break;
            }
        }
        if (num <= -1)
            return;
        try {
            if (tableType == "AT" || tableType == "MD" || tableType == "ED") {
                switch (tableType) {
                    case "AT":
                        try {
                            var list3 = (List<MasterSwing>)currentTable;
                            list3.Sort();
                            // if (opMainTableGen.cbReverseSort.Checked)
                            //     list3.Reverse();
                        }
                        catch {
                            //
                        }
                        break;
                    case "MD":
                        try {
                            var list2 = (List<AttackType>)currentTable;
                            list2.Sort();
                            // if (opMainTableGen.cbReverseSort.Checked)
                            //     list2.Reverse();
                        }
                        catch {
                            //
                        }
                        break;
                    case "ED":
                        try {
                            var list = (List<CombatantData>)currentTable;
                            list.Sort();
                            // if (opMainTableGen.cbReverseSort.Checked)
                            //     list.Reverse();
                        }
                        catch {
                            //
                        }
                        break;
                }
            }
            lastSelectedNode = null;
            // tv1_AfterSelect(sender, new TreeViewEventArgs(tvDG.SelectedNode, TreeViewAction.Unknown));
        }
        catch (Exception ex) {
            WriteExceptionLog(ex, string.Empty);
        }
    }

    private void lvDG_ItemActivate(object? _, EventArgs e) {
        try {
            var index = lvDG.SelectedIndices[0];
            var listViewItem = lvDG.Items[index];
            // if (tableType == "AT") {
            //     MasterSwing masterSwing = ((List<MasterSwing>)currentTable)[lvDG.SelectedIndices[0]];
            //     FormEncounterLogs formEncounterLogs = new FormEncounterLogs();
            //     formEncounterLogs.ShowLogs(masterSwing.ParentEncounter.LogLines);
            //     formEncounterLogs.ScrollToGlobalTimeSorter(masterSwing.TimeSorter);
            //     return;
            // }
            var text = tableType != "EL" ? listViewItem.Text : ((List<EncounterData>)currentTable)[index].ToString();
            var selectedNode = tvDG.SelectedNode;
            TreeNode? treeNode = null;
            if (selectedNode != null) {
                selectedNode.Expand();
                treeNode = selectedNode.Nodes.Cast<TreeNode>().FirstOrDefault(node => node.Text == text);
            }
            if (treeNode != null)
                tvDG.SelectedNode = treeNode;
        }
        catch {
            //
        }
    }

    internal Timer tmrTick;

    private void tmrTick_Tick(object? _, EventArgs e) {
        try {
            if (resizeColumns) {
                resizeColumns = false;
                lvDG.BeginUpdate();
                ResizeLVCols(lvDG);
                lvDG.EndUpdate();
            }
            if (refreshTree && WindowState != FormWindowState.Minimized || oFormActMain.InCombat)
                PopulateTView();
        }
        catch (Exception ex) {
            WriteExceptionLog(ex, "1");
        }
    }


    private void btnNavBack_Click(object? _, EventArgs __) {
        if (tableType == "EL") return;
        if (tvDG.SelectedNode == null) return;
        tvDG.SelectedNode = tvDG.SelectedNode.Parent;
        tvDG.SelectedNode?.Collapse();
    }

    private void tv1_AfterCheck(object? _, TreeViewEventArgs e) {
        if (e.Action == TreeViewAction.Unknown || e.Node == null)
            return;
        e.Node.Expand();
        for (var i = 0; i < e.Node.Nodes.Count; i++) {
            var treeNode = e.Node.Nodes[i];
            if (i != 0 || treeNode.Parent != null && !oFormActMain.ZoneList[treeNode.Parent.Index].PopulateAll)
                treeNode.Checked = e.Node.Checked;
        }
    }

    private void tv1_AfterExpand(object? _, TreeViewEventArgs __) {
        PopulateTView();
    }


    private bool TreeViewIsActiveEncounter() {
        EncounterData? encounterData = null;
        switch (tableType) {
            case "EL":
                return false;
            case "ED":
                if (tvDG.SelectedNode?.Parent == null) return false;
                encounterData = oFormActMain.ZoneList[tvDG.SelectedNode.Parent.Index].Items[tvDG.SelectedNode.Index];
                break;
            case "CD":
                if (tvDG.SelectedNode?.Parent?.Parent == null) return false;
                encounterData = oFormActMain.ZoneList[tvDG.SelectedNode.Parent.Parent.Index].Items[tvDG.SelectedNode.Parent.Index];
                break;
            case "MD":
                if (tvDG.SelectedNode?.Parent?.Parent?.Parent == null) return false;
                encounterData = oFormActMain.ZoneList[tvDG.SelectedNode.Parent.Parent.Parent.Index].Items[tvDG.SelectedNode.Parent.Parent.Index];
                break;
            case "AT":
                if (tvDG.SelectedNode?.Parent?.Parent?.Parent?.Parent == null) return false;
                encounterData = oFormActMain.ZoneList[tvDG.SelectedNode.Parent.Parent.Parent.Parent.Index].Items[tvDG.SelectedNode.Parent.Parent.Parent.Index];
                break;
        }
        return Equals(encounterData, oFormActMain.ActiveZone.ActiveEncounter);
    }

    internal static Bitmap GraphDrawMessage(string Message, float FontSize, Bitmap BlankImage) {
        var graphics = Graphics.FromImage(BlankImage);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.FromArgb(-1842205));
        graphics.DrawString(Message, new Font("Arial Black", FontSize, FontStyle.Regular), new SolidBrush(Color.FromArgb(-16777216)), 12f, 12f);
        return BlankImage;
    }

    internal Bitmap GenDamageTypeGraph(DamageTypeData DamageTypeSource, int SizeX, int SizeY, string Sorting) {
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
                        if (num > 0.0)
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
            var num2 = 16f * DpiScale;
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
                    var num7 = 26f * DpiScale;
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
    internal Bitmap GenAttackTypeGraph(AttackType AttackTypeSource, int SizeX, int SizeY, string Sorting) {
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
            var num = 16f * DpiScale;
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
                var w = num2;
                var num10 = (long)masterSwing.Damage * (float)num6;
                if (i > 0 && masterSwing.Time != list[i - 1].Time)
                    graphics.DrawLine(pen2, num3, rectangleF.Top, num3, rectangleF.Bottom);
                graphics.FillRectangle(masterSwing.Critical ? dictionary2[masterSwing.SwingType] : dictionary[masterSwing.SwingType], num8, num9, w, num10);
                graphics.DrawRectangle(pen, num8, num9, w, num10);
                // ttg.Items.Add(new ToolTipRect(-1, $"{masterSwing.Time}\n{masterSwing.Attacker} -> {masterSwing.Victim}\n{masterSwing.AttackType} {(long)masterSwing.Damage:#,0}", num8, rectangleF.Y, w, rectangleF.Height));
                if (rectangleF.Width / list.Count > 16f * DpiScale) {
                    var s = (((long)masterSwing.Damage > 0)
                        ? oFormActMain.CreateDamageString(masterSwing.Damage, true, true)
                        : masterSwing.Damage.ToString(ShortHand: true));
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
        catch (ThreadAbortException) {
            // WriteInfoLog("GenAttackTypeGraph -> ThreadAbortException");
        }
        catch (Exception ex4) {
            WriteExceptionLog(ex4, string.Empty);
            bitmap = GraphDrawMessage(ex4.ToString(), 12f, bitmap);
        }
        // oFormActMain.WriteDebugLog("GraphHitBars: " + (DateTime.Now - now).TotalMilliseconds.ToString("F"));
        return bitmap;
    }

    internal Bitmap GenEncounterGraph(EncounterData? EncounterSource, int SizeX, int SizeY, string Sorting) {
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
            var solidBrush2 = new SolidBrush(Color.FromArgb(-7667712));
            var solidBrush3 = new SolidBrush(Color.FromArgb(-16777077));
            var brush3 = new SolidBrush(Color.FromArgb(-7077677));
            var brush4 = new SolidBrush(Color.FromArgb(-1));
            var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(solidBrush.Color);
            var num = 0.0;
            var list3 = new List<StrDouble>();
            foreach (var item in list) {
                // opGraphing.cbOnlyGraphAllies.Checked && 
                if (list2.Count > 0 && list2.IndexOf(item) == -1
                    // ||  (opSelectiveParsing.rbSParseExport.Checked && !SelectiveListGetSelected(item.Name))
                   )
                    continue;
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
            var num3 = 16f * DpiScale;
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
            foreach (var item2 in list3) {
                if (item2.Val > num6)
                    num6 = Math.Ceiling(item2.Val);
            }
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

    private void ThreadEncSamples(object EncounterSource) {
        graphingThreadAlive = true;
        pbDG.Image = GenerateEncounterGraph((EncounterData)EncounterSource, pbDG.Width, pbDG.Height, eDSort);
        graphingThreadAlive = false;
    }

    private void StartEncThread(EncounterData EncounterSource) {
        try {
            graphingCts?.Cancel();
            graphingCts?.Dispose();
        }
        catch {
            //
        }
        graphingCts = new CancellationTokenSource();
        Task.Run(() => ThreadEncSamples(EncounterSource), graphingCts.Token);
    }

    private void _tv1_AfterSelect(TreeViewEventArgs e) {
        pbDG.Image = null;
        try {
            if (graphingThreadAlive) {
                graphingCts?.Cancel();
                graphingCts?.Dispose();
            }
            graphingThreadAlive = false;
        }
        catch {
            //
        }
        lvDG.BeginUpdate();
        try {
            lvDGItemsCache.Clear();
            switch ((string)e.Node.Tag) {
                default:
                    return;
                case "ZoneData":
                    tableType = "EL";
                    // lvDG.ContextMenuStrip = cmsEList;
                    currentTable = oFormActMain.ZoneList[e.Node.Index].Items;
                    break;
                case "EncounterData": {
                    tableType = "ED";
                    // lvDG.ContextMenuStrip = cmsEDSort;
                    var encounterData = oFormActMain.ZoneList[e.Node.Parent.Index].Items[e.Node.Index];
                    var list2 = (List<CombatantData>)(currentTable = new List<CombatantData>(encounterData.Items.Values));
                    try {
                        list2.Sort();
                        // if (opMainTableGen.cbReverseSort.Checked)
                        //     list2.Reverse();
                    }
                    catch (Exception ex3) {
                        WriteExceptionLog(ex3, string.Empty);
                    }
                    if (e.Action == TreeViewAction.Expand)
                        break;
                    try {
                        // if (cmiEnabled.Checked)
                        StartEncThread(encounterData);
                    }
                    catch {
                        //
                    }
                    break;
                }
                case "CombatantData": {
                    tableType = "CD";
                    // lvDG.ContextMenuStrip = cmsCDSort;
                    var encounterData = oFormActMain.ZoneList[e.Node.Parent.Parent.Index].Items[e.Node.Parent.Index];
                    var combatant = encounterData.GetCombatant(e.Node.Text);
                    currentTable = new List<DamageTypeData>(combatant.Items.Values);
                    // if (e.Action != TreeViewAction.Expand && cmiEnabled.Checked)
                    // 	StartSoloThread(combatant);
                    break;
                }
                case "DamageTypeData": {
                    tableType = "MD";
                    // lvDG.ContextMenuStrip = cmsMDSort;
                    var damageTypeData = oFormActMain.ZoneList[e.Node.Parent.Parent.Parent.Index].Items[e.Node.Parent.Parent.Index].GetCombatant(e.Node.Parent.Text).Items[e.Node.Text];
                    var list3 = (List<AttackType>)(currentTable = new List<AttackType>(damageTypeData.Items.Values));
                    try {
                        list3.Sort();
                        // if (opMainTableGen.cbReverseSort.Checked)
                        // 	list3.Reverse();
                    }
                    catch (Exception ex2) {
                        WriteExceptionLog(ex2, string.Empty);
                    }
                    // if (cmiEnabled.Checked)
                    pbDG.Image = GenerateDamageTypeGraph(damageTypeData, pbDG.Width, pbDG.Height, mDSort);
                    break;
                }
                case "AttackType": {
                    tableType = "AT";
                    // lvDG.ContextMenuStrip = cmsATSort;
                    oFormActMain.ZoneList[e.Node.Parent.Parent.Parent.Parent.Index].Items[e.Node.Parent.Parent.Parent.Index].GetCombatant(e.Node.Parent.Parent.Text).Items[e.Node.Parent.Text].Items.TryGetValue(e.Node.Text, out var value);
                    var list = (List<MasterSwing>)(currentTable = value.Items);
                    try {
                        list.Sort();
                        // if (opMainTableGen.cbReverseSort.Checked)
                        //     list.Reverse();
                    }
                    catch (Exception ex) {
                        WriteExceptionLog(ex, string.Empty);
                    }
                    // if (cmiEnabled.Checked)
                    pbDG.Image = GenerateAttackTypeGraph(value, pbDG.Width, pbDG.Height, "Time");
                    break;
                }
            }
            PopulateLV();
            Invoke(() => lblDG.Text = e.Node.FullPath);
            ResizeLVCols(lvDG);
        }
        catch (Exception ex4) {
            WriteExceptionLog(ex4, string.Empty);
        }
        lvDG.EndUpdate();
        // ActGlobals.oFormActMain.WriteDebugLog("tv1_AfterSelect: " + (DateTime.Now - now).TotalMilliseconds.ToString("F"));
    }

    private void tv1_AfterSelect(object? _, TreeViewEventArgs e) {
        if (e.Node == null || lastSelectedNode == e.Node && !TreeViewIsActiveEncounter())
            return;
        lastSelectedNode = e.Node;
        _tv1_AfterSelect(e);
    }

    public void ResizeLVCols(ListView listview) {
        try {
            var num = 0;
            var array = new int[listview.Columns.Count];
            var array2 = new int[listview.Columns.Count];
            if (listview.Items.Count == 0) {
                for (var i = 0; i < listview.Columns.Count; i++) {
                    array[i] += 10;
                    num += 10;
                }
            }
            else {
                for (var j = 0; j < listview.Items.Count; j++) {
                    var listViewItem = listview.Items[j];
                    for (var k = 0; k < listview.Columns.Count; k++) {
                        if (array2[k] < listViewItem.SubItems[k].Text.Length)
                            array2[k] = listViewItem.SubItems[k].Text.Length;
                        var num2 = listViewItem.SubItems[k].Text.Length + listview.Columns[k].Text.Length;
                        array[k] += num2;
                        num += num2;
                    }
                }
                for (var l = 0; l < listview.Columns.Count; l++) {
                    array[l] += array2[l] * listview.Items.Count;
                    num += array2[l] * listview.Items.Count;
                }
            }
            var num3 = 0;
            if (listview.VirtualMode) {
                if (listview.VirtualListSize > 0 && listview.Height < (listview.VirtualListSize + 1) * listview.Items[0].Bounds.Height)
                    num3 += SystemInformation.VerticalScrollBarWidth;
            }
            else if (listview.Items.Count > 0 && listview.Height < (listview.Items.Count + 1) * listview.Items[0].Bounds.Height) {
                num3 += SystemInformation.VerticalScrollBarWidth;
            }
            if (listViewWidthOffsetCalc == int.MinValue && num3 == 0 && listview.FindForm().WindowState != FormWindowState.Minimized)
                listViewWidthOffsetCalc = Math.Abs(listview.ClientSize.Width - listview.Width);
            listview.BeginUpdate();
            var num4 = 0;
            for (var m = 0; m < listview.Columns.Count; m++) {
                var num5 = (int)((listview.Width - num3) * (array[m] / (float)num));
                listview.Columns[m].Width = num5;
                num4 += num5;
                if (m == listview.Columns.Count - 1)
                    listview.Columns[0].Width += listview.Width - num4 - (listViewWidthOffsetCalc > 0 ? listViewWidthOffsetCalc : 6) - num3;
            }
            listview.EndUpdate();
        }
        catch (Exception ex) {
            WriteExceptionLog(ex, string.Empty);
        }
    }

    private void PopulateLV() {
        var empty = string.Empty;
        // empty = (ActGlobals.LinuxCheck1 ? ((!opMainTableGen.cbReverseSort.Checked) ? "^ " : "v ") : ((!opMainTableGen.cbReverseSort.Checked) ? "↑" : "↓"));
        var num = -1;
        switch (tableType) {
            case "EL": {
                try {
                    lvDG.VirtualListSize = ((List<EncounterData>)currentTable).Count;
                }
                catch (Exception ex2) {
                    WriteExceptionLog(ex2, "EL Set Size");
                    lvDG.VirtualMode = false;
                    lvDG.VirtualMode = true;
                    lvDG.VirtualListSize = ((List<EncounterData>)currentTable).Count;
                }
                lvDG.Columns.Clear();
                foreach (var text2 in clbZD) {
                    // if (opTableZone.clbZD.GetItemChecked(j))
                    {
                        var columnHeader2 = new ColumnHeader();
                        columnHeader2.Width = 100;
                        columnHeader2.TextAlign = ++num != 0 ? HorizontalAlignment.Center : HorizontalAlignment.Left;
                        columnHeader2.Text = text2;
                        lvDG.Columns.Add(columnHeader2);
                    }
                }
                break;
            }
            case "ED": {
                try {
                    lvDG.VirtualListSize = ((List<CombatantData>)currentTable).Count;
                }
                catch (Exception ex5) {
                    WriteExceptionLog(ex5, "ED Set Size");
                    lvDG.VirtualMode = false;
                    lvDG.VirtualMode = true;
                    lvDG.VirtualListSize = ((List<CombatantData>)currentTable).Count;
                }
                lvDG.Columns.Clear();
                foreach (var text5 in clbED) {
                    // if (clbED.GetItemChecked(m))
                    {
                        var columnHeader5 = new ColumnHeader();
                        columnHeader5.Text = text5 == eDSort ? empty : string.Empty;
                        columnHeader5.Width = 100;
                        columnHeader5.TextAlign = ++num != 0 ? HorizontalAlignment.Center : HorizontalAlignment.Left;
                        columnHeader5.Text += text5;
                        lvDG.Columns.Add(columnHeader5);
                    }
                }
                break;
            }
            case "CD": {
                try {
                    lvDG.VirtualListSize = ((List<DamageTypeData>)currentTable).Count;
                }
                catch (Exception ex3) {
                    WriteExceptionLog(ex3, "CD Set Size");
                    lvDG.VirtualMode = false;
                    lvDG.VirtualMode = true;
                    lvDG.VirtualListSize = ((List<DamageTypeData>)currentTable).Count;
                }
                lvDG.Columns.Clear();
                foreach (var text3 in clbCD) {
                    // if (clbCD.GetItemChecked(k))
                    {
                        var columnHeader3 = new ColumnHeader();
                        columnHeader3.Width = 100;
                        columnHeader3.TextAlign = ++num != 0 ? HorizontalAlignment.Center : HorizontalAlignment.Left;
                        columnHeader3.Text = text3;
                        lvDG.Columns.Add(columnHeader3);
                    }
                }
                break;
            }
            case "MD": {
                try {
                    lvDG.VirtualListSize = ((List<AttackType>)currentTable).Count;
                }
                catch (Exception ex4) {
                    WriteExceptionLog(ex4, "MD Set Size");
                    lvDG.VirtualMode = false;
                    lvDG.VirtualMode = true;
                    lvDG.VirtualListSize = ((List<AttackType>)currentTable).Count;
                }
                lvDG.Columns.Clear();
                foreach (var text4 in clbDT) {
                    // if (clbDT.GetItemChecked(l))
                    {
                        var columnHeader4 = new ColumnHeader();
                        columnHeader4.Text = text4 == mDSort ? empty : string.Empty;
                        columnHeader4.Width = 100;
                        columnHeader4.TextAlign = ++num != 0 ? HorizontalAlignment.Center : HorizontalAlignment.Left;
                        columnHeader4.Text += text4;
                        lvDG.Columns.Add(columnHeader4);
                    }
                }
                break;
            }
            case "AT": {
                try {
                    lvDG.VirtualListSize = ((List<MasterSwing>)currentTable).Count;
                }
                catch (Exception ex) {
                    WriteExceptionLog(ex, "AT Set Size");
                    lvDG.VirtualMode = false;
                    lvDG.VirtualMode = true;
                    lvDG.VirtualListSize = ((List<MasterSwing>)currentTable).Count;
                }
                lvDG.Columns.Clear();
                foreach (var text in clbAT) {
                    // if (clbAT.GetItemChecked(i))
                    {
                        var columnHeader = new ColumnHeader();
                        columnHeader.Text = text == aTSort ? empty : string.Empty;
                        columnHeader.Width = 100;
                        columnHeader.TextAlign = HorizontalAlignment.Center;
                        columnHeader.Text += text;
                        lvDG.Columns.Add(columnHeader);
                    }
                }
                break;
            }
        }
    }

    private void PopulateTView() {
        refreshTree = false;
        try {
            var flag = false;
            try {
                for (var i = 0; i < oFormActMain.ZoneList.Count; i++) {
                    var zoneData = oFormActMain.ZoneList[i];
                    TreeNode treeNode;
                    if (tvDG.Nodes.Count < oFormActMain.ZoneList.Count && i > tvDG.Nodes.Count - 1) {
                        treeNode = new TreeNode {
                            Text = zoneData.ToString(),
                            Tag = "ZoneData"
                        };
                        tvDG.Nodes.Add(treeNode);
                        flag = true;
                    }
                    else
                        treeNode = tvDG.Nodes[i];
                    if (treeNode.Nodes.Count == 0 && zoneData.Items.Count > 0)
                        treeNode.Nodes.Add("...");
                    if (!treeNode.IsExpanded)
                        continue;
                    if (treeNode.Nodes.Count == 0) {
                        Invoke(() => tvDG.Nodes.Clear());
                        refreshTree = true;
                        return;
                    }
                    if (treeNode.Nodes[0].Text == @"...")
                        treeNode.Nodes[0].Remove();
                    for (var j = 0; j < zoneData.Items.Count; j++) {
                        var encounterData = zoneData.Items[j];
                        if (tvDG.Nodes[i].Nodes.Count < zoneData.Items.Count && j > tvDG.Nodes[i].Nodes.Count - 1) {
                            treeNode = new TreeNode {
                                Text = encounterData.ToString()
                            };
                            switch (encounterData.GetEncounterSuccessLevel()) {
                                case 1:
                                    treeNode.ForeColor = Color.FromArgb(-14513374);
                                    break;
                                case 2:
                                    treeNode.ForeColor = Color.FromArgb(-29696);
                                    break;
                                case 3:
                                    treeNode.ForeColor = Color.FromArgb(-2354116);
                                    break;
                                default:
                                    treeNode.ForeColor = Color.FromArgb(-16744193);
                                    break;
                            }
                            treeNode.Tag = "EncounterData";
                            tvDG.Nodes[i].Nodes.Add(treeNode);
                            if (i == oFormActMain.ZoneList.Count - 1)
                                flag = true;
                        }
                        else
                            treeNode = tvDG.Nodes[i].Nodes[j];
                        if (treeNode.Nodes.Count == 0 && encounterData.Items.Count > 0
                            // && !cbShowChecks.Checked
                           )
                            treeNode.Nodes.Add("...");
                        if (!treeNode.IsExpanded)
                            continue;
                        if (treeNode.Nodes[0].Text == @"...")
                            treeNode.Nodes[0].Remove();
                        if (
                            // cbShowChecks.Checked || 
                            !tvDG.Nodes[i].Nodes[j].IsExpanded)
                            continue;
                        for (var k = 0; k < encounterData.Items.Values.Count; k++) {
                            var combatantData = encounterData.Items.Values[k];
                            treeNode = new TreeNode {
                                Text = combatantData.ToString(),
                                Tag = "CombatantData"
                            };
                            var flag2 = false;
                            foreach (TreeNode node in tvDG.Nodes[i].Nodes[j].Nodes) {
                                if (node.Text == treeNode.Text) {
                                    flag2 = true;
                                    break;
                                }
                            }
                            if (!flag2) {
                                tvDG.Nodes[i].Nodes[j].Nodes.Add(treeNode);
                                treeNode.Nodes.Add("...");
                            }
                            var index = -1;
                            foreach (TreeNode node2 in tvDG.Nodes[i].Nodes[j].Nodes) {
                                if (node2.Text == combatantData.ToString()) {
                                    index = node2.Index;
                                    treeNode = node2;
                                    break;
                                }
                            }
                            if (!treeNode.IsExpanded)
                                continue;
                            if (treeNode.Nodes[0].Text == @"...")
                                treeNode.Nodes[0].Remove();
                            var num = -1;
                            foreach (var value in combatantData.Items.Values) {
                                num++;
                                treeNode = new TreeNode {
                                    Tag = "DamageTypeData",
                                    Text = value.Type
                                };
                                if (tvDG.Nodes[i].Nodes[j].Nodes[index].Nodes.Count < num + 1)
                                    tvDG.Nodes[i].Nodes[j].Nodes[index].Nodes.Add(treeNode);
                                else
                                    treeNode = tvDG.Nodes[i].Nodes[j].Nodes[index].Nodes[num];
                                if (treeNode.Nodes.Count == 0 && value.Items.Count > 0)
                                    treeNode.Nodes.Add("...");
                                if (!treeNode.IsExpanded)
                                    continue;
                                if (treeNode.Nodes[0].Text == @"...")
                                    treeNode.Nodes[0].Remove();
                                var array = new AttackType[value.Items.Count];
                                value.Items.Values.CopyTo(array, 0);
                                foreach (var attackType in array) {
                                    treeNode = new TreeNode {
                                        Text = attackType.ToString(),
                                        Tag = "AttackType"
                                    };
                                    flag2 = false;
                                    foreach (TreeNode node3 in tvDG.Nodes[i].Nodes[j].Nodes[index].Nodes[num].Nodes) {
                                        if (node3.Text == treeNode.Text) {
                                            flag2 = true;
                                            break;
                                        }
                                    }
                                    if (!flag2)
                                        tvDG.Nodes[i].Nodes[j].Nodes[index].Nodes[num].Nodes.Add(treeNode);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                WriteExceptionLog(ex, string.Empty);
                refreshTree = true;
            }
            if (flag
                // && !cbLockDG.Checked
               ) {
                if (!tvDG.Nodes[^1].IsExpanded)
                    tvDG.Nodes[^1].Expand();
                tvDG.SelectedNode = tvDG.Nodes[^1].LastNode;
            }
        }
        catch (Exception ex2) {
            WriteExceptionLog(ex2, string.Empty);
        }
        // ActGlobals.oFormActMain.WriteDebugLog("PopulateTView: " + (DateTime.Now - now).TotalMilliseconds.ToString("F"));
    }

    public void WriteExceptionLog(Exception ex, string MoreInfo) {
        var text = string.Empty;
        if (ex.InnerException != null)
            text = $"{ex.InnerException}{Environment.NewLine}";
        var value = $"***** {DateTime.Now:s} - {MoreInfo}\n{text}{ex}{Environment.NewLine}{Environment.StackTrace.Remove(0, 118)}{Environment.NewLine}*****";
        oFormActMain.PluginLog.Error(value);
    }
}