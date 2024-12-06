using System.IO;
using System.Text;

namespace GameSaves {
    public static class Extensions {
        public static void WriteString (this Stream stm, string str) {
            byte[] data = new UTF8Encoding (true).GetBytes (str);
            stm.Write (data, 0, data.Length);
        }
    }
}