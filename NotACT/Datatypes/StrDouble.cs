using System;

namespace Advanced_Combat_Tracker;

public class StrDouble(string name, double val) : IComparable, IEquatable<StrDouble> {
	public string Name { get; } = name;
	public double Val { get; } = val;
	public int CompareTo(object? obj) => obj is not StrDouble other ? throw new ArgumentException("Object is not a StrDouble.") : Val.CompareTo(other.Val);
	public bool Equals(StrDouble? other) => Name == other!.Name && Val == other!.Val;
}