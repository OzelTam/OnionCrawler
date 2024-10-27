namespace OnionCrawler.Lib
{
    public class CrawlerOptions
    {
        public string ProxyHost { get; set; } = "socks5://127.0.0.1";
        public int ProxyPort { get; set; } = 9050;
        public int MaxRetry { get; set; } = 3;
        public int MaxDepth { get; set; } = 10;
        public int MaxPages { get; set; } = 1000;
        public int MaxThreads { get; set; } = 3;
        public bool ForceThreadCount { get; set; }
        public bool IncludeClearnet { get; set; }
        public bool IncludeOnion { get; set; } = true;
        public bool IncludeI2P { get; set; }
        public bool IncludeIP { get; set; }

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(20);

        public int BatchSize { get; set; } = 200;
        public string? PagesSavePath { get; set; }
        public int MaxInQueue { get; set; } = 1000;

        public string? FailedLinksSavePath { get; set; }
        public string? ExternalLinksSavePath { get; set; }

        public bool ValidationErrors(out List<string> errors)
        {
            errors = new List<string>();
            if(MaxRetry < 0)
            {
                errors.Add("MaxRetry must be greater than 0");
            }
            if(MaxDepth < 0)
            {
                errors.Add("MaxDepth must be greater than 0");
            }

            if(MaxPages < 0)
            {
                errors.Add("MaxPages must be greater than 0");
            }

            if(MaxThreads < 0)
            {
                errors.Add("MaxThreads must be greater than 0");
            }

            if(BatchSize < 0)
            {
                errors.Add("BatchSize must be greater than 0");
            }

            if(MaxInQueue < 0)
            {
                errors.Add("MaxInQueue must be greater than 0");
            }

            if(Timeout < TimeSpan.FromSeconds(1))
            {
                errors.Add("Timeout must be greater than 1 second");
            }

            return errors.Any() ? false : true;
        }

    }

}

