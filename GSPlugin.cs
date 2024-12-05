using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace GameSaves {
    [BepInPlugin (PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class GSPlugin : BaseUnityPlugin {
        internal static ManualLogSource LogInstance = null;

        private void Awake () {
            // Plugin startup logic
            LogInstance = Logger;
            Harmony harmony = new Harmony (PluginInfo.PLUGIN_GUID);
            harmony.PatchAll ();
            Logger.LogInfo ($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
