using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;

namespace GameSaves {
    [HarmonyPatch (typeof (Faction), nameof (Faction.PopulateWithPlayerPrefs))]
    class LoadPatcher {
        static bool Prefix (string prefsKey, Faction __instance) {
            try {
                Savefile savefile = new Savefile (false);
                Savefile.ResultUnion result = savefile.ReadFile ();

                int tempInt;

                __instance.ResetDynamicData ();
                __instance.currentSortieSeed = result.table["currentsortieseed"].intResult;
                tempInt = result.table["points"].intResult;
                __instance.AddPoints (tempInt);
                tempInt = result.table["upgradetokens"].intResult;
                __instance.upgradeTokens = (tempInt < 0) ? __instance.upgradeTokens : tempInt;

                List<UnitModifier> allModifiers = CampaignGenerationSettings.Instance.allAvailableModifiers;
                foreach (var listItem in result.table["unlockedmodifiers"].list) {
                    UnitModifier mod = allModifiers.Find (m => m.name.ToLower () == listItem.strResult);
                    if (mod != null)
                        __instance.UnlockModifier (mod);
                    else
                        GSPlugin.LogInstance.LogWarning ($"No modifier found named {listItem.strResult}");
                }

                if (__instance.unlockedModifiers.Count >= allModifiers.Count)
                    SteamStatsAndAchievements.UnlockAchievement (SteamStatsAndAchievements.Achievement.ACH_SHOP_ALLMODIFIERS_PURCHASED);

                List<WeaponBlueprint> allWeapons = CampaignGenerationSettings.Instance.allAvailableWeapons;
                foreach (var listItem in result.table["unlockedweapons"].list) {
                    WeaponBlueprint weapon = allWeapons.Find (w => w.name.ToLower () == listItem.strResult);
                    if (weapon != null)
                        __instance.UnlockWeapon (weapon);
                    else
                        GSPlugin.LogInstance.LogWarning ($"No weapon found named {listItem.strResult}");
                }

                // TODO: Array of tag-strings to load and save, as to accommodate for modded fleets
                LoadUnitConfig (result, "fighter", __instance);
                LoadUnitConfig (result, "destroyer", __instance);
                LoadUnitConfig (result, "frigate", __instance);
                LoadUnitConfig (result, "battleship", __instance);

                if (result.table.ContainsKey ("campaignflags")) {
                    __instance.campaignFlags = new List<string> ();
                    foreach (var listItem in result.table["campaignflags"].list)
                        __instance.AddCampaignFlag (listItem.strResult);
                }

                __instance.currentFleetBlueprintNames = new List<string> ();
                foreach (var listEntry in result.table["currentfleet"].list) {
                    __instance.currentFleetBlueprintNames.Add (listEntry.strResult);
                }

                // Veterans are patched in a separate class, since this happens in a separate step

                __instance.SetCurrentfleetFromBlueprints ();
            }
            catch (IOException ioe) {
                GSPlugin.LogInstance.LogWarning ($"Could not load savefile (using registry): {ioe.Message} (this can be safely ignored if this is your first time using the mod)");
                return true;
            }
            catch (Exception e) {
                GSPlugin.LogInstance.LogError ($"Could not load savefile (using registry): {e.Message}");
                return true;
            }

            return false;
        }

        static void LoadUnitConfig (Savefile.ResultUnion result, string tag, Faction instance) {
            Faction.UnitConfig config = instance.GetConfigByID (tag);
            if (config == null) return;

            var tagTable = result.table["configs"].table[tag].table;
            for (int i = 0; i < tagTable["assignedweapons"].list.Count; i++) {
                WeaponAssignment assignment = new WeaponAssignment ();
                assignment.slot = i;
                
                string wpnString = tagTable["assignedweapons"].list[i].strResult;
                if (wpnString != "NONE") {
                    WeaponBlueprint bp = CampaignGenerationSettings.Instance.allAvailableWeapons.Find (w => w.name.ToLower () == wpnString);
                    if (bp != null)
                        assignment.weaponBlueprint = bp;
                    else
                        GSPlugin.LogInstance.LogWarning ($"No assignable weapon found named {wpnString}");
                }
                config.weaponAssignments[i] = assignment;
            }

            config.unitModifiers = new List<UnitModifier> ();
            foreach (var listItem in tagTable["assignedmodifiers"].list) {
                if (listItem.strResult == "NONE") continue;

                UnitModifier modifier = CampaignGenerationSettings.Instance.allAvailableModifiers.Find (m => m.name.ToLower () == listItem.strResult);
                if (modifier != null)
                    if (!config.unitModifiers.Contains (modifier))
                        config.unitModifiers.Add (modifier);
                else
                    GSPlugin.LogInstance.LogWarning ($"No assignable modifier found named {listItem.strResult}");
            }
        }
    }
}