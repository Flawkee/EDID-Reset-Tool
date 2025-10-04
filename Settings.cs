using System;

namespace EDIDResetTool
{
    /// <summary>
    /// Holds configuration settings parsed from INI file.
    /// </summary>
    public class Settings
    {
        public string MonitorId { get; }
        public int AlternateInput { get; }
        public int OriginalInput { get; }
        public int SleepAfterFirstToggleMs { get; }
        public int SleepAfterSecondToggleMs { get; }
        public int SleepAfterRescanMs { get; }
        public int SleepAfterDisableMs { get; }
        public int SleepAfterEnableMs { get; }
        public string AudioId { get; }

        public Settings(string iniPath)
        {
            MonitorId = IniParser.ReadIni("Settings", "MonitorId", iniPath, @"\\.\DISPLAY1\Monitor0");
            AlternateInput = int.Parse(IniParser.ReadIni("Settings", "AlternateInput", iniPath, "17"));
            OriginalInput = int.Parse(IniParser.ReadIni("Settings", "OriginalInput", iniPath, "15"));
            SleepAfterFirstToggleMs = int.Parse(IniParser.ReadIni("Settings", "SleepAfterFirstToggle", iniPath, "5")) * 1000;
            SleepAfterSecondToggleMs = int.Parse(IniParser.ReadIni("Settings", "SleepAfterSecondToggle", iniPath, "2")) * 1000;
            SleepAfterRescanMs = int.Parse(IniParser.ReadIni("Settings", "SleepAfterRescan", iniPath, "2")) * 1000;
            SleepAfterDisableMs = int.Parse(IniParser.ReadIni("Settings", "SleepAfterDisable", iniPath, "3")) * 1000;
            SleepAfterEnableMs = int.Parse(IniParser.ReadIni("Settings", "SleepAfterEnable", iniPath, "2")) * 1000;
            AudioId = IniParser.ReadIni("Settings", "AudioId", iniPath, "HDAUDIO\\FUNC_01&VEN_1002&DEV_AA01");
        }
    }
}