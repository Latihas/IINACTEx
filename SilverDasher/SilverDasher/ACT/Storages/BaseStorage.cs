using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SilverDasher.ACT.Doppelgangers;

namespace SilverDasher.ACT.Storages;

internal abstract class BaseStorage
{
	internal Keeper Keeper;

	internal int Version;

	internal abstract string ResourceFileName { get; }

	internal BaseStorage(Keeper keeper)
	{
		Keeper = keeper;
	}

	internal abstract void Load();

	internal void LoadData<DataType>(string path, out DataType data) where DataType : new()
	{
		try
		{
			JObject jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(Path.Combine(Utils.GetPluginDirectory(), "data", path)));
			Version = ((dynamic)jObject).version;
			data = jObject["data"]!.ToObject<DataType>();
		}
		catch (Exception)
		{
			Utils.ShowMessageBox("加载本地数据文件失败，本地数据文件很可能已损坏。读取了空数据，稍后尝试更新。如失败请重装插件，后续无报错可正常使用。");
			data = new DataType();
			Version = 10000000;
		}
	}

	internal void Update()
	{
	}
}
internal abstract class BaseStorage<T, K> : BaseStorage
{
	internal BaseStorage(Keeper kp)
		: base(kp)
	{
	}

	internal abstract T Get(K id);

	internal abstract IEnumerable<K> Keys();

	internal abstract bool Contains(K id);

	internal bool TryGet(K id, out T item)
	{
		if (Contains(id))
		{
			item = Get(id);
			return true;
		}
		item = default(T);
		return false;
	}
}
internal abstract class BaseStorage<T> : BaseStorage<T, int>
{
	internal BaseStorage(Keeper kp)
		: base(kp)
	{
	}

	internal T Get(uint id)
	{
		return Get((int)id);
	}
}
