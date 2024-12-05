using HarmonyLib;
using UnityEngine;

namespace GameSaves {
    [HarmonyPatch (typeof (Faction), nameof (Faction.WriteToPlayerPrefs))]
    class SavePatcher {
        static bool Prefix (string prefsKey, bool saveVeteranUnitData) {
            GSPlugin.LogInstance.LogInfo ($"Hello world from the save function! App data path: {Application.persistentDataPath}; prefs key: {prefsKey}");
            return true;    // Skip when you have the save-to-file code written down
        }
    }
}