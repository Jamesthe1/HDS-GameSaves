using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;

namespace GameSaves {
    [HarmonyPatch (typeof (Faction), nameof (Faction.LoadVeteranUnitDataIntoPlayerFleet))]
    class LoadVeteransPatcher {
        static bool Prefix (PlayerFleet fleet) {
            if (fleet == null) return true;     // I do hope their programming practices have improved in this regard...
            try {
                Savefile savefile = new Savefile (false);
                Savefile.ResultUnion result = savefile.ReadFile ();

                List<EmpireFlight> blankFlights = Globals.Instance.blankEmpireFlights;
                for (int i = 0; i < blankFlights.Count && i < fleet.activeFlights.Count; i++)
                    LoadSpecificUnitData (result, blankFlights[i].name, fleet.activeFlights[i]);
            }
            catch (IOException ioe) {
                GSPlugin.LogInstance.LogWarning ($"Could not load savefile in veterans step (using registry): {ioe.Message} (this can be safely ignored if a similar warning appeared before)");
                return true;
            }
            catch (Exception e) {
                GSPlugin.LogInstance.LogError ($"Could not load savefile in veterans step (using registry): {e.Message}");
                return true;
            }

            return false;
        }

        static void LoadSpecificUnitData (Savefile.ResultUnion result, string unitTypeKey, Flight flight) {
            if (flight == null) {
                GSPlugin.LogInstance.LogWarning ($"No flight was given for {unitTypeKey} to load veterancy data into");
                return;
            }

            List<Savefile.ResultUnion> unitList = result.table["veterans"].table[unitTypeKey].list;
            SpawnPoint[] spawnPoints = flight.transform.GetComponentsInChildren<SpawnPoint> ();
            for (int i = 0; i < spawnPoints.Length && i < unitList.Count; i++) {
                var entryTable = unitList[i].table;
                SpawnPoint spawnPoint = spawnPoints[i];
                
                spawnPoint.callsign = entryTable["flightname"].strResult;
                spawnPoint.experience = entryTable["flightxp"].intResult;
            }
        }
    }
}