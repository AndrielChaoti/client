using Dalamud.Game.ClientState.Objects.Types;
using GagSpeak.GameInternals;
using GagSpeak.State;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.PlayerClient;

public partial class AchievementListener
{
    private void OnCommendationsGiven(int amount)
    {
        (ClientAchievements.SaveData[Achievements.KinkyTeacher.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(amount);
        (ClientAchievements.SaveData[Achievements.KinkyProfessor.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(amount);
        (ClientAchievements.SaveData[Achievements.KinkyMentor.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(amount);
    }

    private void OnPairVisible()
    {
        // We need to obtain the total visible user count, then update the respective achievements.
        var visiblePairs = _pairs.GetVisibleUserCount();
        (ClientAchievements.SaveData[Achievements.BondageClub.Id] as ThresholdAchievement)?.UpdateThreshold(visiblePairs);
        (ClientAchievements.SaveData[Achievements.Humiliation.Id] as ConditionalThresholdAchievement)?.UpdateThreshold(visiblePairs);
    }

    private void CheckOnZoneSwitchStart(uint prevZone)
    {
        // Nothing yet.
    }

    private void CheckOnZoneSwitchEnd()
    {
        Logger.LogTrace("Current Territory Id: " + PlayerContent.TerritoryID, LoggerType.AchievementEvents);
        if(PlayerContent.InMainCity)
            (ClientAchievements.SaveData[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

        var territory = PlayerContent.TerritoryID;

        // if present in diadem (for diamdem achievement) (Accounts for going into diadem while a vibe is running)
        if (territory is 939 && !PlayerData.IsInPvP)
            (ClientAchievements.SaveData[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        else
            (ClientAchievements.SaveData[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

        // If we left before completing the duty, check that here.
        if ((ClientAchievements.SaveData[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (ClientAchievements.SaveData[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

        if ((ClientAchievements.SaveData[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (ClientAchievements.SaveData[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

        if ((ClientAchievements.SaveData[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (ClientAchievements.SaveData[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
    }

    private void CheckDeepDungeonStatus()
    {
        // Detect Specific Dungeon Types
        if (!Content.InDeepDungeon()) return;

        var floor = Content.GetFloor();
        if (floor is null) 
            return;

        var deepDungeonType = PlayerData.GetDeepDungeonType();
        if (deepDungeonType is null) 
            return;

        if (PlayerData.PartySize is 1)
            (ClientAchievements.SaveData[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
        // start this under any condition.
        (ClientAchievements.SaveData[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();


        switch (deepDungeonType)
        {
            case DeepDungeonType.PalaceOfTheDead:
                if ((floor > 40 && floor <= 50) || (floor > 90 && floor <= 100))
                {
                    (ClientAchievements.SaveData[Achievements.BondagePalace.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    if (floor is 50 || floor is 100)
                        (ClientAchievements.SaveData[Achievements.BondagePalace.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                if(floor is 200)
                {
                    (ClientAchievements.SaveData[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                    (ClientAchievements.SaveData[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                break;
            case DeepDungeonType.HeavenOnHigh:
                if (floor > 20 && floor <= 30)
                {
                    (ClientAchievements.SaveData[Achievements.HornyOnHigh.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    if (floor is 30)
                        (ClientAchievements.SaveData[Achievements.HornyOnHigh.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                if (floor is 100)
                {
                    (ClientAchievements.SaveData[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                    (ClientAchievements.SaveData[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                break;
            case DeepDungeonType.EurekaOrthos:
                if (floor > 20 && floor <= 30)
                {
                    (ClientAchievements.SaveData[Achievements.EurekaWhorethos.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    if (floor is 30)
                        (ClientAchievements.SaveData[Achievements.EurekaWhorethos.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                if (floor is 100)
                {
                    (ClientAchievements.SaveData[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                    (ClientAchievements.SaveData[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                break;
        }
    }

    private void OnDutyStart(object? sender, ushort e)
    {
        Logger.LogInformation("Duty Started", LoggerType.AchievementEvents);
        if (PlayerData.IsInPvP)
            return;

        (ClientAchievements.SaveData[Achievements.KinkyExplorer.Id] as ConditionalAchievement)?.CheckCompletion();

        (ClientAchievements.SaveData[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
        (ClientAchievements.SaveData[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.BeginConditionalTask(25); // 10s delay.

        if (PlayerData.JobRole is ActionRoles.Healer)
            (ClientAchievements.SaveData[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();

        // If the party size is 8, let's check for the trials.
        if(PlayerData.PartySize is 8 && PlayerData.Level >= 90)
        {
            (ClientAchievements.SaveData[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
            (ClientAchievements.SaveData[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
            (ClientAchievements.SaveData[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
        }

        // check stuff for deep dungeons.
        CheckDeepDungeonStatus();
    }

    private void OnDutyEnd(object? sender, ushort e)
    {
        if (PlayerData.IsInPvP)
            return;
        Logger.LogInformation("Duty Ended", LoggerType.AchievementEvents);
        if ((ClientAchievements.SaveData[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (ClientAchievements.SaveData[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();

        if ((ClientAchievements.SaveData[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (ClientAchievements.SaveData[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();

        if ((ClientAchievements.SaveData[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (ClientAchievements.SaveData[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();

        // Trial has ended, check for completion.
        if ((ClientAchievements.SaveData[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
        {
            if (PlayerData.PartySize is 8 && PlayerData.Level >= 90)
                (ClientAchievements.SaveData[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
            else // if we are not in a full party, we should reset the task.
                (ClientAchievements.SaveData[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }
        
        if ((ClientAchievements.SaveData[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
        {
            if (PlayerData.PartySize is 8 && PlayerData.Level >= 90)
                (ClientAchievements.SaveData[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
            else // if we are not in a full party, we should reset the task.
                (ClientAchievements.SaveData[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }
        
        if ((ClientAchievements.SaveData[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
        {
            if (PlayerData.PartySize is 8 && PlayerData.Level >= 90)
                (ClientAchievements.SaveData[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
            else // if we are not in a full party, we should reset the task.
                (ClientAchievements.SaveData[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }

        // check stuff for deep dungeons.
        CheckDeepDungeonStatus();
    }

    private void OnGagStateChanged(int gagLayer, GagType gagAppliedOrRemoved, bool applying, string enactorUid)
    {
        if (_gags.ServerGagData is not { } gagData || gagAppliedOrRemoved is GagType.None)
            return;

        var trackingKey = gagLayer.ToString() + '_' + gagAppliedOrRemoved.GagName();

        // for enables.
        if (applying)
        {
            // the gag was applied to us by ourselves.
            if (enactorUid == MainHub.UID)
            {
                (ClientAchievements.SaveData[Achievements.SelfApplied.Id] as ProgressAchievement)?.IncrementProgress();
            }
            // the gag was applied to us by someone else.
            else
            {
                (ClientAchievements.SaveData[Achievements.SilencedSlut.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.InDeepSilence.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.SilentObsessions.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.GoldenSilence.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.AKinkForDrool.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.ThePerfectGagSlut.Id] as ProgressAchievement)?.IncrementProgress();

                (ClientAchievements.SaveData[Achievements.ATrueGagSlut.Id] as TimedProgressAchievement)?.IncrementProgress();
            }

            // track regardless of who applied it.
            (ClientAchievements.SaveData[Achievements.WhispersToWhimpers.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.OfMuffledMoans.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.SilentStruggler.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.QuietedCaptive.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.MessyDrooler.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.DroolingDiva.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.EmbraceOfSilence.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.SubjugationToSilence.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.SpeechSilverSilenceGolden.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.TheKinkyLegend.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);

            (ClientAchievements.SaveData[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion();
            (ClientAchievements.SaveData[Achievements.GaggedPleasure.Id] as ConditionalAchievement)?.CheckCompletion();
        }
        // for disables.
        else
        {
            (ClientAchievements.SaveData[Achievements.WhispersToWhimpers.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.OfMuffledMoans.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.SilentStruggler.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.QuietedCaptive.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.MessyDrooler.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.DroolingDiva.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.EmbraceOfSilence.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.SubjugationToSilence.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.SpeechSilverSilenceGolden.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (ClientAchievements.SaveData[Achievements.TheKinkyLegend.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);

            // Halt our Silent But Deadly Progress if gag is removed mid-dungeon
            if ((ClientAchievements.SaveData[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
                (ClientAchievements.SaveData[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
        }

        // Update regardless
        (ClientAchievements.SaveData[Achievements.ShushtainableResource.Id] as ThresholdAchievement)?.UpdateThreshold(gagData.TotalGagsEquipped());
    }

    private void OnPairGagStateChanged(int layer, GagType gag, bool applying, string assignerUid, string affectedUid)
    {
        if(applying)
        {
            if (gag is not GagType.None)
            {
                (ClientAchievements.SaveData[Achievements.SilenceSlut.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.WatchYourTongue.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.TongueTamer.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.KinkyLibrarian.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.OrchestratorOfSilence.Id] as ProgressAchievement)?.IncrementProgress();

                (ClientAchievements.SaveData[Achievements.YourFavoriteNurse.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
            }
        }
        else
        {
            // nothing for removing yet.
        }
    }

    private void OnGagLockStateChange(int layer, Padlocks padlock, bool isLocking, string assignerUid)
    {
        if (isLocking)
        {
            // nothing for locking yet.
        }
        else
        {
            // nothing for removing yet.
        }
    }

    private void OnPairGagLockStateChange(int layer, Padlocks padlock, bool isLocking, string assignerUid, string affectedUid)
    {
        if (isLocking)
        {
            // nothing for locking yet.
        }
        else
        {
            // nothing for removing yet.
        }
    }

    private void OnCharaOnlineCleanupForLatest(UserData user, CharaActiveGags gagInfo, CharaActiveRestrictions restrictionsInfo, CharaActiveRestraint restraintInfo)
    {
        var activeGagTrackingKeys = gagInfo.ActiveGagTrackingKeys();
        Logger.LogDebug("Player Character " + user.AliasOrUID + " went online and has new active data. Cleaning up expired information!", LoggerType.AchievementEvents);
        // Do stuff if its a gag type.
        (ClientAchievements.SaveData[Achievements.WhispersToWhimpers.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (ClientAchievements.SaveData[Achievements.OfMuffledMoans.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (ClientAchievements.SaveData[Achievements.SilentStruggler.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (ClientAchievements.SaveData[Achievements.QuietedCaptive.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (ClientAchievements.SaveData[Achievements.MessyDrooler.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (ClientAchievements.SaveData[Achievements.DroolingDiva.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (ClientAchievements.SaveData[Achievements.EmbraceOfSilence.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (ClientAchievements.SaveData[Achievements.SubjugationToSilence.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (ClientAchievements.SaveData[Achievements.SpeechSilverSilenceGolden.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (ClientAchievements.SaveData[Achievements.TheKinkyLegend.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);

        // Checks spesific to the direction of the application.
        if (user.UID == MainHub.UID)
        {
            (ClientAchievements.SaveData[Achievements.FirstTimeBondage.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.AmateurBondage.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.ComfortRestraint.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.YourBondageMaid.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.YourRubberMaid.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.TrainedBondageSlave.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.YourRubberSlut.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.ATrueBondageSlave.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });

            (ClientAchievements.SaveData[Achievements.ShushtainableResource.Id] as ThresholdAchievement)?.UpdateThreshold(gagInfo.TotalGagsEquipped());
        }
        else
        {
            (ClientAchievements.SaveData[Achievements.RiggersFirstSession.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.MyLittlePlaything.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.SuitsYouBitch.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.TiesThatBind.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.SlaveTrainer.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (ClientAchievements.SaveData[Achievements.CeremonyOfEternalBondage.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
        }

        // Do stuff if it is a pattern.
        (ClientAchievements.SaveData[Achievements.ALittleTease.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (ClientAchievements.SaveData[Achievements.ShortButSweet.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (ClientAchievements.SaveData[Achievements.TemptingRythms.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (ClientAchievements.SaveData[Achievements.MyBuildingDesire.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (ClientAchievements.SaveData[Achievements.WithWavesOfSensation.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (ClientAchievements.SaveData[Achievements.WithHeightenedSensations.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (ClientAchievements.SaveData[Achievements.MusicalMoaner.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (ClientAchievements.SaveData[Achievements.StimulatingExperiences.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (ClientAchievements.SaveData[Achievements.EnduranceKing.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (ClientAchievements.SaveData[Achievements.EnduranceQueen.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });

        // if these are started, inturrupt them so that they do not complete.
        (ClientAchievements.SaveData[Achievements.ForcedFollow.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (ClientAchievements.SaveData[Achievements.ForcedWalkies.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
    }

    private void OnRestrictionStateChange(Guid restrictionId, bool isEnabling, string enactorUID)
    {
        // Nothing yet.
    }

    private void OnRestrictionLock(Guid restrictionId, Padlocks padlock, bool isLocking, string enactorUID)
    {
        // Nothing yet.
    }

    private void OnPairRestrictionStateChange(Guid restrictionId, bool isLocking, string enactorUID, string affectedUID)
    {
        // Nothing yet.
    }

    private void OnPairRestrictionLockChange(Guid restrictionId, Padlocks padlock, bool isLocking, string enactorUID, string affectedUID)
    {
        // Nothing yet.
    }


    private void OnRestraintSetUpdated(RestraintSet set)
    {
        // check for dyes
        if (set.GetGlamour().Any(x => x.Value.GameStain != StainIds.None))
        {
            (ClientAchievements.SaveData[Achievements.ToDyeFor.Id] as ProgressAchievement)?.IncrementProgress();
            (ClientAchievements.SaveData[Achievements.DyeAnotherDay.Id] as ProgressAchievement)?.IncrementProgress();
            (ClientAchievements.SaveData[Achievements.DyeHard.Id] as ProgressAchievement)?.IncrementProgress();
        }
    }

    private void OnRestraintStateChange(Guid restraintId, bool isEnabling, string enactorUID)
    {
        // Check this regardless.
        (ClientAchievements.SaveData[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

        // Set is being enabled.
        if (isEnabling)
        {
            (ClientAchievements.SaveData[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion();
            (ClientAchievements.SaveData[Achievements.FirstTiemers.Id] as ProgressAchievement)?.IncrementProgress();

            // if we are the applier
            if (enactorUID == MainHub.UID)
            {
                (ClientAchievements.SaveData[Achievements.SelfBondageEnthusiast.Id] as ProgressAchievement)?.IncrementProgress();
            }
            else // someone else is enabling our set
            {
                (ClientAchievements.SaveData[Achievements.AuctionedOff.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                // starts the timer.
                (ClientAchievements.SaveData[Achievements.Bondodge.Id] as TimeLimitConditionalAchievement)?.StartTask();

                // track overkill if it is not yet completed
                if((ClientAchievements.SaveData[Achievements.ExtremeBondageEnjoyer.Id] as ThresholdAchievement)?.IsCompleted is false)
                {
                    if (_restraints.Storage.TryGetRestraint(restraintId, out var match))
                        (ClientAchievements.SaveData[Achievements.ExtremeBondageEnjoyer.Id] as ThresholdAchievement)?.UpdateThreshold(match.GetGlamour().Count());
                }

                // Track Bondage Bunny
                (ClientAchievements.SaveData[Achievements.BondageBunny.Id] as TimedProgressAchievement)?.IncrementProgress();

                // see if valid for "cuffed-19" if it is not yet completed
                if ((ClientAchievements.SaveData[Achievements.Cuffed19.Id] as ProgressAchievement)?.IsCompleted is false)
                {
                    // attempt to retrieve the set from our sets.
                    if (_restraints.Storage.TryGetRestraint(restraintId, out var match))
                        if (match.GetGlamour().Any(glam => glam.Key is EquipSlot.Hands))
                            (ClientAchievements.SaveData[Achievements.Cuffed19.Id] as ProgressAchievement)?.IncrementProgress();
                }
            }
        }
        else // set is being disabled
        {
            // must be removed within limit or wont award.
            (ClientAchievements.SaveData[Achievements.Bondodge.Id] as TimeLimitConditionalAchievement)?.CheckCompletion();

            // If a set is being disabled at all, we should reset our conditionals.
            (ClientAchievements.SaveData[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
            (ClientAchievements.SaveData[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
            (ClientAchievements.SaveData[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

            (ClientAchievements.SaveData[Achievements.ExtremeBondageEnjoyer.Id] as ThresholdAchievement)?.UpdateThreshold(0);
        }
    }

    private void OnRestraintLock(Guid restraintId, Padlocks padlock, bool isLocking, string enactorUID)
    {
        Logger.LogTrace(enactorUID + " is " + (isLocking ? "locking" : "unlocking") + " a set that had the padlock: " + padlock.ToName());
        // we locked our set.
        if (isLocking)
        {
            if (padlock is not Padlocks.None or Padlocks.FiveMinutesPadlock)
            {
                // make sure that someone is locking us up in a set.
                if (true /*enactorUID != MainHub.UID*/)
                {
                    (ClientAchievements.SaveData[Achievements.FirstTimeBondage.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.AmateurBondage.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.ComfortRestraint.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.YourBondageMaid.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.YourRubberMaid.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.TrainedBondageSlave.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.YourRubberSlut.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.ATrueBondageSlave.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                }
            }
        }
        else
        { 
            // if the set is being unlocked, stop progress regardless.
            if (padlock is not Padlocks.None or Padlocks.FiveMinutesPadlock)
            {
                (ClientAchievements.SaveData[Achievements.FirstTimeBondage.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.AmateurBondage.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.ComfortRestraint.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.YourBondageMaid.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.YourRubberMaid.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.TrainedBondageSlave.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.YourRubberSlut.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.ATrueBondageSlave.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
            }
        }
    }

    /// <summary> Whenever we are applying a restraint set to a pair. This is fired in our pair manager once we recieve  </summary>
    private void OnPairRestraintStateChange(Guid setName, bool isEnabling, string enactorUID, string affectedUID)
    {
        Logger.LogTrace(enactorUID + " is "+ (isEnabling ? "applying" : "Removing") + " a set to a pair: " + setName);
        // if we enabled a set on someone else
        if (isEnabling && enactorUID == MainHub.UID)
        {
            (ClientAchievements.SaveData[Achievements.FirstTiemers.Id] as ProgressAchievement)?.IncrementProgress();
            (ClientAchievements.SaveData[Achievements.DiDEnthusiast.Id] as ProgressAchievement)?.IncrementProgress();
            (ClientAchievements.SaveData[Achievements.YourFavoriteNurse.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
        }
    }

    private void OnPairRestraintLockChange(Guid restraintId, Padlocks padlock, bool isLocking, string enactorUID, string affectedPairUID) // uid is self applied if client.
    {
        // May need to figure this for pairs upon connection to validate any actions/unlocks that occured while we were away.
        Logger.LogInformation("Pair Restraint Lock Change: " + padlock.ToName() + " " + isLocking + " " + enactorUID, LoggerType.AchievementEvents);

        // if the pair's set is being locked and it is a timed lock.
        if (isLocking)
        {
            if (padlock is not Padlocks.None or Padlocks.FiveMinutesPadlock) // locking
            {
                // make sure we are the locker before continuing
                if(enactorUID == MainHub.UID)
                {
                    (ClientAchievements.SaveData[Achievements.RiggersFirstSession.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (ClientAchievements.SaveData[Achievements.MyLittlePlaything.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (ClientAchievements.SaveData[Achievements.SuitsYouBitch.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (ClientAchievements.SaveData[Achievements.TiesThatBind.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (ClientAchievements.SaveData[Achievements.SlaveTrainer.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (ClientAchievements.SaveData[Achievements.CeremonyOfEternalBondage.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                }
            }
        }
        if(!isLocking)
        {
            // if the padlock is a timed padlock that we have unlocked, we should stop tracking it from these achievements.
            if (padlock is not Padlocks.None or Padlocks.FiveMinutesPadlock)
            {
                (ClientAchievements.SaveData[Achievements.RiggersFirstSession.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (ClientAchievements.SaveData[Achievements.MyLittlePlaything.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (ClientAchievements.SaveData[Achievements.SuitsYouBitch.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (ClientAchievements.SaveData[Achievements.TiesThatBind.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (ClientAchievements.SaveData[Achievements.SlaveTrainer.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (ClientAchievements.SaveData[Achievements.CeremonyOfEternalBondage.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
            }

            // if we are unlocking in general, increment the rescuer
            (ClientAchievements.SaveData[Achievements.TheRescuer.Id] as ProgressAchievement)?.IncrementProgress();
        }
    }

    private void OnPuppetAccessGiven(PuppetPerms permissionGiven)
    {
        if ((permissionGiven & PuppetPerms.All) != 0)
            (ClientAchievements.SaveData[Achievements.CompleteDevotion.Id] as ProgressAchievement)?.IncrementProgress();

        if ((permissionGiven & PuppetPerms.Alias) != 0)
        {
            // Nothing yet.
        }

        if ((permissionGiven & PuppetPerms.Emotes) != 0)
            (ClientAchievements.SaveData[Achievements.ControlMyBody.Id] as ProgressAchievement)?.IncrementProgress();

        if ((permissionGiven & PuppetPerms.Sit) != 0)
        {
            // Nothing yet.
        }
    }

    private void OnPatternAction(PatternInteractionKind actionType, Guid patternGuid, bool wasAlarm)
    {
        switch (actionType)
        {
            case PatternInteractionKind.Published:
                (ClientAchievements.SaveData[Achievements.MyPleasantriesForAll.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.DeviousComposer.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Downloaded:
                (ClientAchievements.SaveData[Achievements.TasteOfTemptation.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.SeekerOfSensations.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.CravingPleasure.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Liked:
                (ClientAchievements.SaveData[Achievements.GoodVibes.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.DelightfulPleasures.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.PatternLover.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.SensualConnoisseur.Id] as ProgressAchievement)?.IncrementProgress();
                (ClientAchievements.SaveData[Achievements.PassionateAdmirer.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Started:
                if (patternGuid != Guid.Empty)
                {
                    (ClientAchievements.SaveData[Achievements.ALittleTease.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.ShortButSweet.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.TemptingRythms.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.MyBuildingDesire.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.WithWavesOfSensation.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.WithHeightenedSensations.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.MusicalMoaner.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.StimulatingExperiences.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.EnduranceKing.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (ClientAchievements.SaveData[Achievements.EnduranceQueen.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);

                    // motivation for restoration: Unlike the DutyStart check, this accounts for us starting a pattern AFTER entering Diadem.
                    if(PlayerContent.TerritoryID is 939 && (ClientAchievements.SaveData[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.TaskStarted is false)
                        (ClientAchievements.SaveData[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.StartTask();
                }
                if (wasAlarm && patternGuid != Guid.Empty)
                    (ClientAchievements.SaveData[Achievements.HornyMornings.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Stopped:
                if (patternGuid != Guid.Empty)
                    (ClientAchievements.SaveData[Achievements.ALittleTease.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.ShortButSweet.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.TemptingRythms.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.MyBuildingDesire.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.WithWavesOfSensation.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.WithHeightenedSensations.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.MusicalMoaner.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.StimulatingExperiences.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.EnduranceKing.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (ClientAchievements.SaveData[Achievements.EnduranceQueen.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                // motivation for restoration:
                (ClientAchievements.SaveData[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
                return;
        }
    }

    private void OnDeviceConnected()
    {
        (ClientAchievements.SaveData[Achievements.CollectorOfSinfulTreasures.Id] as ConditionalAchievement)?.CheckCompletion();
    }

    private void OnTriggerFired()
    {
        (ClientAchievements.SaveData[Achievements.SubtleReminders.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.LostInTheMoment.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.TriggerHappy.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void ClientHardcoreFollowChanged(string enactorUID, bool newState)
    {
        Logger.LogDebug("We just had another pair set our ForceFollow to " + newState, LoggerType.AchievementInfo);
        // client will always be the affectedUID
        var affectedUID = MainHub.UID;

        // if the new state is enabled, we need to begin tracking on the relevant achievements.
        if (newState)
        {
            // begin tracking for the world tour. (if we dont meet all conditions it wont start anyways so dont worry about it.
            (ClientAchievements.SaveData[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

            // Begin tracking for the walkies achievements.
            (ClientAchievements.SaveData[Achievements.TimeForWalkies.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (ClientAchievements.SaveData[Achievements.GettingStepsIn.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (ClientAchievements.SaveData[Achievements.WalkiesLover.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }
        // if anyone is disabling us, run a completion check. Failure to meet required time will result in resetting the task.
        else
        {
            // halt tracking for walk of shame if any requirements are no longer met.
            (ClientAchievements.SaveData[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

            // halt any tracking on walkies achievement.
            (ClientAchievements.SaveData[Achievements.TimeForWalkies.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (ClientAchievements.SaveData[Achievements.GettingStepsIn.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (ClientAchievements.SaveData[Achievements.WalkiesLover.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

            // halt progress on being bound throughout a duty if forcedFollow is disabled at any point.
            (ClientAchievements.SaveData[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }
    }

    private void PairHardcoreFollowChanged(string enactorUID, string affectedUID, bool newState)
    {
        Logger.LogDebug("You have set a pairs forcedFollow to " + newState, LoggerType.AchievementInfo);
        // Check to see if we are the one toggling this or if it was someone else.
        var enactorWasSelf = enactorUID == MainHub.UID;

        // if the new state is enabled but we are not the enactor, we should ignore startTracking period.
        if (newState)
        {
            // dont allow tracking for the enabled state by any pairs that are not us.
            if (!enactorWasSelf)
            {
                Logger.LogDebug("We should not be tracking hardcore achievements for any pairs that we are not directly applying hardcore actions to!", LoggerType.AchievementInfo);
                return;
            }

            // Handle tracking for all achievements that we need to initialize the follow command on another pair for.
            (ClientAchievements.SaveData[Achievements.AllTheCollarsOfTheRainbow.Id] as ProgressAchievement)?.IncrementProgress();

            // Handle the tracking start for the pair we just forced to follow, using our affectedUID as the item to track.
            // (We do this so that if another pair enacts the disable we still remove it.)
            (ClientAchievements.SaveData[Achievements.ForcedFollow.Id] as DurationAchievement)?.StartTracking(affectedUID, affectedUID);
            (ClientAchievements.SaveData[Achievements.ForcedWalkies.Id] as DurationAchievement)?.StartTracking(affectedUID, affectedUID);
        }
        else
        {
            // it doesn't madder who the enactor was, we should halt tracking for any progress made once that pair is disabled.
            (ClientAchievements.SaveData[Achievements.ForcedFollow.Id] as DurationAchievement)?.StopTracking(affectedUID, affectedUID);
            (ClientAchievements.SaveData[Achievements.ForcedWalkies.Id] as DurationAchievement)?.StopTracking(affectedUID, affectedUID);
        }
    }

    private void ClientHardcoreEmoteStateChanged(string enactorUID, bool newState)
    {
        Logger.LogDebug("We just had another pair set our ForceEmote to " + newState, LoggerType.AchievementInfo);
        // client will always be the affectedUID
        var affectedUID = MainHub.UID;

        if (newState)
        {
            (ClientAchievements.SaveData[Achievements.LivingFurniture.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }
        else
        {
            (ClientAchievements.SaveData[Achievements.LivingFurniture.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
        }
    }

    private void PairHardcoreEmoteChanged(string enactorUID, string affectedUID, bool newState)
    {
        // Nothing here currently.
    }

    private void ClientHardcoreStayChanged(string enactorUID, bool newState)
    {
        Logger.LogDebug("We just had another pair set our ForceStay to " + newState, LoggerType.AchievementInfo);
        // client will always be the affectedUID
        var affectedUID = MainHub.UID;

        // and we have been ordered to start being forced to stay:
        if (newState)
        {
            (ClientAchievements.SaveData[Achievements.OfDomesticDiscipline.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (ClientAchievements.SaveData[Achievements.HomeboundSubmission.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (ClientAchievements.SaveData[Achievements.PerfectHousePet.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }
        else
        {
            (ClientAchievements.SaveData[Achievements.OfDomesticDiscipline.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (ClientAchievements.SaveData[Achievements.HomeboundSubmission.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (ClientAchievements.SaveData[Achievements.PerfectHousePet.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
        }
    }

    private void PairHardcoreStayChanged(string enactorUID, string affectedUID, bool newState)
    {
        // Nothing currently.
    }

    private void ClientHardcoreBlindfoldChanged(string enactorUID, bool newState)
    {
        Logger.LogDebug("We just had another pair set our ForceBlindfold to " + newState, LoggerType.AchievementInfo);
        // client will always be the affectedUID
        var affectedUID = MainHub.UID;

        if (newState)
        {
            // always check if walk of shame can be started.
            (ClientAchievements.SaveData[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

            // Check for conditional task.
            (ClientAchievements.SaveData[Achievements.BlindLeadingTheBlind.Id] as ConditionalAchievement)?.CheckCompletion();

            // Startup timed ones.
            (ClientAchievements.SaveData[Achievements.WhoNeedsToSee.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }
        else
        {
            (ClientAchievements.SaveData[Achievements.WhoNeedsToSee.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

            // stop walk of shame since one of its requirements are not fulfilled.
            (ClientAchievements.SaveData[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.InterruptTask();
        }
    }

    private void PairHardcoreBlindfoldChanged(string enactorUID, string affectedUID, NewState newState)
    {
        // Nothing currently.
    }

    /// <summary>
    /// For whenever a hardcore action is enabled or disabled. This can come from client change or pair change, so look out for differences.
    /// </summary>
    /// <param name="actionKind"> The kind of hardcore action that was performed. </param>
    /// <param name="state"> If the hardcore action began or ended. </param>
    /// <param name="affectedPairUID"> who the target of the action is. </param>
    /// <param name="enactorUID"> Who Called the action. </param>
    private void OnHardcoreAction(HardcoreSetting actionKind, bool state, string enactorUID, string affectedPairUID)
    {
        Logger.LogDebug("Hardcore Action: " + actionKind + " State: " + state + " Enactor: " + enactorUID + " Affected: " + affectedPairUID, LoggerType.AchievementInfo);
        
        var affectedPairIsSelf = affectedPairUID == MainHub.UID;

        if (actionKind is HardcoreSetting.ForcedFollow)
        {
            if (affectedPairIsSelf) ClientHardcoreFollowChanged(enactorUID, state);
            else PairHardcoreFollowChanged(enactorUID, affectedPairUID, state);
        }
        else if (actionKind is HardcoreSetting.ForcedEmote)
        {
            if (affectedPairIsSelf) ClientHardcoreEmoteStateChanged(enactorUID, state);
            else PairHardcoreEmoteChanged(enactorUID, affectedPairUID, state);
        }
        else if (actionKind is HardcoreSetting.ForcedStay)
        {
            if (affectedPairIsSelf) ClientHardcoreStayChanged(enactorUID, state);
            else PairHardcoreStayChanged(enactorUID, affectedPairUID, state);
        }
    }

    private void OnShockSent()
    {
        (ClientAchievements.SaveData[Achievements.IndulgingSparks.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.ShockingTemptations.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.TheCrazeOfShockies.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.WickedThunder.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.ElectropeHasNoLimits.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnShockReceived()
    {
        (ClientAchievements.SaveData[Achievements.ElectrifyingPleasure.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.ShockingExperience.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.WiredForObedience.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.ShockAddiction.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.SlaveToTheShock.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.ShockSlut.Id] as ProgressAchievement)?.IncrementProgress();
    }


    private void OnChatMessage(InputChannel channel)
    {
        (ClientAchievements.SaveData[Achievements.HelplessDamsel.Id] as ConditionalAchievement)?.CheckCompletion();

        if (channel is InputChannel.Say)
        {
            (ClientAchievements.SaveData[Achievements.OfVoicelessPleas.Id] as ProgressAchievement)?.IncrementProgress();
            (ClientAchievements.SaveData[Achievements.DefianceInSilence.Id] as ProgressAchievement)?.IncrementProgress();
            (ClientAchievements.SaveData[Achievements.MuffledResilience.Id] as ProgressAchievement)?.IncrementProgress();
            (ClientAchievements.SaveData[Achievements.TrainedInSubSpeech.Id] as ProgressAchievement)?.IncrementProgress();

        }
        else if (channel is InputChannel.Yell)
        {
            (ClientAchievements.SaveData[Achievements.PublicSpeaker.Id] as ProgressAchievement)?.IncrementProgress();
        }
        else if (channel is InputChannel.Shout)
        {
            (ClientAchievements.SaveData[Achievements.FromCriesOfHumility.Id] as ProgressAchievement)?.IncrementProgress();
        }
    }

    private void OnEmoteExecuted(IGameObject emoteCallerObj, ushort emoteId, IGameObject targetObject)
    {
        switch (emoteId)
        {
            case 22: // Lookout
                if(emoteCallerObj.ObjectIndex is 0)
                    (ClientAchievements.SaveData[Achievements.WhatAView.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 36: // Stagger
                if (emoteCallerObj.ObjectIndex is 0)
                    (ClientAchievements.SaveData[Achievements.VulnerableVibrations.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 105: // Stroke
                if (emoteCallerObj.ObjectIndex is 0 && targetObject.ObjectIndex is 0)
                    (ClientAchievements.SaveData[Achievements.ProlificPetter.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 111: // Slap
                if (emoteCallerObj.ObjectIndex is not 0 && targetObject.ObjectIndex is 0)
                    (ClientAchievements.SaveData[Achievements.ICantBelieveYouveDoneThis.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 146: //Dote
                if (emoteCallerObj.ObjectIndex is 0 && targetObject.ObjectIndex is not 0)
                    (ClientAchievements.SaveData[Achievements.WithAKissGoodbye.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 231:
                if (emoteCallerObj.ObjectIndex is 0)
                {
                    (ClientAchievements.SaveData[Achievements.QuietNowDear.Id] as ConditionalAchievement)?.CheckCompletion();
                }
                else if (emoteCallerObj.ObjectIndex is not 0 && targetObject.ObjectIndex is 0)
                {
                    (ClientAchievements.SaveData[Achievements.SilenceOfShame.Id] as ConditionalAchievement)?.CheckCompletion();
                }
                else
                {
                    break;
                }
                break;

        }
    }

    private void OnPuppeteerOrderSent(PuppeteerMsgType orderType)
    {
        switch(orderType)
        {
            case PuppeteerMsgType.GrovelOrder:
                (ClientAchievements.SaveData[Achievements.KissMyHeels.Id] as ProgressAchievement)?.IncrementProgress();
                break;

            case PuppeteerMsgType.DanceOrder:
                (ClientAchievements.SaveData[Achievements.AMaestroOfMyProperty.Id] as ProgressAchievement)?.IncrementProgress();
                break;
        }
        // Increase regardless.
        (ClientAchievements.SaveData[Achievements.MasterOfPuppets.Id] as TimedProgressAchievement)?.IncrementProgress();
        // inc the orders given counters.
        (ClientAchievements.SaveData[Achievements.OrchestratorsApprentice.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.NoStringsAttached.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.PuppetMaster.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.MasterOfManipulation.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.TheGrandConductor.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.MaestroOfStrings.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.OfGrandiousSymphony.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.SovereignMaestro.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.OrchestratorOfMinds.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnPuppeteerReceivedOrder()
    {
        // inc the orders recieved counters.
        (ClientAchievements.SaveData[Achievements.WillingPuppet.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.AtYourCommand.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.YourMarionette.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.TheInstrument.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.AMannequinsMadness.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.DevotedDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.EnthralledDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.ObedientDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.ServiceDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.MastersPlaything.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.MistressesPlaything.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.ThePerfectDoll.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnPuppeteerReceivedEmoteOrder(int emoteId)
    {
        switch(emoteId)
        {
            case 38: // Sulk
                (ClientAchievements.SaveData[Achievements.Ashamed.Id] as ProgressAchievement)?.IncrementProgress();
                break;
            case 50: // Sit/Groundsit
            case 52:
                (ClientAchievements.SaveData[Achievements.AnObedientPet.Id] as ProgressAchievement)?.IncrementProgress();
                break;
            case 223: //Sweep
                (ClientAchievements.SaveData[Achievements.HouseServant.Id] as ProgressAchievement)?.IncrementProgress();
                break;
        }
    }

    private void OnPairAdded()
    {
        (ClientAchievements.SaveData[Achievements.KinkyNovice.Id] as ConditionalAchievement)?.CheckCompletion();
        (ClientAchievements.SaveData[Achievements.TheCollector.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnCursedLootFound()
    {
        (ClientAchievements.SaveData[Achievements.TemptingFatesTreasure.Id] as ProgressAchievement)?.IncrementProgress();
        (ClientAchievements.SaveData[Achievements.BadEndSeeker.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
        (ClientAchievements.SaveData[Achievements.EverCursed.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
    }

    private void OnJobChange(uint newJobId)
    {
        (ClientAchievements.SaveData[Achievements.EscapingIsNotEasy.Id] as ConditionalAchievement)?.CheckCompletion();
    }

    private void OnVibratorToggled(bool newState)
    {
        if (newState)
        {
            (ClientAchievements.SaveData[Achievements.GaggedPleasure.Id] as ConditionalAchievement)?.CheckCompletion();
            (ClientAchievements.SaveData[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion();
        }
        else
        {

        }
    }

    private void OnPvpKill()
    {
        (ClientAchievements.SaveData[Achievements.EscapedPatient.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (ClientAchievements.SaveData[Achievements.BoundToKill.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (ClientAchievements.SaveData[Achievements.TheShackledSlayer.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (ClientAchievements.SaveData[Achievements.DangerousConvict.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (ClientAchievements.SaveData[Achievements.OfUnyieldingForce.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (ClientAchievements.SaveData[Achievements.StimulationOverdrive.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (ClientAchievements.SaveData[Achievements.BoundYetUnbroken.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (ClientAchievements.SaveData[Achievements.ChainsCantHoldMe.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
    }


    //// We need to check for knockback effects in gold sacuer. (Turned off for now, action effects not currently binded to events.
    //private void OnActionEffectEvent(List<ActionEffectEntry> actionEffects)
    //{

    //    // Check if client player is null
    //    if (!PlayerData.IsPresent)
    //        return;

    //    // Return if not in the gold saucer
    //    if (PlayerContent.TerritoryID is not 144)
    //        return;

    //    // Check if the GagReflex achievement is already completed
    //    var gagReflexAchievement = ClientAchievements.SaveData[Achievements.GagReflex.Id] as ProgressAchievement;
    //    if (gagReflexAchievement is null || gagReflexAchievement.IsCompleted)
    //    {
    //        Logger.LogInformation("GagReflex achievement is already completed or is null");
    //        return;
    //    }

    //    Logger.LogInformation("Current State: [GateDirectorValid]: " + Content.GateDirectorIsValid 
    //        + " [GateType]: " + Content.GetActiveGate()
    //        + " [Flags]: " + Content.GetGateFlags()
    //        + " [InGateWithKB] " + Content.IsInGateWithKnockback());

    //    // Check if the player is in a gate with knockback
    //    if (!Content.IsInGateWithKnockback())
    //    {
    //        Logger.LogInformation("Player is not in a gate with knockback");
    //        return;
    //    }

    //    // Check if any effects were a knockback effect targeting the local player
    //    if (actionEffects.Any(x => x.Type == LimitedActionEffectType.Knockback && x.TargetID == PlayerData.ObjectId))
    //    {
    //        // Increment progress if the achievement is not yet completed
    //        gagReflexAchievement.IncrementProgress();
    //    }
    //}
}
