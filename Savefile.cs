using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// TODO: Add loading functions, new patcher class for loading
namespace GameSaves {
    public class Savefile {
        private FileStream fs;
        private int indentation = 0;

        /// <summary>
        /// Creates a class for the save file.
        /// </summary>
        /// <exception cref="IOException" />
        public Savefile (bool overwrite) {
            string path = Application.persistentDataPath + "/" + GSPlugin.SAVE_FILE_NAME;
            if (File.Exists (path) && overwrite) {
                File.Delete (path);
            }
            else if (!File.Exists (path) && !overwrite) {
                throw new IOException ("Savefile does not exist");
            }
            // This mode does not clear the file contents on write. Instead, the writing position is set to the beginning
            fs = File.Open (path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        ~Savefile () {
            fs.Close ();
        }

        private string GetIndent () {
            return new string (' ', indentation * 2);
        }

        public void WriteEntry (string key, object val) {
            fs.WriteString (GetIndent () + key + ": " + val.ToString () + "\n");
        }

        public void BeginBlock (string key) {
            fs.WriteString (GetIndent () + key + ":\n");
            indentation++;
        }

        public void BeginListEntryBlock (string key, object val) {
            WriteListEntry (key + ": " + val.ToString ());
            indentation++;
        }

        public void EndBlock () {
            if (indentation == 0) {
                throw new Exception ("No block existed when EndBlock was called");
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

        private string ReadLine () {
            string result = "";
            bool tabspace = true;
            indentation = 0;
            for (int i = 0; i < int.MaxValue; i++) {    // Just in case we wind up in an infinite loop, we'll have a stopping point
                char c = (char)fs.ReadByte ();
                if (c == ' ' && tabspace) {
                    indentation++;
                    continue;
                }
                if (c == '\n') break;
                if (c == -1) break;         // EOF
                result += c;
                tabspace = false;
            }
            indentation >>= 1;              // Fast division by 2 using bitshift
            return result;
        }
    }
}