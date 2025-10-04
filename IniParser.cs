using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace EDIDResetTool
{
    /// <summary>
    /// Utility class for reading and writing INI files.
    /// </summary>
    public static class IniParser
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern long GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        /// Reads a value from the specified section and key in the INI file.
        public static string ReadIni(string section, string key, string filePath, string defaultValue = "")
        {
            StringBuilder buffer = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, buffer, buffer.Capacity, filePath);
            return buffer.ToString();
        }

        /// Writes a value to the specified section and key in the INI file.
        public static void WriteIni(string section, string key, string value, string filePath)
        {
            string currentValue = "";
            // Read current value
            if (File.Exists(filePath))
            {
                currentValue = ReadIni(section, key, filePath, null);
            }

            if (currentValue == value)
            {
                Console.WriteLine($"Value for [{section}] {key} is already '{value}'. No change needed.");
                return;
            }

            // Write (adds if not exists, replaces if exists)
            WritePrivateProfileString(section, key, value, filePath);

            if (string.IsNullOrEmpty(currentValue))
            {
                Console.WriteLine($"Added [{section}] {key} = '{value}'");
            }
            else
            {
                Console.WriteLine($"Updated [{section}] {key} from '{currentValue}' to '{value}'");
            }
        }
    }
}