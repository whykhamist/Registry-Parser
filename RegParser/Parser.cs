using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace RegParser
{
    /// <summary>
    /// This Class helps in creating a registry backup based on provided registry key
    /// as well as retore them using only the built in Registry class of c#,
    /// with that said, this only supports specific Registry data types supported by the said class.
    /// 
    /// Supported types { REG_SZ, REG_DWORD, REG_QWORD, REG_BINARY, REG_EXPAND_SZ, REG_MULTI_SZ REG_NONE }
    /// 
    /// Unsupported types will likely be treated as string. Any improvement to this class are welcome! (^_^)
    /// </summary>
    internal static class Parser
    {

        /// <summary>
        /// Creates Dictionary of Sections
        /// </summary>
        /// <param name="Content"></param>
        /// <returns></returns>
        public static Dictionary<RegistryKey, string> GetSections(string Content)
        {
            Dictionary<RegistryKey, string> ret = new Dictionary<RegistryKey, string>();
            // Pattern taken from https://www.oreilly.com/library/view/regular-expressions-cookbook/9780596802837/ch08s13.html
            // Slightly modified to group Keynames and values
            string Pattern = @"(^\[[^\]\r\n]+])((?:\r?\n(?:[^[\r\n].*)?)*)";
            MatchCollection MC = Regex.Matches(Content, Pattern, RegexOptions.Multiline);

            foreach (Match m in MC)
            {
                try
                {
                    ret.Add(ParseRoot(m.Groups[1].Value.TrimStart('[').TrimEnd(']')), m.Groups[2].Value);
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Exception thrown on processing string {0}", m.Value), ex);
                }
            }

            return ret;
        }

        public static List<RegistryObject> ParseValues(string Content)
        {
            List<RegistryObject> ret = new List<RegistryObject>();
            //string Pattern = @"^[\t ]*("".+""|@)=(""(?:[^""\\]|\\.)*""|[^""]+)";
            string Pattern = @"^[\t ]*("".+""|@)=(""(?:[^""\\]|\\.)*""|[^""@]+)";
            MatchCollection MC = Regex.Matches(Content, Pattern, RegexOptions.Multiline);

            foreach (Match m in MC)
            {
                string sKey = m.Groups[1].Value;
                string sVal = m.Groups[2].Value;
                sKey = sKey.Trim(' ', '\t', '\n', '\v', '\f', '\r', '"');
                RegistryObject RO = new RegistryObject
                {
                    KeyName = sKey,
                    Value = ParseValueKind(sVal, out RegistryValueKind RVK),
                    ValueKind = RVK
                };
                ret.Add(RO);
            }

            return ret;
        }

        public static RegistryKey ParseRoot(string rKeyString)
        {
            string tmp = rKeyString.Trim('[', ']');
            string[] tmps = tmp.Split(new char[] { '\\' }, 2);
            if (tmps.Length <= 1 || (tmps.Length > 1 && string.IsNullOrWhiteSpace(tmps[1]))) return null;
            string RootStr = tmps[0];
            string RPath = tmps[1];
            RegistryKey RK;
            RegistryView RV = (Environment.Is64BitOperatingSystem) ? RegistryView.Registry64 : RegistryView.Registry32;
            RegistryKey Reg_Base;
            switch (RootStr.ToUpper())
            {
                case "HKEY_LOCAL_MACHINE":
                case "HKLM":
                    Reg_Base = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RV);
                    RK = Reg_Base.OpenSubKey(RPath, true);
                    break;
                case "HKEY_CURRENT_USER":
                case "HKCU":
                    Reg_Base = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RV);
                    RK = Reg_Base.OpenSubKey(RPath, true);
                    break;
                case "HKEY_USERS":
                case "HKU":
                    Reg_Base = RegistryKey.OpenBaseKey(RegistryHive.Users, RV);
                    RK = Reg_Base.OpenSubKey(RPath, true);
                    break;
                case "HKEY_CLASSES_ROOT":
                case "HKCR":
                    Reg_Base = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RV);
                    RK = Reg_Base.OpenSubKey(RPath, true);
                    break;
                case "HKEY_CURRENT_CONFIG":
                case "HKCC":
                    Reg_Base = RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RV);
                    RK = Reg_Base.OpenSubKey(RPath, true);
                    break;
                case "COMPUTER":
                    Reg_Base = null;
                    RK = ParseRoot(RPath);
                    break;
                default:
                    Reg_Base = null;
                    RK = null;
                    break;
            }

            if (RK == null && Reg_Base != null)
            {
                string[] KeySections = RPath.Split('\\');

                if ((Reg_Base.Name == RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RV).Name ||
                    Reg_Base.Name == RegistryKey.OpenBaseKey(RegistryHive.Users, RV).Name) &&
                    Reg_Base.OpenSubKey(KeySections[0]) == null)
                {
                    string errorMessage = "Creating a subkey directly under " + ((Reg_Base.Name == RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RV).Name) ? "HKEY_LOCAL_MACHINE" : "HKEY_USERS") + " is impossible as it's a virtual Key.\nPlease select a different path.";
                    throw new Exception(errorMessage);
                }
                else
                {
                    RK = Reg_Base.CreateSubKey(RPath, true);

                    // this commented section attempts to delete empty registry keys but has some issues so it will remain commented
                    /*string tmpKey = RPath;
                    RegistryKey tmpRK = null;
                    for (int x = KeySections.Length; x > 0; x--)
                    {
                        if (x == 1) tmpKey = tmpKey.Substring(0, tmpKey.Length - (KeySections[x - 1].Length));
                        else tmpKey = tmpKey.Substring(0, tmpKey.Length - (KeySections[x - 1].Length + 1));
                        tmpRK = Reg_Base.OpenSubKey(tmpKey, true);
                        if (tmpRK.OpenSubKey(KeySections[x - 1]).GetSubKeyNames().Length <= 0 &&
                            tmpRK.OpenSubKey(KeySections[x - 1]).GetValueNames().Length <= 0)
                            tmpRK.DeleteSubKey(KeySections[x - 1]);
                    }
                    if (tmpRK != null) tmpRK.Dispose();*/
                }
                Reg_Base.Dispose();
            }
            return RK;
        }

        public static object ParseValueKind(string Value, out RegistryValueKind RVK)
        {
            RVK = RegistryValueKind.String;
            object ret = null;
            string[] tmp = Value.Split(new char[] { ':' }, 2);
            if (tmp.Length >= 2)
            {
                string val = tmp[1];

                switch (tmp[0].ToLower())
                {
                    case "dword":
                        RVK = RegistryValueKind.DWord;
                        val = CleanValueKind(val);
                        ret = Convert.ToInt32(val, 16);
                        break;
                    case "hex(b)":
                        RVK = RegistryValueKind.QWord;
                        val = CleanValueKind(val);
                        ret = Convert.ToInt64(val.ToString(), 16);
                        break;
                    case "hex(2)":
                        RVK = RegistryValueKind.ExpandString;
                        val = CleanValueKind(val, false);
                        string end = ",00,00,00";
                        if (val.EndsWith(end)) { val = val.Substring(0, (val.Length - end.Length)); }

                        ret = HexLiteralToString(val);
                        break;
                    case "hex(7)":
                        RVK = RegistryValueKind.MultiString;
                        val = CleanValueKind(val, false);
                        string newLine = ",00,00,00,";
                        string endAll = ",00,00,00,00,00";
                        if (val.EndsWith(endAll))
                        {
                            val = val.Substring(0, (val.Length - endAll.Length));
                        }
                        string[] Strings = val.Split(new string[] { newLine }, StringSplitOptions.None);
                        List<string> MultiString = new List<string>();
                        foreach (string s in Strings)
                        {
                            MultiString.Add(HexLiteralToString(s));
                        }
                        ret = MultiString.ToArray();
                        break;
                    case "hex":
                        RVK = RegistryValueKind.Binary;
                        val = CleanValueKind(val);
                        ret = Enumerable.Range(0, val.Length / 2).Select(x => Byte.Parse(val.Substring(2 * x, 2), NumberStyles.HexNumber)).ToArray();
                        break;
                    case "hex(0)":
                        RVK = RegistryValueKind.None;
                        val = CleanValueKind(val);
                        ret = Enumerable.Range(0, val.Length / 2).Select(x => Byte.Parse(val.Substring(2 * x, 2), NumberStyles.HexNumber)).ToArray();
                        break;
                    default:
                        RVK = RegistryValueKind.String;
                        ret = Value;
                        break;
                }
            }
            else
            {
                RVK = RegistryValueKind.String;
                ret = Value;
            }
            return ret;
        }

        public static string CleanValueKind(string ValueString, bool ClearComma = true)
        {
            string output = Regex.Replace(ValueString, @"[^a-zA-Z0-9,]", "");
            if (ClearComma)
            {
                output = output.Replace(",", "");
            }
            return output;
        }

        public static string HexLiteralToString(string HexValues)
        {
            string[] HexSplit = HexValues.Split(new string[] { ",00," }, StringSplitOptions.None);
            string tmp = string.Empty;
            foreach (string s in HexSplit)
            {
                tmp += Char.ConvertFromUtf32(Convert.ToInt32(s, 16));
            }
            return tmp;
        }

        public static string RegKindToStr(RegistryValueKind RVK)
        {
            string ret = RVK switch
            {
                //ret = "REG_DWORD";
                RegistryValueKind.DWord => "dword",

                //ret = "REG_QWORD";
                RegistryValueKind.QWord => "hex(b)",

                //ret = "REG_BINARY";
                RegistryValueKind.Binary => "hex",

                // ret = "REG_EXPAND_SZ";
                RegistryValueKind.ExpandString => "hex(2)",

                //ret = "REG_MULTI_SZ";
                RegistryValueKind.MultiString => "hex(7)",

                //ret = "REG_NONE";
                RegistryValueKind.None => "hex(0)",// @=hex(0):  => should look like this in registry file

                RegistryValueKind.Unknown => null,
                _ => string.Empty,
            };
            return ret;
        }

        public static string RKValueToString(RegistryKey RK, string ValueName)
        {
            string ret = string.Empty;
            RegistryValueKind rvk = RK.GetValueKind(ValueName);
            string type = RegKindToStr(rvk);
            switch (rvk)
            {
                case RegistryValueKind.String:
                    string tmp = RK.GetValue(ValueName).ToString()
                        .Replace("\\", "\\\\")
                        .Replace("\"", "\\\"");

                    ret = $"\"{tmp}\"";
                    break;
                case RegistryValueKind.DWord:
                    int dword = Convert.ToInt32(RK.GetValue(ValueName).ToString());
                    ret = $"{type}:{dword:x8}";
                    break;
                case RegistryValueKind.QWord:
                    UInt64 qword = unchecked((UInt64)(Int64)RK.GetValue(ValueName));
                    //ret = type + ":" + BitConverter.ToString(BitConverter.GetBytes(qword)).Replace("-", ",").ToLower();
                    //ret = type + ":" + string.Format("{0:x2}", qword);
                    string formattedQWord = string.Format(
                        "{0:X2},{1:X2},{2:X2},{3:X2},{4:X2},{5:X2},{6:X2},{7:X2}",
                        (qword >> 56) & 0xff,
                        (qword >> 48) & 0xff,
                        (qword >> 40) & 0xff,
                        (qword >> 32) & 0xff,
                        (qword >> 24) & 0xff,
                        (qword >> 16) & 0xff,
                        (qword >> 8) & 0xff,
                        qword & 0xff).ToLower();

                    ret = $"{type}:{formattedQWord}";
                    break;
                case RegistryValueKind.Binary:
                    // get string format of the binary value replacing dashes(-) with commas (,)
                    ret = $"{type}:{BitConverter.ToString((byte[])RK.GetValue(ValueName)).Replace("-", ",").ToLower()}";
                    break;
                case RegistryValueKind.ExpandString:
                    //Convert string to its hexadecimal format avoiding getting the expanded value of the string
                    byte[] bytes = Encoding.ASCII.GetBytes(RK.GetValue(ValueName, "", RegistryValueOptions.DoNotExpandEnvironmentNames).ToString());
                    string expandstring = BitConverter.ToString(bytes).Replace("-", ",00,");
                    // End the value with a newline => (00,00,00) => Read as newline in registry
                    // I'm not really sure about this but this fixes the issue of getting the last character being removed
                    ret = $"{type}:{expandstring.ToLower()},00,00,00";
                    break;
                case RegistryValueKind.MultiString:
                    // get value as array of string (each line in an element)
                    string[] values = (string[])RK.GetValue(ValueName);
                    List<string> multistring = new List<string>();
                    foreach (string s in values)
                    {
                        // Convert each line to its hexadicimal format
                        byte[] byts = Encoding.ASCII.GetBytes(s);
                        // Replace dashes with a space (00) separated with a comma (,)
                        multistring.Add(BitConverter.ToString(byts).Replace("-", ",00,"));
                    }
                    /*
                     * Join string array with a newline => (00,00,00) => Read as newline in registry
                     * End value with a space and newline => (00,00,00,00,00) => this is needed as required
                     * by the registry to avoid having the last character of the string removed/deleted
                     */
                    ret = $"{type}:{string.Join(",00,00,00,", multistring.ToArray()).ToLower()},00,00,00,00,00";
                    break;
                case RegistryValueKind.None:
                    ret = $"{type}:";
                    break;
                case RegistryValueKind.Unknown:
                default:

                    break;
            }
            return ret;
        }

        public static string RegToStr(RegistryKey RegKey, bool isSubKey = false, bool IncludeSubKeys = true)
        {
            string OutPut = isSubKey ? string.Empty : "Windows Registry Editor Version 5.00 \n\r";
            using (RegistryKey RK = RegKey)
            {
                OutPut += $"\n[{RK.Name}]\n";
                foreach (string s in RK.GetValueNames())
                {
                    try
                    {
                        string val = (string.IsNullOrWhiteSpace(s) ? "@" : "\"" + s + "\"") + "=" + RKValueToString(RK, s);
                        int initial = 79;
                        RegistryValueKind RVK = RK.GetValueKind(s);
                        // this part will only format long binary text 
                        if ((RVK == RegistryValueKind.Binary ||
                           RVK == RegistryValueKind.MultiString ||
                           RVK == RegistryValueKind.ExpandString ||
                           RVK == RegistryValueKind.QWord ||
                           RVK == RegistryValueKind.Unknown) && val.Length > initial)
                        {
                            string tmp = "";
                            while (val[initial] != ',')
                            { initial--; }
                            tmp = val.Substring(0, initial + 1) + "\\\n  ";
                            int x = 0;
                            for (int i = initial + 1; i < val.Length; i++)
                            {
                                tmp += val[i];
                                x++;
                                if (x >= 75)
                                {
                                    tmp += "\\\n  ";
                                    x = 0;
                                }
                            }

                            val = tmp;
                        } // Formatter ends here!

                        OutPut += val + "\n";
                    }
                    catch (Exception) { }
                }

                if (IncludeSubKeys)
                {
                    foreach (string SubKeys in RK.GetSubKeyNames())
                    {
                        OutPut += RegToStr(RK.OpenSubKey(SubKeys), true);
                    }
                }
            }
            return OutPut;
        }
    }
}
