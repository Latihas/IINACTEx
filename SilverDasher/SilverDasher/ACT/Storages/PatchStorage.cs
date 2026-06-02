using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class PatchStorage(Keeper kp) : BaseStorage<Patch>(kp) {
	private readonly List<Patch> _patches = [
		new() {
			Code = 2,
			Name = new LocalizedEntry { Chinese = "重生之境（2.0）" }
		},
		new() {
			Code = 3,
			Name = new LocalizedEntry { Chinese = "苍穹之禁城（3.0）" }
		},
		new() {
			Code = 4,
			Name = new LocalizedEntry { Chinese = "红莲之狂潮（4.0）" }
		},
		new() {
			Code = 5,
			Name = new LocalizedEntry { Chinese = "暗影之逆焰（5.0）" }
		},
		new() {
			Code = 6,
			Name = new LocalizedEntry { Chinese = "晓月之终途（6.0）" }
		},
		new() {
			Code = 7,
			Name = new LocalizedEntry { Chinese = "金曦之遗辉（7.0）" }
		},
		new() {
			Code = 8,
			Name = new LocalizedEntry { Chinese = "银海之天舟（8.0）" }
		},
		new() {
			Code = 99,
			Name = new LocalizedEntry { Chinese = "未知版本" }
		}
	];

	public readonly Dictionary<int, Patch> PatchByCode = new();

	internal override string? ResourceFileName => null;

	internal override Patch Get(int k) => PatchByCode[k];

	internal override bool Contains(int id) => PatchByCode.ContainsKey(id);

	internal override void Load() {
		foreach (var patch in _patches) PatchByCode[patch.Code] = patch;
	}
}