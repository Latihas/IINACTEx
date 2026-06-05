using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Group;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Party;

internal interface IPartyMemory70 : IPartyMemory;

public class PartyMemory70(TinyIoCContainer container) : PartyMemory(container), IPartyMemory70 {
	// Due to lack of multi-version support in FFXIVClientStructs, we need to duplicate these structures here per-version
	// We use FFXIVClientStructs versions of the structs because they have more required details than FFXIV_ACT_Plugin's struct definitions

	#region FFXIVClientStructs structs

	[StructLayout(LayoutKind.Explicit, Size = 0x65B0)]
	public unsafe struct GroupManager {
		[FieldOffset(0x0)] public fixed byte PartyMembers[0x3A0 * 8]; // PartyMember type
		[FieldOffset(0x1D00)] public fixed byte AllianceMembers[0x3A0 * 20]; // PartyMember type
		// Immediately after `AllianceMembers`, e.g. ((0x3A0 * 20)+0x1D00) = 0x6580
		[FieldOffset(0x6580)] public uint CurrentPartyFlags; // FFXIVClientStructs doesn't map this, but it contains flags that indicate the alliance the player is in, as well as other flags.
		[FieldOffset(0x6588)] public long PartyId; // both seem to be unique per party and replicated to every member
		[FieldOffset(0x6590)] public long PartyId_2;
		[FieldOffset(0x6598)] public uint PartyLeaderIndex; // index of party leader in array
		[FieldOffset(0x659C)] public byte MemberCount;
		[FieldOffset(0x65A1)] public byte AllianceFlags; // 0x01 == is alliance; 0x02 == alliance with 5 4-man groups rather than 2 8-man
	}

	#endregion

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private unsafe struct DoubleGroupManager {
		public fixed byte vtbls[0x20];
		public GroupManager groupManager1;
		public GroupManager groupManager2;
	}

	public override Version GetVersion() => new(7, 0);


	public unsafe PartyListsStruct GetPartyLists() {
		if (!IsValid()) {
			return new PartyListsStruct();
		}

		ScanPointers();

		if (partyInstanceAddress.ToInt64() == 0) {
			return new PartyListsStruct();
		}
		var groupManager = Marshal.PtrToStructure<DoubleGroupManager>(partyInstanceAddress);

		// `PartyMembers` is a standard array, members are moved up/down as they're added/removed.
		// As such, limit extracting members to the current count to avoid "ghost" members
		var partyMembers = extractPartyMembers(groupManager.groupManager1.PartyMembers, Math.Min((int)groupManager.groupManager1.MemberCount, 8));

		// `AllianceMembers` is a fixed-position array, with removed elements being mostly zero'd out
		// Easiest way to check if an entry is still active is to check for `Flags != 0`
		var alliance1Members = extractAllianceMembers(groupManager.groupManager1.AllianceMembers, 20, 0, 8);
		var alliance2Members = extractAllianceMembers(groupManager.groupManager1.AllianceMembers, 20, 8, 8);
		// TOOD: Actually verify D/E/F alliance info?
		var alliance3Members = extractAllianceMembers(groupManager.groupManager2.PartyMembers, 8, 0, 8);
		var alliance4Members = extractAllianceMembers(groupManager.groupManager2.AllianceMembers, 20, 0, 8);
		var alliance5Members = extractAllianceMembers(groupManager.groupManager2.AllianceMembers, 20, 8, 8);

		return new PartyListsStruct {
			partyId = groupManager.groupManager1.PartyId,
			partyId_2 = groupManager.groupManager1.PartyId_2,
			partyLeaderIndex = groupManager.groupManager1.PartyLeaderIndex,
			memberCount = groupManager.groupManager1.MemberCount,
			allianceFlags = groupManager.groupManager1.AllianceFlags,

			currentPartyFlags = groupManager.groupManager1.CurrentPartyFlags,

			partyMembers = partyMembers,
			alliance1Members = alliance1Members,
			alliance2Members = alliance2Members,
			alliance3Members = alliance3Members,
			alliance4Members = alliance4Members,
			alliance5Members = alliance5Members
		};
	}

	private unsafe PartyListEntry[] extractAllianceMembers(byte* allianceMembers, int elementCount, int start, int count) {
		var allMembers = extractPartyMembers(allianceMembers, elementCount);
		var retMembers = new PartyListEntry[count];
		for (var i = start; i < start + count && i < allMembers.Length; ++i) {
			var member = allMembers[i];
			if (member.flags == 0) continue;
			retMembers[i - start] = member;
		}
		return retMembers;
	}

	private unsafe PartyListEntry[] extractPartyMembers(byte* ptr, int count) {
		var ret = new PartyListEntry[count];
		for (var i = 0; i < count; ++i) {
			var member = Marshal.PtrToStructure<PartyMember>(new IntPtr(ptr + i * sizeof(PartyMember)));
			ret[i] = new PartyListEntry {
				x = member.Position.X,
				y = member.Position.Y,
				z = member.Position.Z,
				contentId = (long)member.ContentId,
				objectId = member.EntityId,
				currentHP = member.CurrentHP,
				maxHP = member.MaxHP,
				currentMP = member.CurrentMP,
				maxMP = member.MaxMP,
				territoryType = member.TerritoryType,
				homeWorld = member.HomeWorld,
				name = member.NameString,
				sex = member.Sex,
				classJob = member.ClassJob,
				level = member.Level,
				flags = member.Flags
			};
		}
		return ret;
	}
}