using CommandLine.Text;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quickshare
{
    [Verb("config", HelpText = "Configure Quickshare.")]
    public class CommandLineConfigOptions
    {
        [Option('g', "access-grant", Required = true, HelpText = "The access grant on Storj DCS, that should be used.")]
        public string AccessGrant { get; set; }

        [Option('b', "bucket-name", Required = true, HelpText = "The name of the bucket that should be used.")]
        public string BucketName { get; set; }
    }
}
