namespace SilverDasher.ACT.Doppelgangers;

internal class Primal(SilverDasher self) : Doppelganger(self) {
    // public static IntPtr Handle { get; private set; }

    // public IntPtr Instance
    // {
    //     get
    //     {
    //         if (instance.HasValue) {
    //             return instance.Value;
    //         }
    //         _ = IntPtr.Zero;
    //         instance = zame.Scanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 40 3A C7 75 ?? 48 8D 0D", 2) + 32;
    //         var intPtr = instance;
    //         var zero = IntPtr.Zero;
    //         if (!intPtr.HasValue) {
    //             _ = 1;
    //         }
    //         else if (intPtr.HasValue) {
    //             if (intPtr.GetValueOrDefault() != zero) {
    //                 _ = 1;
    //             }
    //             else
    //                 _ = 0;
    //         }
    //         else
    //             _ = 0;
    //         return instance.Value;
    //     }
    // }

    internal override void Init() {
        // ChangeProcess(Negotiator.ffdata.GetCurrentFFXIVProcess());
    }

    internal override void Deinit() {
    }

    // private static void ChangeProcess(Process game) {
    //     if (game == null || game.HasExited) {
    //     }
    //     else {
    //         new ZodiarkProcess(game);
    //     }
    // }

    internal uint GetCurrentInstance() {
        var b = SilverDasher.ClientState.Instance;
        Debug($"Scanned instance {b}");
        return b;
    }
}