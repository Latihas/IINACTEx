using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using SilverDasher.ACT.Enums;

namespace SilverDasher.ACT.Models;

public class Opcode {
	[JsonProperty("name")] public string Name;

	[JsonProperty("global")] public string globalRaw = "";

	[JsonProperty("cn")] public string cnRaw = "";

	[JsonProperty("kr")] public string krRaw = "";

	[JsonProperty("cnl")] public int CnLength = -1;

	[JsonProperty("globall")] public int GlobalLength = -1;

	[JsonProperty("types")] public IDictionary<string, int> PacketSubType = new Dictionary<string, int>();

	[JsonIgnore] private ushort Global {
		get {
			if (field == 0) field = ushort.Parse(globalRaw.Replace("0x", ""), NumberStyles.HexNumber);
			return field;
		}
	}

	[JsonIgnore] private ushort Cn {
		get {
			if (field == 0) field = ushort.Parse(cnRaw.Replace("0x", ""), NumberStyles.HexNumber);
			return field;
		}
	}

	[JsonIgnore] private ushort Kr {
		get {
			if (field == 0) field = ushort.Parse(krRaw.Replace("0x", ""), NumberStyles.HexNumber);
			return field;
		}
	}

	[JsonIgnore] private Dictionary<int, string> PacketSubTypeNameByID {
		get {
			if (PacketSubType == null) field = new Dictionary<int, string>();
			else if (field == null) {
				field = new Dictionary<int, string>();
				foreach (var key in PacketSubType.Keys) field[PacketSubType[key]] = key;
			}
			return field;
		}
	}

	internal string GetPacketSubType(short type) => PacketSubTypeNameByID.GetValueOrDefault(type, "");

	internal ushort GetOpcode(Region region) {
		return region switch {
			Region.China => Cn,
			Region.Global => Global,
			Region.Korean => Kr,
			_ => throw new NotImplementedException($"Bad region {Enum.GetName(typeof(Region), region)}.")
		};
	}
}