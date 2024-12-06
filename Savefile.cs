using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameSaves {
    public class Savefile {
        private FileStream fs;
        private string prefsKey;
        private int indentation = 0;

        /// <summary>
        /// Creates a class for the save file.
        /// </summary>
        /// <exception cref="IOException" />
        /// <param name="_prefsKey">The prefs key for the operation</param>
        public Savefile (string _prefsKey, bool overwrite) {
            string path = Application.persistentDataPath + "/" + GSPlugin.SAVE_FILE_NAME;
            // File.OpenWrite does not clear the file contents on write. Instead, the writing position is set to the beginning
            if (overwrite) {
                if (File.Exists (path))
                    File.Delete (path);
            }
            fs = File.Open (path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            
            prefsKey = _prefsKey;
        }

        private string GetIndent () {
            return new string (' ', indentation * 2);
        }

        private string GetTrueKey (string key) {
            if (indentation == 0)
                return prefsKey + key;
            return key;
        }

        public void WriteEntry (string key, object val) {
            fs.WriteString (GetIndent () + GetTrueKey (key) + ": " + val.ToString () + "\n");
        }

        public void BeginBlock (string key) {
            fs.WriteString (GetIndent () + GetTrueKey (key) + ":\n");
            indentation++;
        }

        public void BeginListEntryBlock (string key, object val) {
            WriteListEntry (GetTrueKey (key) + ": " + val.ToString ());
            indentation++;
        }

        public void EndBlock () {
            if (indentation == 0) {
                throw new System.Exception ("No block existed when EndBlock was called");
            }
            indentation--;
        }

        public void WriteListEntry (object val) {
            fs.WriteString (GetIndent () + "- " + val.ToString () + "\n");
        }

        public void WriteStringList (string key, List<string> list) {
            BeginBlock (key);
            foreach (string s in list) WriteListEntry (s);
            EndBlock ();
        }

        ~Savefile () {
            fs.Close ();
        }
    }
}