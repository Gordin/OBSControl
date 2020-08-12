﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Utilities;
using HarmonyLib;
using UnityEngine.SceneManagement;
using UnityEngine;
using OBSControl.OBSComponents;
using IPALogger = IPA.Logging.Logger;
using BeatSaberMarkupLanguage.Settings;
using OBSControl.UI;
#nullable enable
namespace OBSControl
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        internal static Plugin instance;
        internal static PluginConfig config;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        internal static string Name => "OBSControl";
        internal static bool Enabled;

        [Init]
        public void Init(IPALogger logger, Config conf)
        {
            instance = this;
            Logger.log = logger;
            Logger.log?.Debug("Logger initialized.");
            config = conf.Generated<PluginConfig>();
            OBSWebsocketDotNet.OBSLogger.SetLogger(new OBSLogger());
            BSMLSettings.instance.AddSettingsMenu("OBSControl", "OBSControl.UI.SettingsView.bsml", config);
            BS_Utils.Utilities.BSEvents.lateMenuSceneLoadedFresh += BSEvents_lateMenuSceneLoadedFresh;
        }

        private void BSEvents_lateMenuSceneLoadedFresh(ScenesTransitionSetupDataSO obj)
        {
            Logger.log?.Warn("Creating control screen.");
            ControlScreenCoordinator.Instance.ShowControlScreen();
        }
        #region IDisablable

        /// <summary>
        /// Called when the plugin is enabled (including when the game starts if the plugin is enabled).
        /// </summary>
        [OnEnable]
        public void OnEnable()
        {
            //config.Value.FillDefaults();
            Logger.log?.Debug("OnEnable()");
            new GameObject("OBSControl_OBSController").AddComponent<OBSController>();
            new GameObject("OBSControl_RecordingController").AddComponent<RecordingController>();
            new GameObject("OBSControl_StreamingController").AddComponent<StreamingController>();
            ApplyHarmonyPatches();
            Enabled = true;
        }

        /// <summary>
        /// Called when the plugin is disabled. It is important to clean up any Harmony patches, GameObjects, and Monobehaviours here.
        /// The game should be left in a state as if the plugin was never started.
        /// </summary>
        [OnDisable]
        public void OnDisable()
        {
            Logger.log?.Debug("OnDisable()");
            RemoveHarmonyPatches();
            GameObject.Destroy(OBSController.instance?.gameObject);
            GameObject.Destroy(RecordingController.instance?.gameObject);
            Enabled = false;
        }
        #endregion

        /// <summary>
        /// Attempts to apply all the Harmony patches in this assembly.
        /// </summary>
        public static void ApplyHarmonyPatches()
        {
            HarmonyPatches.HarmonyManager.ApplyDefaultPatches();
        }

        /// <summary>
        /// Attempts to remove all the Harmony patches that used our HarmonyId.
        /// </summary>
        public static void RemoveHarmonyPatches()
        {
            // Removes all patches with this HarmonyId
            HarmonyPatches.HarmonyManager.UnpatchAll();
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Logger.log?.Debug("OnApplicationQuit");
            if (RecordingController.instance != null)
                GameObject.Destroy(RecordingController.instance);
            if (OBSController.instance != null)
                GameObject.Destroy(OBSController.instance);
        }
    }
}
