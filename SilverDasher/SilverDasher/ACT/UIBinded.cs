using System.ComponentModel;

namespace SilverDasher.ACT;

public class UIBinded : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged(string info)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
	}
}
