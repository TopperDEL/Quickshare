﻿using CommandLine;
using Newtonsoft.Json;
using Quickshare;
using System.Reflection;
using uplink.NET.Interfaces;
using uplink.NET.Models;
using uplink.NET.Services;

var assembly = Assembly.GetExecutingAssembly();
var assemblyVersion = assembly.GetName().Version;
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine($"\nWelcome to Quickshare v{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build} powered by Storj DCS (https://storj.io)!\n");
Console.ForegroundColor = ConsoleColor.Gray;

string configLocation = Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + @"\quickshare.config";
if (!File.Exists(configLocation))
{
    Parser.Default.ParseArguments<CommandLineConfigOptions>(args)
        .WithParsed(o =>
        {
            var quickshareConfig = new QuickshareConfig { AccessGrant = o.AccessGrant, BucketName = o.BucketName };
            var configJson = JsonConvert.SerializeObject(quickshareConfig);
            File.WriteAllText(configLocation, configJson);

            Console.WriteLine("Your configuration has been set! You may now use 'quickshare FILENAME' to share a file.");
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
        Console.WriteLine("Sharing file '" + s.Filename + "'.");

        Access access = new Access(quickshareConfig.AccessGrant);
        IBucketService bucketService = new BucketService(access);
        Console.WriteLine("Checking bucket...");
        var bucket = bucketService.EnsureBucketAsync(quickshareConfig.BucketName).Result;
        Console.WriteLine("Starting upload...");
        IObjectService objectService = new ObjectService(access);
        var uploadOperation = objectService.UploadObjectAsync(bucket, s.Filename, new UploadOptions(), File.ReadAllBytes(s.Filename), false).Result;
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
                Console.Write("\rDone!            \n");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        };
        uploadOperation.StartUploadAsync().Wait();

        if (!uploadOperation.Completed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Upload failed: " + uploadOperation.ErrorMessage);
            return;
        }

        Console.WriteLine("Preparing file for sharing...");
        var url = access.CreateShareURL(quickshareConfig.BucketName, s.Filename, true, true).Replace("gateway", "link");
        Console.WriteLine("Your URL is:");
        Console.WriteLine(url);
        TextCopy.ClipboardService.SetText(url);
        Console.WriteLine("It has been copied to the clipboard.");
    });
}

