using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace RegParser
{
    public static class Restore
    {

        /// <summary>
        /// Restore a backup of a registry
        /// </summary>
        /// <param name="FileName">Registry file to restore.</param>
        public static void RestoreRegistry(FileInfo RegFile)
        {

            if (!RegFile.Exists)
                throw new Exception(string.Format("File Does not exist!\nFile: \"{0}\"", RegFile.FullName));
            if (RegFile.Extension.ToLower() == "reg")
                throw new Exception(string.Format("Unsupported File type.!\nFile: \"{0}\"", RegFile.FullName));

            string Content = File.ReadAllText(RegFile.FullName);
            RestoreRegistry(Content);
        }

        public static void RestoreRegistry(string Content)
        {
            Dictionary<RegistryKey, string> Sections = Parser.GetSections(Content);
            foreach (KeyValuePair<RegistryKey, string> Sect in Sections)
            {
                List<RegistryObject> Values = Parser.ParseValues(Sect.Value);
                File.WriteAllText("tmp.txt", Sect.Key.Name);
                using (RegistryKey RK = Sect.Key)
                {
                    foreach (RegistryObject RO in Values)
                    {
                        RK.SetValue(RO.KeyName, RO.Value, RO.ValueKind);
                    }
                }
            }
        }
    }
}
