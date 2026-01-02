using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Machina.FFXIV;

// The easiest place to test SetAnimationState is Lunar Subteranne.
// On the first Ruinous Confluence, each staff has this line:
// 273|2023-12-05T10:57:43.4770000-08:00|4000A145|003E|00000001|00000000|00000000|00000000|06e7eff4a949812c
// On the second Ruinous Confluence, each staff has this line:
// 273|2023-12-05T10:58:00.3460000-08:00|4000A144|003E|00000001|00000001|00000000|00000000|a4af9f90928636a3

namespace RainbowMage.OverlayPlugin.NetworkProcessors;

public class LineActorControlExtra {
    public const uint LogFileLineID = 273;
    private ILogger logger;
    private readonly FFXIVRepository ffxiv;

    // Any category defined in this array will be allowed as an emitted line
    public static readonly Server_ActorControlCategory[] AllowedActorControlCategories = [
        Server_ActorControlCategory.SetAnimationState,
        Server_ActorControlCategory.DisplayPublicContentTextMessage,
        Server_ActorControlCategory.VfxUnknown49,
        Server_ActorControlCategory.SetModelState,
        Server_ActorControlCategory.PlayActionTimeline,
        Server_ActorControlCategory.EObjAnimation
    ];

    private class RegionalizedInfo {
        public readonly int packetSize;
        public readonly int packetOpcode;
        public readonly int offsetMessageType;
        public readonly Type headerType;
        public readonly Type actorControlType;
        public readonly FieldInfo fieldCastSourceId;
        public readonly FieldInfo fieldCategory;
        public readonly FieldInfo fieldParam1;
        public readonly FieldInfo fieldParam2;
        public readonly FieldInfo fieldParam3;
        public readonly FieldInfo fieldParam4;

        public RegionalizedInfo(Type headerType, Type actorControlType, NetworkParser netHelper) {
            this.headerType = headerType;
            this.actorControlType = actorControlType;
            fieldCastSourceId = headerType.GetField("ActorID");
            fieldCategory = actorControlType.GetField("category");
            fieldParam1 = actorControlType.GetField("param1");
            fieldParam2 = actorControlType.GetField("param2");
            fieldParam3 = actorControlType.GetField("param3");
            fieldParam4 = actorControlType.GetField("param4");
            packetOpcode = netHelper.GetOpcode("ActorControl");
            packetSize = Marshal.SizeOf(actorControlType);
            offsetMessageType = netHelper.GetOffset(headerType, "MessageType");
        }
    }

    private RegionalizedInfo regionalized;

    private readonly Func<string, DateTime, bool> logWriter;
    private readonly NetworkParser netHelper;

    public LineActorControlExtra(TinyIoCContainer container) {
        logger = container.Resolve<ILogger>();
        ffxiv = container.Resolve<FFXIVRepository>();
        netHelper = container.Resolve<NetworkParser>();
        ffxiv.RegisterNetworkParser(MessageReceived);
        ffxiv.RegisterProcessChangedHandler(ProcessChanged);

        var customLogLines = container.Resolve<FFXIVCustomLogLines>();
        logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry {
            Name = "ActorControlExtra",
            Source = "OverlayPlugin",
            ID = LogFileLineID,
            Version = 1
        });
    }

    private void ProcessChanged(Process process) {
        var region = ffxiv.GetMachinaRegion();
        if (!ffxiv.IsFFXIVPluginPresent())
            return;
        try {
            var mach = Assembly.Load("Machina.FFXIV");
            var headerType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
            string actorControlTypeStr;
            switch (region) {
                case GameRegion.Global: {
                    actorControlTypeStr = "Machina.FFXIV.Headers.Server_ActorControl";
                    break;
                }
                case GameRegion.Korean: {
                    actorControlTypeStr = "Machina.FFXIV.Headers.Korean.Server_ActorControl";
                    break;
                }
                case GameRegion.Chinese: {
                    actorControlTypeStr = "Machina.FFXIV.Headers.Chinese.Server_ActorControl";
                    break;
                }
                default: {
                    return;
                }
            }

            var actorControlType = mach.GetType(actorControlTypeStr);
            var info = new RegionalizedInfo(headerType, actorControlType, netHelper);
            regionalized = info;
        }
        catch (FileNotFoundException) {
            logger.Log(LogLevel.Error, Resources.NetworkParserNoFfxiv);
        }
        catch (Exception e) {
            logger.Log(LogLevel.Error, Resources.NetworkParserInitException, e);
        }
    }

    private unsafe void MessageReceived(string id, long epoch, byte[] message) {
        var info = regionalized;
        if (info == null)
            return;

        if (message.Length < info.packetSize)
            return;

        fixed (byte* buffer = message) {
            if (*(ushort*)&buffer[info.offsetMessageType] == info.packetOpcode) {
                var header = Marshal.PtrToStructure(new IntPtr(buffer), info.headerType);
                var sourceId = (uint)info.fieldCastSourceId.GetValue(header);

                var packet = Marshal.PtrToStructure(new IntPtr(buffer), info.actorControlType);
                var category = (Server_ActorControlCategory)info.fieldCategory.GetValue(packet);

                if (AllowedActorControlCategories.Contains(category)) {
                    var param1 = (uint)info.fieldParam1.GetValue(packet);
                    var param2 = (uint)info.fieldParam2.GetValue(packet);
                    var param3 = (uint)info.fieldParam3.GetValue(packet);
                    var param4 = (uint)info.fieldParam4.GetValue(packet);

                    var line = string.Format(CultureInfo.InvariantCulture,
                        "{0:X8}|{1:X4}|{2:X}|{3:X}|{4:X}|{5:X}",
                        sourceId, (ushort)category, param1, param2, param3, param4);

                    var serverTime = ffxiv.EpochToDateTime(epoch);
                    logWriter(line, serverTime);
                }
            }
        }
    }
}