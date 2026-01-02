using System;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Triggernometry.Core;

public class TestPresets : IActPluginV1 {
    public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
        RealPlugin.Instance.InvokeNamedCallback(
            "preset", "{\n  \"Name\": \"Slot5\",\n  \"MapID\": 676,\n  \"A\": {\n    \"X\": -171,\n    \"Y\": 0,\n    \"Z\": 465,\n    \"Active\": true\n  },\n  \"C\": {\n    \"X\": -171,\n    \"Y\": 0,\n    \"Z\": 471,\n    \"Active\": true\n  }\n}");
    }

    public void DeInitPlugin() {
        throw new NotImplementedException();
    }
}