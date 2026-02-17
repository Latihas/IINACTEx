// SilverDasher.Loader.Loader

using System.IO;
using System.Runtime.InteropServices;
using Advanced_Combat_Tracker;
using IINACT;

// ReSharper disable once CheckNamespace
namespace SilverDasher.Loader;

public class Loader : IActPluginV1 {
    // public Assembly MQTT;
    //
    // public Assembly Newton;//
    // public Assembly SilverDasher;
    //
    // public Assembly SDManagedZodiark;

    public dynamic RealPlugin;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    private IntPtr Weaver;

    public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
        // var text = Path.Combine(Plugin.Instance.PluginAssemblyDirectory, "SilverDasher", "libs");
        // Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Plugin.Instance.PluginAssemblyDirectory);
        // MQTT = Load(Path.Combine(text, "MQTTnet.dll"));
        // Newton = Load(Path.Combine(text, "Newtonsoft.Json.dll"));
        // SilverDasher = Load(Path.Combine(text, "SilverDasher.Core.dll"));
        // SDManagedZodiark = Load(Path.Combine(text, "SilverDasher.ManagedZodiark.dll"));
        // RealPlugin = SilverDasher.CreateInstance("SilverDasher.ACT.SilverDasher")!;
        Weaver = LoadLibrary(Path.Combine(Plugin.Instance.PluginAssemblyDirectory, "SilverDasher.Weaver.dll"));
        RealPlugin = new SilverDasher.ACT.SilverDasher();
        RealPlugin.InitPlugin(pluginScreenSpace, pluginStatusText);
    }

    public void DeInitPlugin() {
        RealPlugin.DeInitPlugin();
        try {
            FreeLibrary(Weaver);
        }
        catch {
            Plugin.Log.Error("Probably Leaked Weaver Dll");
        }

        // RealPlugin.Dispose();
        // MQTT = null;
        // Newton = null;
        // SilverDasher = null;
        // SDManagedZodiark = null;
    }

    // public Assembly Load(string path) {
    //     return Assembly.Load(File.ReadAllBytes(path));
    // }
}