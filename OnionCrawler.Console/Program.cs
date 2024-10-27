// See https://aka.ms/new-console-template for more information


using CommandLine;
using OnionCrawler.Lib;
using System.Net;
using static System.Net.WebRequestMethods;

Console.Clear();
Console.SetCursorPosition(0,0);
Console.WriteLine("============== OnionCrawler v2.0.0 @OzelTam ===================\n\n");

await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(async (opt) =>
{
    var crawler = new Crawler();
    crawler.Configure(options =>
    {
        options.MaxRetry = opt.MaxRetry;
        options.MaxDepth = opt.MaxDepth;
        options.MaxPages = opt.MaxPages;
        options.MaxThreads = opt.MaxThreads;
        options.IncludeClearnet = opt.IncludeClearnet;
        options.IncludeOnion = opt.IncludeOnion;
        options.IncludeI2P = opt.IncludeI2P;
        options.IncludeIP = opt.IncludeIP;
        options.Timeout = TimeSpan.FromSeconds(opt.Timeout);
        options.BatchSize = opt.BatchSize;
        options.MaxInQueue = opt.MaxInQueue;
        options.FailedLinksSavePath = opt.FailedLinksPath;
        options.PagesSavePath = opt.Filepath;
        options.ProxyHost = opt.Socks5Host;
        options.ProxyPort = opt.Port;
        options.ExternalLinksSavePath = opt.ExternalLinksPath;
        options.ForceThreadCount = opt.ForceThreadCount;

    });


    var res = await crawler.CheckProxyIsUp();

    if (!res)
    {
        Console.WriteLine("Proxy check is failed. Possible Reasons:");
        Console.WriteLine("    -Proxy is not set up (download tor expert bundle: https://www.torproject.org/download/tor/)");
        Console.WriteLine($"    -Proxy host/port is not right {opt.Socks5Host}:{opt.Port} (Sample: \"socks5://127.0.0.1\":9050 )");
        Console.WriteLine($"    -Pinging url is down {opt.PingUrl}");
        Console.WriteLine();
        Console.WriteLine($"Make sure the configuration is right.");
        Console.WriteLine($"Press any key to exit...");
        Console.ReadKey();
        return;
    }



    await Task.Delay(100);
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();





    while (true)
    {
        if (!crawler.IsCrawling && stopwatch.Elapsed.TotalSeconds > 1)
        {
            break;
        }
        else if (!crawler.IsCrawling)
        {
            crawler.Crawl(opt.RootUrl);
        }

        Console.SetCursorPosition(0, 3);
        Console.WriteLine($"Crawling: {crawler.Pages.Count + crawler.SavedBatches * opt.BatchSize} pages found, {crawler.InProgress} in progress");
        Console.SetCursorPosition(0, 4);
        Console.WriteLine($"Queue: {crawler._queue.Count} links in queue");
        Console.SetCursorPosition(0, 5);
        Console.WriteLine($"Written Batches to file: {crawler.SavedBatches}x{opt.BatchSize}");
        Console.SetCursorPosition(0, 6);
        Console.WriteLine($"Elapsed: {(int)stopwatch.Elapsed.TotalHours}:{stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds}:{stopwatch.Elapsed.Milliseconds}");
        Console.WriteLine("Press ESC to stop crawling");
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Escape)
            {
                crawler.Stop();
                break;
            }
        }
    }
    stopwatch.Stop();

    Console.WriteLine("Crawling finished");
    Console.WriteLine("\n =================== Crawling Results ===================");
    Console.WriteLine($"Root URL: {opt.RootUrl}");
    Console.WriteLine($"Elapsed: {(int)stopwatch.Elapsed.TotalHours}:{stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds}");
    Console.WriteLine($"Total Pages Found: {crawler.Pages.Count + crawler.SavedBatches * opt.BatchSize}");
    Console.WriteLine($"Total Links Found: {crawler._queue.Count + crawler.Pages.Count}");
    Console.WriteLine($"Total Hosts Found: {crawler.Pages.GroupBy(p => p.Host).Select(o => o.First().Host).Count()}");
    Console.WriteLine($"Total Onion Links Found: {crawler.Pages.Count(p => p.Type == LinkType.Onion) + crawler._queue.Count}");
    Console.WriteLine($"Total Clearnet Links Found (Last Batch): {crawler.Pages.Count(p => p.Type == LinkType.Clearnet) + crawler.ExternalLinks.Count(p => p.Type == LinkType.Clearnet)}");
    Console.WriteLine($"Total I2P Links Found (Last Batch): {crawler.Pages.Count(p => p.Type == LinkType.I2P) + crawler.ExternalLinks.Count(p => p.Type == LinkType.I2P)}");
    Console.WriteLine($"Total IP Links Found (Last Batch): {crawler.Pages.Count(p => p.Type == LinkType.IP) + crawler.ExternalLinks.Count(p => p.Type == LinkType.IP)}");
    Console.WriteLine("=========================================================\n");

    if (!string.IsNullOrEmpty(opt.Filepath))
    {
        if (opt.Filepath != null)
        {
            crawler.SaveToCsv(opt.Filepath, crawler.Pages);
            Console.WriteLine($"Fetched WebPages are saved to {opt.Filepath}");
        }
        if (opt.FailedLinksPath != null)
        {
            crawler.SaveToCsv(opt.FailedLinksPath, crawler.Failed);
            Console.WriteLine($"Failed links are saved to {opt.FailedLinksPath}");
        }
        if (opt.ExternalLinksPath != null)
        {
            crawler.SaveToCsv(opt.ExternalLinksPath, crawler.ExternalLinks);
            Console.WriteLine($"External links are saved to {opt.ExternalLinksPath}");
        }
    }


});
public class Options
{

    [Option('r', "root", Required = true, HelpText = "Root URL to start crawling.")]
    public string RootUrl { get; set; }

    [Option('p', "proxy", Required = false, HelpText = "Proxy host IP.")]
    public string Socks5Host { get; set; } = "socks5://127.0.0.1";

    [Option('o', "port", Required = false, HelpText = "Proxy port.")]
    public int Port { get; set; } = 9050;

    [Option('m', "max-retry", Required = false, HelpText = "Max retry count.")]
    public int MaxRetry { get; set; } = 1;

    [Option('t', "max-threads", Required = false, HelpText = "Max threads.")]
    public int MaxThreads { get; set; } = 3;

    [Option('C', "force-thread-count", Required = false, HelpText = "Force thread count. (Default limits is Processor Count -1)")]
    public bool ForceThreadCount { get; set; }

    [Option('T', "timeout", Required = false, HelpText = "Timeout in seconds.")]
    public int Timeout { get; set; } = 20;

    [Option('c', "include-clearnet", Required = false, HelpText = "Include clearnet.")]
    public bool IncludeClearnet { get; set; }

    [Option('n', "include-onion", Required = false, HelpText = "Include onion.")]
    public bool IncludeOnion { get; set; } = true;

    [Option('i', "include-i2p", Required = false, HelpText = "Include I2P.")]
    public bool IncludeI2P { get; set; }

    [Option('I', "include-ip", Required = false, HelpText = "Include IP.")]
    public bool IncludeIP { get; set; }

    [Option('d', "max-depth", Required = false, HelpText = "Max depth.")]
    public int MaxDepth { get; set; } = 99999;

    [Option('g', "max-pages", Required = false, HelpText = "Max pages.")]
    public int MaxPages { get; set; } = 999999999;

    [Option('u', "ping-url", Required = false, HelpText = "Ping URL to check proxy is up.")]
    public string PingUrl { get; set; } = "https://check.torproject.org/api/ip";

    [Option('f', "filepath", Required = false, HelpText = "Filepath to extract Fetched WebPages as csv.")]
    public string? Filepath { get; set; } = "results.csv";

    [Option('b', "batch-size", Required = false, HelpText = "Batch size to extract fetched WebPages to file.")]
    public int BatchSize { get; set; } = 40;

    [Option('q', "max-in-queue", Required = false, HelpText = "Max links in queue.")]
    public int MaxInQueue { get; set; } = 1000;

    [Option('F', "failed-links-path", Required = false, HelpText = "Filepath to save failed links.")]
    public string? FailedLinksPath { get; set; } = "failed.csv";

    [Option('e', "external-links-path", Required = false, HelpText = "Filepath to save external links.")]
    public string? ExternalLinksPath { get; set; }

}


