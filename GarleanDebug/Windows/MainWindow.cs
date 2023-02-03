using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace GarleanDebug.Windows;

public sealed class MainWindow: Window, IDisposable {
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin): base(
        "Garlean Debugger",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
    ) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.plugin = plugin;
    }

    public void Dispose() {}

    public override void Draw() {
        ImGui.Text($"The random config bool is {this.plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        if (ImGui.Button("Show Settings")) {
            this.plugin.DrawConfigUi();
        }
    }
}
