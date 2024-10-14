using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using FCWelcome;
using System.Linq;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using System.Runtime.CompilerServices;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration configuration;

    private string tempMessage = string.Empty;
    private bool needToSave = false;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("FC Welcome Configuration")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public void ReLoginExtra()
    {
        var threshold = configuration.ReLoginThresholdMinutes;
        if (ImGui.SliderInt("Max time logged out.", ref threshold, 0, 120))
        {
            configuration.ReLoginThresholdMinutes = threshold;
            this.needToSave = true;
        }
    }

    public override void Draw()
    {
        this.needToSave = false;

        if (ImGui.BeginTabBar("Messages"))
        {
            if (ImGui.BeginTabItem("Relogin Messages"))
            {
                
                DrawMessagesConfig(configuration.ReLoginMessages, "reloginMessages", ReLoginExtra);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Login Messages"))
            {
                DrawMessagesConfig(configuration.LoginMessages, "loginMessages");
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Join Messages"))
            {
                DrawMessagesConfig(configuration.JoinMessages, "joinMessages");
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        if (this.needToSave)
        {
            configuration.Save();
            this.needToSave = false;
        }

    }

    public delegate void ExtraConfigDelegate();

    private void DrawMessagesConfig(MessageTypeSettings messageType, string name, ExtraConfigDelegate extraConfig = null!)
    {
        using var id = ImRaii.PushId(name);

        bool enabled = messageType.IsEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            messageType.IsEnabled = enabled;
            this.needToSave = true;
        }

        enabled = messageType.UseOnlyFirstName;
        if (ImGui.Checkbox("Use only first name", ref enabled))
        {
            messageType.UseOnlyFirstName = enabled;
            this.needToSave = true;
        }

        int min = messageType.MinSeconds;
        int max = messageType.MaxSeconds;
        if (ImGui.SliderInt("Min Seconds", ref min, 1, max - 1))
        {
            messageType.MinSeconds = min;
            this.needToSave = true;
        }
        if (ImGui.SliderInt("Max Seconds", ref max, min + 1, 60))
        {
            messageType.MaxSeconds = max;
            this.needToSave = true;
        }

        if (extraConfig != null)
        {
            extraConfig();
        }

        ImGuiHelpers.ScaledDummy(5);

        ImGui.Columns(4);
        ImGui.SetColumnWidth(0, 18 + (5 * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(1, ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - (18 + 16 + 14) - ((5 + 45 + 26) * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(2, 16 + (45 * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(3, 14 + (26 * ImGuiHelpers.GlobalScale));

        ImGui.Separator();

        ImGui.TextUnformatted("#");
        ImGui.NextColumn();
        ImGui.TextUnformatted("Message");
        ImGui.NextColumn();
        ImGui.TextUnformatted("Enabled");
        ImGui.NextColumn();
        ImGui.TextUnformatted(string.Empty);
        ImGui.NextColumn();

        ImGui.Separator();

        MessageSettings locationToRemove = null!;

        var locNumber = 1;
        foreach (var messageSetting in messageType.Messages)
        {
            var isEnabled = messageSetting.IsEnabled;


            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(locNumber.ToString()).X / 2));
            ImGui.TextUnformatted(locNumber.ToString());
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            id.Push(messageSetting.Message);
            var message = messageSetting.Message;
            if (ImGui.InputText($"##devPluginLocationInput{locNumber}", ref message, 65535, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (messageSetting.Message != message)
                {
                    messageSetting.Message = message;
                    this.needToSave = true;
                }
            }

            ImGui.NextColumn();

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 7 - (12 * ImGuiHelpers.GlobalScale));
            ImGui.Checkbox($"##devPluginLocationCheck{locNumber}", ref isEnabled);
            if (messageSetting.IsEnabled != isEnabled)
            {
                messageSetting.IsEnabled = isEnabled;
                this.needToSave = true;
            }
            ImGui.NextColumn();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            {
                locationToRemove = messageSetting;
            }

            id.Pop();

            ImGui.NextColumn();
            ImGui.Separator();

            locNumber++;
        }

        if (locationToRemove != null)
        {
            messageType.Messages.Remove(locationToRemove);
            this.needToSave = true;
        }

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(locNumber.ToString()).X / 2));
        ImGui.TextUnformatted(locNumber.ToString());
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##devPluginLocationInput", ref this.tempMessage, 300, ImGuiInputTextFlags.EnterReturnsTrue)) {
            AddMessageType(messageType);
        }

        ImGui.NextColumn();
        // Enabled button
        ImGui.NextColumn();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
        {
            AddMessageType(messageType);
        }

        ImGui.Columns(1);
    }

    private void AddMessageType(MessageTypeSettings messageType) {
        if (!string.IsNullOrEmpty(this.tempMessage) && this.tempMessage.Contains("<t>"))
        {
            messageType.Messages.Add(new MessageSettings
            {
                Message = this.tempMessage,
                IsEnabled = true,
            });
            this.needToSave = true;
            this.tempMessage = string.Empty;
        }

    }
}
