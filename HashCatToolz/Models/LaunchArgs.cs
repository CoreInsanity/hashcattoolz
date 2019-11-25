using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace HashCatToolz.Models
{
    class LaunchArgs
    {
        [Option('i', "input", Required = true, HelpText = "Path to directory containing files to be processed")]
        public string InDir { get; set; }

        [Option('d', "dictionary", Required = true, HelpText = "Path to directory containing dictionary files")]
        public string DictDir { get; set; }

        [Option('c', "convert", Required = false, HelpText = "Automatically convert source files to HCCAPX format")]
        public bool AutoConvert { get; set; }

        [Option('o', "output", Required = true, HelpText = "Dump results here")]
        public string OutDir { get; set; }

        public string HashCatExec { get; set; } = "hashcat32.exe";
    }
}
