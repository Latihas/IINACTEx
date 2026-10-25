namespace SilverDasher.ACT.Doppelgangers;

internal class Primal(SilverDasher self) : Doppelganger(self) {
	internal override void Init() {
	}

	internal override void Deinit() {
	}

	internal static uint GetCurrentInstance() => SilverDasher.ClientState.Instance;
}