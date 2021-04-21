using Microsoft.Win32;
using System;
using System.IO;

namespace RegParser
{
    public static class Backup
    {
        /// <summary>
        /// Creates a backup of a registry key including its SubKeys
        /// </summary>
        /// <param name="KeyString"></param>
        /// <param name="FilePath"></param>
        /// <param name="IncludeSubKeys"></param>
        public static void BackupRegistry(string KeyString, string FilePath, bool IncludeSubKeys = true)
        {
            RegistryKey RK = Parser.ParseRoot(KeyString);
            if (RK == null)
            { throw new Exception("Invalid registry path!"); }

            using (StreamWriter sw = new StreamWriter(FilePath))
            {
                sw.Write(Parser.RegToStr(RK, false, IncludeSubKeys));
            }
        }
    }
}
