using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Logging.Internal;
using SamplePlugin.Windows;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game;
using FCWelcome;
using System;
using System.Collections.Generic;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static ISigScanner Scanner { get; private set; } = null!;
    [PluginService] static internal IChatGui ChatGui { get; private set; } = null!;
    [PluginService] static internal IFramework Framework { get; private set; } = null!;

    private class User
    {
        public string name = "";
        public DateTime logoutTime;
    }

    private Dictionary<string, User> users = new Dictionary<string, User>();

    private const string CommandName = "/fcwelcome";
    private Random random = new Random();

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("FCWelcome");
    private ConfigWindow ConfigWindow { get; init; }

    internal static ServerChat ServerChat { get; private set; } = null!;

    private List<Message> messages = new List<Message>();

    public static readonly ModuleLog Log = new("FCWelcome");

    public Plugin()
    {
        ServerChat = new(Scanner);
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        ChatGui.ChatMessage += Chat_OnChatMessage;
        Framework.Update += OnceUponAFrame;
    }

    private void OnceUponAFrame(object _)
    {
        // This is where we will send chat messages
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            if (DateTime.Now > messages[i].time)
            {
                ServerChat.SendMessage($"/fc {messages[i].message}");
                messages.RemoveAt(i);
            }
        }
    }

    private void Chat_OnChatMessage(XivChatType type, int senderId, ref SeString sender, ref SeString cmessage, ref bool isHandled)
    {
        // Get message
        var message = cmessage.ToString();
        
        if (message.EndsWith(" has logged out."))
        {
            var fullUserName = message.Replace(" has logged out.", "");
            try
            {
                users.Add(fullUserName, new User { name = fullUserName, logoutTime = DateTime.Now });
            }
            catch
            {
                users[fullUserName].logoutTime = DateTime.Now;
            }
            return;
        }

        // Check if this is a user logging in.
        if (message.EndsWith(" has logged in."))
        {
            var fullUserName = message.Replace(" has logged in.", "");

            if (users.ContainsKey(fullUserName))
            {
                if ((DateTime.Now - users[fullUserName].logoutTime).TotalMinutes < Configuration.ReLoginThresholdMinutes)
                {
                    var userName = fullUserName;
                    if (Configuration.ReLoginMessages.UseOnlyFirstName)
                    {
                        userName = userName.Split(' ')[0];
                    }
                    messages.Add(Configuration.ReLoginMessages.CreateMessage(userName));
                    return;
                }
            }

            messages.Add(Configuration.LoginMessages.CreateMessage(fullUserName));
            return;
        }

        // Check if this is a user joined the FC.
        if (message.EndsWith(" joins the free company."))
        {
            var fullUserName = message.Replace(" joins the free company.", "");
            messages.Add(Configuration.JoinMessages.CreateMessage(fullUserName));
            return;
        }
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        ChatGui.ChatMessage -= Chat_OnChatMessage;
        Framework.Update -= OnceUponAFrame;
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleConfigUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
