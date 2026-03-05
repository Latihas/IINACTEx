namespace SilverDasher.ACT.Doppelgangers;

internal class Primal(SilverDasher self) : Doppelganger(self) {
    internal override void Init() {
    }

    internal override void Deinit() {
    }

    internal uint GetCurrentInstance() {
        var b = SilverDasher.ClientState.Instance;
        // Debug($"Scanned instance {b}");
        return b;
    }
}