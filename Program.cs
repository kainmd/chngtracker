using System;
using System.IO;

namespace MyNamespace
{
    class NewClassCS
    {
        private static string logFilePath;
        private static DateTime lastLogFileCreationTime;

        static void Main()
        {
            Console.WriteLine("Enter the path to the folder you want to monitor:");
            string folderPath = Console.ReadLine();

            // Validate folder path
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                Console.WriteLine("Invalid folder path. Exiting program.");
                return;
            }

            Console.WriteLine("Enter the path for the log file (or press Enter to use default 'log.txt'):");
            string userInput = Console.ReadLine();

            // Set default log file path if user input is empty or whitespace
            logFilePath = string.IsNullOrWhiteSpace(userInput) ? "c:\\files\\logs.txt" : userInput.Trim();

            // Validate the log file path
            if (Path.GetInvalidFileNameChars().Any(c => logFilePath.Contains(c)))
            {
                Console.WriteLine("Invalid log file path. Exiting.");
                return;
            }

            // Initialize lastLogFileCreationTime with the current date and time
            lastLogFileCreationTime = DateTime.Now;

            using var watcher = new FileSystemWatcher(folderPath);

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;
            
            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = true; // Monitor subdirectories
            watcher.EnableRaisingEvents = true;

            Console.WriteLine($"Monitoring folder: {folderPath}");
            Console.WriteLine($"Logging changes to: {logFilePath}");
            Console.WriteLine("Press enter to exit.");

            Console.ReadLine(); // Wait for user input

            // Redirect console output to the log file
            using (var logStream = new StreamWriter(logFilePath, append: true))
            {
                Console.SetOut(logStream);

                Console.ReadLine(); // Wait for user input

                // Restore standard output
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
            }
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Created: {e.FullPath}");

            // Check if 30 days have passed since the last log file creation
            if ((DateTime.Now - lastLogFileCreationTime).TotalDays >= 30)
            {
                // Create a new log file
                CreateNewLogFile();
            }
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Deleted: {e.FullPath}");
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"Renamed:");
            Console.WriteLine($"    Old: {e.OldFullPath}");
            Console.WriteLine($"    New: {e.FullPath}");
        }

        private static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private static void CreateNewLogFile()
        {
            // Create a backup of the current log file with a timestamp in the filename
            string backupFileName = $"log_backup_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string backupFilePath = Path.Combine(Path.GetDirectoryName(logFilePath), backupFileName);

            try
            {
                // Rename the current log file to the backup file
                File.Move(logFilePath, backupFilePath);

                // Create a new log file
                using (var logStream = new StreamWriter(logFilePath, append: true))
                {
                    Console.SetOut(logStream);
                }

                // Update lastLogFileCreationTime with the current date and time
                lastLogFileCreationTime = DateTime.Now;

                Console.WriteLine($"New log file created: {logFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating new log file: {ex.Message}");
            }
        }

        private static void PrintException(Exception? ex)
        {
            if (ex != null)
            {
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                PrintException(ex.InnerException);
            }
        }
    }
}
