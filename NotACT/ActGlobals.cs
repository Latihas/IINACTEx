namespace Advanced_Combat_Tracker;

public static partial class ActGlobals {
	public static readonly bool mainTableShowCommas = true;
	public static readonly bool calcRealAvgDelay = true;
	// internal static SortedDictionary<string, bool> selectiveList;
	public static readonly bool longDuration = false;
	public static readonly bool blockIsHit = true;
	public static readonly bool restrictToAll = false;
	public static readonly string charName = "YOU";
	public static readonly string eDSort = "EncDPS";
	public static readonly string mDSort = "Damage";
	public static readonly string aTSort = "Time";
	public static readonly string eDSort2 = "EncDPS";
	public static readonly string mDSort2 = "Damage";
	public static readonly string aTSort2 = "Time";
	public static FormActMain oFormActMain = null!;
	internal static object ActionDataLock;
	public static ActLocalization.LocalizationStringsHelper Trans { get; private set; }

	public static void Init() {
		Trans = new ActLocalization.LocalizationStringsHelper();
		// selectiveList = new SortedDictionary<string, bool>();
		ActionDataLock = new object();
	}

	public static void Dispose() {
		oFormActMain.RemoveFrameworkUpdates();
		oFormActMain.Dispose();
		// selectiveList.Clear();
	}
}