using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OcrLib
{
    public class Watcher
    {
        private FileSystemWatcher _watcher;
        private static string _destination;

        public Watcher(string path, string dest = null)
        {
            _watcher = new FileSystemWatcher();
            _watcher.Path = path;
            _destination = dest ?? path;

            // Only watch text files.
            _watcher.Filter = "*.pdf";

            // Add event handlers.
            _watcher.Created += new FileSystemEventHandler(OnCreated);
        }

        public void Start()
        {
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created && !e.Name.Contains("ocr"))
            {
                var delay = 200;
                bool finished = false;
                int retries = 8;
                while(!finished && retries > 0)
                {
                    try
                    {
                        Thread.Sleep(delay);
                        delay *= 2;
                        retries--;
                        var scanner = new OcrService(_destination);
                        scanner.Scan(e.FullPath);
                        finished = true;
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Retrying due to {ex.Message}.");
                    }
                }
                if (!finished)
                {
                    Console.WriteLine($"Problems scanning {e.FullPath}");
                }
            }
        }
    }
}
