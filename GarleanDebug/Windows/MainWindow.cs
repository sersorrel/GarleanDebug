using System;
using System.Globalization;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace GarleanDebug.Windows;

internal class MemoryViewConfig {
    private int height = 10;
    private nint startAddress;
    private int width = 16;

    public required nint StartAddress {
        get => this.startAddress;
        set => this.startAddress = nint.Clamp(value, 0, nint.MaxValue - (this.width * this.height));
    }

    public int Width {
        get => this.width;
        set => this.width = int.Clamp(value, 1, int.MaxValue / this.Height);
    }

    public int Height {
        get => this.height;
        set => this.height = int.Clamp(value, 1, int.MaxValue / this.Width);
    }
}

public sealed class MainWindow: Window, IDisposable {
    private readonly MemoryViewConfig memoryViewConfig;
    private readonly Plugin plugin;

    private string nextMemoryAddr = string.Empty;

    public MainWindow(Plugin plugin): base(
        "Garlean Debugger",
        ImGuiWindowFlags.AlwaysAutoResize
    ) {
        this.plugin = plugin;
        this.memoryViewConfig = new MemoryViewConfig { StartAddress = 0 };
    }

    public void Dispose() {}

    public override void Draw() {
        ImGui.PushFont(UiBuilder.MonoFont);
        ImGui.BeginGroup();
        if (ImGui.BeginTable("memory", this.memoryViewConfig.Width + 2)) {
            for (var line = 0; line < this.memoryViewConfig.Height; line++) {
                var addr = this.memoryViewConfig.StartAddress + (line * this.memoryViewConfig.Width);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"0x{addr:x8}");
                for (var i = 0; i < this.memoryViewConfig.Width; i++) {
                    ImGui.TableSetColumnIndex(i + 1);
                    unsafe {
                        var result = "??";
                        try {
                            var val = *(byte*)(addr + i);
                            result = $"{val:x2}";
                        } catch (NullReferenceException) {
                            result = "??";
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

        ImGui.EndGroup();
        ImGui.PopFont();

        ImGui.SameLine();

        ImGui.BeginGroup();
        ImGui.PushButtonRepeat(true);

        if (ImGui.ArrowButton("up", ImGuiDir.Up)) {
            this.memoryViewConfig.StartAddress -= this.memoryViewConfig.Width;
        }

        if (ImGui.ArrowButton("down", ImGuiDir.Down)) {
            this.memoryViewConfig.StartAddress += this.memoryViewConfig.Width;
        }

        ImGui.PopButtonRepeat();
        ImGui.EndGroup();

        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("0x");
            ImGui.SameLine(0, .0f);
            var enterPressed = ImGui.InputText(
                "##newAddr",
                ref this.nextMemoryAddr,
                8,
                ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue
            );
            ImGui.SameLine();
            var goPressed = ImGui.Button("Go");
            if (enterPressed || goPressed) {
                this.memoryViewConfig.StartAddress = nint.Parse(this.nextMemoryAddr, NumberStyles.HexNumber);
            }
        }

        ImGui.Separator();

        if (ImGui.Button("Show Settings")) {
            this.plugin.OpenConfigUi();
        }
    }
}
