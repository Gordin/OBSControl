﻿using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
#nullable enable
[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace OBSControl
{
    internal class PluginConfig
    {
        [UIValue(nameof(Enabled))]
        public virtual bool Enabled { get; set; } = true;
        [UIValue(nameof(ServerAddress))]
        public virtual string? ServerAddress { get; set; } = "ws://127.0.0.1:4444";
        [UIValue(nameof(ServerPassword))]
        public virtual string? ServerPassword { get; set; } = string.Empty;
        [UIValue(nameof(EnableAutoRecord))]
        public virtual bool EnableAutoRecord { get; set; } = true;
        [UIValue(nameof(LevelStartDelay))]
        public virtual float LevelStartDelay
        {
            get => _levelStartDelay;
            set
            {
                if (value < 0)
                    value = 0;
                _levelStartDelay = (float)Math.Round(value, 1);
            }
        }
        [UIValue(nameof(RecordingStopDelay))]
        public virtual float RecordingStopDelay
        {
            get => _recordingStopDelay;
            set
            {
                if (value < 0)
                    value = 0;
                _recordingStopDelay = (float)Math.Round(value, 1);
            }
        }

        [UIValue(nameof(AutoStopOnManual))]
        public virtual bool AutoStopOnManual { get; set; } = true;

        [UIValue(nameof(RecordingFileFormat))]
        public virtual string? RecordingFileFormat { get; set; } = "?N{20}-?A{20}_?%<_[?M]><-?F><-?e>";

        [UIValue(nameof(ReplaceSpacesWith))]
        public virtual string? ReplaceSpacesWith { get; set; } = "_";

        [UIValue(nameof(InvalidCharacterSubstitute))]
        public virtual string? InvalidCharacterSubstitute { get; set; } = "_";

        [UIValue(nameof(StartSceneDuration))]
        public virtual float StartSceneDuration
        {
            get => _startSceneDuration;
            set
            {
                if (value < 0)
                    value = 0;
                _startSceneDuration = (float)Math.Round(value, 1);
            }
        }

        private float _endSceneStartDelay;
        [UIValue(nameof(EndSceneStartDelay))]
        public float EndSceneStartDelay
        {
            get { return _endSceneStartDelay; }
            set
            {
                if (value < 0)
                    value = 0;
                _endSceneStartDelay = (float)Math.Round(value, 1);
            }
        }

        [UIValue(nameof(EndSceneDuration))]
        public virtual float EndSceneDuration
        {
            get => _endSceneDuration;
            set
            {
                if (value < 0)
                    value = 0;
                _endSceneDuration = (float)Math.Round(value, 1);
            }
        }
        [NonNullable]
        [UIValue(nameof(StartSceneName))]
        public virtual string StartSceneName { get; set; } = string.Empty;
        [NonNullable]
        [UIValue(nameof(GameSceneName))]
        public virtual string GameSceneName { get; set; } = string.Empty;
        [NonNullable]
        [UIValue(nameof(EndSceneName))]
        public virtual string EndSceneName { get; set; } = string.Empty;

        [NonNullable]
        [UIValue(nameof(RestingSceneName))]
        public virtual string RestingSceneName { get; set; } = string.Empty;

        [NonNullable]
        public virtual string MaterialName { get; set; } = string.Empty;

        [NonNullable]
        public virtual string ShaderName { get; set; } = string.Empty;
        [NonNullable]
        public virtual string ColorName { get; set; } = string.Empty;
        [NonNullable]
        public virtual float ColorAlpha { get; set; } = 1f;

        #region Floating Screen
        public virtual float ScreenPosX { get; set; } = 0f;
        public virtual float ScreenPosY { get; set; } = 2.9f;
        public virtual float ScreenPosZ { get; set; } = 2.4f;
        public virtual float ScreenRotX { get; set; } = -30f;
        public virtual float ScreenRotY { get; set; } = 0f;
        public virtual float ScreenRotZ { get; set; } = 0f;
        public virtual bool ShowScreenHandle { get; set; } = true;

        #endregion

        public virtual float ObsTimeout { get; set; } = 5000f;

        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            TryAddCurrentNames(StartSceneName, GameSceneName, EndSceneName, RestingSceneName);
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                Plugin.instance.SetThings(MaterialName, ShaderName, ColorName, ColorAlpha);
            });
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
            TryAddCurrentNames(StartSceneName, GameSceneName, EndSceneName, RestingSceneName);
            RefreshDropdowns();
            OBSController.instance?.gameObject.SetActive(Enabled);
        }

#pragma warning disable CS8603 // Possible null reference return.
        /// <summary>
        /// Call this when you want to do multiple changes before saving the file, dispose to save.
        /// </summary>
        /// <returns></returns>
        public virtual IDisposable ChangeTransaction() => null;
#pragma warning restore CS8603 // Possible null reference return.

        public void UpdateSceneOptions(IEnumerable<string> newOptions)
        {
            SceneSelectOptions.Clear();
            SceneSelectOptions.Add(string.Empty);
            SceneSelectOptions.AddRange(newOptions);
            TryAddCurrentNames(StartSceneName, GameSceneName, EndSceneName, RestingSceneName);
            RefreshDropdowns();
        }

        private void TryAddCurrentNames(params string[]? sceneNames)
        {
            if (sceneNames == null) return;
            foreach (string name in sceneNames)
            {
                if (!SceneSelectOptions.Contains(name))
                    SceneSelectOptions.Add(name);
            }
        }

        public void RefreshDropdowns()
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            DropDownListSetting[]? dropDowns = new DropDownListSetting[] { StartSceneDropDown, GameSceneDropdown, EndSceneDropdown, RestingSceneDropdown };
#pragma warning restore CS8601 // Possible null reference assignment.
            foreach (DropDownListSetting dropDown in dropDowns)
            {
                if (dropDown != null)
                {
                    dropDown.tableView.ReloadData();
                    dropDown.ReceiveValue();
                }
            }
        }
        [UIAction("formatter-seconds")]
        public string floatToSeconds(float val)
        {
            return $"{Math.Round(val, 1)}s";
        }
        [Ignore]
        [UIValue("SceneSelectOptions")]
        public List<object> SceneSelectOptions = new List<object>() { string.Empty };

        [Ignore]
        [UIComponent("StartSceneDropdown")]
        public DropDownListSetting? StartSceneDropDown;
        [Ignore]
        [UIComponent("GameSceneDropdown")]
        public DropDownListSetting? GameSceneDropdown;
        [Ignore]
        [UIComponent("EndSceneDropdown")]
        public DropDownListSetting? EndSceneDropdown;
        [Ignore]
        [UIComponent("RestingSceneDropdown")]
        public DropDownListSetting? RestingSceneDropdown;

        #region Backing Fields
        private float _levelStartDelay = 3f;
        private float _recordingStopDelay = 4f;
        private float _startSceneDuration = 1f;
        private float _endSceneDuration = 2f;
        #endregion
    }

    internal static class ConfigExtensions
    {
        public static Vector3 GetScreenPosition(this PluginConfig config)
        {
            return new Vector3(config.ScreenPosX, config.ScreenPosY, config.ScreenPosZ);
        }
        public static Quaternion GetScreenRotation(this PluginConfig config)
        {
            return Quaternion.Euler(config.ScreenRotX, config.ScreenRotY, config.ScreenRotZ);
        }
    }
}
