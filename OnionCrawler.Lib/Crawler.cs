using System.Buffers;
using System.Net;
using System.Security.AccessControl;
using System.Text;
using OnionCrawler.Lib.Models;

namespace OnionCrawler.Lib
{
    public enum LinkType
    {
        Onion,
        Clearnet,
        I2P,
        IP,
        Unknown
    }
    public class Crawler
    {
        public readonly Queue<QueuedLink> _queue = new();
        public readonly HashSet<ILink> _visited = new();
        public readonly HashSet<ILink> Failed = new();
        public readonly HashSet<WebPage> Pages = new();
        public readonly HashSet<ILink> ExternalLinks = new(); // Links that are not in the tor network (Clearnet, I2P, IP)
        private readonly CrawlerOptions _options = new();
        private int _currentDepth = 0;

        public event EventHandler<FetchException>? OnHttpError;
        public event EventHandler<QueuedLink>? OnFailed;
        public event EventHandler<WebPage>? OnPageFetched;
        public event EventHandler<List<QueuedLink>>? OnQueued;
        public event EventHandler<QueuedLink>? OnProcessing;
        public event EventHandler<HashSet<WebPage>>? OnCrawlingFinished;
        public int SavedBatches = 0;
        public bool IsCrawling { get; private set; }

        public Crawler()
        {
            cancellationTokenSrc = new CancellationTokenSource();
        }

        public void Configure(Action<CrawlerOptions> options)
        {
            options(_options);
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient(new HttpClientHandler
            {
                Proxy = new WebProxy(_options.ProxyHost, _options.ProxyPort),
                UseProxy = true,
                AllowAutoRedirect = false,
            });
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; rv:102.0) Gecko/20100101 Firefox/102.0");
            client.Timeout = _options.Timeout;
            return client;
        }
        public async Task<bool> CheckProxyIsUp(string pingUrl = "https://check.torproject.org/api/ip")
        {
            try
            {
                var client = CreateClient();
                var response = await client.GetAsync(pingUrl);
                return response.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        public int InProgress = 0;

        public object ProgressCounterLock = new object();

        private CancellationTokenSource cancellationTokenSrc;
        private CancellationToken? cancellationToken;

        public void Stop()
        {
            cancellationTokenSrc?.Cancel();
        }


        public bool WriteToDiskAndReset()
        {
            if (_options.PagesSavePath == null)
                return false;

            SaveToCsv(_options.PagesSavePath, Pages);

            if (_options.FailedLinksSavePath != null)
                SaveToCsv(_options.FailedLinksSavePath, Failed);
            if (_options.ExternalLinksSavePath != null)
                SaveToCsv(_options.ExternalLinksSavePath, ExternalLinks);

            lock (Failed)
                Failed.Clear();
            
            lock (Pages)
                Pages.Clear();

            lock (ExternalLinks)
                ExternalLinks.Clear();

            SavedBatches++;
            return true;
        }

        public async Task Crawl(string url)
        {
            cancellationTokenSrc = new CancellationTokenSource();
            cancellationToken = cancellationTokenSrc.Token;

            if (_options.MaxRetry < 1)
                throw new ArgumentException("MaxRetry must be greater than 0", nameof(_options.MaxRetry));
            if (_options.MaxThreads < 1)
                throw new ArgumentException("MaxThreads must be greater than 0", nameof(_options.MaxThreads));
            if (_options.MaxDepth < 0)
                throw new ArgumentException("MaxDepth must be greater than or equal to 0", nameof(_options.MaxDepth));
            if (_options.MaxPages < 0)
                throw new ArgumentException("MaxPages must be greater than or equal to 0", nameof(_options.MaxPages));



            IsCrawling = true;
            // Set max threads to be the minimum of the available threads and the max threads specified if ForceThreadCount is false
            var maxThreads = _options.MaxThreads;
            var maxAvailableThreads = Math.Max(1, Environment.ProcessorCount - 1);
            if(!_options.ForceThreadCount)
                maxThreads = Math.Min(maxThreads, maxAvailableThreads);

            var tasks = new List<Task>();
            _queue.Enqueue(new QueuedLink(url, 0));

            OnQueued?.Invoke(this, new List<QueuedLink> { _queue.Peek() });

            while (_queue.Count > 0 || InProgress > 0) // While there are links in the queue or there are links being processed
            {
                if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested || _currentDepth > _options.MaxDepth || Pages.Count > _options.MaxPages)
                    break;
                if (Pages.Count > _options.BatchSize)
                    WriteToDiskAndReset();

                if (tasks.Count < maxThreads && _queue.Count > 0) // If there are available threads and there are links in the queue
                {
                    var linkInQueue = _queue.Dequeue();
                    // Increment the number of links being processed
                    lock (ProgressCounterLock)
                    {
                        InProgress++;
                        OnProcessing?.Invoke(this, linkInQueue);
                    }
                    var newTask = Task.Run(() => ProcessLink(linkInQueue, cancellationToken));
                    tasks.Add(newTask);
                }
                else
                {
                    await Task.WhenAny(tasks);
                    tasks.RemoveAll(t => t.IsCompleted);
                }
            }
            IsCrawling = false;
            OnCrawlingFinished?.Invoke(this, Pages);
        }

        private void ProcessLink(QueuedLink queuedLink, CancellationToken? cancellationToken)
        {
            bool success = FetchWebPage(queuedLink, cancellationToken, out var html); // Fetch the page and get the html content

            if (success)
            {

                // Extract links from the html content and add them to the queue if they haven't been visited yet
                var links = Common.ExtractLinks(html, _options.IncludeOnion, _options.IncludeClearnet, _options.IncludeI2P, _options.IncludeIP);
                var queuedLinks = links.Select(l => new QueuedLink(l.Url, queuedLink.Depth + 1) { Type = l.Type }).ToList();

                lock (_queue)
                {
                    _currentDepth = queuedLink.Depth;
                    foreach (var link in queuedLinks)
                    {
                        if (link.Type == LinkType.Clearnet || link.Type == LinkType.I2P || link.Type == LinkType.IP)
                        {
                            lock (ExternalLinks)
                            {
                                ExternalLinks.Add(link.ToLink());
                            }
                        }
                        else if (_options.MaxInQueue > _queue.Count && !_visited.Contains(link))
                        {
                            _queue.Enqueue(link);
                        }
                    }
                }

                // Extract the title and description from the html content and create a new WebPage object, then add it to the Pages list
                var title = Common.ExtractTitle(html);
                var description = Common.ExtractMetaDescription(html);
                var page = new WebPage(queuedLink.Url, title, description, queuedLink.Depth) { ExtractedLinkCount = links.Count, Type = queuedLink.Type };

                lock (Pages)
                {
                    Pages.Add(page);
                    OnQueued?.Invoke(this, queuedLinks);
                    OnPageFetched?.Invoke(this, page);
                }

            }
            else // If the fetch was unsuccessful, add the link to the failed list
            {
                lock (Failed)
                {
                    Failed.Add(queuedLink);
                    OnFailed?.Invoke(this, queuedLink);
                }
            }

            lock (_visited) // Add the link to the visited list
            {
                _visited.Add(queuedLink);
            }

            lock (ProgressCounterLock) // Decrement the number of links being processed
            {
                InProgress--;
            }
        }


        /// <summary>
        /// Fetches webpage from the given link and retries if failed up to MaxRetry times
        /// returns false if failed to fetch page
        /// Spits out the html content of the page
        /// </summary>
        /// <param name="link">Queued Link to be fetched</param>
        /// <param name="cancellationToken"></param>
        /// <param name="html">Html content of the page</param>
        /// <returns>If the fetch is successful</returns>
        public bool FetchWebPage(QueuedLink link, CancellationToken? cancellationToken, out string html)
        {
            html = string.Empty;
            while (link.RetryCount < _options.MaxRetry)
            {
                if (_currentDepth > _options.MaxDepth || Pages.Count > _options.MaxPages)
                    return false;

                try
                {
                    html = FetchWebPage(link.Url, cancellationToken);
                    return true;
                }
                catch (TaskCanceledException e)
                {
                    if (e.CancellationToken == cancellationToken)
                    {
                        return false;
                    }
                    link.IncrementRetryCount();
                }
                catch (FetchException e)
                {
                    lock (Failed)
                    {
                        OnHttpError?.Invoke(this, e);
                    }
                    return false;
                }
                catch (Exception e)
                {
                    link.IncrementRetryCount();
                }
            }
            return false;
        }

        /// <summary>
        /// Fetches webpage from the given url
        /// </summary>
        /// <param name="url"></param>
        /// <returns>HTML content of the page</returns>
        /// <exception cref="FetchException">Page responded with fail</exception>
        public string FetchWebPage(string url, CancellationToken? cancellationToken = null)
        {

            var client = this.CreateClient();
            HttpResponseMessage? response;

            if (cancellationToken.HasValue)
                response = client.GetAsync(url, cancellationToken.Value).Result;
            else
                response = client.GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                var html = response.Content.ReadAsStringAsync().Result;
                return html;
            }
            else
            {
                throw new FetchException("Failed to fetch page", url, response.StatusCode, response.Content);
            }

        }

        public void SaveToCsv(string path, IEnumerable<WebPage> pages)
        {
            var fileExists = File.Exists(path);
            var csv = pages.ToCsv(!fileExists);
            if (fileExists)
                File.AppendAllText(path, csv.ToString());
            else
                File.WriteAllText(path, csv.ToString());
        }

        public void SaveToCsv(string path, IEnumerable<ILink> links)
        {
            var fileExists = File.Exists(path);
            var csv = links.ToCsv(!fileExists);
            if (fileExists)
                File.AppendAllText(path, csv.ToString());
            else
                File.WriteAllText(path, csv.ToString());
        }

    }

}

