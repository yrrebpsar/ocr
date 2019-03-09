using System;
using System.Collections.Generic;
using System.IO;
using Mono.Options;
using OcrLib;
using Ganss.IO;
using System.IO.Abstractions;

namespace ocrcore
{
    class Program
    {
        static string output = null;
        static void Main(string[] args)
        {
            bool show_help = false;
            bool watch = false;
            bool force = false;
            bool commit = false;
            var options = new OptionSet()
            {
                { "o|output=", "specify output Directory", v => output = v },
                { "c|commit", "commit ocr-version", v => commit = true },
                { "w|watch", "watch directory", v => watch = true },
                { "f|force", "force generation of ocr file", v=> force = true },
                { "h|help", "orccore file1 [file2 ...file-n]", v => show_help=v!=null}
            };
            var pathes = options.Parse(args);

            if (show_help)
            {
                options.WriteOptionDescriptions(Console.Out);
                System.Environment.Exit(0);
            }

            if (watch)
            {
                Watcher watcher = new Watcher(pathes[0], output);
                watcher.Start();
                Console.ReadLine();
                watcher.Stop();
            } else
            {
                foreach (var p in pathes)
                {
                    var globber = new Glob();
                    var files = globber.ExpandNames(p);
                    OcrService scanner = new OcrService(output);
                    foreach (var f in files)
                    {
                        if (commit)
                        {
                            scanner.Commit(f);
                        }
                        else
                        {
                            scanner.Scan(f, force);
                        }
                    }

                }

            }

        }
    }

}
