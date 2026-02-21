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

    internal override string ResourceFileName => "opcodes.json";

    private Dictionary<ushort, OpcodeType> TypeByOpcode => Region switch {
        Region.China => TypeByOpcodeCn,
        Region.Global => TypeByOpcodeGlobal,
        _ => throw new NotImplementedException()
    };

    private static Region Region => Region.China;

    internal override bool Contains(OpcodeType name) => OpcodeBytype.ContainsKey(name);

    internal override int Get(OpcodeType id) => OpcodeBytype[id].GetOpcode(Region);

    internal Opcode GetOpcode(OpcodeType id) => OpcodeBytype[id];

    // internal IEnumerable<OpcodeType> Keys() => OpcodeBytype.Keys;

    // internal List<int> GetOpcodes() {
    //     List<int> list = [];
    //     list.AddRange(Keys().Select(item => Get(item)));
    //     return list;
    // }

    internal OpcodeType GetOpcodeType(ushort packetCode) {
        TypeByOpcode.TryGetValue(packetCode, out var value);
        return value;
    }

    internal override void Load() {
        LoadData(ResourceFileName, out Opcodes);
        TypeByOpcodeGlobal = new Dictionary<ushort, OpcodeType>();
        TypeByOpcodeCn = new Dictionary<ushort, OpcodeType>();
        foreach (var opcode in Opcodes) {
            if (!Enum.TryParse<OpcodeType>(opcode.Name, out var ot)) continue;
            if (ot == OpcodeType.ActorControlSelf) {
                var inst = OpcodeManager.Instance._opcodes;
                opcode.cnRaw = $"0x{inst[GameRegion.Chinese]["ActorControlSelf"]:X4}";
                opcode.globalRaw = $"0x{inst[GameRegion.Global]["ActorControlSelf"]:X4}";
                opcode.krRaw = $"0x{inst[GameRegion.Korean]["ActorControlSelf"]:X4}";
            }
            OpcodeBytype[ot] = opcode;
            TypeByOpcodeGlobal[opcode.GetOpcode(Region.Global)] = ot;
            TypeByOpcodeCn[opcode.GetOpcode(Region.China)] = ot;
        }
    }
}