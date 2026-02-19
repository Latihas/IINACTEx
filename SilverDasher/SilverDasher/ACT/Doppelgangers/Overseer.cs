using System;
using System.Linq;
using SilverDasher.ACT.Enums;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Doppelgangers;

internal class Overseer(SilverDasher self) : Doppelganger(self) {
    internal override void Init() {
    }

    internal void OnNetworkReceive(string connection, long epoch, byte[] message) {
        var packetOpcodeType = GetPacketOpcodeType(message);
        var opcode = Keeper.Opcodes.GetOpcode(packetOpcodeType);
        switch (packetOpcodeType) {
            case OpcodeType.ActorControlSelf:
                HandleActorControlSelf(message, opcode);
                break;
            case OpcodeType.FateInfo:
                HandleFateInfo(message, opcode);
                break;
            case OpcodeType.InitZone:
                HandleInitZone(message, opcode);
                break;
        }
    }

    private void HandleActorControlSelf(byte[] message, Opcode opcode) {
        if (message.Length != opcode.CnLength) {
            Logger.Debug($"Packet {opcode.Name} received unpredicted length. Expected {opcode.CnLength}, Received {message.Length}.");
            return;
        }
        var array = message.Skip(32).ToArray();
        var type = BitConverter.ToInt16(array, 0);
        switch (opcode.GetPacketSubType(type)) {
            case "FateProgress": {
                var num3 = BitConverter.ToUInt16(array, 4);
                var b = array[8];
                Keeper.FateUpdate(num3, b);
                Logger.Debug($"ActorControlSelf FateProgress handled. Fate {num3} Progress {b}.");
                break;
            }
            case "FateEnd": {
                var num2 = BitConverter.ToUInt16(array, 4);
                Keeper.CurrentFates.Remove(num2);
                Logger.Debug($"ActorControlSelf FateEnd handled. Fate {num2} ended.");
                break;
            }
            case "FateStart": {
                var num = BitConverter.ToUInt16(array, 4);
                Keeper.FateUpdate(num, 0);
                Logger.Debug($"ActorControlSelf FateStart handled. Fate {num} started.");
                break;
            }
        }
    }

    private void HandleFateInfo(byte[] message, Opcode opcode) {
        if (message.Length != opcode.CnLength) {
            Logger.Debug($"Packet {opcode.Name} received unpredicted length. Expected {opcode.CnLength}, Received {message.Length}.");
            return;
        }
        var value = message.Skip(32).ToArray();
        var fateId = BitConverter.ToUInt16(value, 0);
        var startTime = BitConverter.ToUInt32(value, 8);
        var duration = BitConverter.ToUInt32(value, 16);
        Keeper.FateUpdate(fateId, -1, null, startTime, duration);
    }

    private void HandleInitZone(byte[] message, Opcode opcode) {
        if (message.Length != opcode.CnLength) {
            Logger.Debug($"Packet {opcode.Name} received unpredicted length. Expected {opcode.CnLength}, Received {message.Length}.");
            return;
        }
        var value = message.Skip(32).ToArray();
        BitConverter.ToUInt16(value, 0);
        var num = BitConverter.ToUInt16(value, 2);
        var num2 = BitConverter.ToUInt16(value, 4);
        if (num2 > 3) return;
        // Keeper.NetworkInstance = num2;
        Keeper.NetworkMapID = num;
        Keeper.CurrentMobs.Clear();
        Keeper.CurrentFates.Clear();
        Log($"Network map changes to {num}");
        Log($"Network instance changes to {num2}");
    }

    // private void HandleCEDirector(byte[] message) {
    // }

    private OpcodeType GetPacketOpcodeType(byte[] message) => Keeper.Opcodes.GetOpcodeType(BitConverter.ToUInt16(message, 18));

    internal override void Deinit() {
    }
}