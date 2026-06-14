using System;
using System.Diagnostics.CodeAnalysis;

namespace Advanced_Combat_Tracker;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class Dnum(long numberValue, string customDamageString) : IComparable {
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public Dnum(long numberValue) : this(numberValue, string.Empty) { }

	private string damageString = customDamageString;
	public static Dnum Miss => -1L;
	public static Dnum Death => -10L;
	public static Dnum ThreatPosition => -11L;
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public long Number { get; } = numberValue;
	public static Dnum NoDamage => 0L;
	public static Dnum Unknown => -9L;

	public string DamageString {
		get => string.IsNullOrEmpty(damageString) ? ToString() : damageString;
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		set => damageString = value;
	}
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	public string DamageString2 { get; set; }

	public int CompareTo(object? obj) {
		var otherDnum = (Dnum)obj!;
		var thisNum = Number;
		var otherNum = otherDnum.Number;
		if (thisNum == -9 && otherNum == -9) return damageString.CompareTo(otherDnum.damageString);
		return thisNum.CompareTo(otherNum);
	}

	public static implicit operator long(Dnum val) => val.Number;
	public static implicit operator Dnum(long val) => val >= -10 ? new Dnum(val) : new Dnum(-9L);

	public static Dnum operator +(Dnum a, Dnum b) {
		if (a.Number > -1 && b.Number > -1) return new Dnum(a.Number + b.Number);
		if (a.Number < 0 && b.Number >= 0) return new Dnum(b.Number);
		if (b.Number < 0 && a.Number >= 0) return new Dnum(a.Number);
		return new Dnum(0L);
	}

	public static bool operator ==(Dnum a, Dnum b) => a.Equals(b);
	public static bool operator !=(Dnum a, Dnum b) => !a.Equals(b);

	public override string ToString() {
		if (Number > 0 && string.IsNullOrEmpty(damageString))
			return Number.ToString(ActGlobals.mainTableShowCommas ? "#,0" : "0");
		var number2 = Number + 10;
		if ((ulong)number2 > 10uL) return damageString + DamageString2;
		return number2 switch {
			10L => "data-dnumNoDamage",
			9L => "data-dnumMiss",
			8L => "data-dnumResist",
			7L => "data-dnumParry",
			6L => "data-dnumRiposte",
			5L => "data-dnumBlock",
			0L => "data-dnumDeath",
			_ => damageString + DamageString2
		};
	}

	public string ToString(bool ShortHand) {
		if (Number > 0) return ShortHand ? Number.ToString() : Number.ToString(ActGlobals.mainTableShowCommas ? "#,0" : "0");
		if (!ShortHand)
			return Number switch {
				0L => "data-dnumNoDamage",
				-1L => "data-dnumMiss",
				-10L => "data-dnumDeath",
				_ => damageString
			};
		return Number switch {
			0L => "0",
			-1L => "data-dnumMiss",
			-10L => "data-dnumDeath",
			_ => damageString.Length < 3 ? damageString : damageString[..3]
		};
	}

	public override bool Equals(object? obj) {
		if (obj is not Dnum dnum) return false;
		return dnum.Number == Number && DamageString.Equals(dnum.DamageString);
	}

	public override int GetHashCode() => ToString(false).GetHashCode();
}