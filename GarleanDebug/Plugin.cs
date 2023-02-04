using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using GarleanDebug.Windows;
using JetBrains.Annotations;

namespace GarleanDebug;

[PublicAPI]
public sealed class Plugin: IDalamudPlugin {
    private const string CommandName = "/gdb";

    private Stack<Action> disposeActions = new();
    public WindowSystem WindowSystem = new("GarleanDebug");

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager
    ) {
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;

        this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Configuration.Initialize(this.PluginInterface);

        this.ConfigWindow = new ConfigWindow(this);
        this.disposeActions.Push(() => this.ConfigWindow.Dispose());
        this.MainWindow = new MainWindow(this);
        this.disposeActions.Push(() => this.MainWindow.Dispose());

        this.WindowSystem.AddWindow(this.ConfigWindow);
        this.WindowSystem.AddWindow(this.MainWindow);
        this.disposeActions.Push(() => this.WindowSystem.RemoveAllWindows());

        this.CommandManager.AddHandler(
            CommandName,
            new CommandInfo(this.OnCommand) {
                HelpMessage = "Show help for the Garlean Debugger",
            }
        );
        this.disposeActions.Push(() => this.CommandManager.RemoveHandler(CommandName));

        this.PluginInterface.UiBuilder.Draw += this.Draw;
        this.disposeActions.Push(() => this.PluginInterface.UiBuilder.Draw -= this.Draw);
        this.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
        this.disposeActions.Push(() => this.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi);
    }

    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    public string Name => "Garlean Debugger";

    public void Dispose() {
        foreach (var action in this.disposeActions) {
            action.Invoke();
        }
    }

    private void OnCommand(string command, string args) {
        // in response to the slash command, just display our main ui
        this.MainWindow.IsOpen = true;
    }

    private void Draw() {
        this.WindowSystem.Draw();
    }

    public void OpenConfigUi() {
        this.ConfigWindow.IsOpen = true;
    }
}
