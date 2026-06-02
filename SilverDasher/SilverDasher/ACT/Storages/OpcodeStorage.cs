using System;
using System.Collections.Generic;
using Machina.FFXIV;
using Machina.FFXIV.Headers.Opcodes;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Enums;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class OpcodeStorage(Keeper kp) : BaseStorage<int, OpcodeType>(kp) {
	private List<Opcode> Opcodes = [];
	private readonly Dictionary<OpcodeType, Opcode> OpcodeBytype = new();
	private Dictionary<ushort, OpcodeType> TypeByOpcodeGlobal = new();
	private Dictionary<ushort, OpcodeType> TypeByOpcodeCn = new();
	internal override string? ResourceFileName => null;
	private static Region Region => Region.China;
	internal override bool Contains(OpcodeType name) => OpcodeBytype.ContainsKey(name);
	internal override int Get(OpcodeType id) => OpcodeBytype[id].GetOpcode(Region);
	internal Opcode GetOpcode(OpcodeType id) => OpcodeBytype[id];
	private Dictionary<ushort, OpcodeType> TypeByOpcode => Region switch {
		Region.China => TypeByOpcodeCn,
		Region.Global => TypeByOpcodeGlobal,
		_ => throw new NotImplementedException()
	};

	internal OpcodeType GetOpcodeType(ushort packetCode) {
		TypeByOpcode.TryGetValue(packetCode, out var value);
		return value;
	}

	internal override void Load() {
		var c = SilverDasher.UnscramblerConstants;
		var inst = OpcodeManager.Instance._opcodes[GameRegion.Chinese]["ActorControlSelf"];
		Opcodes = [
			new Opcode {
				Name = "InitZone",
				cnRaw = $"0x{c.InitZoneOpcode:X4}",
				CnLength = c.InitZoneLength,
				globalRaw = $"0x{c.InitZoneOpcode:X4}",
				GlobalLength = c.InitZoneLength
			},
			new Opcode {
				Name = "FateInfo",
				cnRaw = $"0x{c.FateInfoOpcode:X4}",
				CnLength = c.FateInfoLength,
				globalRaw = $"0x{c.FateInfoOpcode:X4}",
				GlobalLength = c.FateInfoLength
			},
			new Opcode {
				Name = "ActorControlSelf",
				cnRaw = $"0x{inst:X4}",
				CnLength = c.ActorControlSelfLength,
				globalRaw = $"0x{inst:X4}",
				GlobalLength = c.ActorControlSelfLength,
				PacketSubType = new Dictionary<string, int> {
					["FateStart"] = c.FateStart,
					["FateEnd"] = c.FateEnd,
					["FateProgress"] = c.FateProgress
				}
			}
		];
		TypeByOpcodeGlobal = new Dictionary<ushort, OpcodeType>();
		TypeByOpcodeCn = new Dictionary<ushort, OpcodeType>();
		foreach (var opcode in Opcodes) {
			if (!Enum.TryParse<OpcodeType>(opcode.Name, out var ot)) continue;
			OpcodeBytype[ot] = opcode;
			TypeByOpcodeGlobal[opcode.GetOpcode(Region.Global)] = ot;
			TypeByOpcodeCn[opcode.GetOpcode(Region.China)] = ot;
		}
	}
}