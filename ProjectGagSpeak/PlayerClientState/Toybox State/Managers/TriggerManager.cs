using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.Localization;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Components;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;

namespace GagSpeak.PlayerState.Toybox;

public sealed class TriggerManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly PatternManager _patterns;
    private readonly AlarmManager _alarms;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<Trigger> _itemEditor = new();

    public TriggerManager(ILogger<TriggerManager> logger, GagspeakMediator mediator,
        PatternManager patterns, AlarmManager alarms, FavoritesManager favorites,
        ConfigFileProvider fileNames, HybridSaveService saver) : base(logger, mediator)
    {
        _patterns = patterns;
        _alarms = alarms;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;
    }

    public TriggerStorage Storage { get; private set; } = new TriggerStorage();
    public Trigger? ItemInEditor => _itemEditor.ItemInEditor;
    public IEnumerable<Trigger> ActiveTriggers => Storage.Where(x => x.Enabled);

    public Trigger CreateNew(string triggerName)
    {
        triggerName = RegexEx.EnsureUniqueName(triggerName, Storage, (t) => t.Label);
        var newTrigger = new GagTrigger() { Label = triggerName };
        Storage.Add(newTrigger);
        _saver.Save(this);

        Mediator.Publish(new ConfigTriggerChanged(StorageItemChangeType.Created, newTrigger, null));
        return newTrigger;
    }

    public Trigger CreateClone(Trigger other, string newName)
    {
        newName = RegexEx.EnsureUniqueName(newName, Storage, (t) => t.Label);
        Trigger clonedItem = other switch
        {
            SpellActionTrigger   sa => new SpellActionTrigger(sa, false) { Label = newName },
            HealthPercentTrigger hp => new HealthPercentTrigger(hp, false) { Label = newName },
            RestraintTrigger     r  => new RestraintTrigger(r, false) { Label = newName },
            GagTrigger           g  => new GagTrigger(g, false) { Label = newName },
            SocialTrigger        s  => new SocialTrigger(s, false) { Label = newName },
            EmoteTrigger         e  => new EmoteTrigger(e, false) { Label = newName },
            _ => throw new NotImplementedException("Unknown trigger type."),
        };
        Storage.Add(clonedItem);
        _saver.Save(this);

        Logger.LogDebug($"Cloned trigger {other.Label} to {newName}.");
        Mediator.Publish(new ConfigTriggerChanged(StorageItemChangeType.Created, clonedItem, null));
        return clonedItem;
    }

    public void ChangeTriggerType(Trigger newTrigger, TriggerKind newType)
    {
        if (ItemInEditor is null)
            return;

        Trigger convertedTrigger = newType switch
        {
            TriggerKind.SpellAction => new SpellActionTrigger(newTrigger, false),
            TriggerKind.HealthPercent => new HealthPercentTrigger(newTrigger, false),
            TriggerKind.RestraintSet => new RestraintTrigger(newTrigger, false),
            TriggerKind.GagState => new GagTrigger(newTrigger, false),
            TriggerKind.SocialAction => new SocialTrigger(newTrigger, false),
            TriggerKind.EmoteAction => new EmoteTrigger(newTrigger, false),
            _ => throw new NotImplementedException("Unknown trigger type."),
        };

        // Update the editor item to reflect that of the new type.
        _itemEditor.ItemInEditor = convertedTrigger;
    }

    public void Rename(Trigger trigger, string newName)
    {
        var prevName = trigger.Label;
        newName = RegexEx.EnsureUniqueName(newName, Storage, (t) => t.Label);
        trigger.Label = newName;
        _saver.Save(this);

        Logger.LogDebug($"Storage contained trigger, renaming {trigger.Label} to {newName}.");
        Mediator.Publish(new ConfigTriggerChanged(StorageItemChangeType.Renamed, trigger, prevName));
    }

    public void Delete(Trigger trigger)
    {
        if(Storage.Remove(trigger))
        {
            Logger.LogDebug($"Deleted trigger {trigger.Label}.");
            Mediator.Publish(new ConfigTriggerChanged(StorageItemChangeType.Deleted, trigger, null));
            _saver.Save(this);
        }

    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(Trigger item) => _itemEditor.StartEditing(item);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            Logger.LogDebug($"Saved changes to Trigger {sourceItem.Label}.");
            Mediator.Publish(new ConfigTriggerChanged(StorageItemChangeType.Modified, sourceItem));
            _saver.Save(this);
        }
    }

    /// <summary> Attempts to add the gag restriction as a favorite. </summary>
    public bool AddFavorite(Trigger t) => _favorites.TryAddRestriction(FavoriteIdContainer.Trigger, t.Identifier);

    /// <summary> Attempts to remove the gag restriction as a favorite. </summary>
    public bool RemoveFavorite(Trigger t) => _favorites.RemoveRestriction(FavoriteIdContainer.Trigger, t.Identifier);

    // unsure how stable these are to use atm but we will see.
    public void ToggleTrigger(Guid triggerId, string enactor)
    {
        if (Storage.TryGetTrigger(triggerId, out var trigger))
        {
            trigger.Enabled = !trigger.Enabled;
            _saver.Save(this);
        }
    }

    public void EnableTrigger(Guid triggerId, string enactor)
    {
        if (Storage.TryGetTrigger(triggerId, out var trigger))
        {
            trigger.Enabled = true;
            _saver.Save(this);
        }
    }

    public void DisableTrigger(Guid triggerId, string enactor)
    {
        // if this is false it means one is active for us to disable.
        if (Storage.TryGetTrigger(triggerId, out var trigger))
        {
            if(!trigger.Enabled) 
                return;

            trigger.Enabled = false;
            _saver.Save(this);
        }
    }

    #region HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique) => (isAccountUnique = true, files.Triggers).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        // we need to iterate through our list of trigger objects and serialize them.
        var triggerItems = JsonConvert.SerializeObject(Storage, Formatting.Indented);

        // construct the config object to serialize.
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["Triggers"] = triggerItems,
        }.ToString(Formatting.Indented);
    }

    public void Load()
    {
        var file = _fileNames.Triggers;
        Logger.LogInformation("Loading in Triggers Config for file: " + file);

        Storage.Clear();
        if (!File.Exists(file))
        {
            Logger.LogWarning("No Triggers Config file found at {0}", file);
            // create a new file with default values.
            _saver.Save(this);
            return;
        }

        // Read the json from the file.
        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);

        // Migrate the jObject if it is using the old format.
        if (jObject["TriggerStorage"] is JObject)
            jObject = ConfigMigrator.MigrateTriggersConfig(jObject, _fileNames, file);

        var version = jObject["Version"]?.Value<int>() ?? 0;

        // Perform Migrations if any, and then load the data.
        switch (version)
        {
            case 0:
                LoadV0(jObject["Triggers"]);
                break;
            default:
                Logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        Mediator.Publish(new ReloadFileSystem(ModuleSection.Trigger));
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JArray triggers)
            return;

        try
        {
            foreach (var triggerToken in triggers)
            {
                if (!Enum.TryParse(triggerToken["Type"]?.ToString(), out TriggerKind triggerType))
                    continue;

                // Otherwise, try and parse it out.
                Trigger triggerAbstract = triggerType switch
                {
                    TriggerKind.SpellAction => triggerToken.ToObject<SpellActionTrigger>() ?? new SpellActionTrigger(),
                    TriggerKind.HealthPercent => triggerToken.ToObject<HealthPercentTrigger>() ?? new HealthPercentTrigger(),
                    TriggerKind.RestraintSet => triggerToken.ToObject<RestraintTrigger>() ?? new RestraintTrigger(),
                    TriggerKind.GagState => triggerToken.ToObject<GagTrigger>() ?? new GagTrigger(),
                    TriggerKind.SocialAction => triggerToken.ToObject<SocialTrigger>() ?? new SocialTrigger(),
                    TriggerKind.EmoteAction => triggerToken.ToObject<EmoteTrigger>() ?? new EmoteTrigger(),
                    _ => throw new Exception("Invalid Trigger Type")
                };
                // Safely parse the integer to InvokableActionType
                if (Enum.TryParse(triggerToken["ActionType"]?.ToString(), out InvokableActionType executionType))
                {
                    InvokableGsAction executableAction = executionType switch
                    {
                        InvokableActionType.TextOutput => triggerToken["ExecutableAction"]?.ToObject<TextAction>() ?? new TextAction(),
                        InvokableActionType.Gag => triggerToken["ExecutableAction"]?.ToObject<GagAction>() ?? new GagAction(),
                        InvokableActionType.Restraint => triggerToken["ExecutableAction"]?.ToObject<RestraintAction>() ?? new RestraintAction(),
                        InvokableActionType.Moodle => triggerToken["ExecutableAction"]?.ToObject<MoodleAction>() ?? new MoodleAction(),
                        InvokableActionType.ShockCollar => triggerToken["ExecutableAction"]?.ToObject<PiShockAction>() ?? new PiShockAction(),
                        InvokableActionType.SexToy => triggerToken["ExecutableAction"]?.ToObject<SexToyAction>() ?? new SexToyAction(),
                        _ => throw new Exception("Invalid Execution Type")
                    };

                    if (executableAction is not null)
                        triggerAbstract.InvokableAction = executableAction;
                    else
                        throw new Exception("Failed to deserialize ExecutableAction");
                }
                else
                {
                    throw new Exception("Invalid Execution Type");
                }

                Storage.Add(triggerAbstract);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load Triggers");
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    #endregion HybridSavable
}
