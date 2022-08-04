using CommandLine.Text;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quickshare
{
    [Verb("share", isDefault:true, HelpText = "Share a file.")]
    public class CommandLineShareOptions
    {
        [Option('f', "file", Required = true, Default = true, HelpText = "The file to upload.")]
        public string Filename { get; set; }

        [Option('d', "duration", Required = false, HelpText = "The timespan for how long the file should be shared. It will be deleted and be unaccessable after that.\nUse english phrases like '20 minutes' or '3 weeks'.")]
        public string Duration { get; set; }
    }
}
