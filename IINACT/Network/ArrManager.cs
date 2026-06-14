using System;
using System.Linq;
using System.Text;
using Dalamud.Hooking;
using Machina.FFXIV.Headers.Opcodes;
using static IINACT.Plugin;

namespace IINACT.Network;

public unsafe class ArrManager : IDisposable {
	public ArrManager() {
		ReplayPacketHook = GameInteropProvider.HookFromAddress<ReplayPacketDelegate>(SigScanner.ScanText("E8 ?? ?? ?? ?? 80 BB ?? ?? ?? ?? ?? 77 9A"), ReplayPacketDetour);
		ReplayPacketHook.Enable();
	}

	public struct DataSegment {
		public ushort Opcode;
		public ushort DataLength;
		public uint MS;
		public uint ObjectID;

		public uint Length => (uint)sizeof(DataSegment) + DataLength;

		public byte* Data {
			get {
				fixed (void* ptr = &this) {
					return (byte*)ptr + sizeof(DataSegment);
				}
			}
		}
	}

	private static bool ReplayPacketDetour(IntPtr contentsReplayModule, DataSegment* segment, byte* data) {
		var ret = ReplayPacketHook.Original(contentsReplayModule, segment, data);
		var hexBuilder = new StringBuilder();
		var packetStart = (byte*)segment;
		for (var i = 0; i < segment->Length; i++) {
			hexBuilder.Append(packetStart[i].ToString("X2"));
			hexBuilder.Append(' ');
		}
		Log.Warning($"[R][Op:{segment->Opcode:X4}({OpcodeManager.Instance.CurrentOpcodes.FirstOrDefault(kv => kv.Value == segment->Opcode).Key ?? "UNKNOWN"})][L:{segment->Length}B][MS:{segment->MS}][ObjectID:{segment->ObjectID}][D:{hexBuilder}]");
		return ret;
	}

	private delegate bool ReplayPacketDelegate(IntPtr contentsReplayModule, DataSegment* segment, byte* data);

	private static Hook<ReplayPacketDelegate> ReplayPacketHook;

	public void Dispose() {
		ReplayPacketHook.Disable();
		ReplayPacketHook.Dispose();
	}
}