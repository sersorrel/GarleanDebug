using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace GarleanDebug;

[Serializable]
public class Configuration: IPluginConfiguration {
    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private DalamudPluginInterface? PluginInterface;

    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    public int Version { get; set; } = 0;

    public void Initialize(DalamudPluginInterface pluginInterface) {
        this.PluginInterface = pluginInterface;
    }

    public void Save() {
        this.PluginInterface!.SavePluginConfig(this);
    }
}
