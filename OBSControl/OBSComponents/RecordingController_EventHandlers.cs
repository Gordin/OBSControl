﻿using OBSControl.HarmonyPatches;
using OBSControl.Wrappers;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
#nullable enable

namespace OBSControl.OBSComponents
{
    public partial class RecordingController
    {
        /// <summary>
        /// Event handler for <see cref="StartLevelPatch.LevelStarting"/>.
        /// Sets a level start delay if using <see cref="RecordStartOption.LevelStartDelay"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLevelStarting(object sender, LevelStartingEventArgs e)
        {
            RecordStartOption recordStartOption = RecordStartOption;
            Logger.log?.Debug($"RecordingController OnLevelStarting. StartOption is {recordStartOption}");
            switch (recordStartOption)
            {
                case RecordStartOption.None:
                    break;
                case RecordStartOption.SceneSequence:
                    break;
                case RecordStartOption.SongStart:
                    break;
                case RecordStartOption.LevelStartDelay:
                    e.SetResponse(LevelStartingSourceName, (int)(RecordingStartDelay * 1000));
                    break;
                case RecordStartOption.Immediate:
                    break;
                default:
                    break;
            }
        }

        private async void OnLevelStart(object sender, LevelStartEventArgs e)
        {
            RecordStartOption recordStartOption = RecordStartOption;
            switch (e.StartResponseType)
            {
                case LevelStartResponse.None:
                    break;
                case LevelStartResponse.Immediate:
                    break;
                case LevelStartResponse.Delayed:
                    break;
                case LevelStartResponse.Handled:
                    if (recordStartOption == RecordStartOption.SceneSequence)
                        return;
                    break;
                default:
                    break;
            }
            if (recordStartOption == RecordStartOption.LevelStartDelay || recordStartOption == RecordStartOption.Immediate)
            {
                await TryStartRecordingAsync(RecordActionSourceType.Auto, recordStartOption).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Event handler for <see cref="SceneController.SceneStageChanged"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSceneStageChanged(object sender, SceneStageChangedEventArgs e)
        {
#if DEBUG
            Logger.log?.Debug($"RecordingController: OnSceneStageChanged - {e.SceneStage}.");
#endif
            e.AddCallback(SceneSequenceCallback);
        }
        #region Game Event Handlers

        /// <summary>
        /// Triggered after song ends, but before transition out of game scene.
        /// </summary>
        /// <param name="levelScenesTransitionSetupDataSO"></param>
        /// <param name="levelCompletionResults"></param>
        private async void OnLevelFinished(StandardLevelScenesTransitionSetupDataSO levelScenesTransitionSetupDataSO, LevelCompletionResults levelCompletionResults)
        {
            Logger.log?.Debug($"RecordingController OnLevelFinished: {SceneManager.GetActiveScene().name}.");
            bool multipleLevelData = LastLevelData?.LevelResults != null || (LastLevelData?.MultipleLastLevels ?? false) == true;
            try
            {
                PlayerLevelStatsData? stats = null;
                IBeatmapLevel? levelInfo = GameStatus.LevelInfo;
                IDifficultyBeatmap? difficultyBeatmap = GameStatus.DifficultyBeatmap;
                PlayerDataModel? playerData = OBSController.instance?.PlayerData;
                if (playerData != null && levelInfo != null && difficultyBeatmap != null)
                {
                    stats = playerData.playerData.GetPlayerLevelStatsData(
                        levelInfo.levelID, difficultyBeatmap.difficulty, difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic);
                }

                LevelCompletionResultsWrapper levelResults = new LevelCompletionResultsWrapper(levelCompletionResults, stats?.playCount ?? 0, GameStatus.MaxModifiedScore);
                RecordingData? recordingData = LastLevelData;
                if (recordingData == null)
                {
                    recordingData = new RecordingData(new BeatmapLevelWrapper(difficultyBeatmap), levelResults, stats)
                    {
                        MultipleLastLevels = multipleLevelData
                    };
                    LastLevelData = recordingData;
                }
                else
                {
                    if (recordingData.LevelData == null)
                    {
                        recordingData.LevelData = new BeatmapLevelWrapper(difficultyBeatmap);
                    }
                    else if (difficultyBeatmap != null && recordingData.LevelData.DifficultyBeatmap != difficultyBeatmap)
                    {
                        Logger.log?.Debug($"Existing beatmap data doesn't match level completion beatmap data: '{recordingData.LevelData.SongName}' != '{difficultyBeatmap?.level.songName}'");
                        recordingData.LevelData = new BeatmapLevelWrapper(difficultyBeatmap);
                    }
                    recordingData.LevelResults = levelResults;
                    recordingData.PlayerLevelStats = stats;
                    recordingData.MultipleLastLevels = multipleLevelData;
                }

            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Error generating new file name: {ex}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
            if (RecordStopOption == RecordStopOption.SongEnd)
            {
                TimeSpan stopDelay = TimeSpan.FromSeconds(Plugin.config?.RecordingStopDelay ?? 0);
                if (stopDelay > TimeSpan.Zero)
                    await Task.Delay(stopDelay);
                StopRecordingTask = TryStopRecordingAsync();
            }
        }

        private async void OnGameSceneActive()
        {
            Logger.log?.Debug($"RecordingController OnGameSceneActive.");
            StartCoroutine(GameStatusSetup());
            if (RecordStartOption == RecordStartOption.SongStart)
            {
                await TryStartRecordingAsync(RecordActionSourceType.Auto, RecordStartOption.SongStart).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Triggered after transition out of game scene.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="_"></param>
        public async void OnLevelDidFinish(object sender, EventArgs _)
        {
            Logger.log?.Debug($"RecordingController OnLevelDidFinish: {SceneManager.GetActiveScene().name}.");
            if (RecordStopOption == RecordStopOption.ResultsView)
            {
                TimeSpan stopDelay = TimeSpan.FromSeconds(Plugin.config?.RecordingStopDelay ?? 0);
                if (stopDelay > TimeSpan.Zero)
                    await Task.Delay(stopDelay);
                StopRecordingTask = TryStopRecordingAsync();
            }
        }
        #endregion


        #region OBS Event Handlers



        private void OnObsRecordingStateChanged(object sender, OutputState type)
        {
            Logger.log?.Info($"Recording State Changed: {type}");
            OutputState = type;
            LastRecordingStateUpdate = DateTime.UtcNow;
            switch (type)
            {
                case OutputState.Starting:
                    recordingCurrentLevel = true;
                    break;
                case OutputState.Started:
                    RecordStartTime = DateTime.UtcNow;
                    recordingCurrentLevel = true;
                    if (RecordStartSource == RecordActionSourceType.None)
                    {
                        RecordStartSource = RecordActionSourceType.ManualOBS;
                        RecordStartOption = RecordStartOption.None;
                    }
                    Task.Run(() => Obs.GetConnectedObs()?.SetFilenameFormatting(DefaultFileFormat));
                    break;
                case OutputState.Stopping:
                    recordingCurrentLevel = false;
                    break;
                case OutputState.Stopped:
                    recordingCurrentLevel = false;
                    RecordStartTime = DateTime.MaxValue;
                    RecordingData? lastLevelData = LastLevelData;
                    string? renameOverride = RenameStringOverride;
                    RenameStringOverride = null;
                    LastLevelData = null;
                    RecordStartSource = RecordActionSourceType.None;
                    RecordStartOption = RecordStartOption.None;
                    string? renameString = renameOverride ??
                        lastLevelData?.GetFilenameString(Plugin.config.RecordingFileFormat, Plugin.config.InvalidCharacterSubstitute, Plugin.config.ReplaceSpacesWith);
                    if (renameString != null)
                        RenameLastRecording(renameString);
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}