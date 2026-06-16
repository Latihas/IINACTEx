#if TRACEPERF
using System.Diagnostics;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.MemoryProcessors.Aggro;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;
using RainbowMage.OverlayPlugin.MemoryProcessors.EnmityHud;
using RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;
using RainbowMage.OverlayPlugin.MemoryProcessors.Target;
using static RainbowMage.OverlayPlugin.MemoryProcessors.InCombat.LineInCombat;

namespace RainbowMage.OverlayPlugin.EventSources;

public class EnmityEventSource : EventSourceBase {
	// General information about the target, focus target, hover target.  Also, enmity entries for main target.
	private const string EnmityTargetDataEvent = "EnmityTargetData";
	// All of the mobs with aggro on the player.  Equivalent of the sidebar aggro list in game.
	private const string EnmityAggroListEvent = "EnmityAggroList";
	// TargetableEnemies
	private const string TargetableEnemiesEvent = "TargetableEnemies";
	// State of combat, both act and game.
	private const string InCombatEvent = "InCombat";
	private readonly ICombatantMemory combatantMemory;
	private readonly LineInCombat lineInCombat;
	private readonly IAggroMemory aggroMemory;
	private CancellationTokenSource endEncounterToken;
	private readonly IEnmityHudMemory enmityHudMemory;
	private readonly IEnmityMemory enmityMemory;
	private readonly ITargetMemory targetMemory;

	public EnmityEventSource(TinyIoCContainer container) : base(container) {
		var haveCombatantMemory = container.TryResolve(out combatantMemory);
		if (!haveCombatantMemory) {
			Log(LogLevel.Warning, "Could not construct EnmityEventSource: missing combatantMemory");
			return;
		}
		targetMemory = container.Resolve<ITargetMemory>();
		enmityMemory = container.Resolve<IEnmityMemory>();
		aggroMemory = container.Resolve<IAggroMemory>();
		enmityHudMemory = container.Resolve<IEnmityHudMemory>();
		RegisterEventTypes([EnmityTargetDataEvent, EnmityAggroListEvent, TargetableEnemiesEvent]);
		RegisterCachedEventType(InCombatEvent);
		lineInCombat = container.Resolve<LineInCombat>();
		lineInCombat.OnInCombatChanged += OnInCombatChanged;

		EnmityTick += UpdateEnmity;
		EnmityTick += lineInCombat.Update;
	}

	public int endEncounterOutOfCombatDelayMs => ActGlobals.oFormActMain.DalamudPlugin.Configuration.endEncounterOutOfCombatDelayMs;

	public BuiltinEventConfig Config { get; set; }

	public event Action EnmityTick;

	public override void Start() {
		timer.Change(0, Config.EnmityIntervalMs);
	}

	public override void LoadConfig(IPluginConfig cfg) {
		Config = container.Resolve<BuiltinEventConfig>();

		Config.EnmityIntervalChanged += (_, _) => { timer.Change(0, Config.EnmityIntervalMs); };
	}

	public override void SaveConfig(IPluginConfig config) {
	}

	public override void Stop() {
		base.Stop();

		if (endEncounterToken != null) {
			try {
				endEncounterToken.Cancel();
			} catch {
			}
			try {
				endEncounterToken.Dispose();
			} catch {
			}
			endEncounterToken = null;
		}

		if (lineInCombat != null) {
			try {
				lineInCombat.OnInCombatChanged -= OnInCombatChanged;
			} catch {
			}
		}
	}

	public override void Dispose() {
		Stop();
		base.Dispose();
	}

	private void OnInCombatChanged(object? sender, InCombatArgs args) {
		var combatData = new InCombatDataObject {
			inACTCombat = args.InACTCombat,
			inGameCombat = args.InGameCombat
		};
		DispatchAndCacheEvent(JObject.FromObject(combatData));

		if (!args.InGameCombatChanged) return;

		// Handle optional "end encounter of combat" logic.
		var inGameCombat = args.InGameCombat;
		logger.Log(LogLevel.Debug, inGameCombat ? "Entered combat" : "Left combat");

		// If we've transitioned to being out of combat, start a delayed task to end the ACT encounter.
		if (Config.EndEncounterOutOfCombat && !inGameCombat) {
			endEncounterToken = new CancellationTokenSource();
			var token = endEncounterToken.Token;
			Task.Run(async () => {
				try {
					await Task.Delay(endEncounterOutOfCombatDelayMs, token).ConfigureAwait(false);

					var form = ActGlobals.oFormActMain;
					if (form != null && !token.IsCancellationRequested) {
						form.Invoke((Action)(() => {
							try {
								form.EndCombat(true);
							} catch (Exception ex) {
								logger.Log(LogLevel.Warning, "EndCombat invoke failed: {0}", ex);
							}
						}));
					}
				} catch (OperationCanceledException) {
					// ignore cancellation
				} catch (ObjectDisposedException) {
					// ignore disposal during shutdown
				} catch (NullReferenceException) {
					// ignore nulls during shutdown
				} catch (Exception ex) {
					logger.Log(LogLevel.Warning, "Delayed EndEncounter task error: {0}", ex);
				}
			}, token);
		}
		// If combat starts again, cancel any outstanding tasks to stop the ACT encounter.
		// If the task has already run, this will not do anything.
		if (inGameCombat && endEncounterToken != null) {
			endEncounterToken.Cancel();
			endEncounterToken = null;
		}
	}

	private void UpdateEnmity() {
		var targetData = HasSubscriber(EnmityTargetDataEvent);
		var aggroList = HasSubscriber(EnmityAggroListEvent);
		var targetableEnemies = HasSubscriber(TargetableEnemiesEvent);
		if (!targetData && !aggroList && !targetableEnemies)
			return;

		try {
#if TRACEPERF
                var stopwatch = new Stopwatch();
                stopwatch.Start();
#endif

			var allCombatants = combatantMemory.GetCombatantList();
			var combatants = allCombatants.FindAll(c => c.Type is ObjectType.PC or ObjectType.Monster);

			if (targetData) {
				// See CreateTargetData() below
				DispatchEvent(CreateTargetData(combatants));
			}
			if (aggroList) {
				DispatchEvent(CreateAggroList(combatants));
			}
			if (targetableEnemies) {
				DispatchEvent(CreateTargetableEnemyList(combatants));
			}

			foreach (var combatant in allCombatants)
				combatantMemory.ReturnCombatant();
#if TRACEPERF
                Log(LogLevel.Trace, "UpdateEnmity: {0}ms", stopwatch.ElapsedMilliseconds);
#endif
		} catch (Exception ex) {
			Log(LogLevel.Error, "UpdateEnmity: {0}", ex.ToString());
		}
	}

	protected override void Update() {
		if (combatantMemory != null) {
			EnmityTick.Invoke();
		}
	}

	internal JObject CreateTargetData(List<Combatant> combatants) {
		var enmity = new EnmityTargetDataObject();
		try {
			var mychar = combatantMemory.GetSelfCombatant();
			enmity.Target = targetMemory.GetTargetCombatant();
			if (enmity.Target != null) {
				if (enmity.Target.TargetID > 0) {
					enmity.TargetOfTarget = combatants.FirstOrDefault(x => x.ID == enmity.Target.TargetID);
				}
				enmity.Target.Distance = mychar.DistanceString(enmity.Target);
				enmity.Target.EffectiveDistance = mychar.EffectiveDistanceString(enmity.Target);

				if (enmity.Target.Type == ObjectType.Monster) {
					enmity.Entries = enmityMemory.GetEnmityEntryList(combatants);
				}
			}

			enmity.Focus = targetMemory.GetFocusCombatant();
			enmity.Hover = targetMemory.GetHoverCombatant();

			if (mychar != null) {
				if (enmity.Focus != null) {
					enmity.Focus.Distance = mychar.DistanceString(enmity.Focus);
					enmity.Focus.EffectiveDistance = mychar.EffectiveDistanceString(enmity.Focus);
				}
				if (enmity.Hover != null) {
					enmity.Hover.Distance = mychar.DistanceString(enmity.Hover);
					enmity.Hover.EffectiveDistance = mychar.EffectiveDistanceString(enmity.Hover);
				}
				if (enmity.TargetOfTarget != null) {
					enmity.TargetOfTarget.Distance = mychar.DistanceString(enmity.TargetOfTarget);
					enmity.TargetOfTarget.EffectiveDistance = mychar.EffectiveDistanceString(enmity.TargetOfTarget);
				}

				combatantMemory.ReturnCombatant();
			}
		} catch (Exception ex) {
			logger.Log(LogLevel.Error, "CreateTargetData: {0}", ex);
		}

		var ret = JObject.FromObject(enmity);
		combatantMemory.ReturnCombatant();
		combatantMemory.ReturnCombatant();
		combatantMemory.ReturnCombatant();
		return ret;
	}

	internal JObject CreateAggroList(List<Combatant> combatants) {
		var enmity = new EnmityAggroListObject();
		try {
			enmity.AggroList = aggroMemory.GetAggroList(combatants);
			enmity.EnmityHudList = enmityHudMemory.GetEnmityHudEntries();
		} catch (Exception ex) {
			logger.Log(LogLevel.Error, "CreateAggroList: {0}", ex);
		}
		return JObject.FromObject(enmity);
	}

	internal JObject CreateTargetableEnemyList(List<Combatant> combatants) {
		var enemies = new TargetableEnemiesObject();
		try {
			enemies.TargetableEnemyList = GetTargetableEnemyList(combatants);
		} catch (Exception ex) {
			logger.Log(LogLevel.Error, "CreateTargetableEnemyList: {0}", ex);
		}
		return JObject.FromObject(enemies);
	}

	public List<TargetableEnemyEntry> GetTargetableEnemyList(List<Combatant> combatantList) {
		var enemyList = new List<TargetableEnemyEntry>();
		for (var i = 0; i != combatantList.Count; ++i) {
			var combatant = combatantList[i];
			var isHostile = combatant is { Type: ObjectType.Monster, MonsterType: MonsterType.Hostile };
			if (!isHostile || !combatant.IsTargetable) continue;
			var entry = new TargetableEnemyEntry {
				ID = combatant.ID,
				Name = combatant.Name,
				CurrentHP = combatant.CurrentHP,
				MaxHP = combatant.CurrentHP,
				IsEngaged = combatant.AggressionStatus >= AggressionStatus.EngagedPassive,
				EffectiveDistance = combatant.RawEffectiveDistance
			};
			enemyList.Add(entry);
		}
		return enemyList;
	}

	[Serializable]
	internal class InCombatDataObject {
		public bool inACTCombat;
		public bool inGameCombat;
		public string type = InCombatEvent;
	}

	[Serializable]
	internal class EnmityTargetDataObject {
		public List<EnmityEntry> Entries;
		public Combatant? Focus;
		public Combatant? Hover;
		public Combatant? Target;
		public Combatant TargetOfTarget;
		public string type = EnmityTargetDataEvent;
	}

	[Serializable]
	internal class EnmityAggroListObject {
		public List<AggroEntry> AggroList;
		public List<EnmityHudEntry> EnmityHudList;
		public string type = EnmityAggroListEvent;
	}

	[Serializable]
	internal class TargetableEnemiesObject {
		public List<TargetableEnemyEntry> TargetableEnemyList;
		public string type = TargetableEnemiesEvent;
	}
}

[Serializable]
public class TargetableEnemyEntry {
	public int CurrentHP;
	public byte EffectiveDistance;
	public uint ID;
	public bool IsEngaged;
	public int MaxHP;
	public string Name;
}