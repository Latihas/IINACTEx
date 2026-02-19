using System;
using System.Collections.Generic;
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

    internal Region Region { get; set; } = Region.China;

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
        try {
            LoadData(ResourceFileName, out Opcodes);
        }
        catch (Exception ex) {
            Keeper.Log("Failed to load opcodes. Corrupted json file?");
            Keeper.Log(ex.ToString());
        }
        foreach (var opcode in Opcodes) {
            OpcodeBytype[(OpcodeType)Enum.Parse(OpcodeType.InitZone.GetType(), opcode.Name)] = opcode;
        }
        TypeByOpcodeGlobal = new Dictionary<ushort, OpcodeType>();
        TypeByOpcodeCn = new Dictionary<ushort, OpcodeType>();
        foreach (var opcode2 in Opcodes) {
            TypeByOpcodeGlobal[opcode2.GetOpcode(Region.Global)] = (OpcodeType)Enum.Parse(typeof(OpcodeType), opcode2.Name);
            TypeByOpcodeCn[opcode2.GetOpcode(Region.China)] = (OpcodeType)Enum.Parse(typeof(OpcodeType), opcode2.Name);
        }
    }
}