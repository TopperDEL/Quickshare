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

string configLocation = Path.Combine(appData, @"\quickshare.config");

//Check if the user wants to set new config-values
bool requestsConfigUpdate = false;
Parser.Default.ParseArguments<CommandLineConfigOptions>(args)
        .WithParsed(o =>
        {
            requestsConfigUpdate = true;
        });

if (!File.Exists(configLocation) || requestsConfigUpdate)
{
    //There is no config or the user explicitly wants to set new values
    Parser.Default.ParseArguments<CommandLineConfigOptions>(args)
        .WithParsed(o =>
        {
            var quickshareConfig = new QuickshareConfig { AccessGrant = o.AccessGrant, BucketName = o.BucketName };
            var configJson = JsonConvert.SerializeObject(quickshareConfig);
            File.WriteAllText(configLocation, configJson);

            Console.WriteLine("Your configuration has been set! You may now use 'quickshare -f FILENAME' to share a file. Add something like '-d \"1 week\" to restrict the availability of your file and delete it automatically.");
        })
        .WithNotParsed(o =>
        {
            Console.WriteLine("You need to setup Quickshare first. Call 'quickshare -g ACCESS_GRANT -b BUCKET_NAME'.");
        });
}
else
{
    var quickshareConfig = JsonConvert.DeserializeObject<QuickshareConfig>(File.ReadAllText(configLocation));
    if (quickshareConfig == null)
    {
        Console.WriteLine("Invalid config. The config has been removed - please re-init quickshare using 'quickshare config'.");
        return;
    }

    var result = Parser.Default.ParseArguments<CommandLineConfigOptions, CommandLineShareOptions>(args);
    result.WithParsed<CommandLineShareOptions>(s =>
    {
        TimeSpan shareDuration = TimeSpan.MinValue;
        if (!string.IsNullOrEmpty(s.Duration))
        {
            var parsed = TimeSpanParserUtil.TimeSpanParser.TryParse(s.Duration, out shareDuration);
            if (!parsed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not parse the share-duration. Please try a different expression.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
        }

        Console.WriteLine("Sharing file '" + s.Filename + "'.");

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
        var uploadOperation = objectService.UploadObjectAsync(bucket, s.Filename, uploadOptions, File.OpenRead(s.Filename), false).Result;
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
        var url = access.CreateShareURL(quickshareConfig.BucketName, s.Filename, true, true).Replace("gateway", "link");
        Console.WriteLine("Your URL is:");
        Console.WriteLine(url);
        TextCopy.ClipboardService.SetText(url);
        Console.WriteLine("It has been copied to the clipboard.");
        if (shareDuration != TimeSpan.MinValue)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Your shared file will expire on {0}. The file will be automatically deleted afterwards.", uploadOptions.Expires.ToString());
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    });
}


