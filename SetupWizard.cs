using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text.RegularExpressions;

namespace EDIDResetTool
{
    /// <summary>
    /// Interactive wizard for initial configuration on first run.
    /// </summary>
    public class SetupWizard
    {
        private readonly string _iniPath;
        private readonly List<(string DeviceName, string MonitorId)> _monitors = new();
        private readonly List<(string Name, string CleanedId)> _audioDevices = new();

        public SetupWizard(string iniPath)
        {
            _iniPath = iniPath;
        }

        public void ResetConfig()
        {
            if (File.Exists(_iniPath))
            {
                File.Delete(_iniPath);
            }
        }

        public void Run()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== EDID Reset Tool - Initial Setup Wizard ===");
            Console.WriteLine("This will configure your monitor and audio settings.");
            Console.WriteLine("Press Enter to continue...\n");
            Console.ReadLine();

            // Step 1: Detect Monitors
            Console.WriteLine("- Step 1: Detecting available monitors...");
            DetectMonitors();
            if (_monitors.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No monitors detected. Ensure ControlMyMonitor is embedded correctly.");
                Console.ReadKey();
                return;
            }

            string monitorId = SelectMonitor();

            // Step 2: Detect AMD Audio Devices
            Console.WriteLine("\n- Step 2: Detecting HDMI audio devices...");
            DetectAudioDevices();
            if (_audioDevices.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No audio devices found. Ensure devcon is embedded correctly and HDMI Audio Device is configured.");
                Console.ReadKey();
                return;
            }

            string AudioId = SelectAudioDevice();

            // Step 3: Configure Input Toggles (VCP 60)
            Console.WriteLine("\n- Step 3: Configuring Input Toggles to allow Monitor EDID Refresh...");
            Console.WriteLine("\nGetting Monitor VCP Values using DDC/CI");
            int OriginalInput = GetMonitorVCP(monitorId);
            int SuggestedAlternateInput = OriginalInput + 1;
            if (OriginalInput == -1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to read monitor VCP values. Please ensure DDC/CI is enabled for your monitor and try again.");
                return;
            }
            Console.WriteLine($"Detected current input (VCP 60) as: {OriginalInput}");
            Console.WriteLine($"Suggested alternate input (VCP 60) as: {SuggestedAlternateInput}");
            Console.WriteLine("You can accept the defaults or enter custom value for alternate input.\n");
            int AlternateInput = PromptInt($"Enter alternate input value (press ENTER for default {SuggestedAlternateInput}): ", SuggestedAlternateInput);

            // Step 4: Configure Sleep Intervals
            Console.WriteLine("\n- Step 4: Configure Sleep Intervals (in seconds)\n");
            int sleepFirst = PromptInt("Sleep after first toggle (press ENTER for default 5 seconds): ", 5);
            int sleepSecond = PromptInt("Sleep after second toggle (press ENTER for default 2 seconds): ", 2);
            int sleepRescan = PromptInt("Sleep after rescan (press ENTER for default 2 seconds): ", 2);
            int sleepDisable = PromptInt("Sleep after disable (press ENTER for default 3 seconds): ", 3);
            int sleepEnable = PromptInt("Sleep after enable (press ENTER for default 2 seconds): ", 2);
            Console.WriteLine("");


            // Write to INI
            WriteConfig(monitorId, AlternateInput, OriginalInput, AudioId, sleepFirst, sleepSecond, sleepRescan, sleepDisable, sleepEnable);

            // Step 5: Ask the user if a schedule task is needed
            Console.WriteLine("\n- Step 5: Configure Scheduled Task\n");
            string schedResult = "";
            while (schedResult != "Y" && schedResult != "N")
            {
                Console.WriteLine("Would you like to configure a scheduled task to run this tool automatically on logon and resuming from sleep? (Y/N)");
                schedResult = Console.ReadLine().ToUpper();
            }

            if (schedResult == "Y")
            {
                CreateScheduledTask();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nSetup complete!");
            Console.WriteLine("Please run the tool again to execute EDID reset.\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("To change or reset the configuration, run the tool with the --help flag to view available options and instructions.\n");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(0);
        }

        private void DetectMonitors()
        {
            string smonitorsPath = Path.Combine(Path.GetTempPath(), "EDIDResetTool_smonitors.txt");
            RunControlMyMonitor($"/smonitors {smonitorsPath}");
            var output = File.ReadAllText(smonitorsPath);
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string currentDevice = null;
            string currentId = null;
            foreach (var line in lines)
            {
                if (line.StartsWith("Monitor Device Name:"))
                {
                    // Save previous if exists
                    if (currentDevice != null && currentId != null)
                    {
                        _monitors.Add((currentDevice.Trim(), currentId.Trim()));
                    }
                    currentDevice = line.Substring("Monitor Device Name: ".Length).Trim();
                    currentId = null;
                }
                else if (line.StartsWith("Monitor ID:") && currentDevice != null)
                {
                    currentId = line.Substring("Monitor ID: ".Length).Trim();
                }
            }
            // Add the last one
            if (currentDevice != null && currentId != null)
            {
                _monitors.Add((currentDevice.Trim(), currentId.Trim()));
            }
        }

        private string SelectMonitor()
        {
            Console.WriteLine("\nAvailable Monitors:");
            for (int i = 0; i < _monitors.Count; i++)
            {
                var (device, id) = _monitors[i];
                Console.WriteLine($"{i + 1}. Device Name: {device}, Monitor ID: {id}");
            }
            int choice = PromptInt("\nSelect monitor number: ", 1) - 1;
            if (choice < 0 || choice >= _monitors.Count)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid selection. Using first monitor.");
                Console.ResetColor();
                choice = 0;
            }
            return _monitors[choice].DeviceName;
        }

        private int GetMonitorVCP(string monitorId)
        {
            string monitorVCPPath = Path.Combine(Path.GetTempPath(), "EDIDResetTool_monitorVCP.txt");
            RunControlMyMonitor($"/stext {monitorVCPPath} {monitorId}");
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(5000);
                if (File.Exists(monitorVCPPath))
                {
                    break;
                }
            }

            var output = File.ReadAllText(monitorVCPPath);
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("Input Select"))
                {
                    var valueLine = lines[i + 2];
                    var match = Regex.Match(valueLine, @"^Current Value\s+:\s([0-9]{2})$");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int vcpValue))
                    {
                        return vcpValue;
                    }
                }
            }
            return -1; // Not found
        }

        private void DetectAudioDevices()
        {
            string devconOutputPath = Path.Combine(Path.GetTempPath(), "devcon.txt");
            var output =  RunDevcon("find *HDAUDIO*");
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains(": "))
                {
                    var parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        string fullId = parts[0].Trim();
                        string name = parts[1].Trim();
                        string cleanedId = CleanHardwareId(fullId);
                        _audioDevices.Add((name, cleanedId));
                    }
                }
            }
        }
        private string CleanHardwareId(string fullId)
        {
            // Match up to &DEV_XXXX, removing &SUBSYS_..., &REV_..., and trailing \instance
            var match = Regex.Match(fullId, @"^HDAUDIO\\FUNC_01&VEN_[0-9A-F]+&DEV_[0-9A-F]+");
            return match.Success ? match.Value : fullId;
        }

        private string SelectAudioDevice()
        {
            Console.WriteLine("\nAvailable Audio Devices:");
            for (int i = 0; i < _audioDevices.Count; i++)
            {
                var (name, id) = _audioDevices[i];
                Console.WriteLine($"{i + 1}. Name: {name}, ID: {id}");
            }
            int choice = PromptInt("\nSelect audio device number: ", 1) - 1;
            if (choice < 0 || choice >= _audioDevices.Count)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid selection. Using first device.");
                Console.ResetColor();
                choice = 0;
            }
            return _audioDevices[choice].CleanedId;
        }

        private int PromptInt(string prompt, int defaultValue)
        {
            Console.Write(prompt);
            string input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }
            return int.TryParse(input, out int val) ? val : defaultValue;
        }

        private void WriteConfig(string monitorId, int AlternateInput, int OriginalInput, string AudioId, int sleepFirst, int sleepSecond, int sleepRescan, int sleepDisable, int sleepEnable)
        {
            IniParser.WriteIni("Settings", "MonitorId", monitorId, _iniPath);
            IniParser.WriteIni("Settings", "AlternateInput", AlternateInput.ToString(), _iniPath);
            IniParser.WriteIni("Settings", "OriginalInput", OriginalInput.ToString(), _iniPath);
            IniParser.WriteIni("Settings", "AudioId", AudioId, _iniPath);
            IniParser.WriteIni("Settings", "SleepAfterFirstToggle", sleepFirst.ToString(), _iniPath);
            IniParser.WriteIni("Settings", "SleepAfterSecondToggle", sleepSecond.ToString(), _iniPath);
            IniParser.WriteIni("Settings", "SleepAfterRescan", sleepRescan.ToString(), _iniPath);
            IniParser.WriteIni("Settings", "SleepAfterDisable", sleepDisable.ToString(), _iniPath);
            IniParser.WriteIni("Settings", "SleepAfterEnable", sleepEnable.ToString(), _iniPath);
        }

        private string RunControlMyMonitor(string args)
        {
            var tempCMMPath = EDIDResetter.GetEmbeddedExecutablePath("ControlMyMonitor.exe");
            return ProcessRunner.RunProcess(tempCMMPath, args, logOutput: false);
        }

        private string RunDevcon(string args)
        {
            var tempDevconPath = EDIDResetter.GetEmbeddedExecutablePath("devcon.exe");
            return ProcessRunner.RunProcess(tempDevconPath, args, logOutput: false, returnOutput: true);
        }

        public static void CreateScheduledTask()
        {
            // Auto-detect exe path if not provided
            string exePath = Environment.ProcessPath;

            string taskName = "EDID Reset Tool";
            using var ts = new TaskService();
            var td = ts.NewTask();

            // General: Author and Description
            td.RegistrationInfo.Author = "EDID Reset Tool";
            td.RegistrationInfo.Description = "Running EDID Reset Tool in quiet mode. Created by EDID Reset Tool.";

            // Principal: Run with highest privileges, only when user logged on
            td.Principal.RunLevel = TaskRunLevel.Highest;
            td.Principal.LogonType = TaskLogonType.InteractiveToken; // Run only when user is logged on

            // Trigger 1: At logon
            var logonTrigger = new LogonTrigger();
            td.Triggers.Add(logonTrigger);

            // Trigger 2: On Workstation Unlock (Session Unlock)
            var unlockTrigger = new SessionStateChangeTrigger();
            unlockTrigger.StateChange = TaskSessionStateChangeType.SessionUnlock;
            td.Triggers.Add(unlockTrigger);

            // Trigger 3: On event - System log, Microsoft-Windows-Kernel-Power, Event ID 107
            var eventTrigger = new EventTrigger("System", "Microsoft-Windows-Kernel-Power", 107);
            td.Triggers.Add(eventTrigger);

            // Action: Start program with --quiet
            var action = new ExecAction(exePath, "--quiet", null); // null = working directory = exe dir
            td.Actions.Add(action);

            // Settings: Optional - e.g., allow run on battery, wake machine
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.StopIfGoingOnBatteries = false;
            td.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(1);
            td.Settings.Priority = ProcessPriorityClass.Normal;

            // Register the task (updates if exists)
            ts.RootFolder.RegisterTaskDefinition(taskName, td);

            Console.WriteLine($"\nScheduled Task '{taskName}' created successfully.");
        }

        public static void DeleteScheduledTask()
        {
            string taskName = "EDID Reset Tool";
            try
            {
                using var ts = new TaskService();
                var task = ts.GetTask(taskName);
                if (task != null)
                {
                    ts.RootFolder.DeleteTask(taskName);
                    Console.WriteLine($"Scheduled Task '{taskName}' deleted.");
                }
                else
                {
                    Console.WriteLine($"Scheduled Task '{taskName}' not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete Scheduled Task: {ex.Message}");
            }
        }
    }
}