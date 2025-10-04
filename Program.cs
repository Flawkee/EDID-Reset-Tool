using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EDIDResetTool
{
    class Program
    {
        public static bool _quietMode = false;
        static void Main(string[] args)
        {
            string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            bool allowExec = true;

            // Process command-line arguments
            if (args.Length > 0)
            {
                Console.WriteLine("=== EDID Reset Tool ===\n");
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = "";
                    if (args[i] == "-s" || args[i] == "--set")
                    {
                        arg = $"{args[i]} {args[i + 1]}";
                        i++; // Skip next argument as it's part of this switch
                    }
                    else
                    {
                        arg = args[i];
                    }
                    if (args.Length > 1 && (arg == "-q" || arg == "--quiet"))
                    {
                        continue; // Defer quiet mode processing
                    }
                    bool execResult = CLISwitches.ProcessSwitch(value: arg, iniPath: iniPath);
                    if (allowExec) allowExec = execResult;                    
                }
            }

            // Show or hide console window based on quiet mode
            if (_quietMode && File.Exists(iniPath))
            {
                ConsoleWindow.Hide();
            }
            else
            {
                ConsoleWindow.Show();
            }

            // If config file doesn't exist, run setup wizard, else proceed with reset
            if (allowExec)
            {
                if (!File.Exists(iniPath))
                {
                    var wizard = new SetupWizard(iniPath);
                    wizard.Run();
                    if (File.Exists(iniPath))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Setup complete. Please review and edit {iniPath} if needed, then restart the application.");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Setup failed to complete successfully. Please validate your configuration and try again.");
                        Console.WriteLine("\n Press Enter to exit.");
                        Console.ReadLine();
                        Environment.Exit(1);
                    }
                }

                try
                {
                    var settings = new Settings(iniPath);
                    var resetter = new EDIDResetter(settings);
                    resetter.ExecuteReset();

                    if (!_quietMode)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\nEDID reset complete. Test audio in Settings > System > Sound.");
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}