using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EDIDResetTool
{
    public class CLISwitches
    {
        public static bool ProcessSwitch(string value, string iniPath)
        {

            switch (value.ToLower())
            {
                case "-h":
                case "--help":
                case "/?":
                case "/help":
                    ShowHelp();
                    return false;
                case "-v":
                case "--version":
                    ShowVersion();
                    return false;
                case "-c":
                case "--config":
                    ShowConfig(iniPath);
                    return false;
                case "-r":
                case "--reset":
                    var wizard = new SetupWizard(iniPath);
                    wizard.ResetConfig();
                    return true;
                case string when value.StartsWith("-s"):
                case string when value.StartsWith("--set"):
                    SetConfig(value, iniPath);
                    return false;
                case "-q":
                case "--quiet":
                    Program._quietMode = true;
                    return true;
                case "-cst":
                case "--createscheduledtask":
                    SetupWizard.CreateScheduledTask();
                    return false;
                case "-dst":
                case "--deletescheduledtask":
                    SetupWizard.DeleteScheduledTask();
                    return false;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Unknown switch provided: {value}");
                    Console.ResetColor();
                    ShowHelp();
                    return false;
            }

        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage: EDIDResetTool.exe [options]\n");
            Console.WriteLine("On first run, EDID Reset Tool Setup Wizard will execute automatically.");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  -c, --config                   Prints current configuration.");
            Console.WriteLine("  -r, --reset                    Reset current configuration and runs the Setup Wizard.");
            Console.WriteLine("  -cst, --createscheduledtask    Creates a scheduled task to run EDID Reset Tool on user logon and computer wake from sleep.");
            Console.WriteLine("  -dst, --deletescheduledtask    Deletes EDID Reset Tool Scheduled Task if exists.");
            Console.WriteLine("  -q, --quiet                    Execute the program without user prompts.");
            Console.WriteLine("  -v, --version                  Show Version.\n");
            Console.WriteLine("  -s, --set                      Set configuration value (Key=Value], Available Keys:");
            Console.WriteLine("                                     MonitorId=\"\\\\.\\DISPLAY1\\Monitor0\"");
            Console.WriteLine("                                     AlternateInput=15");
            Console.WriteLine("                                     OriginalInput=17");
            Console.WriteLine("                                     AudioId=\"HDAUDIO\\FUNC_01&VEN_1002&DEV_AA01\"");
            Console.WriteLine("                                     SleepAfterFirstToggle=5");
            Console.WriteLine("                                     SleepAfterSecondToggle=5");
            Console.WriteLine("                                     SleepAfterRescan=5");
            Console.WriteLine("                                     SleepAfterDisable=5");
            Console.WriteLine("                                     SleepAfterEnable=5");
        }

        private static void ShowVersion()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Console.WriteLine($"EDID Reset Tool v{version}\n");
            Console.WriteLine("Copyright (c) 2025 Nir Hazan. Licensed under the MIT License.");
            Console.WriteLine("Free to fork, modify, and distribute. See LICENSE for details.\n");
        }

        private static void ShowConfig(string iniPath)
        {
            if (File.Exists(iniPath))
            {
                var settings = new Settings(iniPath);
                Console.WriteLine("Current Configuration:\n");
                Console.WriteLine($"MonitorId: {settings.MonitorId}");
                Console.WriteLine($"AlternateInput: {settings.AlternateInput}");
                Console.WriteLine($"OriginalInput: {settings.OriginalInput}");
                Console.WriteLine($"AudioId: {settings.AudioId}");
                Console.WriteLine($"SleepAfterFirstToggle: {settings.SleepAfterFirstToggleMs / 1000} seconds");
                Console.WriteLine($"SleepAfterSecondToggle: {settings.SleepAfterSecondToggleMs / 1000} seconds");
                Console.WriteLine($"SleepAfterRescan: {settings.SleepAfterRescanMs / 1000} seconds");
                Console.WriteLine($"SleepAfterDisable: {settings.SleepAfterDisableMs / 1000} seconds");
                Console.WriteLine($"SleepAfterEnable: {settings.SleepAfterEnableMs / 1000} seconds\n");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Configuration file not found. Please run the setup wizard first.");
                Console.ResetColor();
            }
        }

        private static void SetConfig(string value, string iniPath)
        {
            var components = value.Replace("--set", "").Replace("-s", "").Trim().Split('=', 2);
            if (components.Length != 2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid format for --set. Use --set Key=Value");
                Console.ResetColor();
                return;
            }
            IniParser.WriteIni("Settings", components[0], components[1], iniPath);
        }
    }
}
