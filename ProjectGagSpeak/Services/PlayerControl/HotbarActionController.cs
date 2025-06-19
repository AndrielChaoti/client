using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using GagSpeak.CkCommons;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using System.Collections.Immutable;
using System.Reflection;
using System.Windows.Forms;
using static Lumina.Data.Parsing.Layer.LayerCommon;


namespace GagSpeak.Services.Controller;

/// <summary>
///     Controls what hotbar actions are restricted or not, and the customized tooltips for each action.
/// </summary>
public sealed class HotbarActionController : DisposableMediatorSubscriberBase
{
    private const uint TITLE_NODE_ID = 5;
    private const uint ACTION_TYPE_NODE_ID = 6;
    private const uint RANGE_NODE_ID = 9;
    private const uint RADIUS_NODE_ID = 12;
    private const uint DESCRIPTION_NODE_ID = 19;

    private readonly TraitsCache _cache;
    private readonly PlayerData _player;
    private readonly IAddonLifecycle _lifecycle;
    private readonly IGameGui _gameGui;

    /// <summary> The currently banned actions determined by the <see cref="_cache"/>'s _finalTrait's </summary>
    private ImmutableDictionary<uint, Traits> _bannedActions = ImmutableDictionary<uint, Traits>.Empty;

    /// <summary> Trait -> Override ActionId map, ordered by priority of application. </summary>
    private static readonly (uint Id, Traits Traits)[] _traitActionIds =
    [
        (2886, Traits.Gagged),
        (99, Traits.Blindfolded),
        (151, Traits.Weighty),
        (2883, Traits.Immobile),
        (55, Traits.BoundLegs),
        (68, Traits.BoundArms)
    ];
    
    private Traits _sources = Traits.None;

    public unsafe HotbarActionController(
        ILogger<HotbarActionController> logger,
        GagspeakMediator mediator,
        TraitsCache cache,
        PlayerData player,
        IAddonLifecycle afc,
        IGameGui gg)
        : base(logger, mediator)
    {
        _cache = cache;
        _player = player;
        _lifecycle = afc;
        _gameGui = gg;

        afc.RegisterListener(AddonEvent.PostRequestedUpdate, "ActionDetail", (_, args) => OnActionTooltip((AtkUnitBase*)args.Addon));
        Mediator.Subscribe<JobChangeMessage>(this, msg => OnJobChange(msg.jobId));
    }

    /// <summary> The currently active traits that are blocking your actions. </summary>
    public Traits Sources => _sources;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _lifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "ActionDetail");
        // just incase.
        RestoreSavedSlots();
    }

    public void UpdateSources(Traits newSources)
    {
        // If the traits changed, update the slots.
        if (newSources != _sources)
        {
            _sources = newSources;
            UpdateSlots();
        }
    }

    private void UpdateSlots()
    {
        // maybe move inside the if statement idk.
        RestoreSavedSlots();

        // If there are no more controlling traits, restore and return.
        if (_sources is Traits.None)
        {
            Logger.LogDebug("No controlling traits, restoring saved slots.", LoggerType.HardcoreActions);
            return;
        }

        // Otherwise, restore and update the slots.
        SetBannedSlots();
    }

    /// <summary>
    ///     the Internal function that updates the current hotbar slots with the banned actions.
    /// </summary>
    private unsafe void SetBannedSlots()
    {
        var hotbarModule = Framework.Instance()->GetUIModule()->GetRaptureHotbarModule();
        // the length of our hotbar count
        var hotbarSpan = hotbarModule->StandardHotbars;

        // Check all active hotbar spans.
        for (var i = 0; i < hotbarSpan.Length; i++)
        {
            var hotbarRow = hotbarSpan.GetPointer(i);
            if (hotbarSpan == Span<RaptureHotbarModule.Hotbar>.Empty)
                continue;

            // get the slots data...
            for (var j = 0; j < 16; j++)
            {
                // From the pointer, get the individual slot.
                var slot = hotbarRow->Slots.GetPointer(j);
                if (slot is null)
                    break;

                // If not a valid action type, ignore it.
                if (slot->CommandType != RaptureHotbarModule.HotbarSlotType.Action &&
                    slot->CommandType != RaptureHotbarModule.HotbarSlotType.GeneralAction)
                    continue;

                // if unable to find the properties for this item, ignore it.
                if (!_bannedActions.TryGetValue(slot->CommandId, out var props))
                    continue;

                // Apply the first matching trait override
                foreach (var (actionId, trait) in _traitActionIds)
                {
                    // If the property exists and we have that trait enabled, set the slot to the restricted item.
                    if (props.HasAny(trait) && _sources.HasAny(trait))
                    {
                        slot->Set(hotbarModule->UIModule, RaptureHotbarModule.HotbarSlotType.Action, actionId);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     To ensure hotbar slot data is not lost, restore them to defaults here.
    /// </summary>
    private unsafe void RestoreSavedSlots()
    {
        var hotbarModule = Framework.Instance()->GetUIModule()->GetRaptureHotbarModule();
        if (hotbarModule is null)
            return;

        Logger.LogDebug("Restoring saved slots", LoggerType.HardcoreActions);
        var baseSpan = hotbarModule->StandardHotbars; // the length of our hotbar count
        for (var i = 0; i < baseSpan.Length; i++)
        {
            var hotbarRow = baseSpan.GetPointer(i);
            // if the hotbar is not null, we can get the slots data
            if (hotbarRow is not null)
                hotbarModule->LoadSavedHotbar(_player.JobId, (uint)i);
        }
    }

    /// <summary>
    ///     Every time we change jobs, we need to aquire the new banned actions.
    /// </summary>
    private void OnJobChange(uint jobId)
    {
        Logger.LogDebug($"Job Changed to [{((JobType)jobId)}], recalculating Banned Actions.");
        _bannedActions = (JobType)jobId switch
        {
            JobType.ADV => RestrictedActions.Adventurer,
            JobType.GLA => RestrictedActions.Gladiator,
            JobType.PGL => RestrictedActions.Pugilist,
            JobType.MRD => RestrictedActions.Marauder,
            JobType.LNC => RestrictedActions.Lancer,
            JobType.ARC => RestrictedActions.Archer,
            JobType.CNJ => RestrictedActions.Conjurer,
            JobType.THM => RestrictedActions.Thaumaturge,
            JobType.CRP => RestrictedActions.Carpenter,
            JobType.BSM => RestrictedActions.Blacksmith,
            JobType.ARM => RestrictedActions.Armorer,
            JobType.GSM => RestrictedActions.Goldsmith,
            JobType.LTW => RestrictedActions.Leatherworker,
            JobType.WVR => RestrictedActions.Weaver,
            JobType.ALC => RestrictedActions.Alchemist,
            JobType.CUL => RestrictedActions.Culinarian,
            JobType.MIN => RestrictedActions.Miner,
            JobType.BTN => RestrictedActions.Botanist,
            JobType.FSH => RestrictedActions.Fisher,
            JobType.PLD => RestrictedActions.Paladin,
            JobType.MNK => RestrictedActions.Monk,
            JobType.WAR => RestrictedActions.Warrior,
            JobType.DRG => RestrictedActions.Dragoon,
            JobType.BRD => RestrictedActions.Bard,
            JobType.WHM => RestrictedActions.WhiteMage,
            JobType.BLM => RestrictedActions.BlackMage,
            JobType.ACN => RestrictedActions.Arcanist,
            JobType.SMN => RestrictedActions.Summoner,
            JobType.SCH => RestrictedActions.Scholar,
            JobType.ROG => RestrictedActions.Rogue,
            JobType.NIN => RestrictedActions.Ninja,
            JobType.MCH => RestrictedActions.Machinist,
            JobType.DRK => RestrictedActions.DarkKnight,
            JobType.AST => RestrictedActions.Astrologian,
            JobType.SAM => RestrictedActions.Samurai,
            JobType.RDM => RestrictedActions.RedMage,
            JobType.BLU => RestrictedActions.BlueMage,
            JobType.GNB => RestrictedActions.Gunbreaker,
            JobType.DNC => RestrictedActions.Dancer,
            JobType.RPR => RestrictedActions.Reaper,
            JobType.SGE => RestrictedActions.Sage,
            JobType.VPR => RestrictedActions.Viper,
            JobType.PCT => RestrictedActions.Pictomancer,
            _ => ImmutableDictionary<uint, Traits>.Empty,
        };
        // Update the slots based on the new job.
        UpdateSlots();
    }

    /// <summary>
    ///     Called when action tooltip has finished drawing.
    ///     This is used to update the tooltip with the correct trait information for restricted actions.
    ///     It replaces the title, description, and other text nodes with the appropriate trait information.
    ///     If the action is not a trait restriction, it will skip updating the tooltip.
    /// </summary>
    /// <param name="addon">pointer to the tooltip addon</param>
    private unsafe void OnActionTooltip(AtkUnitBase* addon)
    {
        // Ensure tooltip exists.
        if (addon is null || _gameGui.HoveredAction is not { } hoveredAct)
            return;

        // Must be an action tooltip.
        if (hoveredAct.ActionKind is not HoverActionKind.Action)
            return;

        // Must be a TraitAction Action.
        if (!_traitActionIds.Any(x => x.Id == hoveredAct.ActionID))
            return;

        Logger.LogTrace($"Action ({hoveredAct.ActionID}) is a TraitRestriction tooltip, altaring display.", LoggerType.HardcoreActions);
        // hide away the recast container, as it is not needed. (but maybe make it work?)
        var castRecastContainer = addon->GetNodeById(13);
        if (castRecastContainer is not null)
        {
            castRecastContainer->SetHeight(0);
            castRecastContainer->ToggleVisibility(false);
        }

        // Set common actions.
        ActionTooltipEx.ReplaceTextNodeText(addon, ACTION_TYPE_NODE_ID, "Hardcore Trait"); // Usually spell (GCD) or ability (oGCD)
        ActionTooltipEx.ReplaceTextNodeText(addon, RANGE_NODE_ID, "0y");
        ActionTooltipEx.ReplaceTextNodeText(addon, RADIUS_NODE_ID, "0y");

        // Replace title and description based on the trait.
        var trait = _traitActionIds.FirstOrDefault(x => x.Id == hoveredAct.ActionID).Traits;
        var title = ActionTooltipEx.GetTitle(trait);
        var desc  = ActionTooltipEx.GetDescription(trait, _cache.GetSourceName(trait));
        ActionTooltipEx.ReplaceTextNodeText(addon, TITLE_NODE_ID, title);
        ActionTooltipEx.ReplaceTextNodeText(addon, DESCRIPTION_NODE_ID, desc);

        // Update the tooltip window dimentions.
        addon->WindowNode->SetHeight(120);
        addon->WindowNode->AtkResNode.SetHeight(addon->WindowNode->Height);
        addon->WindowNode->Component->UldManager.RootNode->SetHeight(addon->WindowNode->Height);
        addon->WindowNode->Component->UldManager.RootNode->PrevSiblingNode->SetHeight(addon->WindowNode->Height);
        addon->RootNode->SetHeight(addon->WindowNode->Height);
    }
}
