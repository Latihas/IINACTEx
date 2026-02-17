using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using SilverDasher.ACT.Enums;

namespace SilverDasher.ACT.Models;

public class Opcode
{
	[JsonProperty("name")]
	public string Name;

	[JsonProperty("global")]
	public string globalRaw = "";

	[JsonIgnore]
	private ushort globalCode;

	[JsonProperty("cn")]
	public string cnRaw = "";

	[JsonIgnore]
	private ushort cnCode;

	[JsonProperty("kr")]
	public string krRaw = "";

	[JsonIgnore]
	private ushort krCode;

	[JsonProperty("cnl")]
	public int CnLength = -1;

	[JsonProperty("globall")]
	public int GlobalLength = -1;

	[JsonProperty("types")]
	public IDictionary<string, int> PacketSubType = new Dictionary<string, int>();

	[JsonIgnore]
	private Dictionary<int, string> packetSubTypeNameByID;

	[JsonIgnore]
	public ushort Global
	{
		get
		{
			if (globalCode == 0)
			{
				globalCode = ushort.Parse(globalRaw.Replace("0x", ""), NumberStyles.HexNumber);
			}
			return globalCode;
		}
	}

	[JsonIgnore]
	public ushort Cn
	{
		get
		{
			if (cnCode == 0)
			{
				cnCode = ushort.Parse(cnRaw.Replace("0x", ""), NumberStyles.HexNumber);
			}
			return cnCode;
		}
	}

	[JsonIgnore]
	public ushort Kr
	{
		get
		{
			if (krCode == 0)
			{
				krCode = ushort.Parse(krRaw.Replace("0x", ""), NumberStyles.HexNumber);
			}
			return krCode;
		}
	}

	[JsonIgnore]
	public Dictionary<int, string> PacketSubTypeNameByID
	{
		get
		{
			if (PacketSubType == null)
			{
				packetSubTypeNameByID = new Dictionary<int, string>();
			}
			else if (packetSubTypeNameByID == null)
			{
				packetSubTypeNameByID = new Dictionary<int, string>();
				foreach (string key in PacketSubType.Keys)
				{
					packetSubTypeNameByID[PacketSubType[key]] = key;
				}
			}
			return packetSubTypeNameByID;
		}
	}

	internal string GetPacketSubType(short type)
	{
		if (PacketSubTypeNameByID.TryGetValue(type, out var value))
		{
			return value;
		}
		return "";
	}

	internal ushort GetOpcode(Region region)
	{
		return region switch
		{
			Region.China => Cn, 
			Region.Global => Global, 
			Region.Korean => Kr, 
			_ => throw new NotImplementedException("Bad region " + Enum.GetName(typeof(Region), region) + "."), 
		};
	}
}
