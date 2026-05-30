namespace Advanced_Combat_Tracker;

public class LocalizationObject(string displayedText, string localizationDescription) {
	public string DisplayedText { get; set; } = displayedText;
	public string LocalizationDescription { get; } = localizationDescription;
	internal string S => DisplayedText;
	public override string ToString() => DisplayedText;
	public static implicit operator string(LocalizationObject val) => val.DisplayedText;
	public static implicit operator LocalizationObject(string val) => new(val, string.Empty);
}