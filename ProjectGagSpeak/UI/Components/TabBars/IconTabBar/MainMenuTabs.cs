using CkCommons;
using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using ImGuiNET;

namespace GagSpeak.Gui.Components;

public class MainMenuTabs : IconTabBar<MainMenuTabs.SelectedTab>
{
    public enum SelectedTab
    {
        Homepage,
        Whitelist,
        PatternHub,
        MoodlesHub,
        GlobalChat,
        MySettings
    }

    private readonly GagspeakMediator _mediator;
    private readonly TutorialService _guides;
    public MainMenuTabs(GagspeakMediator mediator, TutorialService guides)
    {
        _mediator = mediator;
        _guides = guides;

        AddDrawButton(FontAwesomeIcon.Home, SelectedTab.Homepage, "Homepage",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.Homepage, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => TabSelection = SelectedTab.Whitelist));

        AddDrawButton(FontAwesomeIcon.PeopleArrows, SelectedTab.Whitelist, "Kinkster Whitelist", 
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.Whitelist, ImGui.GetWindowPos(), ImGui.GetWindowSize()));

        AddDrawButton(FontAwesomeIcon.Compass, SelectedTab.PatternHub, "Discover Patterns from the community!", 
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHub, ImGui.GetWindowPos(), ImGui.GetWindowSize()));

        AddDrawButton(FontAwesomeIcon.WandMagicSparkles, SelectedTab.MoodlesHub, "Browse Moodles made by others in the community!",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.MoodleHub, ImGui.GetWindowPos(), ImGui.GetWindowSize()));

        AddDrawButton(FontAwesomeIcon.Comments, SelectedTab.GlobalChat, "Meet & Chat with others in a cross-region chat!",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.GlobalChat, ImGui.GetWindowPos(), ImGui.GetWindowSize()));

        AddDrawButton(FontAwesomeIcon.UserCircle, SelectedTab.MySettings, "Account User Settings",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.AccountPage, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => TabSelection = SelectedTab.MySettings));

        TabSelectionChanged += (oldTab, newTab) => _mediator.Publish(new MainWindowTabChangeMessage(newTab));
    }

    public override void Draw(float availableWidth)
    {
        if (_tabButtons.Count == 0)
            return;

        using var color = ImRaii.PushColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));
        var spacing = ImGui.GetStyle().ItemSpacing;
        var buttonX = (availableWidth - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonY = CkGui.IconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);
        var drawList = ImGui.GetWindowDrawList();
        var underlineColor = ImGui.GetColorU32(ImGuiCol.Separator);

        ImGuiHelpers.ScaledDummy(spacing.Y / 2f);

        foreach (var tab in _tabButtons)
            DrawTabButton(tab, buttonSize, spacing, drawList);

        // advance to the new line and dispose of the button color.
        ImGui.NewLine();
        color.Dispose();

        ImGuiHelpers.ScaledDummy(3f);
        ImGui.Separator();
    }

    protected override void DrawTabButton(TabButtonDefinition tab, Vector2 buttonSize, Vector2 spacing, ImDrawListPtr drawList)
    {
        var x = ImGui.GetCursorScreenPos();

        var isDisabled = IsTabDisabled(tab.TargetTab);
        using (ImRaii.Disabled(isDisabled))
        {

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button(tab.Icon.ToIconString(), buttonSize))
                    TabSelection = tab.TargetTab;
            }

            ImGui.SameLine();
            var xPost = ImGui.GetCursorScreenPos();

            if (EqualityComparer<SelectedTab>.Default.Equals(TabSelection, tab.TargetTab))
            {
                drawList.AddLine(
                    x with { Y = x.Y + buttonSize.Y + spacing.Y },
                    xPost with { Y = xPost.Y + buttonSize.Y + spacing.Y, X = xPost.X - spacing.X },
                    ImGui.GetColorU32(ImGuiCol.Separator), 2f);
            }

            if (tab.TargetTab is SelectedTab.GlobalChat)
            {
                if (GlobalChatLog.NewMsgCount > 0)
                {
                    var newMsgTxtPos = new Vector2(x.X + buttonSize.X / 2, x.Y - spacing.Y);
                    var newMsgTxt = GlobalChatLog.NewMsgCount > 99 ? "99+" : GlobalChatLog.NewMsgCount.ToString();
                    var newMsgCol = GlobalChatLog.NewMsgFromDev ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGold;
                    drawList.OutlinedFont(newMsgTxt, newMsgTxtPos, newMsgCol.ToUint(), 0xFF000000, 1);
                }
            }
        }
        CkGui.AttachToolTip(tab.Tooltip);

        // invoke action if we should.
        tab.CustomAction?.Invoke();
    }

}
