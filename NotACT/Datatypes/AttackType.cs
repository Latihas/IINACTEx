using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;

namespace Advanced_Combat_Tracker;

public enum AttackTypeTypeEnum {
	Melee,
	Spell,
	CombatArt,
	UnknownNonMelee
}

public class AttackType : IComparable, IEquatable<AttackType>, IComparable<AttackType> {
	public delegate Color ColorDataCallback(AttackType Data);

	public delegate string StringDataCallback(AttackType Data);

	public static Dictionary<string, ColumnDef> ColumnDefs = new();
	private bool attacktypetypeCached;
	private bool averageDelayCached;
	private bool blockedCached;
	private bool crithitsCached;
	private bool damageCached;
	private bool durationCached;
	private bool endtimeCached;
	private bool hitsCached;
	private bool maxhitCached;
	private bool medianCached;
	private bool minhitCached;
	private bool missesCached;
	private bool resistCached;
	private bool starttimeCached;
	private bool swingsCached;

	public AttackType(string theAttackType, DamageTypeData Parent) {
		Items = [];
		Type = theAttackType;
		InvalidateCachedValues();
		this.Parent = Parent;
	}

	public DamageTypeData? Parent { get; }
	public static string[] ColTypeCollection {
		get {
			var types = new string[ColumnDefs.Count];
			var index = 0;
			foreach (var columnDef in ColumnDefs) {
				types[index] = columnDef.Value.SqlDataType;
				index++;
			}
			return types;
		}
	}
	public static string[] ColHeaderCollection {
		get {
			var headers = new string[ColumnDefs.Count];
			var index = 0;
			foreach (var columnDef in ColumnDefs) {
				headers[index] = columnDef.Value.SqlDataName;
				index++;
			}
			return headers;
		}
	}
	public static string ColHeaderString => string.Join(",", ColHeaderCollection);
	public string[] ColCollection {
		get {
			var result = new string[ColumnDefs.Count];
			var index = 0;
			foreach (var columnDef in ColumnDefs) {
				result[index] = columnDef.Value.GetSqlData(this);
				index++;
			}
			return result;
		}
	}
	public string Type { get; }
	public string Resist {
		get {
			if (resistCached) return field;
			string result;
			if (Type == ActGlobals.Trans["attackTypeTerm-all"]) {
				result = ActGlobals.Trans["attackTypeTerm-all"];
			} else {
				var text = string.Empty;
				var list = new List<string>();
				foreach (var damageType in Items.Select(item => item.DamageType
					         .Replace(ActGlobals.Trans["specialAttackTerm-warded"] + "/", string.Empty))) {
					if (damageType == ActGlobals.Trans["specialAttackTerm-melee"] ||
					    damageType == ActGlobals.Trans["specialAttackTerm-nonMelee"] ||
					    damageType == ActGlobals.Trans["specialAttackTerm-warded"]) {
						text = damageType;
					} else if (!list.Contains(damageType)) {
						list.Add(damageType);
						break;
					}
				}
				result = list.Count == 1
					? list[0]
					: !string.IsNullOrEmpty(text)
						? text
						: ActGlobals.Trans["specialAttackTerm-unknown"];
			}
			field = result;
			resistCached = true;
			return result;
		}
	}
	public long Damage {
		get {
			if (damageCached) return field;
			long _damage;
			try {
				_damage = Items.Where(masterSwing => (long)masterSwing.Damage > 0).Aggregate(0L,
					(current, masterSwing) => (long)(current + masterSwing.Damage));
			} catch (InvalidOperationException) {
				return Damage;
			}
			field = _damage;
			damageCached = true;
			return _damage;
		}
	}
	public int Hits {
		get {
			if (hitsCached) return field;
			var numHits = Items.Count(swing => ActGlobals.blockIsHit && swing.Damage >= 0 || swing.Damage > 0);
			field = numHits;
			hitsCached = true;
			return numHits;
		}
	}

	private static bool IsCritHit(MasterSwing swing) =>
		ActGlobals.blockIsHit
			? swing.Critical && (long)swing.Damage >= 0
			: swing.Critical && (long)swing.Damage > 0;

	public int CritHits {
		get {
			if (crithitsCached) return field;
			var count = Items.Count(IsCritHit);
			field = count;
			crithitsCached = true;
			return count;
		}
	}
	public float CritPerc => CritHits / (float)Hits * 100f;
	public int Swings {
		get {
			if (swingsCached) return field;
			var swings = Items.Count(t => t.Damage != Dnum.Death || Type == ActGlobals.Trans["specialAttackTerm-killing"]);
			field = swings;
			swingsCached = true;
			return swings;
		}
	}
	public int Misses {
		get {
			if (missesCached) return field;
			var misses = Items.Count(t => t.Damage == Dnum.Miss);
			field = misses;
			missesCached = true;
			return misses;
		}
	}
	public int Blocked {
		get {
			if (blockedCached) return field;
			var blocked = Items.Count(masterSwing => (long)masterSwing.Damage < -1 && masterSwing.Damage != Dnum.Death);
			field = blocked;
			blockedCached = true;
			return blocked;
		}
	}
	public float ToHit {
		get {
			try {
				return Hits * 100f / Swings;
			} catch {
				return 0f;
			}
		}
	}
	public double Average {
		get {
			if (Hits > 0) return Damage / (double)Hits;
			return double.NaN;
		}
	}
	public long Median {
		get {
			try {
				if (medianCached) return field;
				var list = (from masterSwing in Items where (long)masterSwing.Damage >= 0 select masterSwing.Damage)
					.ToList();
				try {
					list.Sort();
				} catch (Exception ex) {
					ActGlobals.oFormActMain.WriteExceptionLog(ex, string.Empty);
				}
				var median = list.Count / 2;
				if (list.Count > median)
					field = list[median];
				else
					field = 0L;
				medianCached = true;
				return field;
			} catch {
				return 0L;
			}
		}
	}
	public DateTime StartTime {
		get {
			if (starttimeCached) return field;
			var dateTime = DateTime.MaxValue;
			foreach (var masterSwing in Items.Where(masterSwing => masterSwing.Time < dateTime))
				dateTime = masterSwing.Time;
			field = dateTime;
			starttimeCached = true;
			return dateTime;
		}
	}
	public DateTime EndTime {
		get {
			if (endtimeCached) return field;
			var dateTime = DateTime.MinValue;
			foreach (var masterSwing in Items.Where(masterSwing => masterSwing.Time > dateTime))
				dateTime = masterSwing.Time;
			field = dateTime;
			endtimeCached = true;
			return dateTime;
		}
	}
	public long MinHit {
		get {
			if (minhitCached) return field;
			var minHit = long.MaxValue;
			foreach (var masterSwing in Items)
				if (ActGlobals.blockIsHit) {
					if ((long)masterSwing.Damage >= 0 && (long)masterSwing.Damage < minHit)
						minHit = masterSwing.Damage;
				} else if ((long)masterSwing.Damage > 0 && (long)masterSwing.Damage < minHit)
					minHit = masterSwing.Damage;
			if (minHit == long.MaxValue) return 0L;
			field = minHit;
			minhitCached = true;
			return minHit;
		}
	}
	public long MaxHit {
		get {
			if (maxhitCached) return field;
			var maxHit = 0L;
			foreach (var masterSwing in Items.Where(masterSwing => (long)masterSwing.Damage > 0 && (long)masterSwing.Damage > maxHit))
				maxHit = masterSwing.Damage;
			field = maxHit;
			maxhitCached = true;
			return maxHit;
		}
	}
	public double ExtDPS => EncDPS;
	public double EncDPS {
		get {
			if (Parent?.Parent == null) return double.NaN;
			var totalSeconds = Parent.Parent.Parent?.Duration.TotalSeconds;
			if (totalSeconds > 0.0) return Damage / totalSeconds.Value;
			return 0.0;
		}
	}
	public double CharDPS {
		get {
			if (Parent?.Parent == null) return double.NaN;
			var totalSeconds = Parent.Parent.Duration.TotalSeconds;
			if (totalSeconds > 0.0) return Damage / totalSeconds;
			return 0.0;
		}
	}
	public double DPS {
		get {
			var totalSeconds = Duration.TotalSeconds;
			if (totalSeconds > 0.0) return Damage / totalSeconds;
			return 0.0;
		}
	}
	public TimeSpan Duration {
		get {
			// If there are multiple start times, calculate duration based on item times.
			if (Parent?.Parent?.Parent?.StartTimes.Count > 1) {
				if (durationCached) return field;
				// Sort items by time.
				Items.Sort(MasterSwing.CompareTime);
				var startTimes = Parent.Parent.Parent.StartTimes;
				var endTimes = Parent.Parent.Parent.EndTimes;
				var itemIndex = 0;
				var startList = new List<DateTime>();
				var endList = new List<DateTime>();
				// For each time range, find the items that fall within it.
				for (var i = 0; i < startTimes.Count; i++) {
					var startTime = startTimes[i];
					var endTime = i < endTimes.Count ? endTimes[i] : DateTime.MaxValue;
					while (itemIndex < Items.Count && Items[itemIndex].Time < startTime) {
						itemIndex++;
					}
					while (itemIndex < Items.Count && Items[itemIndex].Time <= endTime) {
						startList.Add(Items[itemIndex].Time);
						itemIndex++;
					}
					endList.Add(endTime);
				}
				// Calculate the duration based on the start and end times.
				var duration = TimeSpan.Zero;
				for (var i = 0; i < startList.Count; i++)
					duration += endList[i] - startList[i];
				field = duration;
				durationCached = true;
				return field;
			}
			// If there is only one start time, use the start and end times to calculate duration.
			if (EndTime > StartTime) {
				return EndTime - StartTime;
			}
			return TimeSpan.Zero;
		}
	}
	public string DurationS => Duration.Hours == 0
		? $"{Duration.Minutes:00}:{Duration.Seconds:00}"
		: $"{Duration.Hours:00}:{Duration.Minutes:00}:{Duration.Seconds:00}";
	public float AverageDelay {
		get {
			if (averageDelayCached) return field;
			if (!ActGlobals.calcRealAvgDelay) return (float)Duration.TotalSeconds / Swings;
			var uniqueTimes = new HashSet<DateTime>();
			foreach (var swing in Items) uniqueTimes.Add(swing.Time);
			field = (float)Duration.TotalSeconds / (uniqueTimes.Count - 1);
			averageDelayCached = true;
			return field;
		}
	}
	public AttackTypeTypeEnum AttackTypeType {
		get {
			if (attacktypetypeCached) return field;
			if (CombatantData.DamageSwingTypes.Count == 1) {
				// If there is only one damage swing type, it's probably not melee.
				return AttackTypeTypeEnum.UnknownNonMelee;
			}
			// Get the most common damage swing type.
			var damageSwingType = CombatantData.DamageSwingTypes[0];
			var swingTypeCounts = GetSwingTypeCounts();
			if (swingTypeCounts.Count == 1 && swingTypeCounts.ContainsKey(damageSwingType)) {
				// If the most common damage swing type is the only one and it matches the swing type of the combatant,
				// then it's probably melee.
				field = AttackTypeTypeEnum.Melee;
			} else {
				// Otherwise, it's probably not melee.
				field = AttackTypeTypeEnum.UnknownNonMelee;
			}
			attacktypetypeCached = true;
			return field;
		}
	}
	public List<MasterSwing> Items { get; set; }
	public Dictionary<string, object> Tags { get; set; } = new();
	public int CompareTo(object? obj) => CompareTo((AttackType?)obj);

	public int CompareTo(AttackType? other) {
		var comparisonResult = 0;
		// Check if a primary sort column is specified
		if (ColumnDefs.TryGetValue(ActGlobals.mDSort, out var value)) {
			// Compare the current object with the other object using the primary sort column
			comparisonResult = value.SortComparer(this, other!);
		}
		// If objects are still equivalent, check if a secondary sort column is specified
		if (comparisonResult == 0 && ColumnDefs.TryGetValue(ActGlobals.mDSort2, out var value2)) {
			// Compare the current object with the other object using the secondary sort column
			comparisonResult = value2.SortComparer(this, other!);
		}
		// If objects are still equivalent, compare them based on their damage
		if (comparisonResult == 0) {
			comparisonResult = Damage.CompareTo(other!.Damage);
		}
		// Return the comparison result
		return comparisonResult;
	}

	public bool Equals(AttackType? other) => Type == other!.Type;

	public void InvalidateCachedValues() {
		damageCached = false;
		hitsCached = false;
		swingsCached = false;
		missesCached = false;
		blockedCached = false;
		medianCached = false;
		starttimeCached = false;
		endtimeCached = false;
		minhitCached = false;
		maxhitCached = false;
		crithitsCached = false;
		durationCached = false;
		attacktypetypeCached = false;
		averageDelayCached = false;
	}

	public void AddCombatAction(MasterSwing action) {
		InvalidateCachedValues();
		Items.Add(action);
	}

	public void Trim() => Items.TrimExcess();
	public string GetColumnByName(string name) => ColumnDefs.TryGetValue(name, out var value) ? value.GetCellData(this) : string.Empty;

	public Dictionary<int, int> GetSwingTypeCounts() {
		var dictionary = new Dictionary<int, int>();
		foreach (var masterSwing in Items.Where(masterSwing => !dictionary.TryAdd(masterSwing.SwingType, 1)))
			dictionary[masterSwing.SwingType]++;
		return dictionary;
	}

	public override string ToString() => Type;

	public override bool Equals(object? obj) {
		if (obj == DBNull.Value) return false;
		var attackType = (AttackType)obj!;
		var value = attackType.Type;
		return Type.Equals(value);
	}

	[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
	public override int GetHashCode() {
		try {
			return Items.Aggregate(0L, (current, masterSwing) => current + masterSwing.GetHashCode()).GetHashCode();
		} catch (InvalidOperationException) {
			return GetHashCode();
		}
	}

	public Dictionary<string, int> GetAttackSpecials() {
		// Get a sorted list of all MasterSwing items for this combatant
		var swings = new List<MasterSwing>(Items);
		swings.Sort(MasterSwing.CompareTime);
		// Keep track of the attack specials encountered so far
		var previousSpecials = new List<string>();
		// Count the occurrence of each attack special
		var specialCounts = new Dictionary<string, int>();
		foreach (var swing in swings) {
			// If the current swing has no special attack, clear the list of previous specials
			if (swing.Special == ActGlobals.Trans["specialAttackTerm-none"])
				previousSpecials.Clear();
			// If the current swing has a new special attack, update the counts
			if (!previousSpecials.Contains(swing.Special)) {
				if (!specialCounts.TryAdd(swing.Special, 1)) specialCounts[swing.Special]++;
				// If the special attack is not "none", also update the "ANY" and "ONCE" counts
				if (swing.Special != ActGlobals.Trans["specialAttackTerm-none"]) {
					if (!specialCounts.TryAdd("ANY", 1)) specialCounts["ANY"]++;
					if (!previousSpecials.Contains("ONCE")) {
						if (!specialCounts.TryAdd("ONCE", 1)) specialCounts["ONCE"]++;
						previousSpecials.Add("ONCE");
					}
				}
			}
			// Add the current special to the list of previous specials
			previousSpecials.Add(swing.Special);
		}
		// Return the dictionary of attack specials and their counts
		return specialCounts;
	}

	[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class ColumnDef(
		string label, bool defaultVisible, string sqlDataType, string sqlDataName,
		StringDataCallback cellDataCallback, StringDataCallback sqlDataCallback,
		Comparison<AttackType> sortComparer) {
		public StringDataCallback GetCellData = cellDataCallback;
		public StringDataCallback GetSqlData = sqlDataCallback;
		public Comparison<AttackType> SortComparer = sortComparer;
		public ColorDataCallback GetCellBackColor = _ => Color.Transparent;
		public ColorDataCallback GetCellForeColor = _ => Color.Transparent;
		public bool DefaultVisible { get; } = defaultVisible;
		public string Label { get; } = label;
		public string SqlDataType { get; } = sqlDataType;
		public string SqlDataName { get; } = sqlDataName;
	}
}