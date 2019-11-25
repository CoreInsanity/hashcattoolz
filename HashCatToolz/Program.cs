using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashCatToolz.Models;
using HashCatToolz.Helpers;
using CommandLine;
using System.IO;
using System.Net.Http;
using System.Diagnostics;

namespace HashCatToolz
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "HashCatToolz";
            Parser.Default.ParseArguments<LaunchArgs>(args)
    .WithParsed<LaunchArgs>(opts => Cont(opts))
    .WithNotParsed<LaunchArgs>((errs) => Environment.Exit(1));
        }
        static void Cont(LaunchArgs opts)
        {
            try
            {
                if (!ValidateArgs(opts)) Die("Couldn't find one or more required files, exiting.");
                if (opts.AutoConvert) ConversionHelper.ConvertHandshakes(opts).Wait();
                RunHashCat(opts);
            }
            catch(Exception ex)
            {
                Die(ex.Message);
            }
        }
        static void Die(string finalWords)
        {
            Console.WriteLine(finalWords);
            Environment.Exit(1);
        }
        static void RunHashCat(LaunchArgs opts)
        {
            Console.WriteLine();
            Console.WriteLine(
                @"
_________________________________________________________
   _____ _____            _____ _  _______ _   _  _____ 
  / ____|  __ \     /\   / ____| |/ /_   _| \ | |/ ____|
 | |    | |__) |   /  \ | |    | ' /  | | |  \| | |  __ 
 | |    |  _  /   / /\ \| |    |  <   | | | . ` | | |_ |
 | |____| | \ \  / ____ \ |____| . \ _| |_| |\  | |__| |
  \_____|_|  \_\/_/    \_\_____|_|\_\_____|_| \_|\_____|
_________________________________________________________");
            var outDir = Path.Combine(opts.OutDir + @"\" + DateTime.Now.ToString("dd-MM-yy hh,mm"));
            Directory.CreateDirectory(outDir);
            foreach (var handshake in Directory.GetFiles(opts.InDir, "*.hccapx"))
            {
                var hskDir = Path.Combine(outDir, Path.GetFileNameWithoutExtension(handshake));
                Directory.CreateDirectory(hskDir);

                var crackedList = new List<string>();
                foreach (var dictionary in Directory.GetFiles(opts.DictDir).OrderBy(d => new FileInfo(d).Length))
                {
                    var logFile = Path.Combine(hskDir, Path.GetFileNameWithoutExtension(dictionary) + ".txt");
                    using (var sw = new StreamWriter(logFile))
                    {
                        Console.Write("{0}: Running {1} {2} ", DateTime.Now.ToString("hh:mm:ss"), Path.GetFileNameWithoutExtension(handshake), Path.GetFileNameWithoutExtension(dictionary));
                        ProcessStartInfo hashCatInf = new ProcessStartInfo(opts.HashCatExec, String.Format("-m 2500 -w 4 \"{0}\" \"{1}\"", handshake, dictionary))
                        {
                            UseShellExecute = false,
                            RedirectStandardOutput = true
                        };

                        var hashCatProc = new Process()
                        {
                            StartInfo = hashCatInf
                        };

                        hashCatProc.OutputDataReceived += (sender, args) =>
                        {
                            sw.WriteLine(args.Data);
                            sw.Flush();
                        };

                        hashCatProc.Start();

                        hashCatProc.BeginOutputReadLine();

                        hashCatProc.WaitForExit();

                        sw.Close();
                    }
                    using (var sr = new StreamReader(logFile))
                    {
                        var res = sr.ReadToEnd();

                        if (!res.Contains("Exhausted"))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(" CRACKED!");
                            crackedList.Add(String.Format("{0} - {1}", Path.GetFileNameWithoutExtension(handshake), Path.GetFileNameWithoutExtension(dictionary)));
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(" Exhausted");
                        }
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                }
                if (crackedList.Any())
                    using (var sw = new StreamWriter(Path.Combine(hskDir, "cracked.txt")))
                        foreach (var hsk in crackedList)
                            sw.WriteLine(hsk);
                
            }
        }

        static bool ValidateArgs(LaunchArgs opts)
        {
            opts.InDir = Path.GetFullPath(opts.InDir);
            opts.OutDir = Path.GetFullPath(opts.OutDir);
            opts.DictDir = Path.GetFullPath(opts.DictDir);

            try
            {
                if (File.Exists("hashcat64.exe")) opts.HashCatExec = "hashcat64.exe";
                if (!File.Exists(opts.HashCatExec)) return false;
                if (!Directory.Exists(opts.OutDir)) Directory.CreateDirectory(opts.OutDir);
                if (opts.AutoConvert && Directory.GetFiles(opts.InDir, "*.pcap").Length == 0) return false;
                if (!opts.AutoConvert && Directory.GetFiles(opts.InDir, "*.hccapx").Length == 0) return false;
                if (Directory.GetFiles(opts.DictDir).Length == 0) return false;
            }
            catch (Exception ex)
            {
                Die(ex.Message);
            }

            return true;
        }

    }
}
