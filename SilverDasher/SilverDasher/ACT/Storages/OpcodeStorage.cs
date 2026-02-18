using System;
using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Enums;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class OpcodeStorage : BaseStorage<int, OpcodeType>
{
	internal List<Opcode> Opcodes = [];

	internal Dictionary<OpcodeType, Opcode> OpcodeBytype = new();

	internal Dictionary<ushort, OpcodeType> TypeByOpcodeGlobal = new();

	internal Dictionary<ushort, OpcodeType> TypeByOpcodeCn = new();

	private Region region = Region.China;

	internal override string ResourceFileName => "opcodes.json";

	internal Dictionary<ushort, OpcodeType> TypeByOpcode => Region switch
	{
		Region.China => TypeByOpcodeCn, 
		Region.Global => TypeByOpcodeGlobal, 
		_ => throw new NotImplementedException(), 
	};

	internal Region Region
	{
		get
		{
			return region;
		}
		set
		{
			region = value;
		}
	}

	internal OpcodeStorage(Keeper kp)
		: base(kp)
	{
	}

	internal override bool Contains(OpcodeType name)
	{
		return OpcodeBytype.ContainsKey(name);
	}

	internal override int Get(OpcodeType id)
	{
		return OpcodeBytype[id].GetOpcode(Region);
	}

	internal Opcode GetOpcode(OpcodeType id)
	{
		return OpcodeBytype[id];
	}

	internal override IEnumerable<OpcodeType> Keys()
	{
		return OpcodeBytype.Keys;
	}

	internal List<int> GetOpcodes()
	{
		List<int> list = [];
		foreach (OpcodeType item in Keys())
		{
			list.Add(Get(item));
		}
		return list;
	}

	internal OpcodeType GetOpcodeType(ushort packetCode)
	{
		TypeByOpcode.TryGetValue(packetCode, out var value);
		return value;
	}

	internal override void Load()
	{
		try
		{
			LoadData<List<Opcode>>(ResourceFileName, out Opcodes);
		}
		catch (Exception ex)
		{
			Keeper.Log("Failed to load opcodes. Corrupted json file?");
			Keeper.Log(ex.ToString());
		}
		foreach (Opcode opcode in Opcodes)
		{
			OpcodeBytype[(OpcodeType)Enum.Parse(OpcodeType.InitZone.GetType(), opcode.Name)] = opcode;
		}
		TypeByOpcodeGlobal = new Dictionary<ushort, OpcodeType>();
		TypeByOpcodeCn = new Dictionary<ushort, OpcodeType>();
		foreach (Opcode opcode2 in Opcodes)
		{
			TypeByOpcodeGlobal[opcode2.GetOpcode(Region.Global)] = (OpcodeType)Enum.Parse(typeof(OpcodeType), opcode2.Name);
			TypeByOpcodeCn[opcode2.GetOpcode(Region.China)] = (OpcodeType)Enum.Parse(typeof(OpcodeType), opcode2.Name);
		}
	}
}
