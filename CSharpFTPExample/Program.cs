﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using WinSCP;

namespace CSharpFTPExample
{
    /// <summary>
    /// Command line definitions used by the CommandLine library
    /// </summary>
    class Options
    {
        // Required
        [Option('f', Required = true,
          HelpText = "The file path of the upload file")]
        public string File { get; set; }

        [Option('l', Required = true,
          HelpText = "The absolute location of where the results file should be placed")]
        public string Location { get; set; }

        [Option('u', DefaultValue = null, // (Optional)
          HelpText = "The username defined in the manage api keys section")]
        public string Key { get; set; }

        [Option('p', Required = true,
          HelpText = "The password defined in the manage api keys section")]
        public string Password { get; set; }

        // Optional
        [Option("poll", DefaultValue = 300,
          HelpText = "The number of seconds to poll for")]
        public int Poll { get; set; }

        [Option("host", DefaultValue = "localhost",
          HelpText = "The host to connect to")]
        public string Host { get; set; }

        [Option("port", DefaultValue = 21,
          HelpText = "The port to connect to")]
        public int Port { get; set; }

        [Option("singleFile", DefaultValue = false,
          HelpText = "Whether to run in single file mode")]
        public bool SingleFile { get; set; }

        [Option("remove", DefaultValue = false,
          HelpText = "Remove the corresponding results file of the uploaded file")]
        public bool Remove { get; set; }

        [Option("protocol", DefaultValue = "http",
          HelpText = "Which type of protocol to use")]
        public string Protocol { get; set; }

        [Option("notify", DefaultValue = null,
          HelpText = "The full email address to notify")]
        public string Notify { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var usage = new StringBuilder();
            usage.AppendLine("Usage: -f [file name] -l [download location] -p [password]\n");
            usage.AppendLine("Example: -f test.csv -l /tmp -p e261742d-fe2f-4569-95e6-312689d049 --poll 10");
            usage.AppendLine("Upload test.csv, process it and download the results to /tmp, poll every 10s\n");

            // Remove the copyright and version lines as they are unnecessary
            var help = HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            string[] helpArray = (help + "").Split('\n');
            usage.AppendLine(String.Join("\n", helpArray, 2, helpArray.Length - 3));

            return usage + "";
        }
    }

    /// <summary>
    /// Demonstrates how the operations object can be used. It is better to require the Operation.cs file
    /// your code directly for increased flexibility.
    /// 1. Uploads the specified file in multifile mode (unless otherwise specified).
    /// 2. Polls the server until the results are complete.
    /// 3. Downloads the results to the specified location.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var opts = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, opts))
            {
                Operations operations = new Operations(opts.Key, opts.Password, opts.Port, opts.Host, opts.Poll,
                                                       opts.Protocol, opts.Notify);
                var result = operations.Init();
                if (!result.Item1)
                {
                    throw new Exception(result.Item2);
                }

                result = operations.Upload(opts.File, opts.SingleFile, opts.Notify);
                if (!result.Item1)
                {
                    throw new Exception(result.Item2);
                }
                Console.WriteLine(result.Item2);

                operations.Download(opts.Location, opts.Remove, delegate(bool noError, string message)
                {
                    if (!noError)
                    {
                        throw new Exception(message);
                    }
                    Console.WriteLine(message);

                    if (opts.Remove)
                    {
                        Console.WriteLine("Also, removed the file from the server.");
                    }

                    Console.WriteLine("Press Enter to close this program...");
                    Console.ReadLine();
                });

                Console.WriteLine("downloading file, press enter any time to quit before downloading...");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Press Enter to close this program...");
                Console.ReadLine();
            }
        }
    }
}
