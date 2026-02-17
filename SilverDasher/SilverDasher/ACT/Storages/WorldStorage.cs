using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class WorldStorage : BaseStorage<World>
{
	private Dictionary<int, World> WorldByID = new Dictionary<int, World>();

	private Dictionary<string, World> WorldByLabel = new Dictionary<string, World>();

	internal override string ResourceFileName => "worlds.json";

	internal WorldStorage(Keeper kp)
		: base(kp)
	{
	}

	internal override World Get(int id)
	{
		return WorldByID[id];
	}

	internal World GetByLabel(string label)
	{
		return WorldByLabel[label];
	}

	internal bool TryGetByLabel(string label, out World w)
	{
		return WorldByLabel.TryGetValue(label, out w);
	}

	internal override IEnumerable<int> Keys()
	{
		return WorldByID.Keys;
	}

	internal override bool Contains(int id)
	{
		return WorldByID.ContainsKey(id);
	}

	internal override void Load()
	{
		LoadData<Dictionary<int, World>>(ResourceFileName, out WorldByID);
		foreach (int item in Keys())
		{
			World world = Get(item);
			world.Id = item;
			WorldByLabel[world.Label] = world;
		}
	}
}
