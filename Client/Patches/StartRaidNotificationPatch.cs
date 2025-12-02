using HarmonyLib;
using System;
using System.Reflection;

namespace RaidPopup.Patches
{
    /// <summary>
    /// Patches the Fika notification handler to intercept raid start notifications.
    /// Uses reflection to avoid compile-time dependency on Fika.Core.
    /// </summary>
    public static class StartRaidNotificationPatch
    {
        private static Type _notificationManagerType;
        private static Type _fikaGlobalsType;
        private static PropertyInfo _isInRaidProperty;
        private static bool _patchFailed = false;

        /// <summary>
        /// Manually applies the patch at runtime after Fika is loaded
        /// </summary>
        public static void ApplyPatch(Harmony harmony)
        {
            if (_patchFailed) return;

            try
            {
                RaidPopupPlugin.Log?.LogWarning("JoinRaid: Attempting to patch Fika notification system...");

                // Find FikaNotificationManager type at runtime
                _notificationManagerType = AccessTools.TypeByName("Fika.Core.Networking.Websocket.FikaNotificationManager");
                RaidPopupPlugin.Log?.LogInfo($"JoinRaid: FikaNotificationManager type: {_notificationManagerType?.FullName ?? "NOT FOUND"}");
                
                if (_notificationManagerType == null)
                {
                    RaidPopupPlugin.Log?.LogError("JoinRaid: Could not find FikaNotificationManager type. Is Fika installed?");
                    _patchFailed = true;
                    return;
                }

                // Find FikaGlobals type and cache IsInRaid property
                _fikaGlobalsType = AccessTools.TypeByName("Fika.Core.Main.Utils.FikaGlobals");
                RaidPopupPlugin.Log?.LogInfo($"JoinRaid: FikaGlobals type: {_fikaGlobalsType?.FullName ?? "NOT FOUND"}");
                
                if (_fikaGlobalsType != null)
                {
                    _isInRaidProperty = _fikaGlobalsType.GetProperty("IsInRaid", BindingFlags.Static | BindingFlags.Public);
                    RaidPopupPlugin.Log?.LogInfo($"JoinRaid: IsInRaid property: {(_isInRaidProperty != null ? "FOUND" : "NOT FOUND")}");
                }

                // Find the WebSocket_OnMessage method
                var targetMethod = AccessTools.Method(_notificationManagerType, "WebSocket_OnMessage");
                RaidPopupPlugin.Log?.LogInfo($"JoinRaid: WebSocket_OnMessage method: {(targetMethod != null ? "FOUND" : "NOT FOUND")}");
                
                if (targetMethod == null)
                {
                    RaidPopupPlugin.Log?.LogError("JoinRaid: Could not find WebSocket_OnMessage method");
                    _patchFailed = true;
                    return;
                }

                // Create postfix patch
                var postfix = new HarmonyMethod(typeof(StartRaidNotificationPatch).GetMethod(nameof(WebSocket_OnMessage_Postfix), 
                    BindingFlags.Static | BindingFlags.Public));

                harmony.Patch(targetMethod, postfix: postfix);

                RaidPopupPlugin.Log?.LogWarning("JoinRaid: Successfully patched Fika WebSocket_OnMessage!");
            }
            catch (Exception ex)
            {
                RaidPopupPlugin.Log?.LogError($"JoinRaid: Failed to patch Fika notification system: {ex.Message}");
                RaidPopupPlugin.Log?.LogError($"JoinRaid: Stack trace: {ex.StackTrace}");
                _patchFailed = true;
            }
        }

        /// <summary>
        /// Postfix patch that intercepts WebSocket messages
        /// </summary>
        public static void WebSocket_OnMessage_Postfix(object sender, object e)
        {
            // Wrap everything in try-catch to prevent crashing the game
            try
            {
                if (e == null)
                {
                    return;
                }

                // Get the Data property from MessageEventArgs via reflection
                var dataProperty = e.GetType().GetProperty("Data");
                if (dataProperty == null)
                {
                    return;
                }

                string data = dataProperty.GetValue(e) as string;
                if (string.IsNullOrEmpty(data))
                {
                    return;
                }

                // Parse the JSON
                var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(data);
                if (!jsonObject.ContainsKey("type"))
                {
                    return;
                }

                // Type can be either a number (1 = StartedRaid) or a string ("StartedRaid")
                var typeToken = jsonObject["type"];
                bool isStartedRaid = false;
                
                if (typeToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                {
                    // Server sends type as number: 1 = StartedRaid
                    int typeNum = (int)typeToken;
                    isStartedRaid = (typeNum == 1);
                }
                else if (typeToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
                {
                    // In case it's sent as string
                    string typeStr = (string)typeToken;
                    isStartedRaid = (typeStr == "StartedRaid" || typeStr == "1");
                }

                if (!isStartedRaid)
                {
                    return;
                }
                
                RaidPopupPlugin.Log?.LogWarning("JoinRaid: Detected StartedRaid notification!");

                // Check if we're in a raid using cached property
                if (_isInRaidProperty != null)
                {
                    try
                    {
                        bool isInRaid = (bool)_isInRaidProperty.GetValue(null);
                        if (isInRaid)
                        {
                            return;
                        }
                    }
                    catch { /* Ignore - just process the notification */ }
                }

                // Parse the notification data
                string nickname = jsonObject.Value<string>("nickname") ?? "Unknown";
                string location = jsonObject.Value<string>("location") ?? "Unknown";
                
                // raidTime can be number (0=CURR, 1=PAST) or string
                JsonType.EDateTime raidTime = JsonType.EDateTime.CURR;
                var raidTimeToken = jsonObject["raidTime"];
                if (raidTimeToken != null)
                {
                    if (raidTimeToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                    {
                        int raidTimeNum = (int)raidTimeToken;
                        raidTime = (JsonType.EDateTime)raidTimeNum;
                    }
                    else if (raidTimeToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
                    {
                        string raidTimeStr = (string)raidTimeToken;
                        if (Enum.TryParse(raidTimeStr, out JsonType.EDateTime parsed))
                        {
                            raidTime = parsed;
                        }
                    }
                }

                RaidPopupPlugin.Log?.LogWarning($"JoinRaid: Raid details - Host: {nickname}, Location: {location}, Time: {raidTime}");

                // Notify our plugin
                RaidPopupPlugin.Instance?.OnRaidStarted(nickname, location, raidTime);
            }
            catch (Exception ex)
            {
                // Log but don't crash
                RaidPopupPlugin.Log?.LogWarning($"Error processing notification: {ex.Message}");
            }
        }
    }
}
