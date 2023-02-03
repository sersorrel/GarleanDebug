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
        this.MainWindow = new MainWindow(this);

        this.WindowSystem.AddWindow(this.ConfigWindow);
        this.WindowSystem.AddWindow(this.MainWindow);

        this.CommandManager.AddHandler(
            CommandName,
            new CommandInfo(this.OnCommand) {
                HelpMessage = "Show help for the Garlean Debugger",
            }
        );

        this.PluginInterface.UiBuilder.Draw += this.DrawUi;
        this.PluginInterface.UiBuilder.OpenConfigUi += this.DrawConfigUi;
    }

    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    public string Name => "Garlean Debugger";

    public void Dispose() {
        this.WindowSystem.RemoveAllWindows();

        this.ConfigWindow.Dispose();
        this.MainWindow.Dispose();

        this.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args) {
        // in response to the slash command, just display our main ui
        this.MainWindow.IsOpen = true;
    }

    private void DrawUi() {
        this.WindowSystem.Draw();
    }

    public void DrawConfigUi() {
        this.ConfigWindow.IsOpen = true;
    }
}
