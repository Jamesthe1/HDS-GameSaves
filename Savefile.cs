using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

// TODO: Add loading functions, new patcher class for loading
namespace GameSaves {
    public class Savefile {
        [StructLayout (LayoutKind.Explicit)]
        public class ResultUnion {
            [FieldOffset (0)]
            public int intResult;
            [FieldOffset (0)]
            public string strResult;
            [FieldOffset (0)]
            public List<ResultUnion> list;
            [FieldOffset (0)]
            public Dictionary<string, ResultUnion> table;
        }

        private FileStream fs;
        private int indentation = 0;

        public static string GetFilePath () {
            return Application.persistentDataPath + "/" + GSPlugin.SAVE_FILE_NAME;
        }

        /// <summary>
        /// Creates a class for the save file.
        /// </summary>
        /// <exception cref="IOException" />
        public Savefile (bool overwrite) {
            string path = GetFilePath ();
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
                int state = fs.ReadByte ();
                if (state == -1) break;                 // EOF check must be done here, otherwise char will never be -1

                char c = (char)state;
                if (state == -1) break;
                if (c == ' ' && tabspace) {
                    indentation++;
                    continue;
                }
                if (c == '\n') break;
                result += c;
                tabspace = false;
            }
            indentation >>= 1;              // Fast division by 2 using bitshift
            return result;
        }

        enum BlockType {
            Unset,
            Table,
            List
        }

        public ResultUnion ReadFile () {
            ResultUnion result = new ResultUnion ();
            result.table = new Dictionary<string, ResultUnion> ();
            List<KeyValuePair<string, BlockType>> blocks = new List<KeyValuePair<string, BlockType>> {new KeyValuePair<string, BlockType> ("", BlockType.Table)};
            ResultUnion currentData = result;

            ResultUnion GetCurrentData () {
                ResultUnion data = result;
                for (int i = 1; i < blocks.Count; i++) {
                    if (blocks[i-1].Value == BlockType.Table)
                        data = data.table[blocks[i].Key];
                    else if (blocks[i-1].Value == BlockType.List)
                        data = data.list.GetLast ();    // We will always be at the end of the array given the circumstances
                }
                return data;
            }

            for (int i = 0; i < int.MaxValue; i++) {
                string line = ReadLine ();
                if (line.Length == 0)
                    break;  // EOF reached

                if (blocks.Count > indentation + 1) {
                    blocks.RemoveRange (indentation + 1, blocks.Count - indentation - 1);
                    currentData = GetCurrentData ();
                }
                KeyValuePair<string, BlockType> block = blocks.GetLast ();

                if (line.Contains ("- ")) {
                    if (block.Value != BlockType.Unset && block.Value != BlockType.List)
                        throw new Exception ($"List in a non-list type is not allowed: {line} (line {i + 1})");

                    if (block.Value == BlockType.Unset) {
                        blocks[blocks.Count - 1] = new KeyValuePair<string, BlockType> (block.Key, BlockType.List);
                        block = blocks.GetLast ();
                        currentData.list = new List<ResultUnion> ();
                    }
                    line = line.Remove (0, 2);  // Remove the leading dash
                }

                // This is not made with an else to the above dash, in case we have a list of tables
                if (line.Contains (":")) {
                    if (block.Value == BlockType.Unset) {
                        blocks[blocks.Count - 1] = new KeyValuePair<string, BlockType> (block.Key, BlockType.Table);
                        block = blocks.GetLast ();
                        currentData.table = new Dictionary<string, ResultUnion> ();
                    }
                    else if (block.Value == BlockType.List) {
                        // This section should safely move us into a new table inside the list entry
                        blocks.Add (new KeyValuePair<string, BlockType> ("", BlockType.Table));
                        block = blocks.GetLast ();

                        ResultUnion tableEntry = new ResultUnion ();
                        tableEntry.table = new Dictionary<string, ResultUnion> ();
                        currentData.list.Add (tableEntry);
                        currentData = tableEntry;
                    }

                    string[] kv = line.Split (':');
                    ResultUnion data = new ResultUnion ();
                    if (kv[1].Length > 0) {
                        kv[1] = kv[1].Substring (1).RemoveChars ('"');  // Remove trailing space, and any double-quotes thereafter
                        if (int.TryParse(kv[1], out int intResult))
                            data.intResult = intResult;
                        else
                            data.strResult = kv[1];
                    }
                    currentData.table.Add (kv[0], data);

                    if (kv[1].Length == 0) {
                        // We aren't sure what this will be yet, so leave it for the next line to decide
                        blocks.Add (new KeyValuePair<string, BlockType> (kv[0], BlockType.Unset));
                        block = blocks.GetLast ();
                        currentData = data;
                    }
                }
                else if (block.Value == BlockType.List) {
                    ResultUnion entry = new ResultUnion ();
                    entry.strResult = line.RemoveChars ('"');   // Just in case there are any double-quotes here
                    currentData.list.Add (entry);
                }
                else {
                    throw new Exception ($"A line did not satisfy any condition: {line} (line {i + 1})");
                }
            }

            return result;
        }
    }
}