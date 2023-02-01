using System.IO;
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

        // you might normally want to embed resources and load them from the manifest stream
        var imagePath = Path.Combine(this.PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
        var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

        this.ConfigWindow = new ConfigWindow(this);
        this.MainWindow = new MainWindow(this, goatImage);

        this.WindowSystem.AddWindow(this.ConfigWindow);
        this.WindowSystem.AddWindow(this.MainWindow);

        this.CommandManager.AddHandler(
            CommandName,
            new CommandInfo(this.OnCommand) {
                HelpMessage = "A useful message to display in /xlhelp",
            }
        );

        this.PluginInterface.UiBuilder.Draw += this.DrawUI;
        this.PluginInterface.UiBuilder.OpenConfigUi += this.DrawConfigUI;
    }

    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    public string Name => "the Garlean Debugger";

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

    private void DrawUI() {
        this.WindowSystem.Draw();
    }

    public void DrawConfigUI() {
        this.ConfigWindow.IsOpen = true;
    }
}
