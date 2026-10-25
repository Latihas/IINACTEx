using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Plugin.Services;
using Machina.FFXIV.Headers;
using RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

public class LineCombatant : IDisposable {
	public const uint LogFileLineID = 261;

	// private readonly CancellationTokenSource? cancellationToken;
	private readonly ICombatantMemory combatantMemoryManager;
	private readonly ConcurrentDictionary<uint, CombatantStateInfo> combatantStateMap = [];
	private readonly FFXIVRepository ffxiv;
	private readonly ILogger logger;
	private readonly IFramework framework;

	private readonly Func<string, DateTime, bool>? logWriter;
	private bool _disposed;
	private bool inCombat;

	public LineCombatant(TinyIoCContainer container) {
		logger = container.Resolve<ILogger>();
		ffxiv = container.Resolve<FFXIVRepository>();
		framework = container.Resolve<IFramework>();
		combatantMemoryManager = container.Resolve<ICombatantMemory>();
		container.Resolve<LineInCombat>().OnInCombatChanged += (_, args) => {
			if (args.InGameCombatChanged) inCombat = args.InGameCombat;
		};
		var customLogLines = container.Resolve<FFXIVCustomLogLines>();
		logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry {
			Name = "CombatantMemory",
			Source = "OverlayPlugin",
			ID = LogFileLineID,
			Version = 1
		});

		try {
			ffxiv.RegisterNetworkParser(MessageReceived);
		} catch (FileNotFoundException) {
			logger.Log(LogLevel.Error, Resources.NetworkParserNoFfxiv);
		} catch (Exception e) {
			logger.Log(LogLevel.Error, Resources.NetworkParserInitException, e);
		}
		framework.Update += PollCombatants;
	}

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~LineCombatant() {
		Dispose(false);
	}

	private void PollCombatants(IFramework _) => CheckCombatants(DateTime.Now);

	private void CheckCombatants(DateTime now, params uint[] filter) {
		var combatants = combatantMemoryManager.GetCombatantList();
		Span<uint> combatantIDs = stackalloc uint[combatants.Count];
		var index = 0;
		foreach (var c in combatants) combatantIDs[index++] = c.ID;

		var criteria = CombatantChangeCriteria.Criteria(inCombat);

		// Check combatants currently in memory first
		foreach (var combatant in combatants.Where(combatant => filter.Length == 0 || Array.IndexOf(filter, combatant.ID) >= 0)) {
			// If this is a new combatant, always write a line for it
			if (!combatantStateMap.TryGetValue(combatant.ID, out var oldState)) {
				var state = new CombatantStateInfo {
					lastUpdated = now,
					combatant = combatant
				};
				// It's possible that another thread has already added this combatant since we checked
				if (combatantStateMap.TryAdd(combatant.ID, state))
					WriteLine(CombatantMemoryChangeType.Add, combatant.ID,
						string.Join("", CombatantChangeCriteria.AllFields.Select(fi => FormatFieldChange(fi, combatant, true))));
				continue;
			}
			var oldCombatant = oldState.combatant;
			var lastUpdatedDiff = (now - oldState.lastUpdated).TotalMilliseconds;
			var changed = new HashSet<string>();


			// Check position/heading first since it has a custom delay timing with threshold
			// and custom behavior (all position data is written)
			if (lastUpdatedDiff > criteria.DelayPosition) {
				var writePosition = false;
				// This check seems redundant but it's less expensive than the check below against distance
				// so it uses less CPU
				if (combatant.PosX != oldCombatant.PosX || combatant.PosY != oldCombatant.PosY || combatant.PosZ != oldCombatant.PosZ) {
					if (Vector3.DistanceSquared(combatant.Position, oldCombatant.Position) > criteria.DistancePosition) {
						writePosition = true;
					}
				} else if (combatant.Heading != oldCombatant.Heading) {
					const double PI2 = Math.PI * 2;
					double normalizedAngle = combatant.Heading - oldCombatant.Heading;
					normalizedAngle += Math.Abs(normalizedAngle > Math.PI ? -PI2 : normalizedAngle < -Math.PI ? PI2 : 0);
					if (normalizedAngle >= criteria.DistanceHeading) {
						writePosition = true;
					}
				}

				// If any position data has changed, write all position data
				if (writePosition) {
					changed.Add(nameof(Combatant.PosX));
					changed.Add(nameof(Combatant.PosY));
					changed.Add(nameof(Combatant.PosZ));
					changed.Add(nameof(Combatant.Heading));
				}
			}

			// Check the general case of "if any mapped field has changed since the specified delay duration, queue a change line"
			// But only if we don't already have a queued change line
			if (changed.Count == 0) {
				foreach (var fi in criteria.CheckFieldDelay) {
					if (fi.Value <= lastUpdatedDiff && !ValueEqual(fi.Key, oldCombatant, combatant)) {
						changed.Add(fi.Key);
					}
				}
			}

			if (changed.Count > 0) {
				// Check all fields for changes, since we're writing a change line
				foreach (var fi in CombatantChangeCriteria.AllFields) {
					// Don't reprocess an already checked line that we know has a change
					if (changed.Contains(fi)) continue;
					if (!ValueEqual(fi, oldCombatant, combatant)) changed.Add(fi);
				}

				combatantStateMap[combatant.ID] = new CombatantStateInfo {
					lastUpdated = now,
					combatant = combatant
				};
				WriteLine(
					CombatantMemoryChangeType.Change,
					combatant.ID,
					string.Join("",
						CombatantChangeCriteria.AllFields.Where(changed.Contains)
							.Select(fi => FormatFieldChange(fi, combatant))));
			}
		}

		// Any combatants no longer in memory, signify that they were removed
		foreach (var ID in combatantStateMap.Keys) {
			// If we're filtering, only consider removing those in the filters
			if (filter.Length > 0 && !filter.Contains(ID)) continue;
			if (combatantIDs.Contains(ID)) continue;
			combatantStateMap.TryRemove(ID, out _);
			WriteLine(CombatantMemoryChangeType.Remove, ID, "");
		}
	}

	private bool ValueEqual(string fi, Combatant oldCombatant, Combatant combatant) {
		return fi switch {
			nameof(Combatant.ID) => oldCombatant.ID == combatant.ID,
			nameof(Combatant.OwnerID) => oldCombatant.OwnerID == combatant.OwnerID,
			nameof(Combatant.Type) => oldCombatant.Type == combatant.Type,
			nameof(Combatant.MonsterType) => oldCombatant.MonsterType == combatant.MonsterType,
			nameof(Combatant.Status) => oldCombatant.Status == combatant.Status,
			nameof(Combatant.ModelStatus) => oldCombatant.ModelStatus == combatant.ModelStatus,
			nameof(Combatant.AggressionStatus) => oldCombatant.AggressionStatus == combatant.AggressionStatus,
			nameof(Combatant.TargetID) => oldCombatant.TargetID == combatant.TargetID,
			nameof(Combatant.IsTargetable) => oldCombatant.IsTargetable == combatant.IsTargetable,
			nameof(Combatant.Job) => oldCombatant.Job == combatant.Job,
			nameof(Combatant.Name) => string.Equals(oldCombatant.Name, combatant.Name, StringComparison.Ordinal),
			nameof(Combatant.CurrentHP) => oldCombatant.CurrentHP == combatant.CurrentHP,
			nameof(Combatant.MaxHP) => oldCombatant.MaxHP == combatant.MaxHP,
			nameof(Combatant.PosX) => oldCombatant.PosX == combatant.PosX,
			nameof(Combatant.PosY) => oldCombatant.PosY == combatant.PosY,
			nameof(Combatant.PosZ) => oldCombatant.PosZ == combatant.PosZ,
			nameof(Combatant.Heading) => oldCombatant.Heading == combatant.Heading,
			nameof(Combatant.Radius) => oldCombatant.Radius == combatant.Radius,
			nameof(Combatant.Distance) => string.Equals(oldCombatant.Distance, combatant.Distance, StringComparison.Ordinal),
			nameof(Combatant.EffectiveDistance) => string.Equals(oldCombatant.EffectiveDistance, combatant.EffectiveDistance, StringComparison.Ordinal),
			nameof(Combatant.BNpcID) => oldCombatant.BNpcID == combatant.BNpcID,
			nameof(Combatant.CurrentMP) => oldCombatant.CurrentMP == combatant.CurrentMP,
			nameof(Combatant.MaxMP) => oldCombatant.MaxMP == combatant.MaxMP,
			nameof(Combatant.Level) => oldCombatant.Level == combatant.Level,
			nameof(Combatant.BNpcNameID) => oldCombatant.BNpcNameID == combatant.BNpcNameID,
			nameof(Combatant.WorldID) => oldCombatant.WorldID == combatant.WorldID,
			nameof(Combatant.CurrentWorldID) => oldCombatant.CurrentWorldID == combatant.CurrentWorldID,
			nameof(Combatant.NPCTargetID) => oldCombatant.NPCTargetID == combatant.NPCTargetID,
			nameof(Combatant.CurrentGP) => oldCombatant.CurrentGP == combatant.CurrentGP,
			nameof(Combatant.MaxGP) => oldCombatant.MaxGP == combatant.MaxGP,
			nameof(Combatant.CurrentCP) => oldCombatant.CurrentCP == combatant.CurrentCP,
			nameof(Combatant.MaxCP) => oldCombatant.MaxCP == combatant.MaxCP,
			nameof(Combatant.PCTargetID) => oldCombatant.PCTargetID == combatant.PCTargetID,
			nameof(Combatant.IsCasting1) => oldCombatant.IsCasting1 == combatant.IsCasting1,
			nameof(Combatant.IsCasting2) => oldCombatant.IsCasting2 == combatant.IsCasting2,
			nameof(Combatant.CastBuffID) => oldCombatant.CastBuffID == combatant.CastBuffID,
			nameof(Combatant.CastTargetID) => oldCombatant.CastTargetID == combatant.CastTargetID,
			nameof(Combatant.CastGroundTargetX) => oldCombatant.CastGroundTargetX == combatant.CastGroundTargetX,
			nameof(Combatant.CastGroundTargetY) => oldCombatant.CastGroundTargetY == combatant.CastGroundTargetY,
			nameof(Combatant.CastGroundTargetZ) => oldCombatant.CastGroundTargetZ == combatant.CastGroundTargetZ,
			nameof(Combatant.CastDurationCurrent) => oldCombatant.CastDurationCurrent == combatant.CastDurationCurrent,
			nameof(Combatant.CastDurationMax) => oldCombatant.CastDurationMax == combatant.CastDurationMax,
			nameof(Combatant.TransformationId) => oldCombatant.TransformationId == combatant.TransformationId,
			nameof(Combatant.WeaponId) => oldCombatant.WeaponId == combatant.WeaponId,
			nameof(Combatant.Effects) => ReferenceEquals(oldCombatant.Effects, combatant.Effects),
			_ =>throw new Exception($"Field {fi} Not Support.")
		};
	}

	private string FormatFieldChange(string info, Combatant combatant, bool skipDefaultValues = false) {
		if (skipDefaultValues && IsDefaultValue(info, combatant))
			return string.Empty;

		return info switch {
			nameof(Combatant.Name) => combatant.Name == null
				? $"|{info}|NULL"
				: $"|{info}|{combatant.Name}",
			nameof(Combatant.OwnerID) => $"|{info}|{combatant.OwnerID:X}",
			nameof(Combatant.BNpcID) => $"|{info}|{combatant.BNpcID:X}",
			nameof(Combatant.BNpcNameID) => $"|{info}|{combatant.BNpcNameID:X}",
			nameof(Combatant.TargetID) => $"|{info}|{combatant.TargetID:X}",
			nameof(Combatant.NPCTargetID) => $"|{info}|{combatant.NPCTargetID:X}",
			nameof(Combatant.PCTargetID) => $"|{info}|{combatant.PCTargetID:X}",
			nameof(Combatant.CastTargetID) => $"|{info}|{combatant.CastTargetID:X}",
			nameof(Combatant.Type) => $"|{info}|{(byte)combatant.Type}",
			nameof(Combatant.MonsterType) => $"|{info}|{(byte)combatant.MonsterType}",
			nameof(Combatant.Status) => $"|{info}|{(byte)combatant.Status}",
			nameof(Combatant.ModelStatus) => $"|{info}|{(byte)combatant.ModelStatus}",
			nameof(Combatant.AggressionStatus) => $"|{info}|{(byte)combatant.AggressionStatus}",
			nameof(Combatant.PosX) => FormatFloat(info, combatant.PosX),
			nameof(Combatant.PosY) => FormatFloat(info, combatant.PosY),
			nameof(Combatant.PosZ) => FormatFloat(info, combatant.PosZ),
			nameof(Combatant.Heading) => FormatFloat(info, combatant.Heading),
			nameof(Combatant.Radius) => FormatFloat(info, combatant.Radius),
			nameof(Combatant.CastGroundTargetX) => FormatFloat(info, combatant.CastGroundTargetX),
			nameof(Combatant.CastGroundTargetY) => FormatFloat(info, combatant.CastGroundTargetY),
			nameof(Combatant.CastGroundTargetZ) => FormatFloat(info, combatant.CastGroundTargetZ),
			nameof(Combatant.CastDurationCurrent) => FormatFloat(info, combatant.CastDurationCurrent),
			nameof(Combatant.CastDurationMax) => FormatFloat(info, combatant.CastDurationMax),
			nameof(Combatant.Distance) => $"|{info}|{combatant.Distance}",
			nameof(Combatant.EffectiveDistance) => $"|{info}|{combatant.EffectiveDistance}",
			nameof(Combatant.ID) => $"|{info}|{combatant.ID:X}",
			nameof(Combatant.Job) => $"|{info}|{combatant.Job}",
			nameof(Combatant.CurrentHP) => $"|{info}|{combatant.CurrentHP}",
			nameof(Combatant.MaxHP) => $"|{info}|{combatant.MaxHP}",
			nameof(Combatant.IsTargetable) => $"|{info}|{combatant.IsTargetable}",
			nameof(Combatant.CurrentMP) => $"|{info}|{combatant.CurrentMP}",
			nameof(Combatant.MaxMP) => $"|{info}|{combatant.MaxMP}",
			nameof(Combatant.Level) => $"|{info}|{combatant.Level}",
			nameof(Combatant.WorldID) => $"|{info}|{combatant.WorldID}",
			nameof(Combatant.CurrentWorldID) => $"|{info}|{combatant.CurrentWorldID}",
			nameof(Combatant.CurrentGP) => $"|{info}|{combatant.CurrentGP}",
			nameof(Combatant.MaxGP) => $"|{info}|{combatant.MaxGP}",
			nameof(Combatant.CurrentCP) => $"|{info}|{combatant.CurrentCP}",
			nameof(Combatant.MaxCP) => $"|{info}|{combatant.MaxCP}",
			nameof(Combatant.IsCasting1) => $"|{info}|{combatant.IsCasting1}",
			nameof(Combatant.IsCasting2) => $"|{info}|{combatant.IsCasting2}",
			nameof(Combatant.CastBuffID) => $"|{info}|{combatant.CastBuffID:X}",
			nameof(Combatant.TransformationId) => $"|{info}|{combatant.TransformationId}",
			nameof(Combatant.WeaponId) => $"|{info}|{combatant.WeaponId}",
			_ => throw new ArgumentException($"Field {info} is not supported", nameof(info))
		};
	}

	private static string FormatFloat(string name, float value) =>
		string.Format(CultureInfo.InvariantCulture, "|{0}|{1:F4}", name, value);

	private static bool IsDefaultValue(string info, Combatant combatant) => info switch {
		nameof(Combatant.ID) => combatant.ID == 0,
		nameof(Combatant.OwnerID) => combatant.OwnerID == 0,
		nameof(Combatant.Type) => combatant.Type == default,
		nameof(Combatant.MonsterType) => combatant.MonsterType == default,
		nameof(Combatant.Status) => combatant.Status == default,
		nameof(Combatant.ModelStatus) => combatant.ModelStatus == default,
		nameof(Combatant.AggressionStatus) => combatant.AggressionStatus == default,
		nameof(Combatant.TargetID) => combatant.TargetID == 0,
		nameof(Combatant.IsTargetable) => !combatant.IsTargetable,
		nameof(Combatant.Job) => combatant.Job == 0,
		nameof(Combatant.Name) => string.IsNullOrEmpty(combatant.Name),
		nameof(Combatant.CurrentHP) => combatant.CurrentHP == 0,
		nameof(Combatant.MaxHP) => combatant.MaxHP == 0,
		nameof(Combatant.PosX) => combatant.PosX == 0,
		nameof(Combatant.PosY) => combatant.PosY == 0,
		nameof(Combatant.PosZ) => combatant.PosZ == 0,
		nameof(Combatant.Heading) => combatant.Heading == 0,
		nameof(Combatant.Radius) => combatant.Radius == 0,
		nameof(Combatant.Distance) => string.IsNullOrEmpty(combatant.Distance),
		nameof(Combatant.EffectiveDistance) => string.IsNullOrEmpty(combatant.EffectiveDistance),
		nameof(Combatant.BNpcID) => combatant.BNpcID == 0,
		nameof(Combatant.CurrentMP) => combatant.CurrentMP == 0,
		nameof(Combatant.MaxMP) => combatant.MaxMP == 0,
		nameof(Combatant.Level) => combatant.Level == 0,
		nameof(Combatant.BNpcNameID) => combatant.BNpcNameID == 0,
		nameof(Combatant.WorldID) => combatant.WorldID == 0,
		nameof(Combatant.CurrentWorldID) => combatant.CurrentWorldID == 0,
		nameof(Combatant.NPCTargetID) => combatant.NPCTargetID == 0,
		nameof(Combatant.CurrentGP) => combatant.CurrentGP == 0,
		nameof(Combatant.MaxGP) => combatant.MaxGP == 0,
		nameof(Combatant.CurrentCP) => combatant.CurrentCP == 0,
		nameof(Combatant.MaxCP) => combatant.MaxCP == 0,
		nameof(Combatant.PCTargetID) => combatant.PCTargetID == 0,
		nameof(Combatant.IsCasting1) => combatant.IsCasting1 == 0,
		nameof(Combatant.IsCasting2) => combatant.IsCasting2 == 0,
		nameof(Combatant.CastBuffID) => combatant.CastBuffID == 0,
		nameof(Combatant.CastTargetID) => combatant.CastTargetID == 0,
		nameof(Combatant.CastGroundTargetX) => combatant.CastGroundTargetX == 0,
		nameof(Combatant.CastGroundTargetY) => combatant.CastGroundTargetY == 0,
		nameof(Combatant.CastGroundTargetZ) => combatant.CastGroundTargetZ == 0,
		nameof(Combatant.CastDurationCurrent) => combatant.CastDurationCurrent == 0,
		nameof(Combatant.CastDurationMax) => combatant.CastDurationMax == 0,
		nameof(Combatant.TransformationId) => combatant.TransformationId == 0,
		nameof(Combatant.WeaponId) => combatant.WeaponId == 0,
		_ => false
	};

	private void WriteLine(CombatantMemoryChangeType type, uint combatantID, string info) =>
		logWriter?.Invoke($"{type}|{combatantID:X8}{info}", ffxiv.GetServerTimestamp());

	private unsafe void MessageReceived(string id, long epoch, byte[] message) {
		fixed (byte* buffer = message) {
			var header = Marshal.PtrToStructure<Server_MessageHeader>(new IntPtr(buffer));
			// Only check if we're not looking at a packet that's for just us
			if (header.ActorID == header.LoginUserID) return;
			var serverTime = ffxiv.EpochToDateTime(epoch);
			var delayDefault = CombatantChangeCriteria.Criteria(inCombat).DelayDefault;
			// Also only check if we're beyond the default delay for this ID, or if this ID doesn't exist yet
			// This check is in place to avoid reading memory every packet, excessively
			if (!combatantStateMap.TryGetValue(header.ActorID, out var value) || (serverTime - value.lastUpdated).TotalMilliseconds > delayDefault) {
				CheckCombatants(serverTime, header.ActorID);
			}
		}
	}

	protected virtual void Dispose(bool disposing) {
		if (_disposed) return;
		if (disposing) framework.Update -= PollCombatants;
		_disposed = true;
	}

	// Only emit a log line when this information changes every X milliseconds
	private class CombatantChangeCriteria {
		// in milliseconds
		public const int PollingRate = 20;

		private const uint InCombatDelayDefault = 1000;

		private const uint OutOfCombatDelayDefault = 5000;

		public static readonly CriteriaData InCombatCriteria = new() {
			DelayDefault = InCombatDelayDefault,
			DelayPosition = 250,
			DistancePosition = Math.Pow(5, 2),
			DistanceHeading = (float)(45 * (Math.PI / 180)), // 45º turns

			CheckFieldDelay = new ReadOnlyDictionary<string, uint>(new Dictionary<string, uint> {
				// Default delay threshold
				[nameof(Combatant.OwnerID)] = InCombatDelayDefault,
				[nameof(Combatant.Type)] = InCombatDelayDefault,
				[nameof(Combatant.MonsterType)] = InCombatDelayDefault,
				[nameof(Combatant.Status)] = InCombatDelayDefault,
				[nameof(Combatant.AggressionStatus)] = InCombatDelayDefault,
				[nameof(Combatant.IsTargetable)] = InCombatDelayDefault,
				[nameof(Combatant.Name)] = InCombatDelayDefault,
				[nameof(Combatant.Radius)] = InCombatDelayDefault,
				[nameof(Combatant.BNpcID)] = InCombatDelayDefault,
				[nameof(Combatant.CurrentMP)] = InCombatDelayDefault,
				[nameof(Combatant.IsCasting1)] = InCombatDelayDefault,
				// No delay threshold
				[nameof(Combatant.BNpcNameID)] = 0,
				[nameof(Combatant.TransformationId)] = 0,
				[nameof(Combatant.WeaponId)] = 0,
				[nameof(Combatant.TargetID)] = 0,
				[nameof(Combatant.ModelStatus)] = 0
			})
		};

		public static readonly CriteriaData OutOfCombatCriteria = new() {
			DelayDefault = OutOfCombatDelayDefault,
			DelayPosition = 1250,
			DistancePosition = Math.Pow(15, 2),
			DistanceHeading = 20f, // Effectively disabled

			CheckFieldDelay = new ReadOnlyDictionary<string, uint>(new Dictionary<string, uint> {
				// Default delay threshold
				[nameof(Combatant.OwnerID)] = OutOfCombatDelayDefault,
				[nameof(Combatant.Type)] = OutOfCombatDelayDefault,
				[nameof(Combatant.MonsterType)] = OutOfCombatDelayDefault,
				[nameof(Combatant.Status)] = OutOfCombatDelayDefault,
				[nameof(Combatant.AggressionStatus)] = OutOfCombatDelayDefault,
				[nameof(Combatant.IsTargetable)] = OutOfCombatDelayDefault,
				[nameof(Combatant.Name)] = OutOfCombatDelayDefault,
				[nameof(Combatant.Radius)] = OutOfCombatDelayDefault,
				[nameof(Combatant.BNpcID)] = OutOfCombatDelayDefault,
				[nameof(Combatant.CurrentMP)] = OutOfCombatDelayDefault,
				[nameof(Combatant.IsCasting1)] = OutOfCombatDelayDefault,
				// No delay threshold
				[nameof(Combatant.BNpcNameID)] = 1000,
				[nameof(Combatant.TransformationId)] = 1000,
				[nameof(Combatant.WeaponId)] = 1000,
				[nameof(Combatant.TargetID)] = 1000,
				[nameof(Combatant.ModelStatus)] = 1000
			})
		};

		private static readonly string[] IgnoreFieldNames = [
			// "ID" is always printed
			nameof(Combatant.ID),
			// Exclude "Effects" due to object complexity
			nameof(Combatant.Effects),
			// Excluded due to not being useful
			// TODO: Maybe this should just add any field flagged as NonSerialized, if additional fields are added?
			nameof(Combatant.RawEffectiveDistance),
			// Excluded due to being included in many, many other lines
			nameof(Combatant.CurrentHP),

			// These are not currently written to but exclude them in case they're updated upstream properly in the future
			nameof(Combatant.Distance), nameof(Combatant.EffectiveDistance),

			// These fields are calculated and can be determined downstream by consumers if needed
			nameof(Combatant.TargetID), nameof(Combatant.IsTargetable),

			// CP and GP are pointless since this is currently restricted to combat-only.
			nameof(Combatant.CurrentCP), nameof(Combatant.MaxCP), nameof(Combatant.CurrentGP), nameof(Combatant.MaxGP)
		];

		// Fields that should be written out for add or full list of changes
		public static readonly string[] AllFields = [
			.. typeof(Combatant).GetFields()
				.Select(f=>f.Name)
				.Where(field => !IgnoreFieldNames.Contains(field))
		];

		public static CriteriaData Criteria(bool inCombat) => inCombat ? InCombatCriteria : OutOfCombatCriteria;

		public class CriteriaData {
			public ReadOnlyDictionary<string, uint> CheckFieldDelay;
			// in milliseconds
			public uint DelayDefault; // If any property has changed in this timeframe, a line will be written
			public uint DelayPosition;
			// in radians
			public float DistanceHeading;
			// in in-game distance, squared
			public double DistancePosition;
		}
	}

	private class CombatantStateInfo {
		public Combatant combatant;
		public DateTime lastUpdated;
	}

	private enum CombatantMemoryChangeType {
		Add,
		Remove,
		Change
	}
}
