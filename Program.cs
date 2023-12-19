using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MyNamespace
{
    class MyClassCS
    {
        // Path for the log file
        private static string logFilePath;

        // List to store pending changes
        private static List<string> pendingChanges = new List<string>();

        // Timer for logging pending changes
        private static Timer timer;

        // Flag to enable or disable silent mode
        private static bool SilentMode = true;

        // Main entry point of the application
        static void Main()
        {
            Console.WriteLine("Enter the path of the directory to monitor:");
            string directoryPath = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                Console.WriteLine("Invalid directory path. Exiting.");
                Environment.Exit(0);
            }

            Console.WriteLine("Enter the path of the log file:");
            logFilePath = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                Console.WriteLine("Invalid log file path. Exiting.");
                Environment.Exit(0);
            }

            // Hide the current console window
            var currentProcess = Process.GetCurrentProcess();
            NativeMethods.ShowWindow(currentProcess.MainWindowHandle, NativeMethods.SW_HIDE);

            // Create a new console window
            var newProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            newProcess.Start();

            // Create a FileSystemWatcher to monitor changes in the specified directory
            using var watcher = new FileSystemWatcher(directoryPath);

            // Specify the types of changes to monitor
            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            // Watch all files in subdirectories
            watcher.IncludeSubdirectories = true;

            // Attach event handlers for various file system events
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;

            // Enable the FileSystemWatcher to begin monitoring
            watcher.EnableRaisingEvents = true;

            // Initialize timer for logging pending changes every second
            timer = new Timer(LogPendingChanges, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            // Schedule the deletion of the log file after 30 days
            Timer deleteTimer = new Timer(DeleteLogFile, null, TimeSpan.FromDays(30), Timeout.InfiniteTimeSpan);

            // Display a message and wait for user input to exit the application
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();

            // Exit the new console window
            newProcess.StandardInput.WriteLine("exit");
            newProcess.WaitForExit();

            // Show the original console window
            NativeMethods.ShowWindow(currentProcess.MainWindowHandle, NativeMethods.SW_SHOW);
        }

        // Event handler for file creation and deletion
        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            AddPendingChange(e.ChangeType.ToString(), e.FullPath);
        }

        // Event handler for file deletion
        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            AddPendingChange(e.ChangeType.ToString(), e.FullPath);
        }

        // Event handler for file renaming
        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            AddPendingChange(e.ChangeType.ToString(), $"Old: {e.OldFullPath}, New: {e.FullPath}");
        }

        // Event handler for FileSystemWatcher errors
        private static void OnError(object sender, ErrorEventArgs e)
        {
            AddPendingChange("Error", e.GetException()?.Message);
        }

        // Add a change to the pending changes list
        private static void AddPendingChange(string changeType, string details)
        {
            string logMessage = $"{DateTime.Now} - {changeType}: {details} by {Environment.UserName}";
            pendingChanges.Add(logMessage);
        }

        // Log pending changes to the specified log file
        private static void LogPendingChanges(object state)
        {
            if (pendingChanges.Count > 0)
            {
                if (!SilentMode)
                {
                    Console.WriteLine($"Logging {pendingChanges.Count} changes...");
                }

                try
                {
                    using (StreamWriter writer = new StreamWriter(logFilePath, true))
                    {
                        foreach (string change in pendingChanges)
                        {
                            writer.WriteLine(change);
                        }
                    }
                    pendingChanges.Clear();
                }
                catch (Exception ex)
                {
                    if (!SilentMode)
                    {
                        Console.WriteLine($"Error writing to log file: {ex.Message}");
                    }
                }
            }
        }

        // Delete the log file if it exists and is older than 30 days
        private static void DeleteLogFile(object state)
        {
            try
            {
                // Check if the log file exists
                if (File.Exists(logFilePath))
                {
                    // Get the creation time of the file
                    DateTime creationTime = File.GetCreationTime(logFilePath);

                    // Check if the file is older than 30 days
                    if (DateTime.Now - creationTime > TimeSpan.FromDays(30))
                    {
                        // Delete the file
                        File.Delete(logFilePath);
                        if (!SilentMode)
                        {
                            Console.WriteLine($"Log file deleted. Path: {logFilePath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!SilentMode)
                {
                    Console.WriteLine($"Error deleting log file: {ex.Message}");
                }
            }
        }

        // NativeMethods class for interop with user32.dll
        private static class NativeMethods
        {
            public const int SW_HIDE = 0;
            public const int SW_SHOW = 5;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        }
    }
}
