using Dalamud.Interface.Colors;
using GagSpeak.Gui.Components;
using GagSpeak.Gui;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface.Utility.Raii;
using OtterGui;
using CkCommons;
using CkCommons.Gui;

namespace GagSpeak.State.Caches;

/// <summary>
///     Caches the current sources that have overlay items applied.
///     Helpful for the overlay controller and overlay services.
/// </summary>
public sealed class OverlayCache
{
    private readonly ILogger<OverlayCache> _logger;
    public OverlayCache(ILogger<OverlayCache> logger)
    {
        _logger = logger;
    }

    private SortedList<CombinedCacheKey, BlindfoldOverlay> _blindfolds = new();
    private SortedList<CombinedCacheKey, HypnoticOverlay> _hypnoEffects = new();
    // Might be better to turn these into structs or something but dont know.
    private KeyValuePair<CombinedCacheKey, BlindfoldOverlay>? _priorityBlindfold = null;
    private KeyValuePair<CombinedCacheKey, HypnoticOverlay>? _priorityEffect = null;

    // public accessors.
    public CombinedCacheKey PriorityBlindfoldKey => _priorityBlindfold?.Key ?? CombinedCacheKey.Empty;
    public BlindfoldOverlay? ActiveBlindfold => _priorityBlindfold?.Value;

    public CombinedCacheKey PriorityEffectKey => _priorityEffect?.Key ?? CombinedCacheKey.Empty;
    public HypnoticOverlay? ActiveEffect => _priorityEffect?.Value;

    public bool ShouldBeFirstPerson => (ActiveBlindfold?.ForceFirstPerson ?? false) || (ActiveEffect?.ForceFirstPerson ?? false);


    /// <summary>
    ///     Adds a Blindfold <paramref name="overlay"/> to the cache with <paramref name="key"/>
    /// </summary>
    public bool TryAddBlindfold(CombinedCacheKey key, BlindfoldOverlay overlay)
    {
        if (!overlay.IsValid())
            return false;

        if (!_blindfolds.TryAdd(key, overlay))
        {
            _logger.LogWarning($"KeyValuePair ([{key}]) already exists in the Cache!");
            return false;
        }
        else
        {
            _logger.LogDebug($"Blindfold Overlay with key [{key}] added to Cache.");
            return true;
        }
    }

    /// <summary>
    ///     Adds a HypnoEffect <paramref name="overlay"/> to the cache with <paramref name="key"/>
    /// </summary>
    public bool TryAddHypnoEffect(CombinedCacheKey key, HypnoticOverlay overlay)
    {
        if (!overlay.IsValid())
            return false;

        if (!_hypnoEffects.TryAdd(key, overlay))
        {
            _logger.LogWarning($"KeyValuePair ([{key}]) already exists in the Cache!");
            return false;
        }
        else
        {
            _logger.LogDebug($"Hypnotic Effect with key [{key}] added to Cache.");
            return true;
        }
    }

    /// <summary>
    ///     Removes the <paramref name="combinedKey"/> from the cache.
    /// </summary>
    public bool TryRemoveBlindfold(CombinedCacheKey combinedKey)
    {
        if (_blindfolds.Remove(combinedKey, out var effect))
        {
            _logger.LogDebug($"Removed Blindfold Overlay from cache at key [{combinedKey}].");
            return true;
        }
        else
        {
            _logger.LogWarning($"Blindfold Cache key ([{combinedKey}]) not found!!");
            return false;
        }
    }

    /// <summary>
    ///     Removes the <paramref name="combinedKey"/> from the cache.
    /// </summary>
    public bool TryRemoveHypnoEffect(CombinedCacheKey combinedKey)
    {
        if (_hypnoEffects.Remove(combinedKey, out var effect))
        {
            _logger.LogDebug($"Removed Hypnotic Overlay from cache at key [{combinedKey}].");
            return true;
        }
        else
        {
            _logger.LogWarning($"Hypnotic Cache key ([{combinedKey}]) not found!!");
            return false;
        }
    }

    /// <summary>
    ///     Careful where and how you call this, use responsibly.
    ///     If done poorly, things will go out of sync.
    /// </summary>
    public void ClearCaches()
    {
        _blindfolds.Clear();
        _hypnoEffects.Clear();
    }

    /// <summary>
    ///     Updates the priority blindfold by finding the highest priority blindfold. 
    /// </summary>
    /// <remarks> Remember, while others see the outermost blindfold, you see the innermost. </remarks>
    /// <returns> If the profile Changed. </returns>
    public bool UpdateFinalBlindfoldCache([NotNullWhen(true)] out CombinedCacheKey prevPriorityKey)
    {
        bool anyChange;
        if (_blindfolds.Count == 0)
        {
            anyChange = _priorityBlindfold != null;
            prevPriorityKey = _priorityBlindfold?.Key ?? CombinedCacheKey.Empty;
            _priorityBlindfold = null;
            return anyChange;
        }

        var newFinalItem = _blindfolds.Last();
        anyChange = !_priorityBlindfold?.Key.Equals(newFinalItem.Key) ?? true;
        prevPriorityKey = _priorityBlindfold?.Key ?? CombinedCacheKey.Empty;
        _priorityBlindfold = newFinalItem;
        return anyChange;
    }


    /// <summary>
    ///     Updates the priority hypnotic effect by finding the highest priority blindfold. 
    /// </summary>
    /// <remarks> Outputs the previous effects enactor. If the effect was null, the string will be empty. </remarks>
    /// <returns> If the profile Changed. </returns>
    public bool UpdateFinalHypnoEffectCache([NotNullWhen(true)] out CombinedCacheKey prevPriorityKey)
    {
        bool anyChange;
        if (_hypnoEffects.Count == 0)
        {
            anyChange = _priorityEffect != null;
            prevPriorityKey = _priorityEffect?.Key ?? CombinedCacheKey.Empty;
            _priorityEffect = null;
            return anyChange;
        }

        var newFinalItem = _hypnoEffects.First();
        anyChange = !_priorityEffect?.Key.Equals(newFinalItem.Key) ?? true;
        prevPriorityKey = _priorityEffect?.Key ?? CombinedCacheKey.Empty;
        _priorityEffect = newFinalItem;
        return anyChange;
    }



    #region DebugHelper
    public void DrawCacheTable()
    {
        using var display = ImRaii.Group();

        var iconSize = new Vector2(ImGui.GetFrameHeight());
        using (var node = ImRaii.TreeNode("Individual Blindfolds"))
        {
            if (node)
            {
                using (var t = ImRaii.Table("BlindfoldsCache", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (!t)
                        return;

                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("Enactor");
                    ImGui.TableSetupColumn("ImagePath");
                    ImGui.TableSetupColumn("1st PoV Forced?");
                    ImGui.TableHeadersRow();
                    foreach (var (key, overlay) in _blindfolds)
                    {
                        ImGuiUtil.DrawFrameColumn($"{key.Manager} / {key.LayerIndex}");
                        ImGuiUtil.DrawFrameColumn(key.EnactorUID);
                        ImGuiUtil.DrawFrameColumn(string.IsNullOrWhiteSpace(overlay.OverlayPath) ? "<No Image Path Set>" : overlay.OverlayPath);
                        ImGui.TableNextColumn();
                        CkGui.BooleanToColoredIcon(overlay.ForceFirstPerson);
                    }
                }
            }
        }

        using (var node = ImRaii.TreeNode("Individual Hypnotic Effects"))
        {
            if (node)
            {
                using (var t = ImRaii.Table("HypnoEffectsCache", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (!t)
                        return;
                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("Enactor");
                    ImGui.TableSetupColumn("ImagePath");
                    ImGui.TableSetupColumn("1st PoV Forced?");
                    ImGui.TableHeadersRow();
                    foreach (var (key, overlay) in _hypnoEffects)
                    {
                        ImGuiUtil.DrawFrameColumn($"{key.Manager} / {key.LayerIndex}");
                        ImGuiUtil.DrawFrameColumn(key.EnactorUID);
                        ImGuiUtil.DrawFrameColumn(string.IsNullOrWhiteSpace(overlay.OverlayPath) ? "<No Image Path Set>" : overlay.OverlayPath);
                        ImGui.TableNextColumn();
                        CkGui.BooleanToColoredIcon(overlay.ForceFirstPerson);
                    }
                }
            }
        }

        ImGui.Separator();
        ImGui.Text("Final Blindfold: ");
        if (_priorityBlindfold is { } validBf && validBf.Value is not null)
        {
            CkGui.ColorTextInline($"[{validBf.Key.ToString()}]", CkGui.Color(ImGuiColors.HealerGreen));
            CkGui.TextInline($" Enactor: {validBf.Key.EnactorUID}");
            CkGui.TextInline($" Overlay: {validBf.Value.OverlayPath}");
        }
        else
        {
            CkGui.ColorTextInline("<No Blindfold Applied>", CkGui.Color(ImGuiColors.DalamudRed));
        }

        ImGui.Text("Final Hypnotic Effect: ");
        if (_priorityEffect is { } validHypno && validHypno.Value is not null)
        {
            CkGui.ColorTextInline($"[{validHypno.Key.ToString()}]", CkGui.Color(ImGuiColors.HealerGreen));
            CkGui.TextInline($" Enactor: {validHypno.Key.EnactorUID}");
            CkGui.TextInline($" Overlay: {validHypno.Value.OverlayPath}");
        }
        else
        {
            CkGui.ColorTextInline("<No Hypnotic Effect Applied>", CkGui.Color(ImGuiColors.DalamudRed));
        }
    }
    #endregion Debug Helper
}
