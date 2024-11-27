using CommandLine.Text;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quickshare
{
    [Verb("sharefolder", HelpText = "Share a folder.")]
    public class CommandLineShareFolderOptions
    {
        [Option('r', "root", Required = true, Default = true, HelpText = "The folder-root to upload.")]
        public string Path { get; set; }

        [Option('s', "sub", Default = false, HelpText = "Select the folder contents recursively.")]
        public bool Sub { get; set; }

        [Option('u', "upload-only", Default = false, HelpText = "Only upload the files but do not create share-URLs")]
        public bool UploadOnly { get; set; }

        [Option('d', "duration", Required = false, HelpText = "The timespan for how long the file should be shared. It will be deleted and be unaccessable after that.\nUse english phrases like '20 minutes' or '3 weeks'.")]
        public string Duration { get; set; }
    }
}
