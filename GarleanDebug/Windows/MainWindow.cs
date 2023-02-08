using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace GarleanDebug.Windows;

public sealed class MainWindow: Window, IDisposable {
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin): base(
        "Garlean Debugger",
        ImGuiWindowFlags.AlwaysAutoResize
    ) {
        this.plugin = plugin;
    }

    public void Dispose() {}

    private nint baseAddr = 0;
    private int stride = 16;
    private int lines = 10;

    public override void Draw() {
        ImGui.PushFont(Dalamud.Interface.UiBuilder.MonoFont);
        if (ImGui.BeginTable("memory", this.stride + 1)) {
            for (var line = 0; line < lines; line++) {
                var addr = this.baseAddr + (line * stride);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"0x{addr:x8}");
                for (var i = 0; i < stride; i++) {
                    ImGui.TableSetColumnIndex(i + 1);
                    unsafe {
                        var result = "??";
                        try {
                            var val = *(byte*)(addr + i);
                            result = $"{val:x2}";
                        } catch (NullReferenceException) {
                            result = "--";
                        } catch (AccessViolationException) {
                            result = "--";
                        } finally {
                            ImGui.Text(result);
                        }
                    }
                }
            }
            ImGui.EndTable();
        }
        ImGui.PopFont();

        ImGui.Separator();

        if (ImGui.Button("Show Settings")) {
            this.plugin.OpenConfigUi();
        }
    }
}
