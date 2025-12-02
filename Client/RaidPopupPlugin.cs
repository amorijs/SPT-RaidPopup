using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RaidPopup.Models;
using RaidPopup.Patches;
using RaidPopup.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RaidPopup
{
    [BepInPlugin("com.raidpopup.client", "RaidPopup", "1.0.0")]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.HardDependency)]
    public class RaidPopupPlugin : BaseUnityPlugin
    {
        public static RaidPopupPlugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        // Config entries (accessible via F12 menu)
        public static ConfigEntry<bool> DebugMode;
        public static ConfigEntry<bool> EnableNotifications;

        /// <summary>
        /// List of currently active raids from other players
        /// </summary>
        public List<ActiveRaid> ActiveRaids { get; private set; } = new List<ActiveRaid>();

        private RaidNotificationPanel _notificationPanel;
        private GameObject _panelObject;
        private Harmony _harmony;
        private bool _initialized = false;
        private bool _patchApplied = false;
        private float _initTimer = 0f;
        private const float INIT_DELAY = 5f;

        // Cached reflection info
        private Type _fikaGlobalsType;
        private PropertyInfo _isInRaidProperty;
        private bool _reflectionCached = false;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // Bind config entries (shows in F12 menu)
            DebugMode = Config.Bind(
                "Debug",
                "Debug Mode",
                false,
                "Show fake raid notifications for UI testing"
            );

            EnableNotifications = Config.Bind(
                "General",
                "Enable Notifications", 
                true,
                "Show raid notification panel when other players start raids"
            );

            // Listen for debug mode changes
            DebugMode.SettingChanged += (sender, args) =>
            {
                if (DebugMode.Value)
                {
                    AddDebugRaids();
                }
                else
                {
                    ClearDebugRaids();
                }
            };

            Log.LogWarning("========================================");
            Log.LogWarning("RaidPopup v1.0.0 - Loaded");
            Log.LogWarning("========================================");

            _harmony = new Harmony("com.raidpopup.client");

            Log.LogInfo("RaidPopup: Waiting for game initialization...");
        }

        private void Update()
        {
            try
            {
                if (!_initialized)
                {
                    _initTimer += Time.deltaTime;
                    if (_initTimer < INIT_DELAY)
                    {
                        return;
                    }
                    
                    Initialize();
                    return;
                }

                if (_notificationPanel == null)
                {
                    TryCreatePanel();
                }

                if (_needsRefresh)
                {
                    _needsRefresh = false;
                    if (_notificationPanel != null)
                    {
                        _notificationPanel.RefreshDisplay();
                    }
                }

                if (Time.frameCount % 30 == 0)
                {
                    CheckRaidState();
                }
            }
            catch (Exception ex)
            {
                if (Time.frameCount % 300 == 0)
                {
                    Log.LogError($"Update error: {ex.Message}");
                }
            }
        }

        private void Initialize()
        {
            try
            {
                Log.LogWarning("RaidPopup: Initializing...");

                CacheReflectionInfo();

                if (!_patchApplied)
                {
                    StartRaidNotificationPatch.ApplyPatch(_harmony);
                    _patchApplied = true;
                }

                if (DebugMode.Value)
                {
                    AddDebugRaids();
                }

                _initialized = true;
                Log.LogWarning("RaidPopup: Ready!");
            }
            catch (Exception ex)
            {
                Log.LogError($"RaidPopup: Failed to initialize: {ex.Message}");
                _initialized = true;
            }
        }

        public void AddDebugRaids()
        {
            Log.LogInfo("RaidPopup: Adding debug raids...");
            
            ActiveRaids.Clear();
            
            ActiveRaids.Add(new ActiveRaid
            {
                Nickname = "TestPlayer1",
                Location = "bigmap",
                RaidTime = JsonType.EDateTime.CURR
            });

            ActiveRaids.Add(new ActiveRaid
            {
                Nickname = "AnotherPlayer",
                Location = "factory4_night",
                RaidTime = JsonType.EDateTime.PAST
            });

            _needsRefresh = true;
        }

        public void ClearDebugRaids()
        {
            ActiveRaids.Clear();
            _needsRefresh = true;
        }

        private void CacheReflectionInfo()
        {
            if (_reflectionCached) return;

            try
            {
                _fikaGlobalsType = AccessTools.TypeByName("Fika.Core.Main.Utils.FikaGlobals");
                if (_fikaGlobalsType != null)
                {
                    _isInRaidProperty = _fikaGlobalsType.GetProperty("IsInRaid", BindingFlags.Static | BindingFlags.Public);
                }
                _reflectionCached = true;
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to cache reflection info: {ex.Message}");
            }
        }

        private bool _wasInRaid = false;

        private void CheckRaidState()
        {
            bool currentlyInRaid = IsInRaid();
            if (_wasInRaid != currentlyInRaid)
            {
                _wasInRaid = currentlyInRaid;
                if (_wasInRaid)
                {
                    DismissAllRaids();
                }
            }
        }

        private bool IsInRaid()
        {
            try
            {
                if (_isInRaidProperty != null)
                {
                    return (bool)_isInRaidProperty.GetValue(null);
                }
            }
            catch { }
            return false;
        }

        private void TryCreatePanel()
        {
            try
            {
                var preloaderUI = GameObject.Find("Preloader UI");
                if (preloaderUI == null)
                {
                    return;
                }

                if (_panelObject != null)
                {
                    return;
                }

                _panelObject = new GameObject("RaidPopupPanel");
                _panelObject.transform.SetParent(preloaderUI.transform, false);

                _notificationPanel = _panelObject.AddComponent<RaidNotificationPanel>();

                Log.LogInfo("RaidPopup: Panel created");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to create panel: {ex.Message}");
            }
        }

        private bool _needsRefresh = false;

        public void OnRaidStarted(string nickname, string location, JsonType.EDateTime raidTime)
        {
            Log.LogWarning($"RaidPopup: {nickname} started raid on {location}");

            var raid = new ActiveRaid
            {
                Nickname = nickname,
                Location = location,
                RaidTime = raidTime
            };

            ActiveRaids.Add(raid);
            _needsRefresh = true;
        }

        public void DismissRaid(string raidId)
        {
            ActiveRaids.RemoveAll(r => r.Id == raidId);
            _needsRefresh = true;
        }

        public void DismissAllRaids()
        {
            ActiveRaids.Clear();
            _needsRefresh = true;
        }
    }
}
