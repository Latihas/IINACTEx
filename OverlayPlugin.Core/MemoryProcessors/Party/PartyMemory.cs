namespace RainbowMage.OverlayPlugin.MemoryProcessors.Party;

public class PartyListEntry {
	public float x;
	public float y;
	public float z;
	public long contentId;
	public uint objectId;
	public uint currentHP;
	public uint maxHP;
	public ushort currentMP;
	public ushort maxMP;
	public ushort territoryType;
	public ushort homeWorld;
	public string name;
	public byte sex;
	public byte classJob;
	public byte level;
	public byte flags;
}

public class PartyListsStruct {
	public long partyId;
	public long partyId_2;
	public uint partyLeaderIndex;
	public byte memberCount;
	public byte allianceFlags;

	public uint currentPartyFlags;

	public PartyListEntry[] partyMembers;
	public PartyListEntry[] alliance1Members;
	public PartyListEntry[] alliance2Members;
	public PartyListEntry[] alliance3Members;
	public PartyListEntry[] alliance4Members;
	public PartyListEntry[] alliance5Members;
}

public abstract class PartyMemory(TinyIoCContainer container) {
	protected readonly FFXIVMemory memory = container.Resolve<FFXIVMemory>();

	public bool IsValid() => memory.IsValid();

	public void ScanPointers() {
	}
}