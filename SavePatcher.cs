using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace GameSaves {
    [HarmonyPatch (typeof (Faction), nameof (Faction.WriteToPlayerPrefs))]
    class SavePatcher {
        static bool Prefix (string prefsKey, bool saveVeteranUnitData, Faction __instance) {
            try {
                Savefile savefile = new Savefile (prefsKey, true);

                Debug.Log ("<color=orange>::: BEGIN FILE SAVE :::</color>");
                Messenger<Faction, Savefile>.Broadcast ("OnFactionFileSaving", __instance, savefile);

                savefile.WriteEntry ("currentsortieseed", __instance.currentSortieSeed);
                savefile.WriteEntry ("points", __instance.points);
                savefile.WriteEntry ("upgradetokens", __instance.upgradeTokens);

                savefile.BeginBlock ("unlockedmodifiers");
                foreach (UnitModifier modifier in __instance.unlockedModifiers)
                    savefile.WriteListEntry (modifier.name.ToLower ());
                savefile.EndBlock ();
                savefile.BeginBlock ("unlockedweapons");
                foreach (WeaponBlueprint weapon in __instance.unlockedWeapons)
                    savefile.WriteListEntry (weapon.name.ToLower ());
                savefile.EndBlock ();

                WriteUnitConfig (savefile, "fighter", __instance);
                WriteUnitConfig (savefile, "destroyer", __instance);
                WriteUnitConfig (savefile, "frigate", __instance);
                WriteUnitConfig (savefile, "battleship", __instance);

                if (saveVeteranUnitData)
                    WriteVeterans (savefile, PlayerFleet.Instance);

                if (__instance.campaignFlags.Count > 0)
                    savefile.WriteStringList ("campaignflags", __instance.campaignFlags);

                savefile.WriteStringList ("currentfleet", __instance.currentFleetBlueprintNames);
            
                Debug.Log ("<color=orange>::: END SAVE :::</color>");
            } catch (IOException ioe) {
                GSPlugin.LogInstance.LogError ($"Assuming default save behavior; save file could not be opened: {ioe.Message}");
                return true;
            }

            return false;    // Skip the original code
        }

        private static void WriteUnitConfig (Savefile savefile, string tag, Faction instance) {
            Faction.UnitConfig config = instance.GetConfigByID (tag);
            if (config == null) return;

            savefile.BeginBlock (tag);

            if (config.weaponAssignments.Count > 0) {
                savefile.BeginBlock ("assignedweapons");
                foreach (WeaponAssignment assignment in config.weaponAssignments) {
                    WeaponBlueprint bp = assignment.weaponBlueprint ?? null;
                    if (bp == null)
                        savefile.WriteListEntry ("NONE");
                    else
                        savefile.WriteListEntry (bp.name.ToLower ());
                }
                savefile.EndBlock ();
            }

            if (config.unitModifiers.Count > 0) {
                savefile.BeginBlock ("assignedmodifiers");
                foreach (UnitModifier modifier in config.unitModifiers) {
                    if (modifier != null)
                        savefile.WriteListEntry (modifier.name.ToLower ());
                }
                savefile.EndBlock ();
            }

            savefile.EndBlock ();
        }

        private static void WriteVeterans (Savefile savefile, PlayerFleet fleet) {
            if (fleet == null) return;

            savefile.BeginBlock ("veterans");
            ref List<EmpireFlight> empireFlights = ref Globals.Instance.blankEmpireFlights;
            for (int i = 0; i < empireFlights.Count; i++) {
                // Not placing active flights into the above statement in case there are more than blank empire flights
                // I'm not actually sure if that condition will be met but it's good to be safe rather than sorry
                if (i >= fleet.activeFlights.Count) break;

                Flight flight = fleet.activeFlights[i];
                WriteFlightMember (savefile, empireFlights[i].unitBlueprint.name, flight);
            }
            savefile.EndBlock ();
        }

        private static void WriteFlightMember (Savefile savefile, string unitTypeKey, Flight flight) {
            savefile.BeginBlock (unitTypeKey);

            foreach (SpawnPoint spawnPoint in flight.GetComponentsInChildren<SpawnPoint> ()) {
                if (spawnPoint.GetStrength () <= 0f) continue;
                
                string callsign = spawnPoint.GetCallsign ();
                int xp = spawnPoint.experience;
                if (spawnPoint.unit != null) {
                    callsign = spawnPoint.unit.callsign;
                    xp = spawnPoint.unit.experience;
                }
                savefile.BeginListEntryBlock ("flightname", $"\"{callsign}\"");
                savefile.WriteEntry ("flightxp", xp);
                savefile.EndBlock ();
            }

            savefile.EndBlock ();
        }
    }
}