using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SilverDasher.ACT.Views;

public class CheckTreeNode : UIBinded
{
	internal static bool BUILDING;

	public CheckTreeNode Parent;

	public List<CheckTreeNode> Related = [];

	public bool changing;

	public bool? isChecked = false;

	public string Name { get; set; }

	public string ID { get; set; }

	public ObservableCollection<CheckTreeNode> Nodes { get; set; }

	public bool? ViewChecked
	{
		get
		{
			return isChecked;
		}
		set
		{
			if (isChecked == value)
			{
				return;
			}
			isChecked = value;
			foreach (CheckTreeNode item in Related)
			{
				item.ViewChecked = value;
			}
			NotifyPropertyChanged("ViewChecked");
			if (Nodes != null && Nodes.Count > 0 && isChecked.HasValue)
			{
				changing = true;
				foreach (CheckTreeNode node in Nodes)
				{
					node.ViewChecked = isChecked;
				}
				changing = false;
			}
			if (Parent != null && !Parent.changing)
			{
				Parent.ValidateStatus();
			}
		}
	}

	public CheckTreeNode(string name, string id, bool deleteOnUncheck = false)
	{
		Name = name;
		ID = id;
		Nodes = [];
	}

	internal void Add(CheckTreeNode node)
	{
		if (!Nodes.Contains(node) && Parent != node)
		{
			node.Parent = this;
			Nodes.Add(node);
			if (!BUILDING && Parent != null)
			{
				Parent.ValidateStatus();
			}
			NotifyPropertyChanged("Nodes");
		}
	}

	internal void Add(string name, string id, bool c)
	{
		CheckTreeNode node = new CheckTreeNode(name, id)
		{
			isChecked = c
		};
		Add(node);
	}

	internal void ValidateStatus()
	{
		int num = 0;
		int num2 = 0;
		foreach (CheckTreeNode node in Nodes)
		{
			if (node.ViewChecked == true)
			{
				num++;
			}
			else if (node.ViewChecked == false)
			{
				num2++;
			}
		}
		if (num == Nodes.Count)
		{
			ViewChecked = true;
		}
		else if (num2 == Nodes.Count)
		{
			ViewChecked = false;
		}
		else
		{
			ViewChecked = null;
		}
	}
}
