using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.EventSources
{
    [Serializable]
    public class BuiltinEventConfig
    {
        public event EventHandler UpdateIntervalChanged;
        public event EventHandler EnmityIntervalChanged;
        public event EventHandler SortKeyChanged;
        public event EventHandler SortDescChanged;
        public event EventHandler UpdateDpsDuringImportChanged;
        public event EventHandler EndEncounterAfterWipeChanged;
        public event EventHandler EndEncounterOutOfCombatChanged;
        public event EventHandler LogLinesChanged;

        private int updateInterval;

        public int UpdateInterval
        {
            get => this.updateInterval;
            set
            {
                if (this.updateInterval == value) return;
                this.updateInterval = value;
                UpdateIntervalChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private int enmityIntervalMs;

        public int EnmityIntervalMs
        {
            get => this.enmityIntervalMs;
            set
            {
                if (this.enmityIntervalMs == value) return;
                this.enmityIntervalMs = value;
                EnmityIntervalChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private string sortKey;

        public string SortKey
        {
            get => this.sortKey;
            set
            {
                if (this.sortKey == value) return;
                this.sortKey = value;
                SortKeyChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool sortDesc;

        public bool SortDesc
        {
            get => this.sortDesc;
            set
            {
                if (this.sortDesc == value) return;
                this.sortDesc = value;
                SortDescChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool updateDpsDuringImport;

        public bool UpdateDpsDuringImport
        {
            get => this.updateDpsDuringImport;
            set
            {
                if (this.updateDpsDuringImport == value) return;
                this.updateDpsDuringImport = value;
                UpdateDpsDuringImportChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool endEncounterAfterWipe;

        public bool EndEncounterAfterWipe
        {
            get => this.endEncounterAfterWipe;
            set
            {
                if (this.endEncounterAfterWipe == value) return;
                this.endEncounterAfterWipe = value;
                EndEncounterAfterWipeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool endEncounterOutOfCombat;

        public bool EndEncounterOutOfCombat
        {
            get => this.endEncounterOutOfCombat;
            set
            {
                if (this.endEncounterOutOfCombat == value) return;
                this.endEncounterOutOfCombat = value;
                EndEncounterOutOfCombatChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool _logLines;

        public bool LogLines
        {
            get => _logLines;
            set
            {
                if (this._logLines == value) return;
                this._logLines = value;
                LogLinesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Data that overlays can save/load via event handlers.
        public Dictionary<string, JToken> OverlayData = new Dictionary<string, JToken>();

        public BuiltinEventConfig()
        {
            this.updateInterval = 1;
            this.enmityIntervalMs = 100;
            this.sortKey = "encdps";
            this.sortDesc = true;
            this.updateDpsDuringImport = false;
            this.endEncounterAfterWipe = true;
            this.endEncounterOutOfCombat = true;
            this._logLines = false;
        }

        public static BuiltinEventConfig LoadConfig(IPluginConfig config)
        {
            var result = new BuiltinEventConfig();

            if (!config.EventSourceConfigs.ContainsKey("MiniParse")) return result;
            var obj = config.EventSourceConfigs["MiniParse"];

            if (obj.TryGetValue("UpdateInterval", out var value))
            {
                result.updateInterval = value.ToObject<int>();
            }

            if (obj.TryGetValue("EnmityIntervalMs", out value))
            {
                result.enmityIntervalMs = value.ToObject<int>();
            }

            if (obj.TryGetValue("SortKey", out value))
            {
                result.sortKey = value.ToString();
            }

            if (obj.TryGetValue("SortDesc", out value))
            {
                result.sortDesc = value.ToObject<bool>();
            }

            if (obj.TryGetValue("UpdateDpsDuringImport", out value))
            {
                result.updateDpsDuringImport = value.ToObject<bool>();
            }

            if (obj.TryGetValue("EndEncounterAfterWipe", out value))
            {
                result.endEncounterAfterWipe = value.ToObject<bool>();
            }

            if (obj.TryGetValue("EndEncounterOutOfCombat", out value))
            {
                result.endEncounterOutOfCombat = value.ToObject<bool>();
            }

            if (obj.TryGetValue("OverlayData", out value))
            {
                result.OverlayData = value.ToObject<Dictionary<string, JToken>>();
            }

            if (obj.TryGetValue("LogLines", out value))
            {
                result._logLines = value.ToObject<bool>();
            }

            return result;
        }

        public void SaveConfig(IPluginConfig Config)
        {
            var newObj = JObject.FromObject(this);
            if (Config.EventSourceConfigs.ContainsKey("MiniParse") &&
                JToken.DeepEquals(Config.EventSourceConfigs["MiniParse"], newObj)) return;
            Config.EventSourceConfigs["MiniParse"] = newObj;
            Config.MarkDirty();
        }
    }

    public enum MiniParseSortType
    {
        None,
        StringAscending,
        StringDescending,
        NumericAscending,
        NumericDescending
    }
}
