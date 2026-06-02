using System.Collections.Generic;
using System.Linq;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class WorldStorage(Keeper kp) : BaseStorage<World>(kp) {
	private Dictionary<int, World> WorldByID = new();
	private readonly Dictionary<string, World> WorldByLabel = new();
	internal override string? ResourceFileName => null;
	internal override World Get(int id) => WorldByID[id];
	internal World GetByLabel(string label) => WorldByLabel[label];
	internal bool TryGetByLabel(string label, out World? w) => WorldByLabel.TryGetValue(label, out w);
	internal override bool Contains(int id) => WorldByID.ContainsKey(id);

	internal override void Load() {
		WorldByID = SilverDasher.DataManager.GetExcelSheet<Lumina.Excel.Sheets.World>()
			.Where(i => i.DataCenter.RowId is 101 or 102 or 103 or 104)
			.ToDictionary(i => (int)i.RowId, i => {
				return new World {
					Name = i.Name.ToString(),
					Label = i.InternalName.ToString(),
					DataCenter = i.DataCenter.Value.Name.ToString(),
					DataCenterLabel = i.DataCenter.RowId switch {
						101 => "LuXingNiao",
						102 => "MoGuLi",
						103 => "MaoXiaoPang",
						104 => "DouDouChai",
						_ => ""
					}
				};
			});
		foreach (var item in WorldByID.Keys) {
			var world = Get(item);
			world.Id = item;
			WorldByLabel[world.Label] = world;
		}
	}
}