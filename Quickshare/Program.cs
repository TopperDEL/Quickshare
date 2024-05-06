using CommandLine;
using Newtonsoft.Json;
using Quickshare;
using System.Reflection;
using uplink.NET.Interfaces;
using uplink.NET.Models;
using uplink.NET.Services;
using static System.Environment;

var assembly = Assembly.GetExecutingAssembly();
var assemblyVersion = assembly.GetName().Version;
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine($"\nWelcome to Quickshare v{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build} powered by Storj DCS (https://storj.io)!");
Console.WriteLine($"It is using uplink-c {uplink.NET.Models.Access.GetStorjVersion()} and Go {uplink.NET.Models.Access.GetGoVersion()}\n");
Console.ForegroundColor = ConsoleColor.Gray;

string appData = Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.DoNotVerify), "quickshare");
if (!Directory.Exists(appData))
{
    Directory.CreateDirectory(appData);
}

string configLocation = Path.Combine(appData, "quickshare.config");

QuickshareConfig? quickshareConfig = null;

try
{
    quickshareConfig = JsonConvert.DeserializeObject<QuickshareConfig>(File.ReadAllText(configLocation));
}
catch { }

Parser.Default.ParseArguments<CommandLineConfigOptions, CommandLineShareOptions, CommandLineShareFolderOptions>(args)
        .WithParsed<CommandLineConfigOptions>(o =>
        {
            var quickshareConfig = new QuickshareConfig { AccessGrant = o.AccessGrant, BucketName = o.BucketName };
            var configJson = JsonConvert.SerializeObject(quickshareConfig);
            File.WriteAllText(configLocation, configJson);

            Console.WriteLine("Your configuration has been set! You may now use 'quickshare -f FILENAME' to share a file. Add something like '-d \"1 week\" to restrict the availability of your file and delete it automatically.");
        })
        .WithParsed<CommandLineShareOptions>(o =>
        {
            if (quickshareConfig == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid config. The config has been removed - please re-init quickshare using 'quickshare config'.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            TimeSpan shareDuration = TimeSpan.MinValue;
            if (!string.IsNullOrEmpty(o.Duration))
            {
                var parsed = TimeSpanParserUtil.TimeSpanParser.TryParse(o.Duration, out shareDuration);
                if (!parsed)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Could not parse the share-duration. Please try a different expression.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }
            }

            Console.WriteLine("Sharing file '" + o.Filename + "'.");

            Access access = new Access(quickshareConfig.AccessGrant);
            IBucketService bucketService = new BucketService(access);
            Console.WriteLine("Checking bucket...");
            var bucket = bucketService.EnsureBucketAsync(quickshareConfig.BucketName).Result;
            Console.WriteLine("Starting upload...");
            IObjectService objectService = new ObjectService(access);
            var uploadOptions = new UploadOptions();
            if (shareDuration != TimeSpan.MinValue)
            {
                uploadOptions.Expires = DateTime.Now + shareDuration;
            }
            var uploadOperation = objectService.UploadObjectAsync(bucket, o.Filename, uploadOptions, File.OpenRead(o.Filename), false).Result;
            uploadOperation.UploadOperationProgressChanged += (uploadOperation) =>
            {
                if (uploadOperation.PercentageCompleted < 100)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("\rStatus: {0}%   ", uploadOperation.PercentageCompleted);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\rDone!                    \n");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            };
            uploadOperation.StartUploadAsync().Wait();

            if (!uploadOperation.Completed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Upload failed: " + uploadOperation.ErrorMessage);
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            Console.WriteLine("Preparing file for sharing...");
            var url = access.CreateShareURL(quickshareConfig.BucketName, o.Filename, true, true).Replace("gateway", "link");
            Console.WriteLine("Your URL is:");
            Console.WriteLine(url);
            try
            {
                TextCopy.ClipboardService.SetText(url);
                Console.WriteLine("It has been copied to the clipboard.");
            }
            catch
            {
                //Might fail on e.g. Linux
            }

            if (shareDuration != TimeSpan.MinValue)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Your shared file will expire on {0}. The file will be automatically deleted afterwards.", uploadOptions.Expires.ToString());
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        })
        .WithParsed<CommandLineShareFolderOptions>(o =>
        {
            if (quickshareConfig == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid config. The config has been removed - please re-init quickshare using 'quickshare config'.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            TimeSpan shareDuration = TimeSpan.MinValue;
            if (!string.IsNullOrEmpty(o.Duration))
            {
                var parsed = TimeSpanParserUtil.TimeSpanParser.TryParse(o.Duration, out shareDuration);
                if (!parsed)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Could not parse the share-duration. Please try a different expression.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }
            }

            Console.WriteLine("Sharing the content of the folder '" + o.Path + "'.");

            Access access = new Access(quickshareConfig.AccessGrant);
            IBucketService bucketService = new BucketService(access);
            Console.WriteLine("Checking bucket...");
            var bucket = bucketService.EnsureBucketAsync(quickshareConfig.BucketName).Result;
            Console.WriteLine("Starting upload...");

            IObjectService objectServiceFolder = new ObjectService(access);
            var uploadOptionsFolder = new UploadOptions();
            if (shareDuration != TimeSpan.MinValue)
            {
                uploadOptionsFolder.Expires = DateTime.Now + shareDuration;
            }
            string htmlFileUrl;

            Dictionary<string, string> finalUrls = new Dictionary<string, string>();
            var files = Directory.GetFiles(o.Path);
            foreach (var file in files)
            {

                var uploadOperationFolder = objectServiceFolder.UploadObjectAsync(bucket, file, uploadOptionsFolder, File.OpenRead(file), false).Result;
                uploadOperationFolder.UploadOperationProgressChanged += (uploadOperation) =>
                {
                    if (uploadOperation.PercentageCompleted < 100)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("\rStatus: {0}%   ", uploadOperation.PercentageCompleted);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("\rDone!                    \n");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                };
                uploadOperationFolder.StartUploadAsync().Wait();

                if (!uploadOperationFolder.Completed)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Upload failed: " + uploadOperationFolder.ErrorMessage);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                Console.WriteLine("Preparing file '" + file + "' for sharing...");
                var urlFolder = access.CreateShareURL(quickshareConfig.BucketName, file, true, true).Replace("gateway", "link");
                finalUrls.Add(Path.GetFileName(file), urlFolder);
            }

            using (var mstream = new MemoryStream())
            {
                SimpleHtmlGenerator.GenerateHtmlFile(finalUrls, mstream);
                mstream.Seek(0, SeekOrigin.Begin);
                var uploadOperationHtml = objectServiceFolder.UploadObjectAsync(bucket, "index.html", uploadOptionsFolder, mstream, false).Result;
                uploadOperationHtml.UploadOperationProgressChanged += (uploadOperation) =>
                {
                    if (uploadOperation.PercentageCompleted < 100)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("\rStatus: {0}%   ", uploadOperation.PercentageCompleted);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("\rDone!                    \n");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                };
                uploadOperationHtml.StartUploadAsync().Wait();
                htmlFileUrl = access.CreateShareURL(quickshareConfig.BucketName, "index.html", true, true).Replace("gateway", "link");
            }

            Console.WriteLine("Your URL is:");
            Console.WriteLine(htmlFileUrl);
            try
            {
                TextCopy.ClipboardService.SetText(htmlFileUrl);
                Console.WriteLine("It has been copied to the clipboard.");
            }
            catch
            {
                //Might fail on e.g. Linux
            }

            if (shareDuration != TimeSpan.MinValue)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Your shared file will expire on {0}. The file will be automatically deleted afterwards.", uploadOptionsFolder.Expires.ToString());
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
        )
        .WithNotParsed(o =>
        {

        });
