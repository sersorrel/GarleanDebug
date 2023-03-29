using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GarleanDebug.Windows;
using JetBrains.Annotations;

namespace GarleanDebug;

[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
internal class CommandAttribute: Attribute {
    public CommandAttribute(string regex, bool hidden = false) {
        this.Hidden = hidden;
        this.OriginalRegex = regex;
        this.Regex = new Regex($"^(?:{regex})$");
    }

    public bool Hidden { get; }
    public string OriginalRegex { get; }
    public Regex Regex { get; }
}

internal class Commands {
    private readonly Plugin plugin;

    public Commands(Plugin plugin) {
        this.plugin = plugin;
        var methods = typeof(Commands).GetMethods();
        var commands = new List<Command>();
        foreach (var method in methods) {
            var attrs = Attribute.GetCustomAttributes(method);
            foreach (var attr in attrs) {
                if (attr is CommandAttribute command) {
                    commands.Add(
                        new Command(
                            command.OriginalRegex,
                            command.Regex,
                            match => method.Invoke(this, new object?[] { match, this.plugin })
                        )
                    );
                }
            }
        }

        this.All = commands.ToArray();
    }

    public Command[] All { get; }

    public void Execute(string s) {
        foreach (var command in this.All) {
            var match = command.Regex.Match(s);
            if (match.Success) {
                command.Action.Invoke(match);
                return;
            }
        }

        throw new ArgumentException($"no command found for: {s}");
    }

    [Command("", hidden: true)]
    public void NullCommand(Match match, Plugin plugin) {
        this.HelpCommand(match, plugin);
    }

    [Command("help")]
    public void HelpCommand(Match match, Plugin plugin) {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Available commands:");
        foreach (var command in this.All) {
            stringBuilder.AppendLine($"- {command.Name}");
        }

        plugin.ChatGui.Print(stringBuilder.ToString().TrimEnd());
    }

    [Command("show")]
    public static void ShowCommand(Match match, Plugin plugin) {
        plugin.MainWindow.IsOpen = true;
    }

    [Command("hide")]
    public static void HideCommand(Match match, Plugin plugin) {
        plugin.MainWindow.IsOpen = false;
    }

    [Command("agent ([1-9][0-9]*)")]
    public static void AgentByIdCommand(Match match, Plugin plugin) {
        if (uint.TryParse(match.Groups[1].Value, out var id)) {
            unsafe {
                var agent = (byte*)Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalID(id);
                plugin.ChatGui.Print($"Agent {id} is at address 0x" + ((nint)agent).ToString("x" + (nint.Size * 2)));
            }
        } else {
            plugin.ChatGui.PrintError("Need an agent number!");
        }
    }

    [Command("agent 0x([0-9a-f]+)")]
    public static void AgentByAddressCommand(Match match, Plugin plugin) {
        if (nint.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var addr)) {
            foreach (var id in (AgentId[])Enum.GetValues(typeof(AgentId))) {
                unsafe {
                    var agent = (byte*)Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(id);
                    if ((nint)agent == addr) {
                        plugin.ChatGui.Print("Agent at 0x" + addr.ToString("x" + (nint.Size * 2)) + $" is '{id}' (ID {(uint)id})");
                        return;
                    }
                }
            }

            plugin.ChatGui.PrintError($"No agent found at address 0x" + addr.ToString("x" + (nint.Size * 2)));
        } else {
            plugin.ChatGui.PrintError("Need an agent address!");
        }
    }

    public class Command {
        internal Command(string name, Regex regex, Action<Match> action) {
            this.Name = name;
            this.Regex = regex;
            this.Action = action;
        }

        public string Name { get; }
        public Regex Regex { get; }
        public Action<Match> Action { get; }
    }
}

[PublicAPI]
public sealed class Plugin: IDalamudPlugin {
    private const string CommandName = "/gdb";
    private Commands commands;

    private Stack<Action> disposeActions = new();
    public WindowSystem WindowSystem = new("GarleanDebug");

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] ChatGui chatGui
    ) {
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;
        this.ChatGui = chatGui;
        this.commands = new Commands(this);

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
                HelpMessage = "Use the Garlean Debugger",
            }
        );
        this.disposeActions.Push(() => this.CommandManager.RemoveHandler(CommandName));

        this.PluginInterface.UiBuilder.Draw += this.Draw;
        this.disposeActions.Push(() => this.PluginInterface.UiBuilder.Draw -= this.Draw);
        this.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
        this.disposeActions.Push(() => this.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi);
    }

    internal DalamudPluginInterface PluginInterface { get; init; }
    internal CommandManager CommandManager { get; init; }
    internal ChatGui ChatGui { get; init; }
    public Configuration Configuration { get; init; }

    internal ConfigWindow ConfigWindow { get; init; }
    internal MainWindow MainWindow { get; init; }
    public string Name => "Garlean Debugger";

    public void Dispose() {
        foreach (var action in this.disposeActions) {
            action.Invoke();
        }
    }

    private void OnCommand(string command, string args) {
        try {
            this.commands.Execute(args);
        } catch (ArgumentException e) {
            this.ChatGui.PrintError(e.Message);
        }
    }

    private void Draw() {
        this.WindowSystem.Draw();
    }

    public void OpenConfigUi() {
        this.ConfigWindow.IsOpen = true;
    }
}
