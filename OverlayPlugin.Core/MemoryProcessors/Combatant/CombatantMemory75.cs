using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

internal interface ICombatantMemory75 : ICombatantMemory;

internal class CombatantMemory75 : CombatantMemory, ICombatantMemory75 {
	public override Version GetVersion() => new(7, 4);

	// Returns a combatant if the combatant is a mob or a PC.
	public override unsafe Combatant? GetMobFromByteArray(GameObject* gameObject, uint mycharID) {
		if (gameObject == null) return null;
		var mem = Marshal.PtrToStructure<Character>((IntPtr)gameObject);
		return mem.EntityId is 0 or emptyID ? null : GetCombatantFromByteArray(gameObject, mycharID, false);
	}

	// Will return any kind of combatant, even if not a mob.
	// This function always returns a combatant object, even if empty.
	protected override unsafe Combatant? GetCombatantFromByteArray(GameObject* gameObject, uint mycharID, bool isPlayer, bool exceptEffects = false) {
		if (gameObject == null) return null;
		var ori = Marshal.PtrToStructure<BattleChara>((IntPtr)gameObject);
		if (isPlayer) mycharID = ori.EntityId;
		var type = (ObjectType)ori.ObjectKind;
		var PCTargetID = (uint)ori.LookAt.Controller.Params[0].TargetParam.TargetId;
		var position = ori.Position;
		var castinfo = ori.CastInfo;
		var targetLocation = castinfo.TargetLocation;
		var combatant = new Combatant {
			Name = ori.NameString,
			Job = ori.ClassJob,
			ID = ori.EntityId,
			OwnerID = ori.OwnerId == emptyID ? 0 : ori.OwnerId,
			Type = type,
			MonsterType = 0,
			Status = (ObjectStatus)(byte)(&ori.TargetableStatus - 2),
			ModelStatus = (ModelStatus)ori.RenderFlags,
			// Normalize all possible aggression statuses into the basic 4 ones.
			AggressionStatus = 0,
			NPCTargetID = (uint)ori.TargetId,
			RawEffectiveDistance = ori.YalmDistanceFromPlayerZ,
			PosX = position.X,
			// Y and Z are deliberately swapped to match FFXIV_ACT_Plugin's data model
			PosY = position.Z,
			PosZ = position.Y,
			Heading = ori.Rotation,
			Radius = ori.HitboxRadius,
			// In-memory there are separate values for PC's current target and NPC's current target
			TargetID = type == ObjectType.PC ? PCTargetID : (uint)ori.TargetId,
			CurrentHP = (int)ori.Health,
			MaxHP = (int)ori.MaxHealth,
			Effects = exceptEffects ? [] : GetEffectEntries(ori.StatusManager, type, mycharID),

			BNpcID = ori.BaseId,
			CurrentMP = (int)ori.Mana,
			MaxMP = (int)ori.MaxMana,
			CurrentGP = ori.GatheringPoints,
			MaxGP = ori.MaxGatheringPoints,
			CurrentCP = ori.CraftingPoints,
			MaxCP = ori.MaxCraftingPoints,
			Level = ori.Level,
			PCTargetID = PCTargetID,

			BNpcNameID = ori.NameId,

			WorldID = ori.HomeWorld,
			CurrentWorldID = ori.CurrentWorld,

			// @TODO: Fix this for 8.0 with breaking change.
			// ref: <https://github.com/aers/FFXIVClientStructs/commit/da0a825b4758b82136ae1826d7168c230e3ab063>
			IsCasting1 = Convert.ToByte(castinfo.IsCasting),
			IsCasting2 = castinfo.ActionType,
			CastBuffID = castinfo.ActionId,
			CastTargetID = (uint)castinfo.TargetId,
			// Y and Z are deliberately swapped to match FFXIV_ACT_Plugin's data model
			CastGroundTargetX = targetLocation.X,
			CastGroundTargetY = targetLocation.Z,
			CastGroundTargetZ = targetLocation.Y,
			CastDurationCurrent = castinfo.CurrentCastTime,
			CastDurationMax = castinfo.BaseCastTime,

			TransformationId = ori.TransformationId,
			WeaponId = ori.Timeline.ModelState
		};
		combatant.IsTargetable = combatant is { ModelStatus: ModelStatus.Visible, Status: ObjectStatus.NormalActorStatus or ObjectStatus.NormalSubActorStatus };
		if (combatant.Type != ObjectType.PC && combatant.Type != ObjectType.Monster) {
			// Other types have garbage memory for hp.
			combatant.CurrentHP = 0;
			combatant.MaxHP = 0;
		}
		return combatant;
	}
}