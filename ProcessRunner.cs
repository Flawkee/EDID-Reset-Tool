using System;
using System.Diagnostics;
using System.IO;

namespace EDIDResetTool
{
    /// <summary>
    /// Utility class for running external processes quietly and capturing output/errors.
    /// </summary>
    public static class ProcessRunner
    {
        /// <summary>
        /// Runs a process with the given arguments and logs output/errors to console.
        /// </summary>
        /// <param name="fileName">The executable path (or embedded resource name).</param>
        /// <param name="arguments">The command-line arguments.</param>
        /// <param name="isEmbedded">If true, treats fileName as an embedded resource and extracts it.</param>
        /// <param name="logOutput">Whether to log stdout to console.</param>
        /// <returns>The temporary path of the extracted EXE (if embedded).</returns>
        public static string RunProcess(string fileName, string arguments, bool logOutput = true, bool returnOutput = false)
        {
            string exePath = fileName;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (logOutput && !string.IsNullOrEmpty(output))
                {
                    Console.WriteLine(output);
                }
                if (!string.IsNullOrEmpty(error))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Process Error ({fileName}): {error}");
                    Console.ResetColor();
                }
                if (returnOutput)
                {
                    return output;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to run {fileName}: {ex.Message}");
                Console.ResetColor();
            }
            return null;
        }
    }
}