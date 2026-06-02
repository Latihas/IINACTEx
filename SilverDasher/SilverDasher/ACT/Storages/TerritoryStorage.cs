using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class TerritoryStorage : BaseStorage<Territory> {
	internal static TerritoryStorage Instance;
	private Dictionary<int, Territory> TerritoryById = new();
	internal override string? ResourceFileName => null;

	internal TerritoryStorage(Keeper kp) : base(kp) {
		Instance = this;
	}

	internal override bool Contains(int id) => TerritoryById.ContainsKey(id);
	internal bool Contains(uint id) => TerritoryById.ContainsKey((int)id);
	internal override Territory Get(int id) => TerritoryById[id];

	internal override void Load() {
		TerritoryById = SilverDasher.DataManager.GetExcelSheet<TerritoryType>()
			.ToDictionary(i => (int)i.RowId, i => {
				var dcmap = i.TerritoryIntendedUse.RowId is 41 or 48 or 61;
				return new Territory {
					Name = i.PlaceNameRegion.Value.Name.ToString(),
					Region = i.PlaceName.Value.Name.ToString(),
					Content = dcmap ? 0 : (int)i.ContentFinderCondition.RowId,
					IsDataCenterMap = dcmap
				};
			});
	}
}