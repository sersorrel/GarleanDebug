using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;

namespace GarleanDebug.Windows;

public class MainWindow: Window, IDisposable {
    private readonly Plugin Plugin;

    public MainWindow(Plugin plugin): base(
        "My Amazing Window",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
    ) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.Plugin = plugin;
    }

    public void Dispose() {
    }

    public override void Draw() {
        ImGui.Text($"The random config bool is {this.Plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        if (ImGui.Button("Show Settings")) {
            this.Plugin.DrawConfigUI();
        }
    }
}
