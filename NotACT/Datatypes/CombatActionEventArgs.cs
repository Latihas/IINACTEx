using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Advanced_Combat_Tracker;

public delegate void CombatActionDelegate(bool isImport, CombatActionEventArgs actionInfo);

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
public class CombatActionEventArgs(MasterSwing combatAction) : EventArgs {
	public readonly MasterSwing combatAction = combatAction;
	public string attacker = combatAction.Attacker;
	public bool cancelAction;
	public bool critical = combatAction.Critical;
	public Dnum damage = combatAction.Damage;
	public string special = combatAction.Special;
	public int swingType = combatAction.SwingType;
	public Dictionary<string, object> tags = combatAction.Tags;
	public string theAttackType = combatAction.AttackType;
	public string theDamageType = combatAction.DamageType;
	public DateTime time = combatAction.Time;
	public int timeSorter = combatAction.TimeSorter;
	public string victim = combatAction.Victim;
}