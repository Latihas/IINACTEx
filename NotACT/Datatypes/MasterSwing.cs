using System;
using System.Collections.Generic;
using System.Drawing;
using static Advanced_Combat_Tracker.ActGlobals;

namespace Advanced_Combat_Tracker;

public class MasterSwing(
	int swingType, bool critical, string special, Dnum damage, DateTime time, int timeSorter,
	string theAttackType, string attacker, string theDamageType, string victim)
	: IComparable, IComparable<MasterSwing> {
	public delegate Color ColorDataCallback(MasterSwing Data);

	public delegate string StringDataCallback(MasterSwing Data);

	public static Dictionary<string, ColumnDef> ColumnDefs = new();
	internal string attacker = attacker;
	internal string attackType = theAttackType;
	internal bool critical = critical;
	internal Dnum damage = damage;
	internal string damageType = theDamageType;
	internal string special = special;
	internal int swingType = swingType;
	internal DateTime time = time;
	internal int timeSorter = timeSorter;
	internal string victim = victim;

	public MasterSwing(
		int SwingType, bool Critical, Dnum damage, DateTime Time, int TimeSorter, string theAttackType,
		string Attacker, string theDamageType, string Victim) : this(SwingType, Critical, "specialAttackTerm-none", damage, Time, TimeSorter, theAttackType, Attacker, theDamageType, Victim) {
	}

	public string Special => special;
	public EncounterData ParentEncounter { get; set; }
	public DateTime Time => time;
	public int TimeSorter => timeSorter;
	public int SwingType => swingType;
	public Dnum Damage => damage;
	public string Attacker => oFormActMain.LocalPlayerName != null && attacker == oFormActMain.LocalPlayerName ? charName : attacker;
	public string Victim => oFormActMain.LocalPlayerName != null && victim == oFormActMain.LocalPlayerName ? charName : victim;
	public string AttackType => attackType;
	public string DamageType => damageType;
	public bool Critical {
		get => critical;
		set => critical = value;
	}
	public static string[] ColTypeCollection {
		get {
			var colTypeCollection = new string[ColumnDefs.Count];
			var i = 0;
			foreach (var columnDef in ColumnDefs) {
				colTypeCollection[i] = columnDef.Value.SqlDataType;
				i++;
			}
			return colTypeCollection;
		}
	}
	public static string[] ColHeaderCollection {
		get {
			var colHeaderCollection = new string[ColumnDefs.Count];
			var i = 0;
			foreach (var columnDef in ColumnDefs) {
				colHeaderCollection[i] = columnDef.Value.SqlDataName;
				i++;
			}
			return colHeaderCollection;
		}
	}
	public static string ColHeaderString => string.Join(",", ColHeaderCollection);
	public string[] ColCollection {
		get {
			var colCollection = new string[ColumnDefs.Count];
			var i = 0;
			foreach (var columnDef in ColumnDefs) {
				colCollection[i] = columnDef.Value.GetSqlData(this);
				i++;
			}
			return colCollection;
		}
	}
	public Dictionary<string, object> Tags { get; set; } = new();
	public int CompareTo(object? obj) => CompareTo((MasterSwing?)obj);

	public int CompareTo(MasterSwing? other) {
		// Compare based on the sort column defined in ActGlobals.aTSort.
		if (ColumnDefs.TryGetValue(aTSort, out var sortColumn)) {
			var result = sortColumn.SortComparer(this, other);
			if (result != 0) return result;
		}
		// Compare based on the secondary sort column defined in ActGlobals.aTSort2.
		if (ColumnDefs.TryGetValue(aTSort2, out var secondarySortColumn)) {
			var result = secondarySortColumn.SortComparer(this, other);
			if (result != 0) return result;
		}
		// Compare based on the time sorter.
		var timeSorterResult = TimeSorter.CompareTo(other.TimeSorter);
		if (timeSorterResult != 0) return timeSorterResult;
		// If all else fails, compare based on the time of the swings.
		return Time.CompareTo(other.Time);
	}

	public string GetColumnByName(string name) =>
		ColumnDefs.TryGetValue(name, out var value) ? value.GetCellData(this) : string.Empty;

	public override string ToString() =>
		$"{Time:s}|{Damage}|{Attacker}|{Special}|{AttackType}|{DamageType}|{Victim}";

	public override bool Equals(object? obj) {
		var masterSwing = (MasterSwing)obj!;
		var text = ToString();
		var value = masterSwing.ToString();
		return text.Equals(value);
	}

	public override int GetHashCode() =>
		ToString().GetHashCode();

	internal static int CompareTime(MasterSwing Left, MasterSwing Right) {
		var timeSorterComparison = Left.TimeSorter.CompareTo(Right.TimeSorter);
		return timeSorterComparison != 0 ? timeSorterComparison : Left.Time.CompareTo(Right.Time);
	}

	public class ColumnDef(
		string label, bool defaultVisible, string sqlDataType, string sqlDataName,
		StringDataCallback cellDataCallback, StringDataCallback sqlDataCallback,
		Comparison<MasterSwing> sortComparer) {
		public ColorDataCallback GetCellBackColor = _ => Color.Transparent;
		public readonly StringDataCallback GetCellData = cellDataCallback;
		public ColorDataCallback GetCellForeColor = _ => Color.Transparent;
		public readonly StringDataCallback GetSqlData = sqlDataCallback;
		public readonly Comparison<MasterSwing> SortComparer = sortComparer;
		public string SqlDataType { get; } = sqlDataType;
		public string SqlDataName { get; } = sqlDataName;
		public bool DefaultVisible { get; } = defaultVisible;
		public string Label { get; } = label;
	}

	public class DualComparison(string sort1, string sort2) : IComparer<MasterSwing> {
		public int Compare(MasterSwing? Left, MasterSwing? Right) {
			if (ColumnDefs.TryGetValue(sort1, out var comparer1)) {
				var result = comparer1.SortComparer(Left!, Right!);
				if (result != 0) return result;
			}
			if (ColumnDefs.TryGetValue(sort2, out var comparer2)) {
				var result = comparer2.SortComparer(Left!, Right!);
				if (result != 0) return result;
			}
			return Left.TimeSorter == Right.TimeSorter
				? Left.Time.CompareTo(Right.Time)
				: Left.TimeSorter.CompareTo(Right.TimeSorter);
		}
	}
}