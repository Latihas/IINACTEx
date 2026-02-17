using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SilverDasher.ACT.Enums;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT.Models;

public class Config : UIBinded
{
	[JsonIgnore]
	public readonly string VersionString = string.Join(".", BitConverter.GetBytes(DataStorage.Version));

	[JsonProperty("TTSNotifications")]
	public Dictionary<HuntState, bool> TTSNotifications = new Dictionary<HuntState, bool>
	{
		{
			HuntState.Healthy,
			true
		},
		{
			HuntState.Taunted,
			true
		},
		{
			HuntState.Dying,
			true
		},
		{
			HuntState.Died,
			true
		}
	};

	private string toastType = "UWP";

	[JsonProperty("ToastNotifications")]
	public Dictionary<HuntState, bool> ToastNotifications = new Dictionary<HuntState, bool>
	{
		{
			HuntState.Healthy,
			true
		},
		{
			HuntState.Taunted,
			true
		},
		{
			HuntState.Dying,
			true
		},
		{
			HuntState.Died,
			true
		}
	};

	[JsonProperty("CWHunts")]
	public Dictionary<Rank, bool> CWHunts = new Dictionary<Rank, bool>
	{
		{
			Rank.SS,
			true
		},
		{
			Rank.SSMinion,
			true
		},
		{
			Rank.S,
			true
		},
		{
			Rank.A,
			false
		},
		{
			Rank.B,
			false
		}
	};

	[JsonIgnore]
	public Dictionary<Rank, bool> CDCHunts = new Dictionary<Rank, bool>
	{
		{
			Rank.SS,
			false
		},
		{
			Rank.SSMinion,
			false
		},
		{
			Rank.S,
			false
		},
		{
			Rank.A,
			false
		},
		{
			Rank.B,
			false
		}
	};

	[JsonProperty("CWFates")]
	public Dictionary<string, bool> CWFates = new Dictionary<string, bool>
	{
		{ "common", false },
		{ "special", true }
	};

	[JsonProperty("hunts")]
	public List<int> HuntSubscriptions = new List<int>();

	[JsonProperty("fates")]
	public List<int> FateSubscriptions = new List<int>();

	[JsonProperty("extend")]
	public bool ExtendedReport { get; set; }

	[JsonProperty("narrow")]
	public bool CurrentWorldOnly { get; set; }

	[JsonProperty("PauseInDuty")]
	public bool PauseInDuty { get; set; }

	[JsonProperty("tts")]
	public bool TTS { get; set; }

	[JsonProperty("TTSExtend")]
	public bool TTSExtend { get; set; }

	[JsonIgnore]
	public bool NotifySpottedTTS
	{
		get
		{
			return TTSNotifications[HuntState.Healthy];
		}
		set
		{
			TTSNotifications[HuntState.Healthy] = value;
		}
	}

	[JsonIgnore]
	public bool NotifyTauntedTTS
	{
		get
		{
			return TTSNotifications[HuntState.Taunted];
		}
		set
		{
			TTSNotifications[HuntState.Taunted] = value;
		}
	}

	[JsonIgnore]
	public bool NotifyBullyingTTS
	{
		get
		{
			return TTSNotifications[HuntState.Dying];
		}
		set
		{
			TTSNotifications[HuntState.Dying] = value;
		}
	}

	[JsonIgnore]
	public bool NotifyDiedTTS
	{
		get
		{
			return TTSNotifications[HuntState.Died];
		}
		set
		{
			TTSNotifications[HuntState.Died] = value;
		}
	}

	[JsonProperty("toast")]
	public bool SystemToast { get; set; }

	[JsonProperty("toastType")]
	public string ToastType
	{
		get
		{
			return toastType;
		}
		set
		{
			toastType = value;
		}
	}

	public bool IsToastTypeUWP => "UWP" == toastType;

	public bool IsToastTypeLegacy => "Legacy" == toastType;

	[JsonIgnore]
	public bool NotifySpottedToast
	{
		get
		{
			return ToastNotifications[HuntState.Healthy];
		}
		set
		{
			ToastNotifications[HuntState.Healthy] = value;
		}
	}

	[JsonIgnore]
	public bool NotifyTauntedToast
	{
		get
		{
			return ToastNotifications[HuntState.Taunted];
		}
		set
		{
			ToastNotifications[HuntState.Taunted] = value;
		}
	}

	[JsonIgnore]
	public bool NotifyBullyingToast
	{
		get
		{
			return ToastNotifications[HuntState.Dying];
		}
		set
		{
			ToastNotifications[HuntState.Dying] = value;
		}
	}

	[JsonIgnore]
	public bool NotifyDiedToast
	{
		get
		{
			return ToastNotifications[HuntState.Died];
		}
		set
		{
			ToastNotifications[HuntState.Died] = value;
		}
	}

	[JsonProperty("CrossworldHunt")]
	public bool CrossWorldHunt { get; set; }

	[JsonIgnore]
	public bool CWHuntSS
	{
		get
		{
			return CWHunts[Rank.SS];
		}
		set
		{
			CWHunts[Rank.SS] = value;
			CWHunts[Rank.SSMinion] = value;
		}
	}

	[JsonIgnore]
	public bool CWHuntS
	{
		get
		{
			return CWHunts[Rank.S];
		}
		set
		{
			CWHunts[Rank.S] = value;
		}
	}

	[JsonIgnore]
	public bool CWHuntA
	{
		get
		{
			return CWHunts[Rank.A];
		}
		set
		{
			CWHunts[Rank.A] = value;
		}
	}

	[JsonIgnore]
	public bool CWHuntB
	{
		get
		{
			return CWHunts[Rank.B];
		}
		set
		{
			CWHunts[Rank.B] = value;
		}
	}

	[JsonProperty("CrossDCHunt")]
	public bool CrossDCHunt { get; set; }

	[JsonIgnore]
	public bool CDCHuntSS
	{
		get
		{
			return CDCHunts[Rank.SS];
		}
		set
		{
			CDCHunts[Rank.SS] = value;
			CDCHunts[Rank.SSMinion] = value;
		}
	}

	[JsonIgnore]
	public bool CDCHuntS
	{
		get
		{
			return CDCHunts[Rank.S];
		}
		set
		{
			CDCHunts[Rank.S] = value;
		}
	}

	[JsonIgnore]
	public bool CDCHuntA
	{
		get
		{
			return CDCHunts[Rank.A];
		}
		set
		{
			CDCHunts[Rank.A] = value;
		}
	}

	[JsonIgnore]
	public bool CDCHuntB
	{
		get
		{
			return CDCHunts[Rank.B];
		}
		set
		{
			CDCHunts[Rank.B] = value;
		}
	}

	[JsonProperty("CrossworldFate")]
	public bool CrossWorldFate { get; set; }

	[JsonIgnore]
	public bool CWFateCommon
	{
		get
		{
			return CWFates["common"];
		}
		set
		{
			CWFates["common"] = value;
		}
	}

	[JsonIgnore]
	public bool CWFateSpecial
	{
		get
		{
			return CWFates["special"];
		}
		set
		{
			CWFates["special"] = value;
		}
	}

	[JsonProperty("ServerSelection")]
	public int ServerSelection { get; set; }

	[JsonProperty("UseExtraServer")]
	public bool UseExtraServer { get; set; }

	[JsonProperty("ExtraServerAddress")]
	public string ExtraServer { get; set; }

	[JsonProperty("WriteActLog")]
	public bool WriteACTLog { get; set; }

	public static Config GetDefaultConfig()
	{
		return new Config
		{
			ExtendedReport = false,
			CurrentWorldOnly = false,
			PauseInDuty = true,
			TTS = true,
			TTSExtend = true,
			TTSNotifications = new Dictionary<HuntState, bool>
			{
				{
					HuntState.Healthy,
					true
				},
				{
					HuntState.Taunted,
					false
				},
				{
					HuntState.Dying,
					false
				},
				{
					HuntState.Died,
					true
				}
			},
			SystemToast = true,
			ToastType = "UWP",
			ToastNotifications = new Dictionary<HuntState, bool>
			{
				{
					HuntState.Healthy,
					true
				},
				{
					HuntState.Taunted,
					false
				},
				{
					HuntState.Dying,
					false
				},
				{
					HuntState.Died,
					true
				}
			},
			CrossWorldHunt = true,
			CWHunts = new Dictionary<Rank, bool>
			{
				{
					Rank.SS,
					true
				},
				{
					Rank.SSMinion,
					true
				},
				{
					Rank.S,
					true
				},
				{
					Rank.A,
					true
				},
				{
					Rank.B,
					true
				}
			},
			CrossDCHunt = false,
			CDCHunts = new Dictionary<Rank, bool>
			{
				{
					Rank.SS,
					false
				},
				{
					Rank.SSMinion,
					false
				},
				{
					Rank.S,
					false
				},
				{
					Rank.A,
					false
				},
				{
					Rank.B,
					false
				}
			},
			CrossWorldFate = true,
			CWFates = new Dictionary<string, bool>
			{
				{ "common", false },
				{ "special", true }
			},
			UseExtraServer = false,
			ExtraServer = "",
			ServerSelection = 0,
			WriteACTLog = false
		};
	}

	public bool StatusPushable(string type, HuntState state)
	{
		if (type.Contains("TTS"))
		{
			return TTSNotifications[state];
		}
		return ToastNotifications[state];
	}

	private List<int> GetListByType(string type)
	{
		if (type.Contains("fate"))
		{
			return FateSubscriptions;
		}
		return HuntSubscriptions;
	}

	public void EditSubscription(bool? edit, string type, string id, List<string> ids)
	{
		if (edit.GetValueOrDefault())
		{
			if (ids != null)
			{
				foreach (string id2 in ids)
				{
					GetListByType(type).Add(int.Parse(id2));
				}
				return;
			}
			GetListByType(type).Add(int.Parse(id));
			return;
		}
		if (ids != null)
		{
			foreach (string id3 in ids)
			{
				GetListByType(type).Remove(int.Parse(id3));
			}
			return;
		}
		GetListByType(type).Remove(int.Parse(id));
	}

	public static Config Load()
	{
		if (!File.Exists(DataStorage.ConfigPath))
		{
			Config defaultConfig = GetDefaultConfig();
			Save(defaultConfig);
			return defaultConfig;
		}
		try
		{
			Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(DataStorage.ConfigPath));
			if (config.HuntSubscriptions == null)
			{
				config.HuntSubscriptions = new List<int>();
			}
			HashSet<int> source = new HashSet<int>(config.HuntSubscriptions);
			HashSet<int> source2 = new HashSet<int>(config.FateSubscriptions);
			config.HuntSubscriptions = source.ToList();
			config.FateSubscriptions = source2.ToList();
			return config;
		}
		catch
		{
			Utils.ShowMessageBox("读取配置文件时发生问题。将使用默认设置，订阅信息已清空，请重新订阅。");
			Config defaultConfig2 = GetDefaultConfig();
			Save(defaultConfig2);
			return defaultConfig2;
		}
	}

	public static void Save(Config c)
	{
		Directory.CreateDirectory(DataStorage.BasePath);
		string contents = JsonConvert.SerializeObject(c);
		try
		{
			File.WriteAllText(DataStorage.ConfigPath, contents);
		}
		catch (IOException)
		{
			Utils.ShowMessageBox("保存配置文件时发生问题！你的设置未能保存。");
		}
	}
}
