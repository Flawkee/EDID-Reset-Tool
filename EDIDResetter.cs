using System;
using System.IO;
using System.Threading;
using System.ServiceProcess;
using System.Reflection;
using System.Net;

namespace EDIDResetTool
{
    /// <summary>
    /// Orchestrates the EDID handshake reset sequence.
    /// </summary>
    public class EDIDResetter
    {
        private readonly Settings _settings;

        // Helper method to extract and save the embedded EXE
        public static string GetEmbeddedExecutablePath(string resourceName)
        {
            // Find the assembly where the resource is embedded
            Assembly assembly = Assembly.GetExecutingAssembly();
            string assemblyName = assembly.GetName().Name;

            // The resource name is usually the full name including the assembly name and any folder structure
            // A common pattern is "YourProjectName.ControlMyMonitor.exe"
            string fullResourceName = $"{assemblyName}.{resourceName}";

            string tempFilePath = Path.Combine(Path.GetTempPath(), resourceName);

            if (!File.Exists(tempFilePath))
            {
                using (Stream resourceStream = assembly.GetManifestResourceStream(fullResourceName))
                {
                    if (resourceStream == null)
                    {
                        throw new FileNotFoundException($"Embedded resource '{fullResourceName}' not found.");
                    }
                    using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }
            }

            return tempFilePath;
        }

        public EDIDResetter(Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Executes the full EDID reset sequence.
        /// </summary>
        public void ExecuteReset()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Initiating EDID handshake reset...");

            // Step 1: Toggle input as fallback (embedded ControlMyMonitor)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Toggling input as fallback...");
            var tempCMMPath = GetEmbeddedExecutablePath("ControlMyMonitor.exe");
            ProcessRunner.RunProcess(tempCMMPath, $"/SetValue \"{_settings.MonitorId}\" 60 {_settings.AlternateInput}");
            Thread.Sleep(_settings.SleepAfterFirstToggleMs);

            ProcessRunner.RunProcess(tempCMMPath, $"/SetValue \"{_settings.MonitorId}\" 60 {_settings.OriginalInput}");
            Thread.Sleep(_settings.SleepAfterSecondToggleMs);

            // Step 2: Force hardware rescan
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Scanning for hardware changes...");
            ProcessRunner.RunProcess("pnputil", "/scan-devices");
            Thread.Sleep(_settings.SleepAfterRescanMs);

            // Step 3: Cycle audio device (embedded devcon)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Cycling audio device...");
            var tempDevconPath = GetEmbeddedExecutablePath("devcon.exe");
            ProcessRunner.RunProcess(tempDevconPath, $"disable {_settings.AudioId}");
            Thread.Sleep(_settings.SleepAfterDisableMs);
            ProcessRunner.RunProcess(tempDevconPath, $"enable {_settings.AudioId}");
            Thread.Sleep(_settings.SleepAfterEnableMs);

            // Step 4: Restart audio services
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Restarting audio services...");
            var sc = new ServiceController();
            RestartWindowsService("AudioEndpointBuilder");
            RestartWindowsService("AudioSrv");
        }
        private void RestartWindowsService(string serviceName)
        {
            ServiceController serviceController = new ServiceController(serviceName);
            if ((serviceController.Status.Equals(ServiceControllerStatus.Running)) || (serviceController.Status.Equals(ServiceControllerStatus.StartPending)))
            {
                serviceController.Stop();
            }
            serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running);
        }
    }
}